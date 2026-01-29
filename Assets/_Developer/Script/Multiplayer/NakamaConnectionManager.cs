using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nakama;

public class NakamaConnectionManager : MonoBehaviour
{
    public static NakamaConnectionManager Instance { get; private set; }

    [Header("Nakama Server Settings")]
    [SerializeField] private string serverScheme = "http";
    [SerializeField] private string serverHost = "127.0.0.1";
    [SerializeField] private int serverPort = 7351; // Port 7351 for HTTP, 7350 is gRPC
    [SerializeField] private string serverKey = "defaultkey";
    [SerializeField] private bool useSSL = false;

    [Header("Timeout Settings")]
    [SerializeField] private float matchmakingTimeout = 15f;
    [SerializeField] private float opponentWaitTimeout = 10f;

    private NakamaClient nakamaClient;
    private CancellationTokenSource connectCancellation;
    private bool isConnecting;
    private bool matchFound;
    private bool waitingForOpponent;
    private float matchmakingTimer;
    private float opponentWaitTimer;
    private int currentPlayerCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureNakamaClient();
    }

    private void OnEnable()
    {
        SubscribeToClientEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromClientEvents();
    }

    private void Update()
    {
        if (isConnecting && !matchFound)
        {
            matchmakingTimer += Time.deltaTime;
            if (matchmakingTimer >= matchmakingTimeout)
            {
                Debug.LogWarning("[NakamaConnectionManager] Matchmaking timeout. Falling back to AI.");
                FallbackToAI();
            }
        }

        if (waitingForOpponent)
        {
            opponentWaitTimer += Time.deltaTime;
            if (opponentWaitTimer >= opponentWaitTimeout)
            {
                Debug.LogWarning("[NakamaConnectionManager] Opponent wait timeout. Falling back to AI.");
                FallbackToAI();
            }
        }
    }

    private void EnsureNakamaClient()
    {
        if (NakamaClient.Instance != null)
        {
            nakamaClient = NakamaClient.Instance;
            return;
        }

        var clientGO = new GameObject("NakamaClient");
        nakamaClient = clientGO.AddComponent<NakamaClient>();
    }

    private void SubscribeToClientEvents()
    {
        if (nakamaClient == null) return;

        nakamaClient.MatchJoined += HandleMatchJoined;
        nakamaClient.MatchPresenceUpdated += HandleMatchPresenceUpdated;
        nakamaClient.SocketClosed += HandleSocketClosed;
    }

    private void UnsubscribeFromClientEvents()
    {
        if (nakamaClient == null) return;

        nakamaClient.MatchJoined -= HandleMatchJoined;
        nakamaClient.MatchPresenceUpdated -= HandleMatchPresenceUpdated;
        nakamaClient.SocketClosed -= HandleSocketClosed;
    }

    public async void ConnectToServer(string matchId, string region)
    {
        //Debug.Log($"[NakamaConnectionManager] === ConnectToServer CALLED ===");
        //Debug.Log($"[NakamaConnectionManager] MatchId: {matchId}, Region: {region}");
        
        // FORCE port to 7351 for HTTP (Unity uses HTTP adapter, not gRPC)
        // This overrides any Inspector value - MUST be done FIRST
        int oldPort = serverPort;
        serverPort = 7351;
        //Debug.Log($"[NakamaConnectionManager] Port changed from {oldPort} to {serverPort} (HTTP gateway)");
        //Debug.Log($"[NakamaConnectionManager] Server: {serverScheme}://{serverHost}:{serverPort}");
        //Debug.Log($"[NakamaConnectionManager] ServerKey: {serverKey}, UseSSL: {useSSL}");
        //Debug.Log($"[NakamaConnectionManager] Port FORCED to: {serverPort} (HTTP gateway for Unity)");
        
        // Show waiting panel immediately
        if (GameManager.instance != null && GameManager.instance.waitingPanel != null)
        {
            GameManager.instance.waitingPanel.SetActive(true);
            GameManager.instance.WaitingScreenActive();
        }
        
        if (nakamaClient == null)
        {
            //Debug.Log("[NakamaConnectionManager] NakamaClient is null. Creating...");
            EnsureNakamaClient();
        }

        // IMPORTANT: Configure server settings BEFORE creating client
        //Debug.Log($"[NakamaConnectionManager] Configuring server settings...");
        //Debug.Log($"[NakamaConnectionManager] Setting port to: {serverPort}");
        nakamaClient.ConfigureServer(serverScheme, serverHost, serverPort, serverKey, useSSL);
        
        // Verify configuration was applied
        //Debug.Log($"[NakamaConnectionManager] Configuration applied. NakamaClient port should now be: {serverPort}");

        isConnecting = true;
        matchFound = false;
        matchmakingTimer = 0f;
        currentPlayerCount = 0;
        waitingForOpponent = false;

        connectCancellation?.Cancel();
        connectCancellation?.Dispose();
        connectCancellation = new CancellationTokenSource();

        try
        {
            string playerName = string.IsNullOrEmpty(PlayerData.playerName) ? "Player" : PlayerData.playerName;
            //Debug.Log($"[NakamaConnectionManager] === STARTING CONNECTION ===");
            //Debug.Log($"[NakamaConnectionManager] PlayerName: {playerName}");
            //Debug.Log($"[NakamaConnectionManager] Calling ConnectAndCreateMatchAsync...");
            
            // Use CreateMatch instead of matchmaking - Player 1 creates, Player 2 joins
            var error = await nakamaClient.ConnectAndCreateMatchAsync(playerName, connectCancellation.Token);

            if (error != null)
            {
                Debug.LogError($"[NakamaConnectionManager] === CONNECTION FAILED ===");
                Debug.LogError($"[NakamaConnectionManager] Error: {error}");
                HandleConnectionFailure();
            }
            else
            {
                //Debug.Log($"[NakamaConnectionManager] === CONNECTION INITIATED SUCCESSFULLY ===");
                //Debug.Log($"[NakamaConnectionManager] Waiting for match...");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NakamaConnectionManager] === CONNECTION EXCEPTION ===");
            Debug.LogError($"[NakamaConnectionManager] Exception: {ex}");
            Debug.LogError($"[NakamaConnectionManager] StackTrace: {ex.StackTrace}");
            HandleConnectionFailure();
        }
    }

    private void HandleMatchJoined(IMatch match)
    {
        //Debug.Log($"[NakamaConnectionManager] === HandleMatchJoined CALLED ===");
        
        matchFound = true;
        matchmakingTimer = 0f;

        // Count includes ourselves, so presences.Count() + 1 = total players
        int playerCount = match.Presences.Count() + 1;
        currentPlayerCount = playerCount;

        //Debug.Log($"[NakamaConnectionManager] === MATCH JOINED ===");
        //Debug.Log($"[NakamaConnectionManager] Match ID: {match.Id}");
        //Debug.Log($"[NakamaConnectionManager] Presences count: {match.Presences.Count()}");
        //Debug.Log($"[NakamaClient] Total player count (including self): {playerCount}");

        // Log all presences for debugging
        if (match.Presences != null && match.Presences.Any())
        {
            //Debug.Log("[NakamaConnectionManager] === PRESENCES LIST ===");
            int index = 0;
            foreach (var presence in match.Presences)
            {
                //Debug.Log($"[NakamaConnectionManager] Presence[{index}] - UserId: {presence.UserId}, Username: {presence.Username}, SessionId: {presence.SessionId}");
                index++;
            }
        }
        else
        {
            Debug.LogWarning("[NakamaConnectionManager] No presences found in match!");
        }
        
        //Debug.Log($"[NakamaConnectionManager] Current UserId: {NakamaClient.Instance?.UserId}");
        //Debug.Log($"[NakamaConnectionManager] GameManager.instance: {(GameManager.instance != null ? "EXISTS" : "NULL")}");

        if (playerCount >= 2)
        {
            //Debug.Log($"[NakamaConnectionManager] === TWO PLAYERS FOUND! ===");
            //Debug.Log($"[NakamaConnectionManager] Starting multiplayer game...");
            waitingForOpponent = false;
            opponentWaitTimer = 0f;
            // Hide waiting panel when both players found
            if (GameManager.instance != null && GameManager.instance.waitingPanel != null)
            {
                GameManager.instance.waitingPanel.SetActive(false);
            }
            BeginMultiplayerGame();
        }
        else
        {
            //Debug.Log($"[NakamaConnectionManager] === WAITING FOR OPPONENT ===");
            //Debug.Log($"[NakamaConnectionManager] Current players: {playerCount}");
            //Debug.Log($"[NakamaConnectionManager] Need 2 players total");
            waitingForOpponent = true;
            opponentWaitTimer = 0f;
            // Ensure waiting panel is visible
            if (GameManager.instance != null)
            {
                //Debug.Log("[NakamaConnectionManager] Showing waiting screen...");
                if (GameManager.instance.waitingPanel != null)
                {
                    GameManager.instance.waitingPanel.SetActive(true);
                }
                GameManager.instance.WaitingScreenActive();
            }
            else
            {
                Debug.LogError("[NakamaConnectionManager] GameManager.instance is NULL! Cannot show waiting screen.");
            }
        }
    }

    private void HandleMatchPresenceUpdated(IMatchPresenceEvent presenceEvent)
    {
        if (presenceEvent == null || nakamaClient.CurrentMatch == null) return;

        // Recalculate player count from current match state
        if (nakamaClient.CurrentMatch != null)
        {
            currentPlayerCount = nakamaClient.CurrentMatch.Presences.Count() + 1;
        }

        if (presenceEvent.Joins != null && presenceEvent.Joins.Any())
        {
            //Debug.Log($"[NakamaConnectionManager] Player(s) joined. Joins: {presenceEvent.Joins.Count()}, Total: {currentPlayerCount}");
            foreach (var join in presenceEvent.Joins)
            {
                //Debug.Log($"[NakamaConnectionManager] Joined - UserId: {join.UserId}, Username: {join.Username}");
            }
        }

        if (presenceEvent.Leaves != null && presenceEvent.Leaves.Any())
        {
            Debug.LogWarning($"[NakamaConnectionManager] Player(s) left. Leaves: {presenceEvent.Leaves.Count()}, Total: {currentPlayerCount}");
            foreach (var leave in presenceEvent.Leaves)
            {
                Debug.LogWarning($"[NakamaConnectionManager] Left - UserId: {leave.UserId}, Username: {leave.Username}");
            }

            if (GameManager.instance != null && 
                (GameManager.instance.gameState == GameState.WaitforOtherPlayer || 
                 GameManager.instance.gameState == GameState.Gameplay))
            {
                IFrameBridge.instance?.PostMatchAbort("Opponent left the game.", "", "");
                ScoreManager.instance?.EndGame(true);
            }
        }

        if (currentPlayerCount >= 2 && waitingForOpponent)
        {
            //Debug.Log($"[NakamaConnectionManager] Two players ready! Starting game...");
            waitingForOpponent = false;
            opponentWaitTimer = 0f;
            // Hide waiting panel when both players found
            if (GameManager.instance != null && GameManager.instance.waitingPanel != null)
            {
                GameManager.instance.waitingPanel.SetActive(false);
            }
            BeginMultiplayerGame();
        }
    }

    private void HandleSocketClosed(string reason)
    {
        //Debug.Log($"[NakamaConnectionManager] Socket closed: {reason}");
        if (!isConnecting) return;
        HandleConnectionFailure();
    }

    private void HandleConnectionFailure()
    {
        isConnecting = false;
        matchFound = false;
        waitingForOpponent = false;
        connectCancellation?.Cancel();
    }

    private void BeginMultiplayerGame()
    {
        //Debug.Log("[NakamaConnectionManager 11111] Starting multiplayer game...");
        if (GameManager.instance != null)
        {
            StartCoroutine(GameManager.instance.SpawnPlayerNakama());
        }
    }

    private async void FallbackToAI()
    {
        isConnecting = false;
        waitingForOpponent = false;

        try
        {
            await nakamaClient.CancelMatchmakingAsync();
            await nakamaClient.LeaveMatchAsync();
            await nakamaClient.DisconnectAsync();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[NakamaConnectionManager] Error cleaning up: {ex}");
        }

        // Fallback to AI game
        PlayerData.gameMode = GameModeType.SINGLEPLAYER;
        PlayerData.isAIMode = true;
        PlayerData.aiDifficulty = AiSkillLevels.Normal;

        if (GameManager.instance != null)
        {
            GameManager.instance.Bot(PlayerData.aiDifficulty);
        }
    }

    private void OnDestroy()
    {
        connectCancellation?.Cancel();
        connectCancellation?.Dispose();
    }
}
