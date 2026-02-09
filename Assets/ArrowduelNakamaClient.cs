using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

/// <summary>
/// Nakama client wrapper specifically for Arrow Duel game.
/// Handles connection, matchmaking, and match state synchronization.
/// This replaces Photon Fusion's NetworkRunner functionality.
/// </summary>
public class ArrowduelNakamaClient : MonoBehaviour
{
    public static ArrowduelNakamaClient Instance { get; private set; }

    [Header("Server Settings")] [SerializeField]
    private string scheme = "http";

    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 7351; // Port 7351 for HTTP API gateway (Nakama 3.24.0+)
    [SerializeField] private string serverKey = "defaultkey";
    [SerializeField] private bool useSSL = false;

    [Header("Matchmaking Settings")] [SerializeField]
    private int minPlayers = 2;

    [SerializeField] private int maxPlayers = 2;

    // Nakama objects
    private IClient client;
    private ISocket socket;
    private ISession session;
    private IMatchmakerTicket matchmakerTicket;
    private IMatch currentMatch;
    private IMatchmakerMatched lastMatchedData;

    // Track intentional disconnects
    private bool isIntentionallyDisconnecting = false;

    // Player info
    public string PlayerUsername { get; private set; }
    public string PlayerUserId => session?.UserId;

    // Events (executed on Unity main thread)
    public event Action<IMatch> MatchJoined;
    public event Action<IMatchPresenceEvent> MatchPresenceUpdated;
    public event Action<IMatchState> MatchStateReceived;
    public event Action<string> SocketClosed;

    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    // Properties
    public bool IsSocketConnected => socket != null && socket.IsConnected;
    public bool IsMatchmaking => matchmakerTicket != null;
    public bool HasActiveMatch => currentMatch != null;
    public IMatch CurrentMatch => currentMatch;
    public ISocket Socket => socket;
    public ISession Session => session;
    public IMatchmakerMatched LastMatchedData => lastMatchedData;

    // Host determination (replaces State Authority from Photon Fusion)
    public bool IsHost { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Process queued actions on main thread
        while (mainThreadActions.TryDequeue(out var action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArrowduelNakamaClient] Exception while executing queued action: {ex}");
            }
        }
    }

    /// <summary>
    /// Configures server settings. Call this before connecting.
    /// </summary>
    public void ConfigureServer(string customScheme, string customHost, int customPort, string customServerKey,
        bool customUseSSL)
    {
        scheme = customScheme;
        host = customHost;
        port = customPort;
        serverKey = customServerKey;
        useSSL = customUseSSL;

        // Unity uses HTTP adapter which connects to HTTP API gateway
        // In Nakama 3.24.0+: Port 7351 is HTTP API gateway, Port 7350 is gRPC
        // WebSocket connections will automatically use port 7351 (same as HTTP gateway)

        // Recreate client if it exists
        if (client != null)
        {
            client = null;
        }

        //Debug.Log($"[ArrowduelNakamaClient] Server configured: {scheme}://{host}:{port}");
    }

    /// <summary>
    /// Connects to Nakama, authenticates, and starts matchmaking.
    /// </summary>
    public async Task<Exception> ConnectAndStartMatchmakingAsync(string username,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Unity uses HTTP adapter which connects to HTTP API gateway
            // In Nakama 3.24.0+: Port 7351 is HTTP API gateway, Port 7350 is gRPC
            // WebSocket connections will automatically use port 7351 (same as HTTP gateway)

            // Clean up existing connections
            await CleanupExistingConnectionAsync();
            await Task.Delay(100, cancellationToken);

            PlayerUsername = username;
            EnsureClient();

            //Debug.Log($"[ArrowduelNakamaClient] Connecting to server: {scheme}://{host}:{port}");

            // Authenticate
            string playerPrefsKey = $"NakamaCustomId_{username}";
            string customId = PlayerPrefs.GetString(playerPrefsKey, string.Empty);

            if (string.IsNullOrEmpty(customId))
            {
                string deviceId = SystemInfo.deviceUniqueIdentifier;
                customId = $"{deviceId}_{username}_{System.Guid.NewGuid().ToString("N")[..8]}";
                if (customId.Length < 6) customId = customId.PadRight(6, '0');
                if (customId.Length > 128) customId = customId.Substring(0, 128);
                PlayerPrefs.SetString(playerPrefsKey, customId);
                PlayerPrefs.Save();
            }

            // Authenticate
            try
            {
                session = await client.AuthenticateCustomAsync(customId, username, create: false);
                PlayerUsername = session.Username;
                //Debug.Log(
                    //$"[ArrowduelNakamaClient] Authenticated to existing account! UserID: {session.UserId}, Username: {session.Username}");
            }
            catch (ApiResponseException ex) when (ex.StatusCode == 404 || ex.StatusCode == 401)
            {
                session = await client.AuthenticateCustomAsync(customId, username, create: true);
                PlayerUsername = session.Username;
                //Debug.Log(
                    //$"[ArrowduelNakamaClient] Created and authenticated new account! UserID: {session.UserId}, Username: {session.Username}");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Create socket and connect
            //Debug.Log("[ArrowduelNakamaClient] Creating socket connection...");
            socket = client.NewSocket();
            SubscribeSocketEvents();

            var connectTask = socket.ConnectAsync(session);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Socket connection timed out after 10 seconds");
            }

            await connectTask;

            if (!socket.IsConnected)
            {
                throw new Exception("Socket connection completed but socket is not connected");
            }

            //Debug.Log("[ArrowduelNakamaClient] Socket connected successfully!");

            cancellationToken.ThrowIfCancellationRequested();

            // Start matchmaking
            //Debug.Log(
                //$"[ArrowduelNakamaClient] Starting matchmaking: minPlayers={minPlayers}, maxPlayers={maxPlayers}");

            if (!socket.IsConnected)
            {
                throw new Exception("Socket disconnected before matchmaking");
            }

            matchmakerTicket =
                await socket.AddMatchmakerAsync("*", minPlayers, maxPlayers, new Dictionary<string, string>());
            //Debug.Log($"[ArrowduelNakamaClient] ✓ Matchmaking ticket created! Ticket: {matchmakerTicket.Ticket}");
            //Debug.Log($"[ArrowduelNakamaClient] ⏳ Waiting for match...");

            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ArrowduelNakamaClient] Failed to connect or start matchmaking: {ex}");
            QueueMainThreadAction(() => CleanupSocket());
            return ex;
        }
    }

    /// <summary>
    /// Cancels active matchmaking.
    /// </summary>
    public async Task CancelMatchmakingAsync(CancellationToken cancellationToken = default)
    {
        if (socket != null && matchmakerTicket != null)
        {
            try
            {
                //Debug.Log($"[ArrowduelNakamaClient] Cancelling matchmaker ticket: {matchmakerTicket.Ticket}");
                await socket.RemoveMatchmakerAsync(matchmakerTicket);
                //Debug.Log("[ArrowduelNakamaClient] Matchmaker ticket cancelled successfully");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ArrowduelNakamaClient] Failed to cancel matchmaking: {ex}");
            }
        }

        matchmakerTicket = null;
    }

    /// <summary>
    /// Leaves the current match.
    /// </summary>
    public async Task LeaveMatchAsync(CancellationToken cancellationToken = default)
    {
        if (socket != null && currentMatch != null)
        {
            try
            {
                await socket.LeaveMatchAsync(currentMatch.Id);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ArrowduelNakamaClient] Failed to leave match: {ex}");
            }
        }

        currentMatch = null;
        IsHost = false;
    }

    /// <summary>
    /// Disconnects from Nakama completely.
    /// </summary>
    public async Task DisconnectAsync()
    {
        isIntentionallyDisconnecting = true;
        if (socket != null)
        {
            try
            {
                if (socket.IsConnected)
                {
                    await socket.CloseAsync();
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ArrowduelNakamaClient] Error while closing socket: {ex}");
            }
        }

        CleanupSocket();
        session = null;
        client = null;
        PlayerUsername = null;
        IsHost = false;
        isIntentionallyDisconnecting = false;
    }

    /// <summary>
    /// Sends match state message (replaces RPC calls from Photon Fusion).
    /// </summary>
    public Task SendMatchStateAsync(long opCode, string payloadJson, CancellationToken cancellationToken = default)
    {
        if (socket == null || currentMatch == null || !socket.IsConnected)
        {
            Debug.LogWarning($"[ArrowduelNakamaClient] Cannot send match state: socket or match is null/not connected");
            return Task.CompletedTask;
        }

        byte[] payload = string.IsNullOrEmpty(payloadJson) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(payloadJson);
        return socket.SendMatchStateAsync(currentMatch.Id, opCode, payload, null);
    }

    /// <summary>
    /// Determines if this client is the host (first player by UserId).
    /// Host has authority over game state (replaces State Authority from Photon Fusion).
    /// </summary>
    public void UpdateHostStatus()
    {
        if (session == null)
        {
            IsHost = false;
            return;
        }

        string currentUserId = session.UserId;

        // PRIMARY: Use matchmaker data (most reliable - contains ALL matched players)
        if (lastMatchedData?.Users != null && lastMatchedData.Users.Any())
        {
            var sortedUserIds = lastMatchedData.Users
                .Select(u => u.Presence.UserId)
                .OrderBy(id => id)
                .ToList();

            IsHost = sortedUserIds.Count > 0 && sortedUserIds[0] == currentUserId;
            Debug.Log($"[ArrowduelNakamaClient] Host status (from matchmaker): IsHost={IsHost}, currentUserId={currentUserId}, firstSorted={sortedUserIds[0]}, totalPlayers={sortedUserIds.Count}");
            return;
        }

        // FALLBACK: Use current match presences (may be stale)
        if (currentMatch == null)
        {
            IsHost = false;
            return;
        }

        var allPlayers = new List<IUserPresence>(currentMatch.Presences);

        if (allPlayers.Count == 0)
        {
            // No other players visible yet - assume host (first to join)
            IsHost = true;
            Debug.Log($"[ArrowduelNakamaClient] Host status (no presences): IsHost=true (assumed first joiner)");
            return;
        }

        bool selfInPresences = allPlayers.Any(p => p.UserId == currentUserId);

        if (!selfInPresences)
        {
            var sortedPresences = allPlayers.OrderBy(p => p.UserId).ToList();
            IsHost = string.Compare(currentUserId, sortedPresences[0].UserId) < 0;
        }
        else
        {
            var sortedPresences = allPlayers.OrderBy(p => p.UserId).ToList();
            IsHost = sortedPresences[0].UserId == currentUserId;
        }

        Debug.Log($"[ArrowduelNakamaClient] Host status (from presences): IsHost={IsHost}, totalPresences={allPlayers.Count}");
    }

    private void EnsureClient()
    {
        // Unity uses HTTP adapter which connects to HTTP API gateway
        // In Nakama 3.24.0+: Port 7351 is HTTP API gateway, Port 7350 is gRPC
        // WebSocket connections will automatically use port 7351 (same as HTTP gateway)

        if (client != null)
        {
            return;
        }

        var connectionScheme = useSSL ? "https" : scheme;
        //Debug.Log(
            //$"[ArrowduelNakamaClient] Creating client with: {connectionScheme}://{host}:{port}, serverKey: {serverKey}");
        client = new Client(connectionScheme, host, port, serverKey, UnityWebRequestAdapter.Instance);
        //Debug.Log($"[ArrowduelNakamaClient] Client created successfully with port: {port}");
    }

    private void SubscribeSocketEvents()
    {
        if (socket == null) return;

        // Unsubscribe first to prevent duplicates
        socket.Closed -= HandleSocketClosed;
        socket.ReceivedMatchmakerMatched -= HandleMatchmakerMatched;
        socket.ReceivedMatchPresence -= HandleMatchPresence;
        socket.ReceivedMatchState -= HandleMatchState;

        // Subscribe
        socket.Closed += HandleSocketClosed;
        socket.ReceivedMatchmakerMatched += HandleMatchmakerMatched;
        socket.ReceivedMatchPresence += HandleMatchPresence;
        socket.ReceivedMatchState += HandleMatchState;

        //Debug.Log("[ArrowduelNakamaClient] Socket events subscribed");
    }

    private void UnsubscribeSocketEvents()
    {
        if (socket == null) return;

        socket.Closed -= HandleSocketClosed;
        socket.ReceivedMatchmakerMatched -= HandleMatchmakerMatched;
        socket.ReceivedMatchPresence -= HandleMatchPresence;
        socket.ReceivedMatchState -= HandleMatchState;
    }

    private void HandleSocketClosed(string reason)
    {
        if (isIntentionallyDisconnecting)
        {
            //Debug.Log($"[ArrowduelNakamaClient] Socket closed (intentional). Reason: {reason ?? "N/A"}");
        }
        else
        {
            Debug.LogWarning($"[ArrowduelNakamaClient] Socket closed unexpectedly. Reason: {reason ?? "N/A"}");
        }

        QueueMainThreadAction(() =>
        {
            SocketClosed?.Invoke(reason ?? "Unknown");
            CleanupSocket();
        });
    }

    private void HandleMatchmakerMatched(IMatchmakerMatched matched)
    {
        //Debug.Log($"[ArrowduelNakamaClient] ===== Matchmaker Matched! =====");
        //Debug.Log($"[ArrowduelNakamaClient] MatchID: {matched?.MatchId}, Users: {matched?.Users?.Count() ?? 0}");

        if (matched == null)
        {
            Debug.LogError("[ArrowduelNakamaClient] HandleMatchmakerMatched called with null matched object!");
            return;
        }

        lastMatchedData = matched;

        if (matched.Users != null)
        {
            //Debug.Log($"[ArrowduelNakamaClient] Found {matched.Users.Count()} players in match:");
            foreach (var user in matched.Users)
            {
                //Debug.Log(
                    //$"[ArrowduelNakamaClient]   - User: {user.Presence.Username} ({user.Presence.UserId})");
            }
        }
        else
        {
            Debug.LogWarning("[ArrowduelNakamaClient] Matched but Users list is null!");
            Debug.LogWarning("[ArrowduelNakamaClient] Matched but Users list is null!");
        }

        // Join match asynchronously
        _ = JoinMatchAsync(matched);
    }

    private async Task JoinMatchAsync(IMatchmakerMatched matched)
    {
        if (socket == null)
        {
            Debug.LogError("[ArrowduelNakamaClient] Cannot join match: socket is null");
            return;
        }

        try
        {
            //Debug.Log($"[ArrowduelNakamaClient] Joining match: {matched.MatchId}");
            var match = await socket.JoinMatchAsync(matched);
            currentMatch = match;
            matchmakerTicket = null;

            // Update host status
            UpdateHostStatus();

            int playerCount = match.Presences.Count() + 1; // +1 for self
            //Debug.Log(
                //$"[ArrowduelNakamaClient] Successfully joined match! MatchID: {match.Id}, Players: {playerCount}, IsHost: {IsHost}");

            QueueMainThreadAction(() => { MatchJoined?.Invoke(match); });
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ArrowduelNakamaClient] Failed to join matched game: {ex}");
            QueueMainThreadAction(() => { CleanupSocket(); });
        }
    }

    private void HandleMatchPresence(IMatchPresenceEvent presenceEvent)
    {
        QueueMainThreadAction(() =>
        {
            // Update host status when players join/leave
            UpdateHostStatus();
            MatchPresenceUpdated?.Invoke(presenceEvent);
        });
    }

    private void HandleMatchState(IMatchState matchState)
    {
        QueueMainThreadAction(() => { MatchStateReceived?.Invoke(matchState); });
    }

    private async Task CleanupExistingConnectionAsync()
    {
        try
        {
            if (matchmakerTicket != null)
            {
                await CancelMatchmakingAsync();
            }

            if (currentMatch != null)
            {
                await LeaveMatchAsync();
            }

            if (socket != null)
            {
                UnsubscribeSocketEvents();
                if (socket.IsConnected)
                {
                    try
                    {
                        await socket.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[ArrowduelNakamaClient] Error closing socket during cleanup: {ex}");
                    }
                }

                CleanupSocket();
            }

            //Debug.Log("[ArrowduelNakamaClient] Cleanup completed");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ArrowduelNakamaClient] Error during cleanup: {ex}");
        }
    }

    private void CleanupSocket()
    {
        UnsubscribeSocketEvents();
        socket = null;
        currentMatch = null;
        matchmakerTicket = null;
        IsHost = false;
        isIntentionallyDisconnecting = false;
    }

    private void QueueMainThreadAction(Action action)
    {
        if (action != null)
        {
            mainThreadActions.Enqueue(action);
        }
    }
}