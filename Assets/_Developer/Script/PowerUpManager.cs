using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager instance;

    [Header("Power-Up Settings")]
    [SerializeField] private GameObject powerUpPrefab;
    public Transform[] powerUpSpawnPoints;
    public List<PowerUpData> powerUpDatas = new List<PowerUpData>();

    [Header("Power-Up Effects")]
    public float freezeDuration = 3f;

    private PowerUpType currentPlayerPower = PowerUpType.None;

    [Header("Power-Up Display Settings")]
    [SerializeField] private float initialPowerUpDelay = 5f; // Delay before first power-up appears
    //[SerializeField] private float minPowerUpDelay = 8f;
    [SerializeField] private float maxPowerUpDelay = 15f;
    [SerializeField] private float powerUpDisplayDuration = 13f; // Time power-up stays on screen

    public int powerUpSpawnPointIndex;
    public int powerUpDataIndex;

    private Coroutine powerUpSpawnCoroutine;
    private bool isPowerUpActive = false;
    private GameObject currentPowerUp;


    [System.Serializable]
    public class PowerUpData
    {
        public PowerUpType powerUpType;
        public Sprite powerUpSprite;
    }

    private void Awake()
    {
        instance = this;

    }

    void OnEnable()
    {
        GameManager.onGameStart += OnGameStart;
        GameManager.onGameLevelChange += OnGameLevelChange;
    }

    void OnDisable()
    {
        GameManager.onGameStart -= OnGameStart;
        GameManager.onGameLevelChange -= OnGameLevelChange;
    }

    private void OnGameStart()
    {
        // Only host starts the power-up spawn routine in multiplayer
        // Non-host will receive power-up spawns via network messages
        if (GameManager.gameMode == GameModeType.MULTIPLAYER)
        {
            if (ArrowduelNakamaClient.Instance != null && !ArrowduelNakamaClient.Instance.IsHost)
            {
                return; // Non-host doesn't start the routine, receives via network
            }
        }

        powerUpSpawnCoroutine = StartCoroutine(PowerUpSpawnRoutine());
    }

    private IEnumerator PowerUpSpawnRoutine()
    {
        yield return new WaitForSeconds(initialPowerUpDelay);

        while (GameManager.instance.gameState == GameState.Gameplay)
        {

            if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
                SpawnPowerUp();
            else
            {
                if (ArrowduelNetworkManager.Instance != null)
                {
                    ArrowduelNetworkManager.Instance.PowerUp_RPC();
                }
            }

            yield return new WaitForSeconds(powerUpDisplayDuration);

            if (currentPowerUp != null)
            {
                Destroy(currentPowerUp);
                currentPowerUp = null;
                isPowerUpActive = false;
                currentPlayerPower = PowerUpType.None;
                // //Debug.Log($"DESTROY POWERUP...");
            }

            // yield return new WaitForSeconds(UnityEngine.Random.Range(minPowerUpDelay, maxPowerUpDelay));
            yield return new WaitForSeconds(maxPowerUpDelay);

        }
    }

    public IEnumerator InitSpawnPowerUp()
    {
        // When called from ArrowduelNetworkManager, both clients should spawn the power-up
        // The network message ensures synchronization
        Debug.Log($"[PowerUpManager] InitSpawnPowerUp called - SpawnPointIndex: {powerUpSpawnPointIndex}, DataIndex: {powerUpDataIndex}");

        SpawnPowerUp();

        yield return new WaitForSeconds(powerUpDisplayDuration);

        if (currentPowerUp != null)
        {
            Destroy(currentPowerUp);
            currentPowerUp = null;
            isPowerUpActive = false;
            currentPlayerPower = PowerUpType.None;
            Debug.Log($"[PowerUpManager] DESTROY POWERUP after duration");
        }

        yield return new WaitForSeconds(maxPowerUpDelay);
    }

    public int dummyPowerIndex = 0;

    public void SpawnPowerUp()
    {
        if (powerUpDatas.Count == 0 || isPowerUpActive) return;

        if (GameManager.instance.gameState == GameState.WaitForLevelChange)
        {
            // //Debug.Log($"PowerUp not spwan, Level change...");
            return;
        }

        if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
            powerUpSpawnPointIndex = Random.Range(0, powerUpSpawnPoints.Length);

        // Random position at top of screen
        Vector3 spawnPos = powerUpSpawnPoints[powerUpSpawnPointIndex].position;

        currentPowerUp = Instantiate(powerUpPrefab, spawnPos, Quaternion.identity);

        PowerUp powerScript = currentPowerUp.GetComponent<PowerUp>();

        // Assign random power type
        //  int randomValue = Random.Range(0, powerUpDatas.Count);

        if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
            powerUpDataIndex = Random.Range(0, powerUpDatas.Count);

        // Test dummy power-up index :
        // powerUpDataIndex = 2;

        PowerUpType randomPower = powerUpDatas[powerUpDataIndex].powerUpType;
        Sprite powerSprite = powerUpDatas[powerUpDataIndex].powerUpSprite;

        powerScript.Initialize(randomPower, powerSprite);

        isPowerUpActive = true;
    }


    public void CollectPowerUp(PowerUpType powerType, BowController controller)
    {

        isPowerUpActive = false;
        currentPowerUp = null;
        currentPlayerPower = powerType;
        controller.playerPowerUp.CollectPowerUp(powerType);

        // //Debug.Log($"PCOLLECT POWER: {powerType}...");

    }

    private void OnGameLevelChange()
    {
        if (powerUpSpawnCoroutine != null)
            StopCoroutine(powerUpSpawnCoroutine);

        if (currentPowerUp != null)
        {
            Destroy(currentPowerUp);
            currentPowerUp = null;
            isPowerUpActive = false;
            currentPlayerPower = PowerUpType.None;
            // //Debug.Log($"FORCEEEE DESTROY POWERUP...");
        }

        OnGameStart();

    }

}
