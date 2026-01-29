using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nakama;

public class NakamaClient : MonoBehaviour
{
    public static NakamaClient Instance { get; private set; }

    [Header("Nakama Server Settings")]
    [SerializeField] private string serverScheme = "http";
    [SerializeField] private string serverHost = "127.0.0.1";
    [SerializeField] private int serverPort = 7351; // Port 7351 for HTTP, 7350 is gRPC
    [SerializeField] private string serverKey = "defaultkey";
    [SerializeField] private bool useSSL = false;

    private IClient client;
    private ISession session;
    private ISocket socket;
    private IMatch currentMatch;
    private string currentUserId;
    private string matchmakingTicket;

    public event Action<IMatch> MatchJoined;
    public event Action<IMatchPresenceEvent> MatchPresenceUpdated;
    public event Action<string> SocketClosed;

    public IMatch CurrentMatch => currentMatch;
    public ISession Session => session;
    public string UserId => currentUserId;
    public bool IsConnected => socket != null && socket.IsConnected;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ConfigureServer(string scheme, string host, int port, string key, bool ssl)
    {
        //Debug.Log($"[NakamaClient] ConfigureServer called: {scheme}://{host}:{port}");
        //Debug.Log($"[NakamaClient] Old port was: {serverPort}, New port is: {port}");
        serverScheme = scheme;
        serverHost = host;
        serverPort = port; // FORCE use configured port
        serverKey = key;
        useSSL = ssl;
        //Debug.Log($"[NakamaClient] Server configuration updated. Port is now: {serverPort}");
        
        // If client already exists, clear it to force recreation with new port
        if (client != null)
        {
            //Debug.Log("[NakamaClient] Client already exists, will be recreated with new port");
            client = null; // Force recreation with new port
        }
    }

    public async Task<string> ConnectAndCreateMatchAsync(string username, CancellationToken cancellationToken = default)
    {
        //Debug.Log($"[NakamaClient] === ConnectAndCreateMatchAsync START ===");
        //Debug.Log($"[NakamaClient] Username: {username}");
        
        // FORCE port to 7351 for HTTP (Unity uses HTTP adapter, not gRPC)
        // This ensures correct port regardless of any cached Inspector values
        int oldPort = serverPort;
        serverPort = 7351;
        //Debug.Log($"[NakamaClient] Port changed from {oldPort} to {serverPort} (HTTP gateway)");
        //Debug.Log($"[NakamaClient] Server: {serverScheme}://{serverHost}:{serverPort}");
        
        try
        {
            // Create client with forced port
            string serverUri = $"{serverScheme}://{serverHost}:{serverPort}";
            //Debug.Log($"[NakamaClient] Creating client with URI: {serverUri}");
            //Debug.Log($"[NakamaClient] Port being used: {serverPort} (must be 7351 for HTTP)");
            client = new Client(serverScheme, serverHost, serverPort, serverKey, UnityWebRequestAdapter.Instance);
            //Debug.Log("[NakamaClient] Client created successfully");

            // Authenticate
            //Debug.Log($"[NakamaClient] Authenticating with device ID: {SystemInfo.deviceUniqueIdentifier}");
            session = await client.AuthenticateDeviceAsync(SystemInfo.deviceUniqueIdentifier, username, true);
            currentUserId = session.UserId;

            //Debug.Log($"[NakamaClient] === AUTHENTICATION SUCCESS ===");
            //Debug.Log($"[NakamaClient] Username: {username}");
            //Debug.Log($"[NakamaClient] UserId: {currentUserId}");
            //Debug.Log($"[NakamaClient] Session Created: {session.CreateTime}");
            //Debug.Log($"[NakamaClient] Session Expires: {session.ExpireTime}");
            if (!string.IsNullOrEmpty(session.AuthToken))
            {
                int tokenLength = Math.Min(20, session.AuthToken.Length);
                //Debug.Log($"[NakamaClient] AuthToken: {session.AuthToken.Substring(0, tokenLength)}...");
            }

            // Create socket
            //Debug.Log("[NakamaClient] Creating socket...");
            socket = client.NewSocket();
            //Debug.Log("[NakamaClient] Socket created. Setting up event handlers...");
            
            socket.Closed += (reason) => 
            {
                Debug.LogWarning($"[NakamaClient] === SOCKET CLOSED ===");
                Debug.LogWarning($"[NakamaClient] Reason: {reason}");
                SocketClosed?.Invoke(reason);
            };
            socket.ReceivedMatchPresence += (presence) => 
            {
                //Debug.Log($"[NakamaClient] === MATCH PRESENCE EVENT ===");
                //Debug.Log($"[NakamaClient] Joins: {presence.Joins?.Count() ?? 0}, Leaves: {presence.Leaves?.Count() ?? 0}");
                MatchPresenceUpdated?.Invoke(presence);
            };
            socket.ReceivedMatchState += OnMatchStateReceived;
            socket.ReceivedMatchmakerMatched += OnMatchmakerMatched;
            //Debug.Log("[NakamaClient] Event handlers registered");

            await socket.ConnectAsync(session, true);
            //Debug.Log("[NakamaClient] === SOCKET CONNECTED ===");
            //Debug.Log($"[NakamaClient] Socket.IsConnected: {socket.IsConnected}");

            // Use matchmaking with fixed query - both players use same query to match together
            // Player 1 starts matchmaking first (creates match), Player 2 joins later
            string matchQuery = "*"; // Match any player
            //Debug.Log($"[NakamaClient] Starting matchmaking with query: {matchQuery}");
            //Debug.Log($"[NakamaClient] Min players: 2, Max players: 2");
            var ticket = await socket.AddMatchmakerAsync(matchQuery, 2, 2);
            matchmakingTicket = ticket?.Ticket ?? "";
            //Debug.Log($"[NakamaClient] === MATCHMAKING STARTED ===");
            //Debug.Log($"[NakamaClient] Matchmaking Ticket: {matchmakingTicket}");
            //Debug.Log($"[NakamaClient] Waiting for opponent... (Player 1 creates match, Player 2 will join)");

            // Match will be received via OnMatchmakerMatched event when both players are ready
            return null; // Success
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NakamaClient] === CONNECTION ERROR ===");
            Debug.LogError($"[NakamaClient] Exception Type: {ex.GetType().Name}");
            Debug.LogError($"[NakamaClient] Message: {ex.Message}");
            Debug.LogError($"[NakamaClient] StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.LogError($"[NakamaClient] Inner Exception: {ex.InnerException.Message}");
            }
            return ex.Message;
        }
    }

    public async Task<string> ConnectAndStartMatchmakingAsync(string username, CancellationToken cancellationToken = default)
    {
        //Debug.Log($"[NakamaClient] === ConnectAndStartMatchmakingAsync START ===");
        //Debug.Log($"[NakamaClient] Username: {username}");
        //Debug.Log($"[NakamaClient] Server: {serverScheme}://{serverHost}:{serverPort}");
        
        try
        {
            // Create client
            string serverUri = $"{serverScheme}://{serverHost}:{serverPort}";
            //Debug.Log($"[NakamaClient] Creating client with URI: {serverUri}");
            client = new Client(serverScheme, serverHost, serverPort, serverKey, UnityWebRequestAdapter.Instance);
            //Debug.Log("[NakamaClient] Client created successfully");

            // Authenticate
            //Debug.Log($"[NakamaClient] Authenticating with device ID: {SystemInfo.deviceUniqueIdentifier}");
            session = await client.AuthenticateDeviceAsync(SystemInfo.deviceUniqueIdentifier, username, true);
            currentUserId = session.UserId;

            //Debug.Log($"[NakamaClient] === AUTHENTICATION SUCCESS ===");
            //Debug.Log($"[NakamaClient] Username: {username}");
            //Debug.Log($"[NakamaClient] UserId: {currentUserId}");
            //Debug.Log($"[NakamaClient] Session Created: {session.CreateTime}");
            //Debug.Log($"[NakamaClient] Session Expires: {session.ExpireTime}");
            if (!string.IsNullOrEmpty(session.AuthToken))
            {
                int tokenLength = Math.Min(20, session.AuthToken.Length);
                //Debug.Log($"[NakamaClient] AuthToken: {session.AuthToken.Substring(0, tokenLength)}...");
            }

            // Create socket
            //Debug.Log("[NakamaClient] Creating socket...");
            socket = client.NewSocket();
            //Debug.Log("[NakamaClient] Socket created. Setting up event handlers...");
            
            socket.Closed += (reason) => 
            {
                Debug.LogWarning($"[NakamaClient] === SOCKET CLOSED ===");
                Debug.LogWarning($"[NakamaClient] Reason: {reason}");
                SocketClosed?.Invoke(reason);
            };
            socket.ReceivedMatchPresence += (presence) => 
            {
                //Debug.Log($"[NakamaClient] === MATCH PRESENCE EVENT ===");
                //Debug.Log($"[NakamaClient] Joins: {presence.Joins?.Count() ?? 0}, Leaves: {presence.Leaves?.Count() ?? 0}");
                MatchPresenceUpdated?.Invoke(presence);
            };
            socket.ReceivedMatchState += OnMatchStateReceived;
            socket.ReceivedMatchmakerMatched += OnMatchmakerMatched;
            //Debug.Log("[NakamaClient] Event handlers registered");

            //Debug.Log("[NakamaClient] Connecting socket...");
            await socket.ConnectAsync(session, true);
            //Debug.Log("[NakamaClient] === SOCKET CONNECTED ===");
            //Debug.Log($"[NakamaClient] Socket.IsConnected: {socket.IsConnected}");

            // Start matchmaking - use AddMatchmakerAsync (correct API name)
            //Debug.Log("[NakamaClient] Starting matchmaking...");
            //Debug.Log("[NakamaClient] Matchmaking query: *, Min: 2, Max: 2");
            var ticket = await socket.AddMatchmakerAsync("*", 2, 2);
            // Extract ticket string from the matchmaking ticket object
            matchmakingTicket = ticket?.Ticket ?? "";
            //Debug.Log($"[NakamaClient] === MATCHMAKING STARTED ===");
            //Debug.Log($"[NakamaClient] Matchmaking Ticket: {matchmakingTicket}");
            //Debug.Log($"[NakamaClient] Waiting for match...");

            // Match will be received via OnMatchmakerMatched event
            // Don't invoke MatchJoined here - it will be called in OnMatchmakerMatched

            return null; // Success
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NakamaClient] === CONNECTION ERROR ===");
            Debug.LogError($"[NakamaClient] Exception Type: {ex.GetType().Name}");
            Debug.LogError($"[NakamaClient] Message: {ex.Message}");
            Debug.LogError($"[NakamaClient] StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.LogError($"[NakamaClient] Inner Exception: {ex.InnerException.Message}");
            }
            return ex.Message;
        }
    }

    public async Task CancelMatchmakingAsync()
    {
        if (socket != null && socket.IsConnected && !string.IsNullOrEmpty(matchmakingTicket))
        {
            try
            {
                await socket.RemoveMatchmakerAsync(matchmakingTicket);
                matchmakingTicket = null;
                //Debug.Log("[NakamaClient] Matchmaking cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NakamaClient] Error canceling matchmaking: {ex}");
            }
        }
    }

    public async Task LeaveMatchAsync()
    {
        if (socket != null && currentMatch != null)
        {
            try
            {
                await socket.LeaveMatchAsync(currentMatch.Id);
                currentMatch = null;
                //Debug.Log("[NakamaClient] Left match");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NakamaClient] Error leaving match: {ex}");
            }
        }
    }

    public async Task DisconnectAsync()
    {
        if (socket != null)
        {
            try
            {
                await socket.CloseAsync();
                socket = null;
                //Debug.Log("[NakamaClient] Disconnected");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NakamaClient] Error disconnecting: {ex}");
            }
        }
    }

    public void SendMatchData(long opCode, byte[] data)
    {
        if (socket != null && currentMatch != null && socket.IsConnected)
        {
            socket.SendMatchStateAsync(currentMatch.Id, opCode, data);
        }
    }

    public void SendMatchData<T>(long opCode, T data) where T : class
    {
        string json = JsonUtility.ToJson(data);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        SendMatchData(opCode, bytes);
    }

    private void OnMatchStateReceived(IMatchState matchState)
    {
        // Handle received match data
        //Debug.Log($"[NakamaClient] Received match state: OpCode={matchState.OpCode}, UserId={matchState.UserPresence.UserId}");

        // Dispatch to handlers based on opCode
        if (NakamaNetworkManager.Instance != null)
        {
            NakamaNetworkManager.Instance.OnMatchDataReceived(matchState);
        }
    }

    private async void OnMatchmakerMatched(IMatchmakerMatched matched)
    {
        //Debug.Log($"[NakamaClient] === MATCH FOUND ===");
        //Debug.Log($"[NakamaClient] MatchId: {matched.MatchId}");
        //Debug.Log($"[NakamaClient] Token: {matched.Token}");
        //Debug.Log($"[NakamaClient] Users in match: {matched.Users?.Count() ?? 0}");
        
        if (matched.Users != null)
        {
            int userIndex = 0;
            foreach (var user in matched.Users)
            {
                // IMatchmakerUser has Presence property with UserId and Username
                var presence = user.Presence;
                //Debug.Log($"[NakamaClient] User {userIndex}: UserId={presence?.UserId ?? "null"}, Username={presence?.Username ?? "null"}");
                userIndex++;
            }
        }
        
        try
        {
            //Debug.Log("[NakamaClient] Joining match...");
            // Join match
            currentMatch = await socket.JoinMatchAsync(matched);
            //Debug.Log($"[NakamaClient] === MATCH JOINED ===");
            //Debug.Log($"[NakamaClient] MatchId: {currentMatch.Id}");
            //Debug.Log($"[NakamaClient] Authoritative: {currentMatch.Authoritative}");
            //Debug.Log($"[NakamaClient] Label: {currentMatch.Label}");
            //Debug.Log($"[NakamaClient] Size: {currentMatch.Size}");
            //Debug.Log($"[NakamaClient] Presences count: {currentMatch.Presences.Count()}");
            //Debug.Log($"[NakamaClient] Total players (including self): {currentMatch.Presences.Count() + 1}");
            
            if (currentMatch.Presences != null && currentMatch.Presences.Any())
            {
                int presenceIndex = 0;
                foreach (var presence in currentMatch.Presences)
                {
                    //Debug.Log($"[NakamaClient] Presence {presenceIndex}: UserId={presence.UserId}, Username={presence.Username}, SessionId={presence.SessionId}");
                    presenceIndex++;
                }
            }
            
            //Debug.Log($"[NakamaClient] Current UserId: {currentUserId}");
            //Debug.Log("[NakamaClient] Invoking MatchJoined event...");
            MatchJoined?.Invoke(currentMatch);
            //Debug.Log("[NakamaClient] MatchJoined event invoked");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NakamaClient] === ERROR JOINING MATCH ===");
            Debug.LogError($"[NakamaClient] Exception Type: {ex.GetType().Name}");
            Debug.LogError($"[NakamaClient] Message: {ex.Message}");
            Debug.LogError($"[NakamaClient] StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.LogError($"[NakamaClient] Inner Exception: {ex.InnerException.Message}");
            }
        }
    }

    private void OnDestroy()
    {
        _ = DisconnectAsync();
    }
}
