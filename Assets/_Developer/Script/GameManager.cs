using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Nakama;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public TMP_Text playerScreenNameText;

    public GameObject loadingPanel;
    [Header("TESTING : ")]
    [SerializeField] private bool isLocalPlayerTest = false;
    public bool hasAutoPlay = false;

    [Header("UI.")]
    [SerializeField] private TextMeshProUGUI player1NameText;
    [SerializeField] private TextMeshProUGUI player2NameText;

    [SerializeField] private Button spwanBombBtn;


    [Header("MULTIPLAYER REF.")]
    public GameObject player1NetworkStatePrefab;
    public GameObject player2NetworkStatePrefab;

    
    [Header("Game State")]
    public GameState gameState;
    public OpponentType opponentType;
    public static GameModeType gameMode;

    [Header("Ref.")]
    public PlayerController playerController; // Left player
    [Space(05)]
    public OpponentController opponentPlayerController; // Right player

    public GameObject waitingPanel;
    [SerializeField] private TextMeshProUGUI waitingText;

    public GameObject[] windIndicators;

    public GameObject arrowHitEffect;

    private int halfScreenWidth;
    private bool movingLeft = false;
    private bool movingRight = false;

    // Multiplayer player tracking
    private int trackedPlayerCount = 0; // Track player count from presence events
    private bool playersReady = false; // Flag to track when both players are detected
    private HashSet<string> readyPlayers = new HashSet<string>(); // Track which players have sent ready signals
    private bool hasSentReadySignal = false; // Track if we've sent our ready signal
    private bool bothPlayersReady = false; // Flag to track when both players have sent ready signals

    public static Action onGameStart;
    public static Action<bool> onHitTarget;

    public static Action onGameLevelChange;


    private void Awake()
    {
        instance = this;
        

    }

    private void Start()
    {
        //Debug.Log($"[GameManager] === GAMESCENE LOADED ===");
        //Debug.Log($"[GameManager] GameMode: {PlayerData.gameMode}, IsAIMode: {PlayerData.isAIMode}, PlayerName: {PlayerData.playerName}");
        //Debug.Log($"[GameManager] Current Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        //Debug.Log($"[GameManager] ArrowduelConnectionManager.Instance: {(ArrowduelConnectionManager.Instance != null ? "EXISTS" : "NULL")}");
        //Debug.Log($"[GameManager] ArrowduelNakamaClient.Instance: {(ArrowduelNakamaClient.Instance != null ? "EXISTS" : "NULL")}");

        // Check if players already exist (prevent duplicate spawning)
        spwanBombBtn.onClick.AddListener(SpawnBomb);

        if (playerController != null || opponentPlayerController != null)
        {
            Debug.LogWarning("[GameManager] Players already exist! Skipping spawn.");
            Debug.LogWarning($"[GameManager] playerController: {(playerController != null ? "EXISTS" : "NULL")}");
            Debug.LogWarning($"[GameManager] opponentPlayerController: {(opponentPlayerController != null ? "EXISTS" : "NULL")}");
            return;
        }

        // Check if we should start in AI mode
        if (PlayerData.isAIMode && PlayerData.gameMode == GameModeType.SINGLEPLAYER)
        {
            //Debug.Log("[GameManager] Starting AI game mode...");
            StartCoroutine(StartAIGame());
        }
        // Check if we should spawn players for multiplayer mode
        else if (!PlayerData.isAIMode && PlayerData.gameMode == GameModeType.MULTIPLAYER)
        {
            //Debug.Log("[GameManager] === MULTIPLAYER MODE DETECTED ===");
            //Debug.Log("[GameManager] Checking if match is ready and spawning players...");

            // Wait a frame for everything to initialize, then spawn players
            StartCoroutine(WaitAndSpawnPlayers());
        }
        else
        {
            Debug.LogWarning($"[GameManager] Unknown game mode state! GameMode: {PlayerData.gameMode}, IsAIMode: {PlayerData.isAIMode}");
            Debug.LogWarning("[GameManager] Defaulting to multiplayer spawn attempt...");
            PlayerData.gameMode = GameModeType.MULTIPLAYER;
            PlayerData.isAIMode = false;
            StartCoroutine(WaitAndSpawnPlayers());
        }
    }

    public void SpawnBomb()
    {
        Debug.Log("[GameManager] Spawning bomb...");
        ArrowduelNetworkManager.Instance.PowerUp_RPC();
    }
    private IEnumerator WaitAndSpawnPlayers()
    {
        //Debug.Log("[GameManager] === WaitAndSpawnPlayers STARTED ===");
        
        // Reset tracking
        trackedPlayerCount = 0;
        playersReady = false;
        readyPlayers.Clear();
        hasSentReadySignal = false;
        bothPlayersReady = false;
        
        //Debug.Log("[GameManager] Reset all tracking flags");
        
        // Wait a frame for scene to fully load
        yield return null;
        yield return null; // Wait another frame for all components to initialize

        // Initialize tracked player count from current match state
        if (ArrowduelNakamaClient.Instance?.CurrentMatch != null)
        {
            int presenceCount = ArrowduelNakamaClient.Instance.CurrentMatch.Presences.Count();
            trackedPlayerCount = presenceCount + 1; // +1 for self
            //Debug.Log($"[GameManager] Initial tracked player count: {trackedPlayerCount} (presences: {presenceCount} + self)");
            
            if (trackedPlayerCount >= 2)
            {
                playersReady = true;
                //Debug.Log("[GameManager] ✓ Initial check: Both players already detected!");
            }
        }

        // Wait for match to be ready - need at least 2 players
        float timeout = 15f; // Increased timeout
        float elapsed = 0f;
        float checkInterval = 0.1f; // Check every 0.1 seconds

        //Debug.Log("[GameManager] Waiting for match to be ready...");
        //Debug.Log($"[GameManager] ArrowduelNakamaClient.Instance: {(ArrowduelNakamaClient.Instance != null ? "EXISTS" : "NULL")}");
        
        while (elapsed < timeout)
        {
            if (ArrowduelNakamaClient.Instance != null)
            {
                if (ArrowduelNakamaClient.Instance.CurrentMatch != null)
                {
                    // Use tracked player count (updated from presence events) instead of just presences count
                    // This ensures we detect players even if they join after we check initially
                    int currentPlayerCount = trackedPlayerCount;
                    int presenceCount = ArrowduelNakamaClient.Instance.CurrentMatch.Presences.Count();
                    
                    // Update tracked count if it's different (shouldn't happen, but safety check)
                    if (trackedPlayerCount == 0)
                    {
                        trackedPlayerCount = presenceCount + 1;
                        currentPlayerCount = trackedPlayerCount;
                        //Debug.Log($"[GameManager] Updated tracked count from presences: {trackedPlayerCount}");
                    }
                    
                    // Get sorted presences for opponent lookup
                    var presences = ArrowduelNakamaClient.Instance.CurrentMatch.Presences;
                    var sortedPresences = presences.ToList();
                    sortedPresences.Sort((a, b) => string.Compare(a.UserId, b.UserId));
                    
                  //  //Debug.Log($"[GameManager] Match found! Tracked players: {currentPlayerCount}, Presences: {presenceCount}, playersReady: {playersReady}");
                    
                    // Wait for at least 2 players total (self + at least 1 other)
                    if (currentPlayerCount >= 2 && playersReady)
                    {
                        // Send ready signal if we haven't already
                        if (!hasSentReadySignal)
                        {
                            SendPlayerReadySignal();
                            hasSentReadySignal = true;
                            //Debug.Log("[GameManager] ✓ Sent PLAYER_READY signal");
                        }
                        
                        // Check if both players are ready (including ourselves)
                        string localUserId = ArrowduelNakamaClient.Instance.Session?.UserId;
                        if (localUserId != null && readyPlayers.Contains(localUserId))
                        {
                            // We're ready, check if opponent is also ready
                            var opponentPresence = sortedPresences.FirstOrDefault(p => p.UserId != localUserId);
                            if (opponentPresence != null && readyPlayers.Contains(opponentPresence.UserId))
                            {
                                // Both players are ready!
                                if (!bothPlayersReady)
                                {
                                    bothPlayersReady = true;
                                    Debug.Log("[GameManager] ✓✓✓ BOTH PLAYERS READY! Spawning players simultaneously...");
                                    StartCoroutine(SpawnPlayerNakama());
                                    yield break;
                                }
                            }
                            else
                            {
                                //Debug.Log($"[GameManager] Waiting for opponent ready signal... Ready players: {readyPlayers.Count}/2 (Local: {localUserId}, Opponent: {opponentPresence?.UserId ?? "NULL"})");
                            }
                        }
                        else
                        {
                            //Debug.Log($"[GameManager] Waiting for ready signals... Ready players: {readyPlayers.Count}/2");
                        }
                    }
                    else
                    {
                     //   //Debug.Log($"[GameManager] Waiting for more players... Currently: {currentPlayerCount}/2 (playersReady: {playersReady})");
                    }
                }
                else
                {
                    //Debug.Log("[GameManager] CurrentMatch is null, waiting...");
                }
            }
            else
            {
                //Debug.Log("[GameManager] ArrowduelNakamaClient.Instance is null, waiting...");
            }

            elapsed += checkInterval;
            yield return new WaitForSeconds(checkInterval);
        }

        Debug.LogWarning($"[GameManager] Timeout ({timeout}s) waiting for match. Attempting to spawn players anyway...");
        Debug.LogWarning($"[GameManager] Tracked player count: {trackedPlayerCount}, playersReady: {playersReady}");
        Debug.LogWarning($"[GameManager] ArrowduelNakamaClient.Instance: {(ArrowduelNakamaClient.Instance != null ? "EXISTS" : "NULL")}");
        if (ArrowduelNakamaClient.Instance != null)
        {
            Debug.LogWarning($"[GameManager] CurrentMatch: {(ArrowduelNakamaClient.Instance.CurrentMatch != null ? "EXISTS" : "NULL")}");
            if (ArrowduelNakamaClient.Instance.CurrentMatch != null)
            {
                int presenceCount = ArrowduelNakamaClient.Instance.CurrentMatch.Presences.Count();
                int totalPlayers = presenceCount + 1;
                Debug.LogWarning($"[GameManager] Presences count: {presenceCount}, Total players: {totalPlayers}");
            }
        }
        
        // CRITICAL: Always try to spawn players, even if match check failed
        // This ensures both clients spawn players
        Debug.Log("[GameManager2222222] === FORCING PLAYER SPAWN (timeout fallback) ===");
        StartCoroutine(SpawnPlayerNakama());
    }

    private IEnumerator StartMultiplayerGame()
    {
        //Debug.Log("[GameManager] === StartMultiplayerGame CALLED ===");
        //Debug.Log("[GameManager] Waiting for ArrowduelConnectionManager to be ready...");

        // REPLACE: Create ArrowduelConnectionManager if it doesn't exist
        if (ArrowduelConnectionManager.Instance == null)
        {
            //Debug.Log("[GameManager] ArrowduelConnectionManager.Instance is null! Creating...");
            var connectionManagerGO = new GameObject("ArrowduelConnectionManager");
            connectionManagerGO.AddComponent<ArrowduelConnectionManager>();
            yield return new WaitUntil(() => ArrowduelConnectionManager.Instance != null);
            //Debug.Log("[GameManager] ArrowduelConnectionManager created ✓");
        }
        else
        {
            //Debug.Log("[GameManager] ArrowduelConnectionManager already exists ✓");
        }

        // REMOVE: NakamaClient check (not needed - ArrowduelConnectionManager handles it)

        //Debug.Log($"[GameManager] === INITIATING CONNECTION ===");
        //Debug.Log($"[GameManager] PlayerName: {PlayerData.playerName}");

        // REPLACE: The new system handles connection automatically when scene loads
        // Connection is initiated from the menu scene, not here
        // If connection manager exists, it should already be connected
        if (ArrowduelConnectionManager.Instance != null)
        {
            //Debug.Log("[GameManager] ArrowduelConnectionManager exists - connection should already be established");
        }
    }

    private IEnumerator StartAIGame()
    {
        // Wait a frame to ensure everything is initialized
        yield return null;
        
        // Start AI game with selected difficulty
        Bot(PlayerData.aiDifficulty);
    }

    void OnEnable()
    {
        halfScreenWidth = Screen.width / 2;

        onGameLevelChange += OnGameLevelChange;

        // Subscribe to match state and presence events for multiplayer
        if (ArrowduelNakamaClient.Instance != null)
        {
            ArrowduelNakamaClient.Instance.MatchStateReceived += OnMatchStateReceived;
            ArrowduelNakamaClient.Instance.MatchPresenceUpdated += OnMatchPresenceUpdated;
        }
    }

    void OnDisable()
    {
        onGameLevelChange -= OnGameLevelChange;

        // Unsubscribe from match state and presence events
        if (ArrowduelNakamaClient.Instance != null)
        {
            ArrowduelNakamaClient.Instance.MatchStateReceived -= OnMatchStateReceived;
            ArrowduelNakamaClient.Instance.MatchPresenceUpdated -= OnMatchPresenceUpdated;
        }
    }

    // Handle presence updates to track player count
    private void OnMatchPresenceUpdated(IMatchPresenceEvent presenceEvent)
    {
        if (presenceEvent == null || ArrowduelNakamaClient.Instance?.CurrentMatch == null)
            return;

        // Update tracked player count
        int joinsCount = presenceEvent.Joins?.Count() ?? 0;
        int leavesCount = presenceEvent.Leaves?.Count() ?? 0;
        
        //Debug.Log($"[GameManager] Presence updated: Joins={joinsCount}, Leaves={leavesCount}");
        
        // Recalculate total player count from current match state
        int presenceCount = ArrowduelNakamaClient.Instance.CurrentMatch.Presences.Count();
        trackedPlayerCount = presenceCount + 1; // +1 for self
        
        //Debug.Log($"[GameManager] Updated tracked player count: {trackedPlayerCount} (presences: {presenceCount} + self)");
        
        // Log joining/leaving players
        if (presenceEvent.Joins != null)
        {
            foreach (var join in presenceEvent.Joins)
            {
                //Debug.Log($"[GameManager] Player joined: {join.Username} ({join.UserId})");
            }
        }
        
        if (presenceEvent.Leaves != null)
        {
            foreach (var leave in presenceEvent.Leaves)
            {
                //Debug.Log($"[GameManager] Player left: {leave.Username} ({leave.UserId})");
            }
        }
        
        // Check if we now have 2 players
        if (trackedPlayerCount >= 2 && !playersReady)
        {
            playersReady = true;
            //Debug.Log($"[GameManager] ✓ Both players detected via presence events! ({trackedPlayerCount} players)");
        }
        else if (trackedPlayerCount < 2)
        {
            playersReady = false;
        }
    }

#if UNITY_EDITOR
    void Update()
    {

        if (gameState != GameState.Gameplay || gameState == GameState.WaitForLevelChange)
            return;

        /*playerController.UpdatePlayerBehavior();

        if (opponentType == OpponentType.Player)
        {
            opponentPlayerController.UpdatePlayerBehavior();
        }
        else
        {
           // aiPlayerController.UpdateAiBehavior();
        }*/


        if (isLocalPlayerTest)
        {

            movingLeft = false;
            movingRight = false;

            // Check all touches
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (touch.position.x < halfScreenWidth)
                {
                    movingLeft = true;
                }
                else
                {
                    movingRight = true;
                }
            }

            // Apply movement
            if (movingLeft && !movingRight)
            {
                // transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
                playerController.HandleInput();
            }
            else if (movingRight && !movingLeft)
            {
                // transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
                opponentPlayerController.HandleInput();
            }

        }

    }
#endif


    public void SetPlayersName(bool isLocalPlayer)
    {
        string localPlayerName = string.IsNullOrEmpty(PlayerData.playerName) ? "Me" : PlayerData.playerName;
        string opponentName = "Opponent";
        
        // Try to get opponent name from match presences in multiplayer
        if (GameManager.gameMode == GameModeType.MULTIPLAYER && 
            ArrowduelNakamaClient.Instance != null && 
            ArrowduelNakamaClient.Instance.CurrentMatch != null)
        {
            var presences = ArrowduelNakamaClient.Instance.CurrentMatch.Presences;
            string currentUserId = ArrowduelNakamaClient.Instance.Session?.UserId;
            
            //Debug.Log($"[GameManager] === SETTING PLAYER NAMES ===");
            //Debug.Log($"[GameManager] Local Player Name: {localPlayerName}");
            //Debug.Log($"[GameManager] Current UserId: {currentUserId}");
            //Debug.Log($"[GameManager] Presences count: {presences.Count()}");
            
            // Find opponent's name from presences
            foreach (var presence in presences)
            {
                //Debug.Log($"[GameManager] Presence - UserId: {presence.UserId}, Username: {presence.Username}");
                if (presence.UserId != currentUserId)
                {
                    opponentName = string.IsNullOrEmpty(presence.Username) ? "Opponent" : presence.Username;
                    //Debug.Log($"[GameManager] Found opponent: {opponentName} (UserId: {presence.UserId})");
                    break;
                }
            }
            
            // Also check self in presences to get our own username
            foreach (var presence in presences)
            {
                if (presence.UserId == currentUserId && !string.IsNullOrEmpty(presence.Username))
                {
                    localPlayerName = presence.Username;
                    //Debug.Log($"[GameManager] Updated local player name from session: {localPlayerName}");
                    break;
                }
            }
        }
        
        if (isLocalPlayer)
        {
            player1NameText.text = localPlayerName;
            player2NameText.text = opponentName;
            //Debug.Log($"[GameManager] Player 1 (Left): {localPlayerName}");
            //Debug.Log($"[GameManager] Player 2 (Right): {opponentName}");
        }
        else
        {
            player1NameText.text = opponentName;
            player2NameText.text = localPlayerName;
            //Debug.Log($"[GameManager] Player 1 (Left): {opponentName}");
            //Debug.Log($"[GameManager] Player 2 (Right): {localPlayerName}");
        }
        
        //Debug.Log($"[GameManager] === PLAYER NAMES SET ===");
        //Debug.Log($"[GameManager] Player1NameText: {player1NameText.text}");
        //Debug.Log($"[GameManager] Player2NameText: {player2NameText.text}");
    }

    // Player screen name text ko gameplay mode mein set karega
    public void SetPlayerScreenName()
    {
        if (playerScreenNameText == null)
        {
            Debug.LogWarning("[GameManager] playerScreenNameText is null! Cannot set player name.");
            return;
        }

        string playerName = string.IsNullOrEmpty(PlayerData.playerName) ? "Player" : PlayerData.playerName;
        
        // Multiplayer mode mein session se naam le sakte hain
        if (GameManager.gameMode == GameModeType.MULTIPLAYER && 
            ArrowduelNakamaClient.Instance != null && 
            ArrowduelNakamaClient.Instance.Session != null)
        {
            string sessionUsername = ArrowduelNakamaClient.Instance.Session.Username;
            if (!string.IsNullOrEmpty(sessionUsername))
            {
                playerName = sessionUsername;
            }
        }

        playerScreenNameText.text = playerName;
        Debug.Log($"[GameManager] Player screen name set to: {playerName}");

        // Debug: Host status and network playerIDs on both screens (multiplayer only)
        if (GameManager.gameMode == GameModeType.MULTIPLAYER &&
            ArrowduelNakamaClient.Instance != null)
        {
            bool isHost = ArrowduelNakamaClient.Instance.IsHost;
            string hostStatus = isHost ? "HOST" : "CLIENT";
            Debug.Log($"[GameManager] === MULTIPLAYER DEBUG (this screen) ===");
            Debug.Log($"[GameManager] Who is host: {hostStatus} (IsHost={isHost})");
            if (playerController != null)
                Debug.Log($"[GameManager] Player 1 (left) network playerID: {playerController.playerID}");
            else
                Debug.Log("[GameManager] Player 1 (left) network playerID: NULL (playerController missing)");
            if (opponentPlayerController != null)
                Debug.Log($"[GameManager] Player 2 (right) network playerID: {opponentPlayerController.playerID}");
            else
                Debug.Log("[GameManager] Player 2 (right) network playerID: NULL (opponentPlayerController missing)");
        }
    
    }


    public void TwoArrowHitEffectActive(Vector3 pos)
    {
        if (arrowHitEffect.activeInHierarchy)
        {
            // //Debug.Log($"ARROW.... RETURN TO EFFECT ******");
            return;
        }

        arrowHitEffect.transform.position = pos;
        arrowHitEffect.SetActive(true);
        Invoke(nameof(ArrowHitEffectDeactive), 1f);
    }

    private void ArrowHitEffectDeactive()
    {
        arrowHitEffect.gameObject.SetActive(false);
        arrowHitEffect.transform.position = Vector3.zero;
    }


    [ContextMenu("- ForceToChangeLevel -")]
    public void ForceToChangeLevel()
    {
        onGameLevelChange?.Invoke();
    }

    IEnumerator WaitForPlayerIEnum()
    {
        if (!waitingPanel.activeSelf)
            waitingPanel.SetActive(true);

        yield return new WaitForEndOfFrame();
        gameState = GameState.WaitforOtherPlayer;
        waitingText.text = "WAITING FOR OPPONENT";

        yield return new WaitForSeconds(0.25f);
        waitingText.text = "WAITING FOR OPPONENT.";

        yield return new WaitForSeconds(0.5f);
        waitingText.text = "WAITING FOR OPPONENT. .";

        yield return new WaitForSeconds(1.0f);
        waitingText.text = "WAITING FOR OPPONENT. . .";

    }

    public void WaitingScreenActive()
    {
        opponentType = OpponentType.Player;
        gameMode = GameModeType.MULTIPLAYER;

        StartCoroutine(WaitForPlayerIEnum());

    }

    public void Bot(AiSkillLevels skillLevels)
    {
        // Prevent double spawning - check if players already exist
        if (playerController != null || opponentPlayerController != null)
        {
            Debug.LogWarning("[GameManager] Bot() called but players already exist! Skipping spawn to prevent duplicates.");
            return;
        }

        // Check if prefabs are assigned
        if (player1NetworkStatePrefab == null)
        {
            Debug.LogError("[GameManager] player1NetworkStatePrefab is not assigned! Please assign it in the Inspector.");
            return;
        }

        if (player2NetworkStatePrefab == null)
        {
            Debug.LogError("[GameManager] player2NetworkStatePrefab is not assigned! Please assign it in the Inspector.");
            return;
        }

        opponentType = OpponentType.Ai;
        gameMode = GameModeType.SINGLEPLAYER;

        /// PLAYER :
        var playerObj = Instantiate(player1NetworkStatePrefab);
        playerController = playerObj.GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("[GameManager] PlayerController component not found on player1NetworkStatePrefab!");
            return;
        }
        playerController.playerID = 0;
        playerController.playerType = BowController.PlayerType.Player;

        /// OPPONENT - Ai :
        playerObj = Instantiate(player2NetworkStatePrefab);
        opponentPlayerController = playerObj.GetComponent<OpponentController>();
        if (opponentPlayerController == null)
        {
            Debug.LogError("[GameManager] OpponentController component not found on player2NetworkStatePrefab!");
            return;
        }
        opponentPlayerController.playerID = 1;
        opponentPlayerController.playerType = BowController.PlayerType.Ai;
        opponentPlayerController.aiSkill = skillLevels;

        SetPlayersName(true);

        waitingPanel.SetActive(false);
        gameState = GameState.Gameplay;
        SetPlayerScreenName(); // Player screen name set karo gameplay mode mein
        onGameStart?.Invoke();

    }

    public void OnGameLevelChange()
    {
        Arrow[] allArrows = FindObjectsByType<Arrow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Arrow arrow in allArrows)
        {
            Destroy(arrow.gameObject);
        }

    }

    internal void CheckForGameCompletion()
    {
        if (playerController.isGameCompleted == 1 && opponentPlayerController.isGameCompleted == 1)
        {
            ScoreManager.instance.EndGameWaitForSomeTime();
        }
    }

    public IEnumerator SpawnPlayerNakama()
    {
        //Debug.Log($"[GameManager] === SpawnPlayerNakama CALLED ===");

        // CRITICAL: Set gameMode to MULTIPLAYER before spawning
        // This ensures PlayerNetworkLocalSync components can properly initialize
        gameMode = GameModeType.MULTIPLAYER;
        Debug.Log($"[GameManager] GameMode set to MULTIPLAYER");

        // Check if prefabs are assigned
        if (player1NetworkStatePrefab == null || player2NetworkStatePrefab == null)
        {
            Debug.LogError("[GameManager] === PREFAB ASSIGNMENT ERROR ===");
            Debug.LogError(
                $"[GameManager] player1NetworkStatePrefab: {(player1NetworkStatePrefab != null ? "ASSIGNED" : "NULL")}");
            Debug.LogError(
                $"[GameManager] player2NetworkStatePrefab: {(player2NetworkStatePrefab != null ? "ASSIGNED" : "NULL")}");
            Debug.LogError(
                "[GameManager] Please assign player1NetworkStatePrefab and player2NetworkStatePrefab in the Inspector.");
            yield break;
        }

        //Debug.Log("[GameManager] Prefabs are assigned ✓");

        // Prevent double spawning
        if (playerController != null || opponentPlayerController != null)
        {
            Debug.LogWarning("[GameManager] === DUPLICATE SPAWN PREVENTED ===");
            Debug.LogWarning($"[GameManager] playerController: {(playerController != null ? "EXISTS" : "NULL")}");
            Debug.LogWarning(
                $"[GameManager] opponentPlayerController: {(opponentPlayerController != null ? "EXISTS" : "NULL")}");
            yield break;
        }

        //Debug.Log("[GameManager] No existing players found ✓");

        // REPLACE: Determine if we're player 1 or 2 based on match presences
        bool isPlayer1 = false;
        string currentUserId = null;
        string currentUsername = "Unknown";
        
        if (ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.CurrentMatch != null)
        {
            //Debug.Log("[GameManager] ArrowduelNakamaClient and CurrentMatch exist ✓");
            var presences = ArrowduelNakamaClient.Instance.CurrentMatch.Presences;
            int presenceCount = presences.Count();
            int totalPlayers = presenceCount + 1; // +1 for self
            //Debug.Log($"[GameManager] Presences count: {presenceCount}, Total players: {totalPlayers}");

            // Get current user info
            currentUserId = ArrowduelNakamaClient.Instance.Session?.UserId;
            currentUsername = ArrowduelNakamaClient.Instance.Session?.Username ?? PlayerData.playerName ?? "Unknown";
            
            //Debug.Log($"[GameManager] Current UserId: {currentUserId ?? "NULL"}");
            //Debug.Log($"[GameManager] Current Username: {currentUsername}");

            // Sort presences by UserId to determine player order
            var sortedPresences = presences.ToList();
            sortedPresences.Sort((a, b) => string.Compare(a.UserId, b.UserId));
            
            //Debug.Log($"[GameManager] Sorted presences:");
            for (int i = 0; i < sortedPresences.Count; i++)
            {
                bool isSelf = sortedPresences[i].UserId == currentUserId;
                //Debug.Log($"[GameManager]   [{i}] UserId: {sortedPresences[i].UserId}, Username: {sortedPresences[i].Username ?? "NULL"} {(isSelf ? "[SELF]" : "")}");
            }

            // Determine player order: create list of all UserIds (presences + self), sort, and check if we're first
            var allUserIds = new List<string>();
            foreach (var p in sortedPresences)
            {
                if (!string.IsNullOrEmpty(p.UserId))
                {
                    allUserIds.Add(p.UserId);
                }
            }
            if (!string.IsNullOrEmpty(currentUserId) && !allUserIds.Contains(currentUserId))
            {
                allUserIds.Add(currentUserId);
            }
            
            allUserIds.Sort();
            
            //Debug.Log($"[GameManager] All UserIds sorted (for player order):");
            for (int i = 0; i < allUserIds.Count; i++)
            {
                bool isSelf = allUserIds[i] == currentUserId;
                //Debug.Log($"[GameManager]   [{i}] UserId: {allUserIds[i]} {(isSelf ? "[SELF - THIS PLAYER]" : "")}");
            }
            
            // Determine player order: first player alphabetically by UserId is Player 1
            if (allUserIds.Count > 0 && !string.IsNullOrEmpty(currentUserId))
            {
                isPlayer1 = allUserIds[0] == currentUserId;
                //Debug.Log($"[GameManager] Player order determined: isPlayer1 = {isPlayer1} (first UserId: {allUserIds[0]}, our UserId: {currentUserId})");
            }
            else if (currentUserId != null)
            {
                // Fallback: if no presences, we're player 1
                Debug.LogWarning("[GameManager] No UserIds found, defaulting to Player 1");
                isPlayer1 = true;
            }

            //Debug.Log($"[GameManager] Is Player 1: {isPlayer1}");
            
            // Debug: Show both player names
            if (sortedPresences.Count >= 1)
            {
                string player1Name = sortedPresences[0].Username ?? sortedPresences[0].UserId ?? "Player1";
                //Debug.Log($"[GameManager] === PLAYER NAMES DEBUG ===");
                //Debug.Log($"[GameManager] Player 1 Name: {player1Name}");
                
                if (sortedPresences.Count >= 2)
                {
                    string player2Name = sortedPresences[1].Username ?? sortedPresences[1].UserId ?? "Player2";
                    //Debug.Log($"[GameManager] Player 2 Name: {player2Name}");
                }
                else
                {
                    //Debug.Log($"[GameManager] Player 2 Name: Waiting for opponent...");
                }
                
                //Debug.Log($"[GameManager] Local Player Name (from PlayerData): {PlayerData.playerName ?? "NULL"}");
                //Debug.Log($"[GameManager] Local Player Name (from Session): {currentUsername}");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] === NAKAMA CLIENT/MATCH CHECK FAILED ===");
            Debug.LogWarning(
                $"[GameManager] ArrowduelNakamaClient.Instance: {(ArrowduelNakamaClient.Instance != null ? "EXISTS" : "NULL")}");
            if (ArrowduelNakamaClient.Instance != null)
            {
                Debug.LogWarning(
                    $"[GameManager] CurrentMatch: {(ArrowduelNakamaClient.Instance.CurrentMatch != null ? "EXISTS" : "NULL")}");
            }
            
            // Fallback: Default to Player 1 if we can't determine order
            // This ensures players still spawn even if match info is unavailable
            Debug.LogWarning("[GameManager] Defaulting to Player 1 due to missing match info");
            isPlayer1 = true;
            currentUserId = "unknown";
            currentUsername = PlayerData.playerName ?? "Player";
        }

        // Around line 488, replace the comment with actual spawning code:

        // Both players spawn BOTH characters - one they control, one for the opponent
        if (isPlayer1)
        {
            //Debug.Log($"[GameManager] === SPAWNING AS PLAYER 1 ===");

            // Spawn Player 1 (left side) - this player controls this
            var player1Obj = Instantiate(player1NetworkStatePrefab);
            Debug.Log($"[GameManager] Player 1 prefab instantiated: {(player1Obj != null ? "SUCCESS" : "FAILED")}");
            yield return new WaitUntil(() => player1Obj != null);

            playerController = player1Obj.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("[GameManager] PlayerController component not found on player1NetworkStatePrefab!");
                yield break;
            }

            playerController.playerID = 0;
            playerController.playerType = BowController.PlayerType.Player;
            
            // Attach local sync component to Player 1 (we control this)
            var localSync = player1Obj.GetComponent<PlayerNetworkLocalSync>();
            if (localSync == null)
            {
                localSync = player1Obj.AddComponent<PlayerNetworkLocalSync>();
            }
            // Ensure component is enabled and wait a frame for initialization
            localSync.enabled = true;
            yield return null; // Wait a frame to ensure Start() is called
            localSync.enabled = true; // Re-enable after Start() might have disabled it
            //Debug.Log($"[GameManager] PlayerNetworkLocalSync enabled: {localSync.enabled}, GameMode: {GameManager.gameMode}");
            //Debug.Log("[GameManager] Player 1 (left) setup complete ✓");

            // Spawn Player 2 (right side) - opponent controls this
            var player2Obj = Instantiate(player2NetworkStatePrefab);
            //Debug.Log($"[GameManager] Player 2 prefab instantiated: {(player2Obj != null ? "SUCCESS" : "FAILED")}");
            yield return new WaitUntil(() => player2Obj != null);

            opponentPlayerController = player2Obj.GetComponent<OpponentController>();
            if (opponentPlayerController == null)
            {
                Debug.LogError("[GameManager] OpponentController component not found on player2NetworkStatePrefab!");
                yield break;
            }

            opponentPlayerController.playerID = 1;
            opponentPlayerController.playerType = BowController.PlayerType.Player;
            
            // ADD THIS: Remove PlayerNetworkLocalSync from remote player if it exists
            var localSyncToRemove = player2Obj.GetComponent<PlayerNetworkLocalSync>();
            if (localSyncToRemove != null)
            {
                //Debug.Log(
                    //$"[GameManager] Removing PlayerNetworkLocalSync from remote Player 2 (opponent controls this)");
                Destroy(localSyncToRemove);
            }

            yield return null; // Wait a frame for component destruction

// CRITICAL: Remove PlayerNetworkRemoteSync from all child objects first
            // This prevents components from being on child objects like "Bow Handler", "Hand Pivots", etc.
            var allRemoteSyncs = player2Obj.GetComponentsInChildren<PlayerNetworkRemoteSync>(true);
            foreach (var sync in allRemoteSyncs)
            {
                if (sync.transform != player2Obj.transform)
                {
                    Debug.LogWarning($"[GameManager] Removing PlayerNetworkRemoteSync from child object: {sync.gameObject.name}");
                    DestroyImmediate(sync);
                }
            }
            yield return null; // Wait for destruction

// Attach remote sync component to Player 2 (opponent controls this) - ONLY on root
            var remoteSync = player2Obj.GetComponent<PlayerNetworkRemoteSync>();
            if (remoteSync == null)
            {
                remoteSync = player2Obj.AddComponent<PlayerNetworkRemoteSync>();
            }

// CRITICAL: Wait a frame to ensure Start() has been called and bowController is initialized
            yield return null;

// Setup remote player network data
            if (ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.CurrentMatch != null)
            {
                var presences = ArrowduelNakamaClient.Instance.CurrentMatch.Presences;
                var sortedPresences = presences.ToList();
                sortedPresences.Sort((a, b) => string.Compare(a.UserId, b.UserId));

                // Find opponent's presence (not us)
                string localUserId = ArrowduelNakamaClient.Instance.Session?.UserId;
                var opponentPresence = sortedPresences.FirstOrDefault(p => p.UserId != localUserId);

                if (opponentPresence != null)
                {
                    remoteSync.NetworkData = new RemotePlayerNetworkData
                    {
                        MatchId = ArrowduelNakamaClient.Instance.CurrentMatch.Id,
                        User = opponentPresence
                    };
                    Debug.Log(
                        $"[GameManager] ✓ Remote sync setup for Player 2 (opponent) - UserId: {opponentPresence.UserId}, " +
                        $"SessionId: {opponentPresence.SessionId}, playerID: {opponentPlayerController.playerID}, " +
                        $"RemoteSync component: {(remoteSync != null ? "EXISTS" : "NULL")}");
                }
                else
                {
                    Debug.LogWarning(
                        $"[GameManager] ✗ Could not find opponent presence for Player 2 remote sync! Presences count: {sortedPresences.Count}");
                }
            }
            
            //Debug.Log("[GameManager] Player 2 (right - opponent) setup complete ✓");

            SetPlayersName(true);
        }
        else
        {
            //Debug.Log($"[GameManager] === SPAWNING AS PLAYER 2 ===");

            // Spawn Player 1 (left side) - opponent controls this
            var player1Obj = Instantiate(player1NetworkStatePrefab);
            Debug.Log($"[GameManager] Player 1 prefab instantiated: {(player1Obj != null ? "SUCCESS" : "FAILED")}");
            yield return new WaitUntil(() => player1Obj != null);

            playerController = player1Obj.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("[GameManager] PlayerController component not found on player1NetworkStatePrefab!");
                yield break;
            }

// CRITICAL: Set playerID before removing components
            playerController.playerID = 0;
            playerController.playerType = BowController.PlayerType.Player;

// Remove PlayerNetworkLocalSync from remote player if it exists
            var localSyncToRemove1 = player1Obj.GetComponent<PlayerNetworkLocalSync>();
            if (localSyncToRemove1 != null)
            {
                //Debug.Log(
                    //$"[GameManager] Removing PlayerNetworkLocalSync from remote Player 1 (opponent controls this)");
                Destroy(localSyncToRemove1);
            }

            yield return null; // Wait a frame for component destruction

// CRITICAL: Remove PlayerNetworkRemoteSync from all child objects first
            // This prevents components from being on child objects like "Bow Handler", "Hand Pivots", etc.
            var allRemoteSyncs1 = player1Obj.GetComponentsInChildren<PlayerNetworkRemoteSync>(true);
            foreach (var sync in allRemoteSyncs1)
            {
                if (sync.transform != player1Obj.transform)
                {
                    Debug.LogWarning($"[GameManager] Removing PlayerNetworkRemoteSync from child object: {sync.gameObject.name}");
                    DestroyImmediate(sync);
                }
            }
            yield return null; // Wait for destruction

// Attach remote sync component to Player 1 (opponent controls this) - ONLY on root
            var remoteSync1 = player1Obj.GetComponent<PlayerNetworkRemoteSync>();
            if (remoteSync1 == null)
            {
                remoteSync1 = player1Obj.AddComponent<PlayerNetworkRemoteSync>();
            }

// CRITICAL: Wait a frame to ensure Start() has been called and bowController is initialized
            yield return null;

// Setup remote player network data for opponent
            if (ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.CurrentMatch != null)
            {
                var presences = ArrowduelNakamaClient.Instance.CurrentMatch.Presences;
                var sortedPresences = presences.ToList();
                sortedPresences.Sort((a, b) => string.Compare(a.UserId, b.UserId));

                // Find opponent's presence (not us) - opponent is Player 1
                string localUserId2 = ArrowduelNakamaClient.Instance.Session?.UserId;
                var opponentPresence = sortedPresences.FirstOrDefault(p => p.UserId != localUserId2);

                if (opponentPresence != null)
                {
                    remoteSync1.NetworkData = new RemotePlayerNetworkData
                    {
                        MatchId = ArrowduelNakamaClient.Instance.CurrentMatch.Id,
                        User = opponentPresence
                    };
                    Debug.Log(
                        $"[GameManager] ✓ Remote sync setup for Player 1 (opponent) - UserId: {opponentPresence.UserId}, " +
                        $"SessionId: {opponentPresence.SessionId}, playerID: {playerController.playerID}, " +
                        $"RemoteSync component: {(remoteSync1 != null ? "EXISTS" : "NULL")}");
                }
                else
                {
                    Debug.LogWarning(
                        $"[GameManager] ✗ Could not find opponent presence for Player 1 remote sync! Presences count: {sortedPresences.Count}");
                }
            }

            //Debug.Log("[GameManager] Player 1 (left - opponent) setup complete ✓");

            // Spawn Player 2 (right side) - this player controls this
            var player2Obj = Instantiate(player2NetworkStatePrefab);
            //Debug.Log($"[GameManager] Player 2 prefab instantiated: {(player2Obj != null ? "SUCCESS" : "FAILED")}");
            yield return new WaitUntil(() => player2Obj != null);

            opponentPlayerController = player2Obj.GetComponent<OpponentController>();
            if (opponentPlayerController == null)
            {
                Debug.LogError("[GameManager] OpponentController component not found on player2NetworkStatePrefab!");
                yield break;
            }

            opponentPlayerController.playerID = 1;
            opponentPlayerController.playerType = BowController.PlayerType.Player;
            
            // Attach local sync component to Player 2 (we control this)
            var localSync2 = player2Obj.GetComponent<PlayerNetworkLocalSync>();
            if (localSync2 == null)
            {
                localSync2 = player2Obj.AddComponent<PlayerNetworkLocalSync>();
            }
            // Ensure component is enabled and wait a frame for initialization
            localSync2.enabled = true;
            yield return null; // Wait a frame to ensure Start() is called
            localSync2.enabled = true; // Re-enable after Start() might have disabled it
            //Debug.Log($"[GameManager] PlayerNetworkLocalSync enabled: {localSync2.enabled}, GameMode: {GameManager.gameMode}");
            //Debug.Log("[GameManager] Player 2 (right) setup complete ✓");

            SetPlayersName(false);
        }

        // Verify both players were spawned
        //Debug.Log("[GameManager] === VERIFYING PLAYER SPAWN ===");
        //Debug.Log($"[GameManager] playerController: {(playerController != null ? "SPAWNED ✓" : "MISSING ✗")}");
        //Debug.Log($"[GameManager] opponentPlayerController: {(opponentPlayerController != null ? "SPAWNED ✓" : "MISSING ✗")}");
        
        if (playerController == null || opponentPlayerController == null)
        {
            Debug.LogError("[GameManager] === CRITICAL: PLAYER SPAWN FAILED ===");
            Debug.LogError($"[GameManager] playerController is null: {playerController == null}");
            Debug.LogError($"[GameManager] opponentPlayerController is null: {opponentPlayerController == null}");
            Debug.LogError("[GameManager] Game cannot proceed without both players!");
            yield break;
        }

        // Start game for both players
        waitingPanel.SetActive(false);
        gameState = GameState.Gameplay;
        SetPlayerScreenName(); // Player screen name set karo gameplay mode mein
        onGameStart?.Invoke();

        //Debug.Log("[GameManager] === SpawnPlayerNakama COMPLETE ===");
        //Debug.Log("[GameManager] Both players spawned successfully! Game ready to start.");

        //Debug.Log("[GameManager] === SpawnPlayerNakama COMPLETE ===");
    }

    
    public void OnGameStart()
    {
        StartCoroutine(IStartGame());
    }

    private IEnumerator IStartGame()
    {
        yield return new WaitForSeconds(1);

        waitingPanel.SetActive(false);
        gameState = GameState.Gameplay;
        SetPlayerScreenName(); // Player screen name set karo gameplay mode mein
        //GameNetworkManager.instance.SetGameState_RPC(GameState.Gameplay);

        onGameStart?.Invoke();

    }

    public void SetPlayerController(BowController playerController, bool isOpponent = false)
    {
        if (isOpponent)
        {
            this.opponentPlayerController = (OpponentController)playerController;
        }
        else
        {
            this.playerController = (PlayerController)playerController;
        }
    }

    private void OnMatchStateReceived(IMatchState matchState)
    {
        const long OPCODE_HIT_TARGET = 7;
        
        if (matchState.OpCode == OPCODE_HIT_TARGET)
        {
            string json = System.Text.Encoding.UTF8.GetString(matchState.State);
            var data = JsonUtility.FromJson<HitTargetData>(json);
            Debug.Log($"[GameManager] OnMatchStateReceived: HitTargetData - isPlayerArrow: {data.isPlayerArrow}");
            GameManager.onHitTarget?.Invoke(data.isPlayerArrow);
        }
        else if (matchState.OpCode == ArrowduelNetworkManager.OPCODE_PLAYER_READY)
        {
            // Handle player ready signal
            HandlePlayerReadySignal(matchState);
        }
        // Handle other opcodes here (wind, theme change, level change, etc.) as needed
    }
    
    /// <summary>
    /// Sends a ready signal to indicate this player is ready to spawn.
    /// </summary>
    private async void SendPlayerReadySignal()
    {
        if (ArrowduelNakamaClient.Instance == null || ArrowduelNakamaClient.Instance.CurrentMatch == null)
        {
            Debug.LogWarning("[GameManager] Cannot send ready signal - no match available");
            return;
        }
        
        string localUserId = ArrowduelNakamaClient.Instance.Session?.UserId;
        if (string.IsNullOrEmpty(localUserId))
        {
            Debug.LogWarning("[GameManager] Cannot send ready signal - no user ID");
            return;
        }
        
        // Add ourselves to ready players immediately
        readyPlayers.Add(localUserId);
        
        // Send ready signal with user ID (simple JSON format)
        string json = $"{{\"userId\":\"{localUserId}\"}}";
        
        await ArrowduelNakamaClient.Instance.SendMatchStateAsync(
            ArrowduelNetworkManager.OPCODE_PLAYER_READY,
            json
        );
        
        //Debug.Log($"[GameManager] Sent PLAYER_READY signal for user: {localUserId}");
    }
    
    /// <summary>
    /// Handles incoming player ready signals from other players.
    /// </summary>
    private void HandlePlayerReadySignal(IMatchState matchState)
    {
        if (matchState?.UserPresence == null)
            return;
        
        string userId = matchState.UserPresence.UserId;
        if (string.IsNullOrEmpty(userId))
            return;
        
        // Add to ready players set
        if (!readyPlayers.Contains(userId))
        {
            readyPlayers.Add(userId);
            //Debug.Log($"[GameManager] ✓ Player ready signal received from: {userId} (Total ready: {readyPlayers.Count})");
            
            // Check if we should spawn now
            CheckIfBothPlayersReady();
        }
    }
    
    /// <summary>
    /// Checks if both players have sent ready signals and spawns if ready.
    /// </summary>
    private void CheckIfBothPlayersReady()
    {
        if (bothPlayersReady)
            return; // Already spawning
        
        if (ArrowduelNakamaClient.Instance == null || ArrowduelNakamaClient.Instance.CurrentMatch == null)
            return;
        
        string localUserId = ArrowduelNakamaClient.Instance.Session?.UserId;
        if (string.IsNullOrEmpty(localUserId))
            return;
        
        // Check if we're ready
        if (!readyPlayers.Contains(localUserId))
            return;
        
        // Check if opponent is ready
        var presences = ArrowduelNakamaClient.Instance.CurrentMatch.Presences;
        var sortedPresences = presences.ToList();
        sortedPresences.Sort((a, b) => string.Compare(a.UserId, b.UserId));
        
        var opponentPresence = sortedPresences.FirstOrDefault(p => p.UserId != localUserId);
        if (opponentPresence != null && readyPlayers.Contains(opponentPresence.UserId))
        {
            // Both players are ready!
            bothPlayersReady = true;
            Debug.Log("[GameManager] ✓✓✓ BOTH PLAYERS READY (from signal)! Spawning players simultaneously...");
            StartCoroutine(SpawnPlayerNakama());
        }
    }

}