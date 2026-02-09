using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Nakama;

/// <summary>
/// Manages game state synchronization via Nakama match state messages.
/// Replaces Photon Fusion's GameNetworkManager and RPC system.
/// </summary>
public class ArrowduelNetworkManager : MonoBehaviour
{
    public static ArrowduelNetworkManager Instance { get; private set; }

    // OpCodes for match state messages (replaces RPC system)
    public const long OPCODE_GAME_START = 1;
    public const long OPCODE_GAME_STATE = 2;
    public const long OPCODE_LEVEL_CHANGE = 3;
    public const long OPCODE_THEME_CHANGE = 4;
    public const long OPCODE_WIND = 5;
    public const long OPCODE_POWERUP = 6;
    public const long OPCODE_HIT_TARGET = 7;
    public const long OPCODE_ARROW_SPAWN = 8;
    public const long OPCODE_ARROW_DESPAWN = 9;
    public const long OPCODE_GAME_COMPLETED = 10;
    public const long OPCODE_WIND_STOP = 11;
    public const long OPCODE_POSITION_ROTATION = 12; // Player position and rotation sync
    public const long OPCODE_INPUT = 13; // Player input sync
    public const long OPCODE_PLAYER_READY = 14; // Player ready signal for synchronized spawning
    public const long OPCODE_ROTATION_STOP = 15; // Stop bow rotation (when charging starts)
    public const long OPCODE_ROTATION_START = 16; // Start bow rotation (when arrow spawns)

    [Header("Game State")] public GameState gameState;
    public int currentLevelIndex;
    public int lastLevelIndex;
    public int currentThemeIndex;
    public int lastThemeIndex;

    [Header("PowerUp")] public int powerUpSpawnPointIndex;
    public int powerUpDataIndex;

    [Header("Wind")] public bool isWindActive;
    public bool isWindDirectionRight;
    public float windEndTime;
    public Vector2 windForce;
    public Vector2 windDirection;

    // Host authority (replaces State Authority from Photon Fusion)
    public bool HasStateAuthorityGameData =>
        ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.IsHost;

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

    private void Start()
    {
        //Debug.Log($"[ArrowduelNetworkManager] Start() called - Instance: {(Instance != null ? "EXISTS" : "NULL")}");

        // Subscribe to match state events
        if (ArrowduelNakamaClient.Instance != null)
        {
            ArrowduelNakamaClient.Instance.MatchStateReceived += OnMatchStateReceived;
            //Debug.Log("[ArrowduelNetworkManager] Subscribed to MatchStateReceived event");
        }
        else
        {
            Debug.LogError(
                "[ArrowduelNetworkManager] ArrowduelNakamaClient.Instance is NULL - cannot subscribe to events!");
        }
    }

    private void OnDestroy()
    {
        if (ArrowduelNakamaClient.Instance != null)
        {
            ArrowduelNakamaClient.Instance.MatchStateReceived -= OnMatchStateReceived;
        }
    }

    /// <summary>
    /// Handles incoming match state messages (replaces RPC handlers).
    /// </summary>
    private void OnMatchStateReceived(IMatchState matchState)
    {
        string json = System.Text.Encoding.UTF8.GetString(matchState.State);

        switch (matchState.OpCode)
        {
            case OPCODE_GAME_START:
                HandleGameStart();
                break;
            case OPCODE_GAME_STATE:
                HandleGameState(json);
                break;
            case OPCODE_LEVEL_CHANGE:
                HandleLevelChange(json);
                break;
            case OPCODE_THEME_CHANGE:
                HandleThemeChange(json);
                break;
            case OPCODE_WIND:
                HandleWind(json);
                break;
            case OPCODE_WIND_STOP:
                HandleWindStop();
                break;
            case OPCODE_POWERUP:
                HandlePowerUp(json);
                break;
            case OPCODE_HIT_TARGET:
                HandleHitTarget(json);
                break;
            case OPCODE_ARROW_SPAWN:
                Debug.Log($"[ArrowduelNetworkManager------->>>>>>>> ] Received match state: OpCode={OPCODE_ARROW_SPAWN}, from SessionId: {matchState.UserPresence?.SessionId}");
                HandleArrowSpawn(json);
                break;
            case OPCODE_ARROW_DESPAWN:
                HandleArrowDespawn(json);
                break;
            case OPCODE_GAME_COMPLETED:
                HandleGameCompleted(json);
                break;
            // ... existing code ...
            case OPCODE_ROTATION_STOP:
                Debug.Log(
                    $"[ArrowduelNetworkManager] 🔴 Received match state: OpCode={OPCODE_ROTATION_STOP}, from PlayerID: {json}, SessionId: {matchState.UserPresence?.SessionId}");
                HandleRotationStop(json, matchState);
                break;
            case OPCODE_ROTATION_START:
                Debug.Log(
                    $"[ArrowduelNetworkManager] 🟢 Received match state: OpCode={OPCODE_ROTATION_START}, from PlayerID: {json}, SessionId: {matchState.UserPresence?.SessionId}");
                HandleRotationStart(json, matchState);
                break;

        }
    }

    // ========== RPC Methods (Send Match State) ==========

    /// <summary>
    /// Sends game start signal (replaces GameStart_RPC).
    /// </summary>
    public void GameStart_RPC()
    {
        if (HasStateAuthorityGameData)
        {
            SendMatchState(OPCODE_GAME_START, "");
        }

        // Apply locally on all clients
        if (GameManager.instance != null)
        {
            GameManager.instance.OnGameStart();
        }
    }

    /// <summary>
    /// Sends level change signal (replaces OnChangeLevel_RPC).
    /// </summary>
    public void OnChangeLevel_RPC()
    {
        if (HasStateAuthorityGameData && LevelManager.instance != null)
        {
            lastLevelIndex = currentLevelIndex;
            currentLevelIndex = UnityEngine.Random.Range(0, LevelManager.instance.availableLevelIndices.Count);
            lastThemeIndex = currentThemeIndex;
            currentThemeIndex = UnityEngine.Random.Range(0, LevelManager.instance.availableThemeIndices.Count);

            var data = new LevelChangeData
            {
                currentLevelIndex = currentLevelIndex,
                lastLevelIndex = lastLevelIndex,
                currentThemeIndex = currentThemeIndex,
                lastThemeIndex = lastThemeIndex
            };

            SendMatchState(OPCODE_LEVEL_CHANGE, JsonUtility.ToJson(data));
        }

        // Apply locally
        if (LevelManager.instance != null)
        {
            LevelManager.instance.currentLevelIndex = currentLevelIndex;
            LevelManager.instance.lastLevelIndex = lastLevelIndex;
            LevelManager.instance.currentThemeIndex = currentThemeIndex;
            LevelManager.instance.lastThemeIndex = lastThemeIndex;
            LevelManager.instance.LevelInit();
        }
    }

    /// <summary>
    /// Sends theme change signal (replaces ChangeTheme_RPC).
    /// </summary>
    public void ChangeTheme_RPC()
    {
        if (HasStateAuthorityGameData && LevelManager.instance != null)
        {
            lastLevelIndex = currentLevelIndex;
            currentLevelIndex = UnityEngine.Random.Range(0, LevelManager.instance.availableLevelIndices.Count);
            lastThemeIndex = currentThemeIndex;
            currentThemeIndex = UnityEngine.Random.Range(0, LevelManager.instance.availableThemeIndices.Count);

            var data = new ThemeChangeData
            {
                currentLevelIndex = currentLevelIndex,
                lastLevelIndex = lastLevelIndex,
                currentThemeIndex = currentThemeIndex,
                lastThemeIndex = lastThemeIndex
            };

            SendMatchState(OPCODE_THEME_CHANGE, JsonUtility.ToJson(data));
        }

        GameManager.onGameLevelChange?.Invoke();
    }

    /// <summary>
    /// Sends wind activation signal (replaces Wind_RPC).
    /// </summary>
    public void Wind_RPC()
    {
        if (HasStateAuthorityGameData && WindManager.instance != null)
        {
            isWindActive = true;
            isWindDirectionRight = UnityEngine.Random.value > 0.5f;
            windDirection = isWindDirectionRight ? Vector2.right : Vector2.left;
            windForce = windDirection * WindManager.instance.maxWindStrength;

            var data = new WindData
            {
                isWindActive = isWindActive,
                isWindDirectionRight = isWindDirectionRight,
                windForce = windForce,
                windDirection = windDirection
            };

            SendMatchState(OPCODE_WIND, JsonUtility.ToJson(data));
        }

        // Apply locally
        if (WindManager.instance != null)
        {
            WindManager.instance.isWindDirectionRight = isWindDirectionRight;
            WindManager.instance.windDirection = windDirection;
            WindManager.instance.windForce = windForce;
            WindManager.instance.ChangeWind();
        }
    }

    /// <summary>
    /// Sends wind stop signal.
    /// </summary>
    public void WindStop_RPC()
    {
        if (HasStateAuthorityGameData)
        {
            isWindActive = false;
            windForce = Vector2.zero;
            SendMatchState(OPCODE_WIND_STOP, "");
        }

        // Apply locally
        if (WindManager.instance != null)
        {
            WindManager.instance.StopWind();
        }
    }

    /// <summary>
    /// Sends power-up spawn signal (replaces PowerUp_RPC).
    /// </summary>
    public void PowerUp_RPC()
    {
        if (HasStateAuthorityGameData && PowerUpManager.instance != null)
        {
            powerUpSpawnPointIndex = UnityEngine.Random.Range(0, PowerUpManager.instance.powerUpDatas.Count);
            powerUpDataIndex = UnityEngine.Random.Range(0, PowerUpManager.instance.powerUpSpawnPoints.Length);

            Debug.Log($"[ArrowduelNetworkManager] Spawning power-up at index: {powerUpSpawnPointIndex}, {powerUpDataIndex}");
            var data = new PowerUpData
            {
                powerUpSpawnPointIndex = powerUpSpawnPointIndex,
                powerUpDataIndex = powerUpDataIndex
            };

            SendMatchState(OPCODE_POWERUP, JsonUtility.ToJson(data));
            Debug.Log($"[ArrowduelNetworkManager] Sent power-up spawn to network - index: {powerUpSpawnPointIndex}, {powerUpDataIndex}");
        }

        // Apply locally
        if (PowerUpManager.instance != null)
        {
            PowerUpManager.instance.powerUpSpawnPointIndex = powerUpSpawnPointIndex;
            PowerUpManager.instance.powerUpDataIndex = powerUpDataIndex;
            StartCoroutine(PowerUpManager.instance.InitSpawnPowerUp());
        }
    }

    /// <summary>
    /// Sends hit target signal (replaces OnHitTarget_RPC).
    /// </summary>
    public void OnHitTarget_RPC(bool isPlayerArrow)
    {
        var data = new HitTargetData { isPlayerArrow = isPlayerArrow };
        SendMatchState(OPCODE_HIT_TARGET, JsonUtility.ToJson(data));
    }

    /// <summary>
    /// Sends game completed signal.
    /// </summary>
    public void GameCompleted_RPC(int playerIndex)
    {
        if (HasStateAuthorityGameData)
        {
            var data = new GameCompletedData { playerIndex = playerIndex };
            SendMatchState(OPCODE_GAME_COMPLETED, JsonUtility.ToJson(data));
        }
    }

    // ========== Handlers (Receive Match State) ==========

    private void HandleGameStart()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnGameStart();
        }
    }

    private void HandleGameState(string json)
    {
        var data = JsonUtility.FromJson<GameStateData>(json);
        gameState = data.gameState;
        if (GameManager.instance != null)
        {
            GameManager.instance.gameState = gameState;
        }
    }

    private void HandleLevelChange(string json)
    {
        var data = JsonUtility.FromJson<LevelChangeData>(json);
        currentLevelIndex = data.currentLevelIndex;
        lastLevelIndex = data.lastLevelIndex;
        currentThemeIndex = data.currentThemeIndex;
        lastThemeIndex = data.lastThemeIndex;

        if (LevelManager.instance != null)
        {
            LevelManager.instance.currentLevelIndex = currentLevelIndex;
            LevelManager.instance.lastLevelIndex = lastLevelIndex;
            LevelManager.instance.currentThemeIndex = currentThemeIndex;
            LevelManager.instance.lastThemeIndex = lastThemeIndex;
            LevelManager.instance.LevelInit();
        }
    }

    private void HandleThemeChange(string json)
    {
        var data = JsonUtility.FromJson<ThemeChangeData>(json);
        currentLevelIndex = data.currentLevelIndex;
        lastLevelIndex = data.lastLevelIndex;
        currentThemeIndex = data.currentThemeIndex;
        lastThemeIndex = data.lastThemeIndex;

        GameManager.onGameLevelChange?.Invoke();
    }

    private void HandleWind(string json)
    {
        var data = JsonUtility.FromJson<WindData>(json);
        isWindActive = data.isWindActive;
        isWindDirectionRight = data.isWindDirectionRight;
        windDirection = data.windDirection;
        windForce = data.windForce;

        if (WindManager.instance != null)
        {
            WindManager.instance.isWindDirectionRight = isWindDirectionRight;
            WindManager.instance.windDirection = windDirection;
            WindManager.instance.windForce = windForce;
            WindManager.instance.ChangeWind();
        }
    }

    private void HandleWindStop()
    {
        isWindActive = false;
        windForce = Vector2.zero;

        if (WindManager.instance != null)
        {
            WindManager.instance.StopWind();
        }
    }

    private void HandlePowerUp(string json)
    {
        var data = JsonUtility.FromJson<PowerUpData>(json);
        powerUpSpawnPointIndex = data.powerUpSpawnPointIndex;
        powerUpDataIndex = data.powerUpDataIndex;

        if (PowerUpManager.instance != null)
        {
            PowerUpManager.instance.powerUpSpawnPointIndex = powerUpSpawnPointIndex;
            PowerUpManager.instance.powerUpDataIndex = powerUpDataIndex;
            Debug.Log($"[ArrowduelNetworkManager] Received power-up spawn from network - index: {powerUpSpawnPointIndex}, {powerUpDataIndex}");
            StartCoroutine(PowerUpManager.instance.InitSpawnPowerUp());
        }
    }

    private void HandleHitTarget(string json)
    {
        Debug.Log($"[ArrowduelNetworkManager] HandleHitTarget: HitTargetData - isPlayerArrow: {json}");
        var data = JsonUtility.FromJson<HitTargetData>(json);
        Debug.Log($"[ArrowduelNetworkManager] HandleHitTarget: HitTargetData - isPlayerArrow: {data.isPlayerArrow}");
        GameManager.onHitTarget?.Invoke(data.isPlayerArrow);
    }

    private void HandleArrowSpawn(string json)
    {
        Debug.Log($"[ArrowduelNetworkManager] ⚡ HandleArrowSpawn called with json: {json}");
        var data = JsonUtility.FromJson<ArrowSpawnData>(json);
        Debug.Log($"[ArrowduelNetworkManager] ⚡ Parsed arrow spawn data - playerID: {data.playerID}, force: {data.shootForce}, isBomb: {data.isBomb}, position: ({data.positionX}, {data.positionY}, {data.positionZ}), rotationZ: {data.rotationZ}");

        // CRITICAL: Check if this is from the local player - if so, skip (arrow already spawned locally)
        if (ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.CurrentMatch != null)
        {
            bool isLocalPlayer = false;
            bool isHost = ArrowduelNakamaClient.Instance.IsHost;

            if (isHost && data.playerID == 0)
            {
                // Host is Player 1 (playerID 0)
                isLocalPlayer = true;
            }
            else if (!isHost && data.playerID == 1)
            {
                // Non-host is Player 2 (playerID 1)
                isLocalPlayer = true;
            }

            Debug.Log($"[ArrowduelNetworkManager] ⚡ Local player check - IsHost: {isHost}, data.playerID: {data.playerID}, isLocalPlayer: {isLocalPlayer}");

            if (isLocalPlayer)
            {
                Debug.Log($"[ArrowduelNetworkManager] ⏭️ Skipping arrow spawn - this is from local player (playerID: {data.playerID}), arrow already spawned locally");
                return;
            }
        }
        else
        {
            Debug.LogWarning($"[ArrowduelNetworkManager] ⚠️ Cannot check local player - ArrowduelNakamaClient.Instance or CurrentMatch is null");
        }

        // Find the appropriate player controller based on playerID
        BowController bowController = null;
        if (data.playerID == 0)
        {
            bowController = GameManager.instance?.playerController;
            Debug.Log($"[ArrowduelNetworkManager] 🔍 Looking for playerController (playerID 0): {(bowController != null ? "✓ FOUND" : "✗ NOT FOUND")}");
        }
        else if (data.playerID == 1)
        {
            bowController = GameManager.instance?.opponentPlayerController;
            Debug.Log($"[ArrowduelNetworkManager] 🔍 Looking for opponentPlayerController (playerID 1): {(bowController != null ? "✓ FOUND" : "✗ NOT FOUND")}");
        }

        if (bowController == null)
        {
            Debug.LogError($"[ArrowduelNetworkManager] ❌ Cannot spawn arrow - BowController not found for playerID: {data.playerID}");
            return;
        }

        // Determine arrow prefab - verify correct prefab is being used
        GameObject arrowPrefab = data.isBomb ? bowController.bombArrowPrefab : bowController.arrowPrefab;
        if (arrowPrefab == null)
        {
            Debug.LogError($"[ArrowduelNetworkManager] Arrow prefab is null for playerID: {data.playerID}, isBomb: {data.isBomb}");
            return;
        }

        Debug.Log($"[ArrowduelNetworkManager] 🎯 Using arrow prefab: {arrowPrefab.name} for playerID: {data.playerID}, isBomb: {data.isBomb}");

        // Spawn arrow at the specified position and rotation
        Vector3 spawnPosition = new Vector3(data.positionX, data.positionY, data.positionZ);
        Quaternion spawnRotation = Quaternion.Euler(0, 0, data.rotationZ);

        Debug.Log($"[ArrowduelNetworkManager] 🏹 Spawning arrow at position: {spawnPosition}, rotation: {spawnRotation.eulerAngles.z}°, playerID: {data.playerID}");

        GameObject arrowObj = Instantiate(arrowPrefab, spawnPosition, spawnRotation);
        Rigidbody2D arrowRb = arrowObj.GetComponent<Rigidbody2D>();
        // Ensure GameObject is enabled
        arrowObj.SetActive(true);


        if (arrowRb != null)
        {
            // Use the rotationZ from network data to calculate direction
            // The rotationZ sent from the local player is the exact rotation of arrowSpawnPoint at spawn time
            // The arrowSpawnPoint is already correctly positioned and rotated for each player's bow,
            // so we don't need to negate for playerID 1
            Quaternion rotation = Quaternion.Euler(0, 0, data.rotationZ);
            Vector2 shootDirection = rotation * Vector2.right;
            // Negate direction for playerID 1 to match BowController behavior
            if (data.playerID == 1)
            {
                shootDirection = -shootDirection;
            }

            // Normalize rotationZ to 0-360 range for debugging
            float normalizedRotationZ = data.rotationZ;
            while (normalizedRotationZ < 0) normalizedRotationZ += 360;
            while (normalizedRotationZ >= 360) normalizedRotationZ -= 360;

            Debug.Log($"[ArrowduelNetworkManager] 🎯 Direction calculation - playerID: {data.playerID}, rotationZ: {data.rotationZ}° (normalized: {normalizedRotationZ}°), shootDirection: {shootDirection}");

            arrowRb.linearVelocity = shootDirection * data.shootForce;
            Debug.Log($"[ArrowduelNetworkManager] ✓ Set arrow velocity: {arrowRb.linearVelocity}, direction: {shootDirection}, force: {data.shootForce}, playerID: {data.playerID}, rotationZ: {data.rotationZ}");
        }
        else
        {
            Debug.LogError($"[ArrowduelNetworkManager] Arrow Rigidbody2D not found on spawned arrow!");
        }

        Arrow arrow = arrowObj.GetComponent<Arrow>();
        if (arrow != null)
        {
            arrow.Initialize(bowController, bowController.playerPowerUp, bowController.playerTag);

            // CRITICAL: Enable arrow sprite and trail immediately
            // Arrow.Start() will also call WairForEnable with 0.2s delay, but we enable it immediately for visibility
            StartCoroutine(EnableArrowVisuals(arrow));

            // Also enable immediately as backup (Arrow.Start() will handle it too, but this ensures visibility)
            if (arrow.arrowSprite != null)
            {
                arrow.arrowSprite.enabled = true;
            }
            if (arrow.trail != null)
            {
                var trailRenderer = arrow.trail.GetComponent<TrailRenderer>();
                if (trailRenderer != null)
                {
                    trailRenderer.enabled = true;
                }
            }

            Debug.Log($"[ArrowduelNetworkManager] ✅ Arrow initialized and enabled for playerID: {data.playerID}");
        }
        else
        {
            Debug.LogError($"[ArrowduelNetworkManager] ❌ Arrow component not found on spawned arrow!");
        }

        Debug.Log($"[ArrowduelNetworkManager] ✅ Successfully spawned remote arrow for playerID: {data.playerID}, force: {data.shootForce}, position: {spawnPosition}");
    }
    private IEnumerator EnableArrowVisuals(Arrow arrow)
    {
        yield return new WaitForEndOfFrame();
        if (arrow != null && arrow.arrowSprite != null)
        {
            arrow.arrowSprite.enabled = true;
            if (arrow.trail != null)
            {
                var trailRenderer = arrow.trail.GetComponent<TrailRenderer>();
                if (trailRenderer != null)
                {
                    trailRenderer.enabled = true;
                }
            }
        }
    }

    private void HandleArrowDespawn(string json)
    {
        // Arrow despawning is handled locally
    }

    private void HandleGameCompleted(string json)
    {
        var data = JsonUtility.FromJson<GameCompletedData>(json);
        if (GameManager.instance != null)
        {
            // Handle game completion logic
        }
    }

    private void HandleRotationStop(string json, IMatchState matchState = null)
    {
        Debug.Log($"[ArrowduelNetworkManager] 🔴 HandleRotationStop called with json: {json}");

        var data = JsonUtility.FromJson<NewRotationControlData>(json);
        Debug.Log($"[ArrowduelNetworkManager] 🔴 Rotation stop received for playerID: {data.playerID}, SessionId: {matchState?.UserPresence?.SessionId}");

        // CRITICAL: Check if this is from the local player - if so, skip (already handled locally)
        if (ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.CurrentMatch != null && matchState != null)
        {
            // Use UserId comparison - 100% reliable (doesn't depend on IsHost or playerID mapping)
            string incomingUserId = matchState.UserPresence?.UserId;
            string localUserId = ArrowduelNakamaClient.Instance.Session?.UserId;

            bool isLocalPlayer = !string.IsNullOrEmpty(incomingUserId) &&
                                !string.IsNullOrEmpty(localUserId) &&
                                incomingUserId == localUserId;

            Debug.Log($"[ArrowduelNetworkManager] 🔴 Local player check - IncomingUserId: {incomingUserId}, LocalUserId: {localUserId}, isLocalPlayer: {isLocalPlayer}, data.playerID: {data.playerID}, rotatedAngle: {data.rotatedAngle}");

            if (isLocalPlayer)
            {
                Debug.Log($"[ArrowduelNetworkManager] ⏭️ Skipping rotation stop - this is from local player (playerID: {data.playerID}, UserId: {incomingUserId}), already handled locally");
                return;
            }
        }
        else if (matchState == null)
        {
            Debug.LogWarning($"[ArrowduelNetworkManager] ⚠️ Cannot check local player - matchState is null");
        }
        else
        {
            Debug.LogWarning($"[ArrowduelNetworkManager] ⚠️ Cannot check local player - ArrowduelNakamaClient.Instance or CurrentMatch is null");
        }
        // Find the appropriate PlayerNetworkRemoteSync based on playerID
        PlayerNetworkRemoteSync remoteSync = null;

        // Try to find via GameManager references first
        if (data.playerID == 0)
        {
            var playerController = GameManager.instance?.playerController;
            Debug.Log($"[ArrowduelNetworkManager] Looking for playerController (playerID 0): {(playerController != null ? "FOUND" : "NOT FOUND")}");
            if (playerController != null)
            {
                remoteSync = playerController.GetComponent<PlayerNetworkRemoteSync>();
                Debug.Log($"[ArrowduelNetworkManager] PlayerNetworkRemoteSync on playerController: {(remoteSync != null ? "FOUND" : "NOT FOUND")}");
                if (remoteSync != null)
                {
                    playerController.isCharging = true;
                 }
            }
        }
        else if (data.playerID == 1)
        {
            var opponentController = GameManager.instance?.opponentPlayerController;
            Debug.Log($"[ArrowduelNetworkManager] Looking for opponentController (playerID 1): {(opponentController != null ? "FOUND" : "NOT FOUND")}");
            if (opponentController != null)
            {
                remoteSync = opponentController.GetComponent<PlayerNetworkRemoteSync>();
                Debug.Log($"[ArrowduelNetworkManager] PlayerNetworkRemoteSync on opponentController: {(remoteSync != null ? "FOUND" : "NOT FOUND")}");
                if (remoteSync != null)
                {
                    opponentController.isCharging = true;
                }
            }
        }

        // Fallback: Search all PlayerNetworkRemoteSync components
        if (remoteSync == null)
        {
            Debug.Log($"[ArrowduelNetworkManager] Fallback: Searching all PlayerNetworkRemoteSync components...");
            var allRemoteSyncs = FindObjectsByType<PlayerNetworkRemoteSync>(FindObjectsSortMode.None);
            Debug.Log($"[ArrowduelNetworkManager] Found {allRemoteSyncs.Length} PlayerNetworkRemoteSync components");

            foreach (var sync in allRemoteSyncs)
            {
                var bowController = sync.GetComponent<BowController>();
                if (bowController != null)
                {
                    Debug.Log($"[ArrowduelNetworkManager] Checking PlayerNetworkRemoteSync on GameObject: {sync.gameObject.name}, has playerID: {bowController.playerID}, looking for: {data.playerID}");
                    if (bowController.playerID == data.playerID)
                    {
                        remoteSync = sync;
                        Debug.Log($"[ArrowduelNetworkManager] ✓ Found PlayerNetworkRemoteSync via fallback search for playerID: {data.playerID} on GameObject: {sync.gameObject.name}");
                        break;
                    }
                }
                else
                {
                    Debug.LogWarning($"[ArrowduelNetworkManager] PlayerNetworkRemoteSync found but no BowController on GameObject: {sync.gameObject.name}");
                }
            }
        }

        if (remoteSync != null)
        {
            byte[] stateBytes = Encoding.UTF8.GetBytes(json);
            remoteSync.HandleRotationStop(stateBytes);
            Debug.Log($"[ArrowduelNetworkManager] ✅ Successfully forwarded rotation stop to PlayerNetworkRemoteSync for playerID: {data.playerID}");
        }
        else
        {
            Debug.LogError($"[ArrowduelNetworkManager] ❌ PlayerNetworkRemoteSync not found for playerID: {data.playerID}");
            //Debug.LogError($"[ArrowduelNetworkManager] GameManager.instance: {(GameManager.instance != null ? "EXISTS" : "NULL")}");
            //Debug.LogError($"[ArrowduelNetworkManager] playerController: {(GameManager.instance?.playerController != null ? "EXISTS" : "NULL")}");
           //Debug.LogError($"[ArrowduelNetworkManager] opponentPlayerController: {(GameManager.instance?.opponentPlayerController != null ? "EXISTS" : "NULL")}");
        }
    }

    private void HandleRotationStart(string json, IMatchState matchState = null)
    {
        Debug.Log($"[ArrowduelNetworkManager] 🟢 HandleRotationStart called with json: {json}");

        var data = JsonUtility.FromJson<NewRotationControlData>(json);
        Debug.Log($"[ArrowduelNetworkManager] 🟢 Rotation START received for playerID: {data.playerID}, SessionId: {matchState?.UserPresence?.SessionId}, rotatedAngle: {data.rotatedAngle}");

        // CRITICAL: Check if this is from the local player - if so, skip (already handled locally)
        if (ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.CurrentMatch != null && matchState != null)
        {
            // Use UserId comparison - 100% reliable (doesn't depend on IsHost or playerID mapping)
            string incomingUserId = matchState.UserPresence?.UserId;
            string localUserId = ArrowduelNakamaClient.Instance.Session?.UserId;

            bool isLocalPlayer = !string.IsNullOrEmpty(incomingUserId) &&
                                !string.IsNullOrEmpty(localUserId) &&
                                incomingUserId == localUserId;

            Debug.Log($"[ArrowduelNetworkManager] 🟢 Local player check - IncomingUserId: {incomingUserId}, LocalUserId: {localUserId}, isLocalPlayer: {isLocalPlayer}, data.playerID: {data.playerID}");

            if (isLocalPlayer)
            {
                Debug.Log($"[ArrowduelNetworkManager] ⏭️ Skipping rotation start - this is from local player (playerID: {data.playerID}, UserId: {incomingUserId}), already handled locally");
                return;
            }
        }
        else if (matchState == null)
        {
            Debug.LogWarning($"[ArrowduelNetworkManager] ⚠️ Cannot check local player - matchState is null");
        }
        else
        {
            Debug.LogWarning($"[ArrowduelNetworkManager] ⚠️ Cannot check local player - ArrowduelNakamaClient.Instance or CurrentMatch is null");
        }
        // Find the appropriate PlayerNetworkRemoteSync based on playerID
        PlayerNetworkRemoteSync remoteSync = null;

        // Try to find via GameManager references first
        if (data.playerID == 0)
        {
            var playerController = GameManager.instance?.playerController;
            Debug.Log($"[ArrowduelNetworkManager] Looking for playerController (playerID 0): {(playerController != null ? "FOUND" : "NOT FOUND")}");
            if (playerController != null)
            {
                remoteSync = playerController.GetComponent<PlayerNetworkRemoteSync>();
                Debug.Log($"[ArrowduelNetworkManager] PlayerNetworkRemoteSync on playerController: {(remoteSync != null ? "FOUND" : "NOT FOUND")}");
                if (remoteSync != null)
                {
                    playerController.isCharging = false;
                }
            }
        }
        else if (data.playerID == 1)
        {
            var opponentController = GameManager.instance?.opponentPlayerController;
            Debug.Log($"[ArrowduelNetworkManager] Looking for opponentController (playerID 1): {(opponentController != null ? "FOUND" : "NOT FOUND")}");
            if (opponentController != null)
            {
                remoteSync = opponentController.GetComponent<PlayerNetworkRemoteSync>();
                Debug.Log($"[ArrowduelNetworkManager] PlayerNetworkRemoteSync on opponentController: {(remoteSync != null ? "FOUND" : "NOT FOUND")}");
                if (remoteSync != null)
                {
                    opponentController.isCharging = false;
                }
            }
        }

        // Fallback: Search all PlayerNetworkRemoteSync components
        if (remoteSync == null)
        {
            Debug.Log($"[ArrowduelNetworkManager] Fallback: Searching all PlayerNetworkRemoteSync components...");
            var allRemoteSyncs = FindObjectsByType<PlayerNetworkRemoteSync>(FindObjectsSortMode.None);
            Debug.Log($"[ArrowduelNetworkManager] Found {allRemoteSyncs.Length} PlayerNetworkRemoteSync components");

            foreach (var sync in allRemoteSyncs)
            {
                var bowController = sync.GetComponent<BowController>();
                if (bowController != null)
                {
                    Debug.Log($"[ArrowduelNetworkManager] Checking PlayerNetworkRemoteSync on GameObject: {sync.gameObject.name}, has playerID: {bowController.playerID}, looking for: {data.playerID}");
                    if (bowController.playerID == data.playerID)
                    {
                        remoteSync = sync;
                        Debug.Log($"[ArrowduelNetworkManager] ✓ Found PlayerNetworkRemoteSync via fallback search for playerID: {data.playerID} on GameObject: {sync.gameObject.name}");
                        break;
                    }
                }
                else
                {
                    Debug.LogWarning($"[ArrowduelNetworkManager] PlayerNetworkRemoteSync found but no BowController on GameObject: {sync.gameObject.name}");
                }
            }
        }

        if (remoteSync != null)
        {
            byte[] stateBytes = Encoding.UTF8.GetBytes(json);
            remoteSync.HandleRotationStart(stateBytes);
            Debug.Log($"[ArrowduelNetworkManager] ✅ Successfully forwarded rotation stop to PlayerNetworkRemoteSync for playerID: {data.playerID}");
        }
        else
        {
            Debug.LogError($"[ArrowduelNetworkManager] ❌ PlayerNetworkRemoteSync not found for playerID: {data.playerID}");
            Debug.LogError($"[ArrowduelNetworkManager] GameManager.instance: {(GameManager.instance != null ? "EXISTS" : "NULL")}");
            Debug.LogError($"[ArrowduelNetworkManager] playerController: {(GameManager.instance?.playerController != null ? "EXISTS" : "NULL")}");
            Debug.LogError($"[ArrowduelNetworkManager] opponentPlayerController: {(GameManager.instance?.opponentPlayerController != null ? "EXISTS" : "NULL")}");
        }
    }

    // ========== Helper Methods ==========

    public void SendMatchState(long opCode, string json)
    {
        if (ArrowduelNakamaClient.Instance == null)
        {
            Debug.LogWarning($"[ArrowduelNetworkManager] ⚠️ Cannot send match state - ArrowduelNakamaClient.Instance is NULL, OpCode: {opCode}");
            return;
        }

        if (!ArrowduelNakamaClient.Instance.HasActiveMatch)
        {
            Debug.LogWarning($"[ArrowduelNetworkManager] ⚠️ Cannot send match state - No active match, OpCode: {opCode}");
            return;
        }

        if (!ArrowduelNakamaClient.Instance.IsSocketConnected)
        {
            Debug.LogWarning($"[ArrowduelNetworkManager] ⚠️ Cannot send match state - Socket not connected, OpCode: {opCode}");
            return;
        }

        ArrowduelNakamaClient.Instance.SendMatchStateAsync(opCode, json);
        Debug.Log($"[ArrowduelNetworkManager] ✅ Sent match state - OpCode: {opCode}, json: {json}");
    }

    // ========== Data Classes ==========

    [Serializable]
    public class LevelChangeData
    {
        public int currentLevelIndex;
        public int lastLevelIndex;
        public int currentThemeIndex;
        public int lastThemeIndex;
    }

    [Serializable]
    public class ThemeChangeData
    {
        public int currentLevelIndex;
        public int lastLevelIndex;
        public int currentThemeIndex;
        public int lastThemeIndex;
    }

    [Serializable]
    public class WindData
    {
        public bool isWindActive;
        public bool isWindDirectionRight;
        public Vector2 windForce;
        public Vector2 windDirection;
    }

    [Serializable]
    public class PowerUpData
    {
        public int powerUpSpawnPointIndex;
        public int powerUpDataIndex;
    }

    [Serializable]
    public class HitTargetData
    {
        public bool isPlayerArrow;
    }

    [Serializable]
    public class GameStateData
    {
        public GameState gameState;
    }

    [Serializable]
    public class GameCompletedData
    {
        public int playerIndex;
    }

    [Serializable]
    public class ArrowSpawnData
    {
        public int playerID;
        public float positionX;
        public float positionY;
        public float positionZ;
        public float rotationZ;
        public float shootForce;
        public bool isBomb;
        public float currentForce;
    }

    [Serializable]
    public class RotationControlData
    {
        public int playerID;
    }

    [Serializable]
    public class NewRotationControlData
    {
        public int playerID;
        public int rotatedAngle;
    }
}