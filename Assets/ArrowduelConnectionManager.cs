using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Nakama;


public class ArrowduelConnectionManager : MonoBehaviour
{
    public static ArrowduelConnectionManager Instance { get; private set; }
    [Header("UI References")] public Button connectButton;
    public TMP_Text statusText;
    public TMP_InputField usernameInput;

    [Header("Nakama Server Settings")] [SerializeField]
    private string serverScheme = "http";

    [SerializeField] private string serverHost = "127.0.0.1";
    [SerializeField] private int serverPort = 7351; // Port 7351 for HTTP API gateway (Nakama 3.24.0+ uses 7351 for HTTP, 7350 for gRPC)
    [SerializeField] private string serverKey = "defaultkey";
    [SerializeField] private bool useSSL = false;

    [Header("Timeout Settings")]
    [Tooltip(
        "Matchmaking timeout in seconds. NOTE: If players aren't matching, increase this to 45-60s and ensure both players connect simultaneously.")]
    [SerializeField]
    private float matchmakingTimeout = 60f; // Increased from 45f to allow more time for simultaneous connections

    [Tooltip("Time to wait for opponent after joining match (seconds)")] [SerializeField]
    private float opponentWaitTimeout = 20f;

    [Tooltip("Maximum number of matchmaking retry attempts")] [SerializeField]
    private int maxRetryAttempts = 3;

    [Tooltip("Delay between retry attempts (seconds)")] [SerializeField]
    private float retryDelay = 2f;

    private ArrowduelNakamaClient nakamaClient;
    private CancellationTokenSource connectCancellation;
    private bool isConnecting;
    private bool isMatchmaking; // Separate state for matchmaking vs initial connection
    private bool matchFound;
    private bool waitingForOpponent;
    private float matchmakingTimer;
    private float opponentWaitTimer;
    private bool transitionStarted = false;
    private int currentPlayerCount;
    private HashSet<string> seenPlayerIds = new HashSet<string>(); // Track players we've seen join
    private int retryAttempts = 0; // Add this
    private bool isRetrying = false; // Add this
    private HashSet<string> readyPlayers = new HashSet<string>(); // Track players who are ready to start
    private bool isReadySent = false; // Track if we've sent ready signal
    private const long OPCODE_READY_TO_START = 10; // Opcode for synchronization

    public static string LastUsedUsername { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        //Debug.Log("[ArrowduelConnectionManager] Awake");
        EnsureNakamaClient();
        //Debug.Log("[ArrowduelConnectionManager] EnsureNakamaClient");
        
        // Configure server settings early to ensure correct port is used
        // Nakama 3.24.0+: Port 7351 is HTTP API gateway, Port 7350 is gRPC
        // Unity uses HTTP adapter, so we need port 7351
        if (nakamaClient != null)
        {
            nakamaClient.ConfigureServer(serverScheme, serverHost, serverPort, serverKey, useSSL);
            //Debug.Log($"[ArrowduelConnectionManager] Server configured: {serverScheme}://{serverHost}:{serverPort}");
        }
    }

    void OnEnable()
    {
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectClicked);
        }

        SubscribeToClientEvents();
    }

    void Start()
    {
        if (!ValidateUI())
        {
            return;
        }

        //usernameInput.text=PlayerInfoManager.Instance.playerName;
        ResetState();
        UpdateStatus("Enter username and press Connect.");
        connectButton.interactable = true;

        nakamaClient.ConfigureServer(serverScheme, serverHost, serverPort, serverKey, useSSL);
    }

    void OnDisable()
    {
        if (connectButton != null)
        {
            connectButton.onClick.RemoveListener(OnConnectClicked);
        }

        UnsubscribeFromClientEvents();
    }

    void OnDestroy()
    {
        connectCancellation?.Cancel();
        connectCancellation?.Dispose();
    }


    void Update()
    {
        // Handle waiting for opponent synchronization
        if (waitingForOpponent && matchFound && !transitionStarted)
        {
            opponentWaitTimer += Time.deltaTime;
            
            // Timeout fallback: if we've been waiting too long, start anyway
            if (opponentWaitTimer >= opponentWaitTimeout)
            {
                Debug.LogWarning($"[ArrowduelConnectionManager] Synchronization timeout ({opponentWaitTimeout}s). Starting anyway...");
                Debug.LogWarning($"[ArrowduelConnectionManager] Ready players: {readyPlayers.Count}/{currentPlayerCount}");
                if (!transitionStarted)
                {
                    BeginMultiplayerGameTransition();
                }
            }
            else if (Time.frameCount % 60 == 0) // Log every second
            {
                UpdateStatus($"Synchronizing... ({readyPlayers.Count}/{currentPlayerCount} ready, {opponentWaitTimeout - opponentWaitTimer:F1}s remaining)");
            }
        }
        
        // Only count matchmaking timer when actually matchmaking (not during initial connection)
        if (isMatchmaking && !matchFound)
        {
            matchmakingTimer += Time.deltaTime;
            float remaining = Mathf.Max(0f, matchmakingTimeout - matchmakingTimer);

            // Log every 5 seconds to track progress
            if (Mathf.FloorToInt(matchmakingTimer) % 5 == 0 && Time.frameCount % 300 == 0)
            {
                bool hasTicket = nakamaClient != null && nakamaClient.IsMatchmaking;
                string ticketInfo = hasTicket ? $"Ticket active" : "No ticket";
                Debug.Log(
                    $"[ArrowduelConnectionManager] Matchmaking in progress: {matchmakingTimer:F1}s / {matchmakingTimeout}s (remaining: {remaining:F1}s), {ticketInfo}, Socket: {(nakamaClient?.IsSocketConnected ?? false)}");
            }

            UpdateStatus($"Searching for players... ({remaining:F1}s remaining)");

            if (matchmakingTimer >= matchmakingTimeout)
            {
                bool hasTicket = nakamaClient != null && nakamaClient.IsMatchmaking;
                string ticketStatus = hasTicket ? "Ticket still active" : "No active ticket";
                Debug.LogWarning(
                    $"[ArrowduelConnectionManager] ⚠️ Matchmaking timeout reached ({matchmakingTimeout}s).");
                Debug.LogWarning(
                    $"[ArrowduelConnectionManager] Status: isMatchmaking={isMatchmaking}, matchFound={matchFound}, {ticketStatus}");
                Debug.LogWarning(
                    $"[ArrowduelConnectionManager] Socket connected: {nakamaClient?.IsSocketConnected ?? false}");
                Debug.LogWarning(
                    $"[ArrowduelConnectionManager] ⚠️ IMPORTANT: Both players must click Connect within {matchmakingTimeout}s of each other!");

                // Check if socket is still connected - if not, retry connection
                if (nakamaClient != null && !nakamaClient.IsSocketConnected)
                {
                    Debug.LogWarning(
                        $"[ArrowduelConnectionManager] Socket disconnected during matchmaking. Will retry connection...");
                    _ = RetryMatchmakingAsync();
                }
                // Retry matchmaking if we haven't exceeded max attempts
                else if (retryAttempts < maxRetryAttempts && !isRetrying)
                {
                    //Debug.Log(
                        //$"[ArrowduelConnectionManager] Retrying matchmaking (attempt {retryAttempts + 1}/{maxRetryAttempts})...");
                    //_ = RetryMatchmakingAsync();
                }
                else
                {
                    Debug.LogWarning(
                        $"[ArrowduelConnectionManager] Max retry attempts reached ({maxRetryAttempts}).");
                    Debug.LogWarning(
                        $"[ArrowduelConnectionManager] NOTE: Make sure BOTH players click Connect at the same time!");
                    Debug.LogWarning(
                        $"[ArrowduelConnectionManager] Check Nakama server logs: docker-compose logs -f nakama");
                    // Don't fallback to AI - let user retry manually
                    UpdateStatus("No match found. Make sure both players click Connect simultaneously. Click Connect to retry.");
                    connectButton.interactable = true;
                    ResetState();
                }
            }
        }

        if (waitingForOpponent)
        {
            opponentWaitTimer += Time.deltaTime;
            float remaining = Mathf.Max(0f, opponentWaitTimeout - opponentWaitTimer);
            UpdateStatus($"Waiting for opponent... ({currentPlayerCount}/2 players, {remaining:F1}s)");

            if (opponentWaitTimer >= opponentWaitTimeout)
            {
                Debug.LogWarning(
                    $"[ArrowduelConnectionManager] Opponent wait timeout reached ({opponentWaitTimeout}s). Players: {currentPlayerCount}/2. Falling back to AI.");
                _ = FallbackToAIGameAsync("Opponent not found. Starting AI game...");
            }
        }
    }

    private void EnsureNakamaClient()
    {
        if (ArrowduelNakamaClient.Instance != null)
        {
            nakamaClient = ArrowduelNakamaClient.Instance;
            // Ensure port is correct even if Inspector has wrong value
            nakamaClient.ConfigureServer(serverScheme, serverHost, serverPort, serverKey, useSSL);
            //Debug.Log($"[ArrowduelConnectionManager] Reusing existing NakamaClient, configured port: {serverPort}");
            return;
        }

        var clientGO = new GameObject("ArrowduelNakamaClient");
        nakamaClient = clientGO.AddComponent<ArrowduelNakamaClient>();
        // Configure immediately after creation
        nakamaClient.ConfigureServer(serverScheme, serverHost, serverPort, serverKey, useSSL);
        //Debug.Log($"[ArrowduelConnectionManager] Created new NakamaClient, configured port: {serverPort}");
    }

    private bool ValidateUI()
    {
        if (statusText == null || connectButton == null || usernameInput == null)
        {
            Debug.LogError("[ArrowduelConnectionManager] Required UI elements are missing.");
            return false;
        }

        return true;
    }

    private void SubscribeToClientEvents()
    {
        if (nakamaClient == null)
        {
            return;
        }

        nakamaClient.MatchJoined += HandleMatchJoined;
        nakamaClient.MatchPresenceUpdated += HandleMatchPresenceUpdated;
        nakamaClient.SocketClosed += HandleSocketClosed;
        nakamaClient.MatchStateReceived += HandleMatchStateReceived;
    }

    private void UnsubscribeFromClientEvents()
    {
        if (nakamaClient == null)
        {
            return;
        }

        nakamaClient.MatchJoined -= HandleMatchJoined;
        nakamaClient.MatchPresenceUpdated -= HandleMatchPresenceUpdated;
        nakamaClient.SocketClosed -= HandleSocketClosed;
        nakamaClient.MatchStateReceived -= HandleMatchStateReceived;
    }
    
    private void HandleMatchStateReceived(Nakama.IMatchState matchState)
    {
        if (matchState.OpCode == OPCODE_READY_TO_START)
        {
            string json = System.Text.Encoding.UTF8.GetString(matchState.State);
            //Debug.Log($"[ArrowduelConnectionManager] Received READY_TO_START from: {matchState.UserPresence?.UserId ?? "unknown"}");
            
            if (matchState.UserPresence != null && !string.IsNullOrEmpty(matchState.UserPresence.UserId))
            {
                readyPlayers.Add(matchState.UserPresence.UserId);
                //Debug.Log($"[ArrowduelConnectionManager] Ready players count: {readyPlayers.Count}, Total needed: {currentPlayerCount}");
                
                // Check if both players are ready
                if (readyPlayers.Count >= currentPlayerCount && currentPlayerCount >= 2)
                {
                    //Debug.Log("[ArrowduelConnectionManager] ✓ Both players ready! Starting transition...");
                    if (!transitionStarted)
                    {
                        BeginMultiplayerGameTransition();
                    }
                }
            }
        }
    }

    private void ResetState()
    {
        isConnecting = false;
        isMatchmaking = false;
        matchFound = false;
        waitingForOpponent = false;
        matchmakingTimer = 0f;
        opponentWaitTimer = 0f;
        transitionStarted = false;
        currentPlayerCount = 0;
        seenPlayerIds.Clear();
        readyPlayers.Clear();
        isReadySent = false;
        // Don't reset retryAttempts here - only reset on successful match or manual cancel
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private async void OnConnectClicked()
    {
        if (!ValidateUI())
        {
            return;
        }

        string username = usernameInput.text?.Trim();
        //Debug.Log($"[ArrowduelConnectionManager] === USERNAME INPUT DEBUG ===");
        //Debug.Log($"[ArrowduelConnectionManager] Raw username from input field: '{username}'");
        //Debug.Log($"[ArrowduelConnectionManager] UsernameInput.text: '{usernameInput.text}'");
        //Debug.Log($"[ArrowduelConnectionManager] UsernameInput is null: {usernameInput == null}");
        
        if (!TicTacToeInputValidator.ValidateUsername(username, out string errorMessage))
        {
            Debug.LogWarning($"[ArrowduelConnectionManager] Username validation failed: {errorMessage}");
            UpdateStatus(errorMessage);
            return;
        }

        string sanitizedUsername = TicTacToeInputValidator.SanitizeUsername(username);
        //Debug.Log($"[ArrowduelConnectionManager] Sanitized username: '{sanitizedUsername}'");
        
        if (string.IsNullOrEmpty(sanitizedUsername))
        {
            Debug.LogError("[ArrowduelConnectionManager] Sanitized username is empty!");
            UpdateStatus("Please enter a valid username.");
            return;
        }
        
        // Store username in PlayerData for use in game scene
        PlayerData.playerName = sanitizedUsername;
        //Debug.Log($"[ArrowduelConnectionManager] Username stored in PlayerData.playerName: '{PlayerData.playerName}'");

        connectButton.interactable = false;
        ResetState();
        UpdateStatus("Connecting to server...");
        // Don't set isConnecting = true yet - wait until connection succeeds

        connectCancellation?.Cancel();
        connectCancellation?.Dispose();
        connectCancellation = new CancellationTokenSource();

        // Ensure server configuration is applied before connecting
        // Nakama 3.24.0+: Port 7351 is HTTP API gateway, Port 7350 is gRPC
        if (nakamaClient != null)
        {
            nakamaClient.ConfigureServer(serverScheme, serverHost, serverPort, serverKey, useSSL);
            //Debug.Log($"[ArrowduelConnectionManager] Server configuration applied before connect: {serverScheme}://{serverHost}:{serverPort}");
        }

        try
        {
            var error = await nakamaClient.ConnectAndStartMatchmakingAsync(sanitizedUsername,
                connectCancellation.Token);
            if (error != null)
            {
                string connectionErrorMsg = "Connection failed. ";
                if (error is System.Net.Http.HttpRequestException || error.Message.Contains("Empty reply") || error.Message.Contains("Curl error"))
                {
                    connectionErrorMsg += "Please ensure the Nakama server is running (docker-compose up -d). ";
                    connectionErrorMsg += $"Server: {serverScheme}://{serverHost}:{serverPort}";
                }
                else
                {
                    connectionErrorMsg += "Please try again.";
                }
                HandleConnectionFailure(connectionErrorMsg);
                return;
            }
        }
        catch (OperationCanceledException)
        {
            HandleConnectionFailure("Connection cancelled.");
            return;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ArrowduelConnectionManager] Unexpected error while connecting: {ex}");
            string connectionErrorMsg = "Connection failed. ";
            if (ex.Message.Contains("Empty reply") || ex.Message.Contains("Curl error") || ex is System.Net.Http.HttpRequestException)
            {
                connectionErrorMsg += "Please ensure the Nakama server is running (docker-compose up -d). ";
                connectionErrorMsg += $"Server: {serverScheme}://{serverHost}:{serverPort}";
            }
            else
            {
                connectionErrorMsg += "Please try again.";
            }
            HandleConnectionFailure(connectionErrorMsg);
            return;
        }

        // Connection succeeded and matchmaking ticket created - NOW start the timer
        //Debug.Log($"[ArrowduelConnectionManager] ✓ Connection successful! Starting matchmaking timer.");
        //Debug.Log(
            //$"[ArrowduelConnectionManager] Matchmaking settings: timeout={matchmakingTimeout}s, isMatchmaking={true}, timer starts at 0");
        //Debug.Log(
            //$"[ArrowduelConnectionManager] ⚠️ CRITICAL: Both players must be searching SIMULTANEOUSLY for matchmaking to work!");
        //Debug.Log(
            //$"[ArrowduelConnectionManager] ⚠️ If no match found, ensure another player is also searching NOW (within {matchmakingTimeout}s)");
        //Debug.Log(
            //$"[ArrowduelConnectionManager] Server: {serverHost}:{serverPort}, Query: *, minPlayers: 2, maxPlayers: 2");
        LastUsedUsername = sanitizedUsername;
        //Debug.Log($"[ArrowduelConnectionManager] LastUsedUsername set to: '{LastUsedUsername}'");
        //Debug.Log($"[ArrowduelConnectionManager] PlayerData.playerName: '{PlayerData.playerName}'");
        isConnecting = true;
        isMatchmaking = true;
        matchmakingTimer = 0f; // Ensure timer starts from 0
        UpdateStatus($"Searching for players... (Need another player searching NOW!)");
    }

    private void HandleConnectionFailure(string userMessage)
    {
        ResetState();
        UpdateStatus(userMessage);
        connectButton.interactable = true;
    }

    private void HandleMatchJoined(Nakama.IMatch match)
    {
        //Debug.Log($"[ArrowduelConnectionManager] ===== HandleMatchJoined CALLED =====");
        //Debug.Log($"[ArrowduelConnectionManager] MatchID: {match.Id}");
        //Debug.Log($"[ArrowduelConnectionManager] === USERNAME DEBUG ON MATCH JOIN ===");
        //Debug.Log($"[ArrowduelConnectionManager] LastUsedUsername: '{LastUsedUsername}'");
        //Debug.Log($"[ArrowduelConnectionManager] PlayerData.playerName: '{PlayerData.playerName}'");

        matchFound = true;
        isMatchmaking = false;
        matchmakingTimer = 0f;
        retryAttempts = 0;
        isRetrying = false;

        // Reset player tracking
        seenPlayerIds.Clear();

        // Add ourselves to seen players
        string localUserId = null;
        string localUsername = null;
        if (nakamaClient != null && nakamaClient.Session != null)
        {
            localUserId = nakamaClient.Session.UserId;
            localUsername = nakamaClient.Session.Username ?? LastUsedUsername ?? PlayerData.playerName;
            seenPlayerIds.Add(localUserId);
            //Debug.Log($"[ArrowduelConnectionManager] === LOCAL PLAYER INFO ===");
            //Debug.Log($"[ArrowduelConnectionManager] Local UserId: {localUserId}");
            //Debug.Log($"[ArrowduelConnectionManager] Local Username (from Session): {nakamaClient.Session.Username ?? "NULL"}");
            //Debug.Log($"[ArrowduelConnectionManager] Local Username (from LastUsedUsername): {LastUsedUsername ?? "NULL"}");
            //Debug.Log($"[ArrowduelConnectionManager] Local Username (from PlayerData): {PlayerData.playerName ?? "NULL"}");
            //Debug.Log($"[ArrowduelConnectionManager] Final Local Username: {localUsername}");
        }

        // Count presences (other players already in match) + 1 (self)
        int presenceCount = match.Presences.Count();

        //Debug.Log($"[ArrowduelConnectionManager] === ALL PLAYERS IN MATCH ===");
        //Debug.Log($"[ArrowduelConnectionManager] Presences count: {presenceCount}");
        
        // Add all current presences to seen players
        foreach (var presence in match.Presences)
        {
            if (!string.IsNullOrEmpty(presence.UserId))
            {
                seenPlayerIds.Add(presence.UserId);
                //Debug.Log($"[ArrowduelConnectionManager] Player in match - Username: '{presence.Username ?? "NULL"}', UserId: {presence.UserId}");
            }
        }

        currentPlayerCount = seenPlayerIds.Count;

        //Debug.Log($"[ArrowduelConnectionManager] === MATCH JOIN SUMMARY ===");
        //Debug.Log($"[ArrowduelConnectionManager] Presences: {presenceCount}, Total players (including self): {currentPlayerCount}");
        //Debug.Log($"[ArrowduelConnectionManager] Local player: {localUsername} ({localUserId})");
        
        // IMPORTANT: Only transition if we have a valid match AND nakamaClient has current match
        if (currentPlayerCount >= 2 && nakamaClient != null && nakamaClient.CurrentMatch != null)
        {
            //Debug.Log($"[ArrowduelConnectionManager] ===== BOTH PLAYERS JOINED! =====");
            //Debug.Log($"[ArrowduelConnectionManager] ✓ Enough players on join ({currentPlayerCount}/2)!");
            //Debug.Log($"[ArrowduelConnectionManager] Player 1: {localUsername}");
            
            // Log opponent info
            foreach (var presence in match.Presences)
            {
                if (presence.UserId != localUserId)
                {
                    //Debug.Log($"[ArrowduelConnectionManager] Player 2 (Opponent): {presence.Username ?? "NULL"} ({presence.UserId})");
                }
            }
            
            // Send ready signal and wait for other player
            SendReadyToStart();
            
            // Add self to ready players
            if (!string.IsNullOrEmpty(localUserId))
            {
                readyPlayers.Add(localUserId);
            }
            
            // Start waiting for ready signals - transition will happen when both are ready
            //Debug.Log($"[ArrowduelConnectionManager] Waiting for synchronization... ({readyPlayers.Count}/{currentPlayerCount} ready)");
            waitingForOpponent = true;
            opponentWaitTimer = 0f;
            UpdateStatus($"Synchronizing with opponent... ({readyPlayers.Count}/{currentPlayerCount} ready)");
        }
        else
        {
            //Debug.Log($"[ArrowduelConnectionManager] Waiting for more players ({currentPlayerCount}/2)... Will wait for presence events.");
            waitingForOpponent = true;
            opponentWaitTimer = 0f;
            UpdateStatus($"Waiting for opponent... ({currentPlayerCount}/2 players)");
        }
    }
    
    private async void SendReadyToStart()
    {
        if (nakamaClient == null || nakamaClient.CurrentMatch == null || isReadySent)
        {
            return;
        }
        
        try
        {
            isReadySent = true;
            string userId = nakamaClient.Session?.UserId ?? "unknown";
            string json = $"{{\"userId\":\"{userId}\",\"ready\":true}}";
            await nakamaClient.SendMatchStateAsync(OPCODE_READY_TO_START, json);
            //Debug.Log($"[ArrowduelConnectionManager] Sent READY_TO_START signal: {userId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ArrowduelConnectionManager] Failed to send READY_TO_START: {ex.Message}");
            isReadySent = false;
        }
    }

    private void HandleMatchPresenceUpdated(Nakama.IMatchPresenceEvent presenceEvent)
    {
        if (presenceEvent == null || nakamaClient.CurrentMatch == null)
        {
            Debug.LogWarning(
                "[ArrowduelConnectionManager] HandleMatchPresenceUpdated: presenceEvent or CurrentMatch is null");
            return;
        }

        // Log presence changes
        int joinsCount = presenceEvent.Joins?.Count() ?? 0;
        int leavesCount = presenceEvent.Leaves?.Count() ?? 0;
        //Debug.Log(
            //$"[ArrowduelConnectionManager] Presence updated: Joins={joinsCount}, Leaves={leavesCount}, CurrentPlayerCount={currentPlayerCount}");

        // Log all joins/leaves
        if (presenceEvent.Joins != null)
        {
            foreach (var join in presenceEvent.Joins)
            {
                //Debug.Log($"[ArrowduelConnectionManager] Player joined: {join.Username} ({join.UserId})");
            }
        }

        if (presenceEvent.Leaves != null)
        {
            foreach (var leave in presenceEvent.Leaves)
            {
                //Debug.Log($"[ArrowduelConnectionManager] Player left: {leave.Username} ({leave.UserId})");
            }
        }

        // ✅ FIX: Track players by UserId to avoid double-counting
        // Add all joining players to our seen set
        if (presenceEvent.Joins != null)
        {
            foreach (var join in presenceEvent.Joins)
            {
                if (!string.IsNullOrEmpty(join.UserId) && !seenPlayerIds.Contains(join.UserId))
                {
                    seenPlayerIds.Add(join.UserId);
                    //Debug.Log(
                        //$"[ArrowduelConnectionManager] Added new player to seen set: {join.Username} ({join.UserId})");
                }
            }
        }

        // Remove leaving players from our seen set
        if (presenceEvent.Leaves != null)
        {
            foreach (var leave in presenceEvent.Leaves)
            {
                if (!string.IsNullOrEmpty(leave.UserId))
                {
                    seenPlayerIds.Remove(leave.UserId);
                    //Debug.Log(
                        //$"[ArrowduelConnectionManager] Removed player from seen set: {leave.Username} ({leave.UserId})");
                }
            }
        }

        // Use seenPlayerIds count as authoritative source
        // Include self in count
        currentPlayerCount = seenPlayerIds.Count;
        if (nakamaClient != null && nakamaClient.Session != null && !string.IsNullOrEmpty(nakamaClient.Session.UserId))
        {
            if (!seenPlayerIds.Contains(nakamaClient.Session.UserId))
            {
                currentPlayerCount++; // Add self if not in seen set
            }
        }

        //Debug.Log(
                //$"[ArrowduelConnectionManager] Player count from tracking: {currentPlayerCount} (seen: {string.Join(", ", seenPlayerIds)})");

        // Handle player leaves
        if (presenceEvent.Leaves != null && presenceEvent.Leaves.Any())
        {
            // Only reset to waiting if we don't have enough players AND haven't started transition
            if (currentPlayerCount < 2 && !transitionStarted)
            {
                waitingForOpponent = true;
                opponentWaitTimer = 0f;
                UpdateStatus("Opponent disconnected. Waiting for another player...");
            }

            return;
        }

        // Check if we now have enough players
        if (currentPlayerCount >= 2)
        {
            //Debug.Log($"[ArrowduelConnectionManager] ===== BOTH PLAYERS JOINED (via presence update)! =====");
            //Debug.Log($"[ArrowduelConnectionManager] ✓ Have {currentPlayerCount} players! Sending ready signal...");
            
            // Send ready signal if not already sent
            if (!isReadySent)
            {
                SendReadyToStart();
            }
            
            // Add self to ready if not already added
            if (nakamaClient != null && nakamaClient.Session != null && !string.IsNullOrEmpty(nakamaClient.Session.UserId))
            {
                if (!readyPlayers.Contains(nakamaClient.Session.UserId))
                {
                    readyPlayers.Add(nakamaClient.Session.UserId);
                }
            }
            
            // Check if both are ready
            if (readyPlayers.Count >= currentPlayerCount && !transitionStarted)
            {
                //Debug.Log("[ArrowduelConnectionManager] ✓ Both players ready! Starting transition...");
                waitingForOpponent = false;
                opponentWaitTimer = 0f;
                BeginMultiplayerGameTransition();
            }
            else if (!transitionStarted)
            {
                //Debug.Log($"[ArrowduelConnectionManager] Waiting for synchronization... ({readyPlayers.Count}/{currentPlayerCount} ready)");
                waitingForOpponent = true;
                opponentWaitTimer = 0f;
                UpdateStatus($"Synchronizing... ({readyPlayers.Count}/{currentPlayerCount} ready)");
            }
            //Debug.Log($"[ArrowduelConnectionManager] === USERNAME DEBUG WHEN BOTH PLAYERS JOIN ===");
            //Debug.Log($"[ArrowduelConnectionManager] LastUsedUsername: '{LastUsedUsername}'");
            //Debug.Log($"[ArrowduelConnectionManager] PlayerData.playerName: '{PlayerData.playerName}'");
            
            // Log all player usernames
            if (nakamaClient != null && nakamaClient.CurrentMatch != null)
            {
                string localUserId = nakamaClient.Session?.UserId;
                //Debug.Log($"[ArrowduelConnectionManager] Local UserId: {localUserId}");
                //Debug.Log($"[ArrowduelConnectionManager] Local Username: {nakamaClient.Session?.Username ?? LastUsedUsername ?? PlayerData.playerName}");
                
                //Debug.Log($"[ArrowduelConnectionManager] All players in match:");
                int playerIndex = 1;
                foreach (var presence in nakamaClient.CurrentMatch.Presences)
                {
                    bool isLocal = presence.UserId == localUserId;
                    //Debug.Log($"[ArrowduelConnectionManager]   Player {playerIndex}: {presence.Username ?? "NULL"} ({presence.UserId}) {(isLocal ? "[LOCAL]" : "[OPPONENT]")}");
                    playerIndex++;
                }
                // Include self
                if (nakamaClient.Session != null)
                {
                    //Debug.Log($"[ArrowduelConnectionManager]   Player {playerIndex}: {nakamaClient.Session.Username ?? LastUsedUsername ?? PlayerData.playerName} ({localUserId}) [LOCAL - SELF]");
                }
            }
            
            waitingForOpponent = false;
            opponentWaitTimer = 0f;

            // If we were waiting for the opponent, trigger the transition now.
            if (!transitionStarted)
            {
                //Debug.Log("[ArrowduelConnectionManager] Starting multiplayer game transition...");
                BeginMultiplayerGameTransition(true);
            }
        }
        else
        {
            //Debug.Log($"[ArrowduelConnectionManager] Still waiting: {currentPlayerCount}/2 players");
        }
        
    }

    private void HandleSocketClosed(string reason)
    {
        if (!isConnecting)
        {
            return;
        }

        Debug.LogWarning($"[ArrowduelConnectionManager] Socket closed: {reason ?? "Unknown reason"}");
        HandleConnectionFailure("Disconnected from server. Please try again.");
    }

    private async void BeginMultiplayerGameTransition(bool isWaitingForOpponent = false)
    {
        if (transitionStarted)
        {
            Debug.LogWarning("[ArrowduelConnectionManager] Transition already started, ignoring duplicate call");
            return;
        }

        //Debug.Log("[ArrowduelConnectionManager] ===== BeginMultiplayerGameTransition CALLED =====");
        //Debug.Log($"[ArrowduelConnectionManager] === USERNAME DEBUG BEFORE SCENE TRANSITION ===");
        //Debug.Log($"[ArrowduelConnectionManager] LastUsedUsername: '{LastUsedUsername}'");
        //Debug.Log($"[ArrowduelConnectionManager] PlayerData.playerName: '{PlayerData.playerName}'");
        //Debug.Log($"[ArrowduelConnectionManager] UsernameInput.text: '{usernameInput?.text ?? "NULL"}'");
        
        if (nakamaClient != null && nakamaClient.Session != null)
        {
            //Debug.Log($"[ArrowduelConnectionManager] Session Username: '{nakamaClient.Session.Username ?? "NULL"}'");
            //Debug.Log($"[ArrowduelConnectionManager] Session UserId: '{nakamaClient.Session.UserId ?? "NULL"}'");
        }
        
        // Ensure PlayerData has the username and game mode
        if (string.IsNullOrEmpty(PlayerData.playerName) && !string.IsNullOrEmpty(LastUsedUsername))
        {
            PlayerData.playerName = LastUsedUsername;
            //Debug.Log($"[ArrowduelConnectionManager] Updated PlayerData.playerName from LastUsedUsername: '{PlayerData.playerName}'");
        }
        
        // Ensure game mode is set for multiplayer
        PlayerData.gameMode = GameModeType.MULTIPLAYER;
        PlayerData.isAIMode = false;
        //Debug.Log($"[ArrowduelConnectionManager] Set PlayerData.gameMode = MULTIPLAYER, isAIMode = false");
        
        transitionStarted = true;
        connectButton.interactable = false;
        isConnecting = true;

        // Add a small delay to ensure both clients are synchronized
        float delay = isWaitingForOpponent ? 1.5f : TicTacToeConfig.GetSceneTransitionDelay();

        if (!isWaitingForOpponent)
        {
            UpdateStatus("Starting multiplayer game...");
        }
        else
        {
            UpdateStatus($"Opponent joined! Starting game in {delay:F1}s...");
        }

        //Debug.Log($"[ArrowduelConnectionManager] Waiting {delay}s before loading scene...");
        await Task.Delay(TimeSpan.FromSeconds(delay));

        //Debug.Log($"[ArrowduelConnectionManager] Loading scene: {TicTacToeConfig.Scenes.TICTACTOE_MULTIPLAYER}");
        SceneManager.LoadScene(TicTacToeConfig.Scenes.TICTACTOE_MULTIPLAYER);
    }

    private async Task RetryMatchmakingAsync()
    {
        if (isRetrying)
        {
            Debug.LogWarning("[ArrowduelConnectionManager] Retry already in progress, skipping...");
            return;
        }

        isRetrying = true;
        retryAttempts++;

        //Debug.Log($"[ArrowduelConnectionManager] Retrying matchmaking - Attempt {retryAttempts}/{maxRetryAttempts}");
        UpdateStatus($"Retrying connection... ({retryAttempts}/{maxRetryAttempts})");

        try
        {
            // Cancel existing matchmaking if any
            if (nakamaClient != null && nakamaClient.IsMatchmaking)
            {
                await nakamaClient.CancelMatchmakingAsync();
            }

            // Wait before retrying
            await Task.Delay(TimeSpan.FromSeconds(retryDelay));

            // Check if socket is still connected
            if (nakamaClient == null || !nakamaClient.IsSocketConnected)
            {
                Debug.LogWarning("[ArrowduelConnectionManager] Socket disconnected. Reconnecting...");
                // Reconnect
                string username = LastUsedUsername ?? usernameInput?.text?.Trim() ?? "Player";
                string sanitizedUsername = TicTacToeInputValidator.SanitizeUsername(username);

                if (string.IsNullOrEmpty(sanitizedUsername))
                {
                    Debug.LogError("[ArrowduelConnectionManager] Cannot retry - no valid username");
                    isRetrying = false;
                    _ = FallbackToAIGameAsync("Connection failed. Please try again.");
                    return;
                }

                connectCancellation?.Cancel();
                connectCancellation?.Dispose();
                connectCancellation = new CancellationTokenSource();

                var error = await nakamaClient.ConnectAndStartMatchmakingAsync(sanitizedUsername,
                    connectCancellation.Token);
                if (error != null)
                {
                    Debug.LogError($"[ArrowduelConnectionManager] Retry connection failed: {error}");
                    if (retryAttempts >= maxRetryAttempts)
                    {
                        isRetrying = false;
                        _ = FallbackToAIGameAsync("Connection failed after retries. Starting AI game...");
                        return;
                    }

                    // Will retry again on next timeout
                    isRetrying = false;
                    return;
                }
            }
            else
            {
                // Socket is connected, just restart matchmaking
                //Debug.Log("[ArrowduelConnectionManager] Socket still connected. Restarting matchmaking...");

                if (nakamaClient != null)
                {
                    try
                    {
                        await nakamaClient.CancelMatchmakingAsync();
                        await Task.Delay(500); // Brief delay before restarting

                        // Restart matchmaking by calling ConnectAndStartMatchmakingAsync again
                        // This will reuse the existing connection
                        string username = LastUsedUsername ?? usernameInput?.text?.Trim() ?? "Player";
                        string sanitizedUsername = TicTacToeInputValidator.SanitizeUsername(username);

                        if (!string.IsNullOrEmpty(sanitizedUsername))
                        {
                            // Create a new cancellation token for retry
                            connectCancellation?.Cancel();
                            connectCancellation?.Dispose();
                            connectCancellation = new CancellationTokenSource();

                            var error = await nakamaClient.ConnectAndStartMatchmakingAsync(sanitizedUsername,
                                connectCancellation.Token);
                            if (error != null)
                            {
                                Debug.LogError($"[ArrowduelConnectionManager] Retry matchmaking failed: {error}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ArrowduelConnectionManager] Error restarting matchmaking: {ex}");
                    }
                }
            }

            // Reset timer and continue matchmaking
            matchmakingTimer = 0f;
            isMatchmaking = true;
            matchFound = false;
            isRetrying = false;
            UpdateStatus($"Searching for players... (Retry {retryAttempts}/{maxRetryAttempts})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ArrowduelConnectionManager] Error during retry: {ex}");
            isRetrying = false;
            if (retryAttempts >= maxRetryAttempts)
            {
                _ = FallbackToAIGameAsync("Connection failed after retries. Starting AI game...");
            }
        }
    }

    private async Task FallbackToAIGameAsync(string message)
    {
        UpdateStatus(message);
        connectButton.interactable = true;
        ResetState();
        retryAttempts = 0; // Reset retry counter
        isRetrying = false;

        try
        {
            await nakamaClient.CancelMatchmakingAsync();
            await nakamaClient.LeaveMatchAsync();
            await nakamaClient.DisconnectAsync();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ArrowduelConnectionManager] Error while cleaning up Nakama connection: {ex}");
        }

        // Don't auto-load AI scene - let user choose
        // Or change to GameScene if you want to test with AI
        Debug.LogWarning("[ArrowduelConnectionManager] Matchmaking failed. Please try again or play with AI.");
        UpdateStatus("No match found. Click Connect to try again or Play with AI.");
        
        // REMOVED: SceneManager.LoadScene(TicTacToeConfig.Scenes.TICTACTOE_AUTO);
        // This scene doesn't exist in build settings and causes errors
    }
}
    


