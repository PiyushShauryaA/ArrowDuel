using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Nakama;

public class BowController : MonoBehaviour
{

    public int playerIndex = -1;
    public bool isCharging = false;

    public int playerID = -1;
    public string playerTag;

    public PlayerType playerType;

    public enum PlayerType
    {
        None,
        Player,
        Ai
    }

    public Transform bowParent;
    public Transform frontHand;
    public Transform backHand;

    private WindManager windManager;
    public PlayerPowerUp playerPowerUp;

    [Header("Rotation Settings")]
    public float autoRotationSpeed = 45f; // Degrees per second
    public float manualRotationSpeed = 10f;
    public float rotationOffset = 90f;
    public float maxUpAngle = 90f;
    public float maxDownAngle = -90f;

    public float currentAutoRotationAngle;

    [Header("Force Meter")]
    public GameObject fillbarParentObj;
    public Image fillbarImage;
    [SerializeField] private float fillSpeed = 50f; // Percent per second
    public float minForce = 5f;
    public float maxForce = 20f;

    [Header("Arrow")]
    public GameObject arrowPrefab;
    public GameObject[] arrowPrefabs;
    public GameObject bombArrowPrefab;
    [Space(05)]
    public GameObject aiArrowPrefab;
    public GameObject aiBombArrowPrefab;
    [Space(05)]
    public Transform arrowSpawnPoint;
    public GameObject heartBreakEffect;

    
    public float currentForce = 0f;
    public int fillDirection = 1; // 1 for increasing, -1 for decreasing

    [Header("Auto Rotation")]
    // [SerializeField] private bool autoRotate = true;
    [SerializeField] private float autoRotationDirection = 1f; // 1 for down, -1 for up

    private void Awake()
    {
        playerPowerUp = GetComponent<PlayerPowerUp>();

    }

    public void Start()
    {
        playerTag = gameObject.tag;
        windManager = FindAnyObjectByType<WindManager>();

        currentAutoRotationAngle = maxUpAngle;
        fillbarParentObj.SetActive(false);
        fillbarImage.fillAmount = 0f;
        GameManager.instance.loadingPanel.SetActive(false);

    }

    void OnEnable()
    {
        GameManager.onGameLevelChange += OnGameLevelChange;
    }

    void OnDisable()
    {
        GameManager.onGameLevelChange -= OnGameLevelChange;
    }

    private void OnGameLevelChange()
    {
        isCharging = false;
        currentForce = 0f;
        fillDirection = 1;
        fillbarParentObj.SetActive(false);
        fillbarImage.fillAmount = 0f;
    }

    private void FixedUpdate()
    {
        // Only update in multiplayer if we have authority (host)
        if (GameManager.gameMode == GameModeType.MULTIPLAYER)
        {
            bool hasAuthority = false;
            if (ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.CurrentMatch != null)
            {
                var presences = ArrowduelNakamaClient.Instance.CurrentMatch.Presences;
                var sortedPresences = presences.ToList();
                sortedPresences.Sort((a, b) => string.Compare(a.UserId, b.UserId));
                hasAuthority = sortedPresences.Count > 0 && sortedPresences[0].UserId == ArrowduelNakamaClient.Instance.Session?.UserId;
            }
            if (!hasAuthority) return;
        }
    }

    public int isGameCompleted { get; set; }

    internal void SetGameCompleted()
    {
        // In multiplayer, only host can set game completed
        if (GameManager.gameMode == GameModeType.MULTIPLAYER)
        {
            bool hasAuthority = false;
            if (ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.CurrentMatch != null)
            {
                var presences = ArrowduelNakamaClient.Instance.CurrentMatch.Presences;
                var sortedPresences = presences.ToList();
                sortedPresences.Sort((a, b) => string.Compare(a.UserId, b.UserId));
                hasAuthority = sortedPresences.Count > 0 && sortedPresences[0].UserId == ArrowduelNakamaClient.Instance.Session?.UserId;
            }
            if (!hasAuthority) return;
        }
        isGameCompleted = 1;
        GameManager.instance.CheckForGameCompletion();
    }

    public void StartCharging()
    {
        isCharging = true;
        currentForce = 0f;
        fillDirection = 1;
        fillbarParentObj.gameObject.SetActive(true);
        fillbarImage.fillAmount = 0f;
        
        // Send rotation stop event to network in multiplayer
        if (GameManager.gameMode == GameModeType.MULTIPLAYER && ArrowduelNetworkManager.Instance != null)
        {
            var rotationStopData = new ArrowduelNetworkManager.NewRotationControlData
            {
                playerID = playerID,
                rotatedAngle = (int)currentAutoRotationAngle
            };
            string json = JsonUtility.ToJson(rotationStopData);
            Debug.Log($"[BowController] 🔍 Before sending rotation STOP event - playerID: {playerID}, json: {json}");
            ArrowduelNetworkManager.Instance.SendMatchState(ArrowduelNetworkManager.OPCODE_ROTATION_STOP, json);
            Debug.Log($"[BowController] ✅ Sent rotation STOP event to network - playerID: {playerID}, json: {json}");
        }
    }

    public void UpdateForceMeter()
    {
        // Use deltaTime consistently (called from Update, not FixedUpdate)
        currentForce += fillDirection * fillSpeed * Time.deltaTime;

        if (currentForce >= 1f)
        {
            currentForce = 1f;
            fillDirection = -1;
        }
        else if (currentForce <= 0f)
        {
            currentForce = 0f;
            fillDirection = 1;
        }

        if (fillbarImage != null)
        {
            fillbarImage.fillAmount = currentForce;
        }

    }

    public void AutoRotate()
    {
        // Use deltaTime consistently (called from Update, not FixedUpdate)
        currentAutoRotationAngle += autoRotationDirection * autoRotationSpeed * Time.deltaTime;

        // Debug log every 60 frames (approximately once per second)
        if (Time.frameCount % 60 == 0)
        {
           // Debug.Log(
              //  $"[BowController] AutoRotate - playerID: {playerID},  isCharging: {isCharging}");
        }
        if (currentAutoRotationAngle >= maxUpAngle)
        {
            currentAutoRotationAngle = maxUpAngle;
            autoRotationDirection = -1f;
        }
        else if (currentAutoRotationAngle <= maxDownAngle)
        {
            currentAutoRotationAngle = maxDownAngle;
            autoRotationDirection = 1f;
        }

        // Apply rotation - check if bowParent is assigned
        if (bowParent != null)
        {
            float zRot = currentAutoRotationAngle + rotationOffset;
            bowParent.rotation = Quaternion.Euler(0f, 0f, zRot);
            if (frontHand != null && backHand != null)
            {
                frontHand.rotation = Quaternion.Euler(0f, 0f, zRot);
                backHand.rotation = Quaternion.Euler(0f, 0f, zRot - 10f);
            }
        }
        else
        {
            Debug.LogWarning($"[BowController] bowParent is null! Cannot rotate bow. GameObject: {gameObject.name}");
        }
    }


    public void ReleaseArrow()
    {
        // CRITICAL: Only spawn arrow if player is actually charging
        if (!isCharging)
        {
            Debug.LogWarning($"[BowController] ReleaseArrow called but player is not charging! playerID: {playerID}");
            return;
        }
        
        if (GameManager.instance.hasAutoPlay)
            currentForce = 0.5f;

        StartCoroutine(WaitShootDelay(false));

    }

    public void AiReleaseArrow(float _enemyShootAngle)
    {
        StartCoroutine(WaitShootDelay(true, _enemyShootAngle));

    }


    IEnumerator WaitShootDelay(bool isAiPlayer, float _enemyShootAngle = 0)
    {

        Debug.Log($"[BowController] WaitShootDelay called - isAiPlayer: {isAiPlayer}, _enemyShootAngle: {_enemyShootAngle},");
        int defaultShootArrowIndex = 1;
        GameObject _prefab;

        if (playerPowerUp.playerPowerUpType == PowerUpType.BigArrow)
        {
            defaultShootArrowIndex = 2;
            playerPowerUp.DeactiveBigArrowPower();
        }


        if (!isAiPlayer)
        {

            float shootForce = minForce + (maxForce - minForce) * currentForce;
            ////Debug.Log($"shootForce : {shootForce} || {currentForce}");

            if (playerPowerUp.playerPowerUpType != PowerUpType.Bomb)
            {
                _prefab = arrowPrefab;
            }
            else
            {
                _prefab = bombArrowPrefab;
            }

            for (int i = 0; i < defaultShootArrowIndex; i++)
            {

                if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
                {
                    GameObject arrowObj = Instantiate(_prefab, arrowSpawnPoint.position, arrowSpawnPoint.rotation);

                    Rigidbody2D arrowRb = arrowObj.GetComponent<Rigidbody2D>();
                    arrowRb.linearVelocity = arrowSpawnPoint.right * shootForce;

                    Arrow arrow = arrowObj.GetComponent<Arrow>();
                    arrow.Initialize(this, playerPowerUp, playerTag);

                    StartCoroutine(WairForEnable(arrow));

                }
                else
                {
                    Debug.Log($"[BowController---->>>>>>>>>>>  ] 📤 Spawning arrow network: {_prefab.name}, force: {shootForce}");
                    // StartCoroutine(SpawnArrowNetwork(_prefab, shootForce));
                    SpawnArrowNetwork(_prefab, shootForce);

                }

                yield return new WaitForSeconds(.2f);

            }

            fillbarParentObj.gameObject.SetActive(false);

        }
        else
        {
            if (playerPowerUp.playerPowerUpType != PowerUpType.Bomb) _prefab = aiArrowPrefab;
            else _prefab = aiBombArrowPrefab;

            for (int i = 0; i < defaultShootArrowIndex; i++)
            {
                GameObject arrowObj = Instantiate(_prefab, arrowSpawnPoint.position, Quaternion.Euler(0, 0, -45));

                Arrow arrow = arrowObj.GetComponent<Arrow>();
                arrow.enemyShootAngle = _enemyShootAngle;
                arrow.Initialize(this, playerPowerUp, playerTag);

                StartCoroutine(WairForEnable(arrow));

                yield return new WaitForSeconds(.2f);
            }

        }

        Invoke(nameof(WaitSomeTime), 0.1f);

    }

        private void SpawnArrowNetwork(GameObject _prefab, float shootForce)
    {
        // CRITICAL: Add comprehensive debugging to identify controller instance and playerID
        string controllerType = this.GetType().Name;
        bool isPlayerController = this is PlayerController;
        bool isOpponentController = this is OpponentController;
        
        Debug.Log($"[BowController] 🔍 SpawnArrowNetwork called - Controller Type: {controllerType}, IsPlayerController: {isPlayerController}, IsOpponentController: {isOpponentController}, playerID: {playerID}, GameObject: {gameObject.name}");
        
        // Verify playerID is set correctly
        if (playerID == -1)
        {
            Debug.LogError($"[BowController] ❌ CRITICAL ERROR: playerID is -1 (not set)! Controller Type: {controllerType}, GameObject: {gameObject.name}");
        }
        
        Debug.Log($"[BowController---->>>>>>>>>>>  ] 📤📤 Spawning arrow network: {_prefab.name}, force: {shootForce}, playerID: {playerID}");
        
        // Verify correct prefab is being used
        if (_prefab != arrowPrefab && playerPowerUp.playerPowerUpType != PowerUpType.Bomb)
        {
            Debug.LogWarning($"[BowController] ⚠️ Using different prefab! Expected: {(arrowPrefab != null ? arrowPrefab.name : "NULL")}, Using: {_prefab.name}");
        }
        
        // Spawn arrow locally
        var arrowObj = Instantiate(_prefab, arrowSpawnPoint.position, arrowSpawnPoint.rotation);

        Rigidbody2D arrowRb = arrowObj.GetComponent<Rigidbody2D>();

        // Use arrowSpawnPoint.right for both players - the arrowSpawnPoint is already correctly
        // positioned and rotated for each player's bow, so we don't need to negate for playerID 1
        //Vector2 shootDirection = arrowSpawnPoint.right;
        Vector2 shootDirection = playerID == 0 ? arrowSpawnPoint.right : -arrowSpawnPoint.right;
        
        // Debug log for direction calculation
        float rotationZ = arrowSpawnPoint.rotation.eulerAngles.z;
        Debug.Log($"[BowController] 🎯 Arrow spawn direction - playerID: {playerID}, rotationZ: {rotationZ}°, shootDirection: {shootDirection}, arrowSpawnPoint.right: {arrowSpawnPoint.right}");

        arrowRb.linearVelocity = shootDirection * shootForce;

        Arrow arrow = arrowObj.GetComponent<Arrow>();
        arrow.Initialize(this, playerPowerUp, playerTag);

        StartCoroutine(WairForEnable(arrow));

        // Reset charging state after arrow spawns
        isCharging = false;

        // Send rotation start event to network (rotation resumes after arrow spawns)
        if (GameManager.gameMode == GameModeType.MULTIPLAYER && ArrowduelNetworkManager.Instance != null)
        {
            var rotationStartData = new ArrowduelNetworkManager.NewRotationControlData
            {
                playerID = playerID,
                rotatedAngle = (int)currentAutoRotationAngle
            };
            string rotationJson = JsonUtility.ToJson(rotationStartData);
            ArrowduelNetworkManager.Instance.SendMatchState(ArrowduelNetworkManager.OPCODE_ROTATION_START, rotationJson);
            Debug.Log($"[BowController] ✅ Sent rotation START event to network - playerID: {playerID}, json: {rotationJson}");
        }

        // Send arrow spawn data to network for remote clients
        if (GameManager.gameMode == GameModeType.MULTIPLAYER && ArrowduelNetworkManager.Instance != null)
        {
                
                // CRITICAL: Double-check playerID before sending
                Debug.Log($"[BowController] 🔍 Before sending arrow spawn - playerID: {playerID}, Controller Type: {controllerType}, GameObject: {gameObject.name}");
            
            var arrowSpawnData = new ArrowduelNetworkManager.ArrowSpawnData
            {
                playerID = playerID,  // Make sure this is the correct playerID
                positionX = arrowSpawnPoint.position.x,
                positionY = arrowSpawnPoint.position.y,
                positionZ = arrowSpawnPoint.position.z,
                rotationZ = arrowSpawnPoint.rotation.eulerAngles.z,
                shootForce = shootForce,
                isBomb = playerPowerUp.playerPowerUpType == PowerUpType.Bomb,
                currentForce = currentForce
            };

            string json = JsonUtility.ToJson(arrowSpawnData);
            Debug.Log($"[BowController---->>>>>>>>>>>  ] 📤 Sending arrow spawn data: {json}");
            Debug.Log($"[BowController] 🔍 ArrowSpawnData.playerID = {arrowSpawnData.playerID}, this.playerID = {this.playerID}");
            ArrowduelNetworkManager.Instance.SendMatchState(ArrowduelNetworkManager.OPCODE_ARROW_SPAWN, json);
            Debug.Log($"[BowController] 📤 Sent arrow spawn to network - playerID: {playerID}, force: {shootForce}, position: ({arrowSpawnPoint.position.x}, {arrowSpawnPoint.position.y}, {arrowSpawnPoint.position.z}), rotationZ: {arrowSpawnPoint.rotation.eulerAngles.z}");
        }
    }

    public IEnumerator WairForEnable(Arrow arrow)
    {
        yield return new WaitForEndOfFrame();
        arrow.arrowSprite.enabled = true;
        arrow.trail.GetComponent<TrailRenderer>().enabled = true;
    }

    private void WaitSomeTime()
    {
        isCharging = false;
    }

    private Vector3 GetInputPosition()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.mousePosition;
#else
        return Input.GetTouch(0).position;
#endif
    }


}
