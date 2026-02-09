using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nakama;

public class NakamaNetworkManager : MonoBehaviour
{
    public static NakamaNetworkManager Instance { get; private set; }

    // Match data opcodes
    public const long OPCODE_GAME_START = 1;
    public const long OPCODE_GAME_STATE = 2;
    public const long OPCODE_LEVEL_CHANGE = 3;
    public const long OPCODE_THEME_CHANGE = 4;
    public const long OPCODE_WIND = 5;
    public const long OPCODE_POWERUP = 6;
    public const long OPCODE_HIT_TARGET = 7;
    public const long OPCODE_ARROW_SPAWN = 8;
    public const long OPCODE_ARROW_DESPAWN = 9;

    [Header("Game State")]
    public GameState gameState;
    public int currentLevelIndex;
    public int lastLevelIndex;
    public int currentThemeIndex;
    public int lastThemeIndex;

    [Header("PowerUp")]
    public int powerUpSpawnPointIndex;
    public int powerUpDataIndex;

    [Header("Wind")]
    public bool isWindActive;
    public bool isWindDirectionRight;
    public float windEndTime;
    public Vector2 windForce;
    public Vector2 windDirection;

    public bool HasStateAuthorityGameData { get; private set; }

    private NakamaClient nakamaClient;
    private bool isHost;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        nakamaClient = NakamaClient.Instance;

        // Determine host (first player in match is host)
        if (nakamaClient != null && nakamaClient.CurrentMatch != null)
        {
            var presences = nakamaClient.CurrentMatch.Presences;
            var sortedPresences = new List<IUserPresence>(presences);
            sortedPresences.Sort((a, b) => string.Compare(a.UserId, b.UserId));

            isHost = sortedPresences.Count > 0 &&
                     sortedPresences[0].UserId == nakamaClient.UserId;

            HasStateAuthorityGameData = isHost;
            //Debug.Log($"[NakamaNetworkManager] IsHost: {isHost}, HasStateAuthority: {HasStateAuthorityGameData}");
        }
    }

    public void OnMatchDataReceived(IMatchState matchState)
    {
        string json = System.Text.Encoding.UTF8.GetString(matchState.State);

        //Debug.Log($"[NakamaNetworkManager] Received match data: OpCode={matchState.OpCode}");

        switch (matchState.OpCode)
        {
            case OPCODE_GAME_START:
                HandleGameStart();
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
            case OPCODE_POWERUP:
                HandlePowerUp(json);
                break;
            case OPCODE_HIT_TARGET:
                HandleHitTarget(json);
                break;
            case OPCODE_ARROW_SPAWN:
                HandleArrowSpawn(json);
                break;
            case OPCODE_ARROW_DESPAWN:
                HandleArrowDespawn(json);
                break;
        }
    }

    public void GameStart_RPC()
    {
        if (HasStateAuthorityGameData)
        {
            nakamaClient.SendMatchData(OPCODE_GAME_START, new byte[0]);
        }
        if (GameManager.instance != null)
        {
            GameManager.instance.OnGameStart();
        }
    }

    public void OnChangeLevel_RPC()
    {
        if (HasStateAuthorityGameData && LevelManager.instance != null)
        {
            lastLevelIndex = currentLevelIndex;
            //currentLevelIndex = UnityEngine.Random.Range(0, LevelManager.instance.availableLevelIndices.Count);
            currentLevelIndex = 0;
            lastThemeIndex = currentThemeIndex;
            //currentThemeIndex = UnityEngine.Random.Range(0, LevelManager.instance.availableThemeIndices.Count);
            currentThemeIndex = 0;

            var data = new LevelChangeData
            {
                currentLevelIndex = currentLevelIndex,
                lastLevelIndex = lastLevelIndex,
                currentThemeIndex = currentThemeIndex,
                lastThemeIndex = lastThemeIndex
            };
            nakamaClient.SendMatchData(OPCODE_LEVEL_CHANGE, data);
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
            nakamaClient.SendMatchData(OPCODE_THEME_CHANGE, data);
        }

        GameManager.onGameLevelChange?.Invoke();
    }

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
            nakamaClient.SendMatchData(OPCODE_WIND, data);
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

    public void PowerUp_RPC()
    {
        if (HasStateAuthorityGameData && PowerUpManager.instance != null)
        {
            powerUpSpawnPointIndex = UnityEngine.Random.Range(0, PowerUpManager.instance.powerUpDatas.Count);
            powerUpDataIndex = UnityEngine.Random.Range(0, PowerUpManager.instance.powerUpSpawnPoints.Length);

            var data = new PowerUpData
            {
                powerUpSpawnPointIndex = powerUpSpawnPointIndex,
                powerUpDataIndex = powerUpDataIndex
            };
            nakamaClient.SendMatchData(OPCODE_POWERUP, data);
        }

        // Apply locally
        if (PowerUpManager.instance != null)
        {
            PowerUpManager.instance.powerUpSpawnPointIndex = powerUpSpawnPointIndex;
            PowerUpManager.instance.powerUpDataIndex = powerUpDataIndex;
            StartCoroutine(PowerUpManager.instance.InitSpawnPowerUp());
        }
    }

    public void OnHitTarget_RPC(bool isPlayerArrow)
    {
        var data = new HitTargetData { isPlayerArrow = isPlayerArrow };
        nakamaClient.SendMatchData(OPCODE_HIT_TARGET, data);
    }

    // Handlers
    private void HandleGameStart()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnGameStart();
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

    private void HandlePowerUp(string json)
    {
        var data = JsonUtility.FromJson<PowerUpData>(json);
        powerUpSpawnPointIndex = data.powerUpSpawnPointIndex;
        powerUpDataIndex = data.powerUpDataIndex;

        if (PowerUpManager.instance != null)
        {
            PowerUpManager.instance.powerUpSpawnPointIndex = powerUpSpawnPointIndex;
            PowerUpManager.instance.powerUpDataIndex = powerUpDataIndex;
            StartCoroutine(PowerUpManager.instance.InitSpawnPowerUp());
        }
    }

    private void HandleHitTarget(string json)
    {
        Debug.Log($"[NakamaNetworkManager] HandleHitTarget called with json: {json}");
        var data = JsonUtility.FromJson<HitTargetData>(json);
        GameManager.onHitTarget?.Invoke(data.isPlayerArrow);
    }

    private void HandleArrowSpawn(string json) { }
    private void HandleArrowDespawn(string json) { }

    // Data classes
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
}
