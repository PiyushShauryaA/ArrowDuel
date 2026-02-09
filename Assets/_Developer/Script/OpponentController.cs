using System.Collections;
using System.Linq;
using UnityEngine;
using Nakama;

//Difficulty settings
public enum AiSkillLevels
{
    Easy,
    Normal,
    Hard,
    Robinhood
}

public class OpponentController : BowController
{
    [SerializeField] private GameManager gameManager;


    [Header("----- AI -----")] public AiSkillLevels aiSkill = AiSkillLevels.Easy;

    [Header("Public GamePlay settings")] public int enemyHealth = 100;
    internal int enemyCurrentHealth;

    public float
        baseShootAngle =
            61.5f; //Very important! - avoid editing this value! (it has been calculated based on the size/shape/weight of the arrow)

    public float
        shootAngleError =
            0; //We use this to give some erros to enemy shoots. Setting this to 0 will results in accurate shoots

    // public static float fakeWindPower = 0;         //We use this if we need to add more randomness to enemy shots.
    public static bool isEnemyDead; //flag for gameover event

    //Enemy shoot settings
    private bool canShoot;

    [Header("AI Settings")] [SerializeField]
    private float minDecisionTime = 1f;

    [SerializeField] private float maxDecisionTime = 3f;
    //[SerializeField] private float angleAdjustmentSpeed = 0.5f;

    private float decisionTimer;
    private float nextDecisionTime;

    [SerializeField] private float minAngle = 55f;
    [SerializeField] private float maxAngle = 70f;


    private void Awake()
    {
        enemyCurrentHealth = enemyHealth;
        canShoot = false;
        isEnemyDead = false;
    }

    private new void Start()
    {
        base.Start();
        gameManager = GameManager.instance;
        hasRemoteSync = GetComponent<PlayerNetworkRemoteSync>() != null;
        hasLocalSync = GetComponent<PlayerNetworkLocalSync>() != null;
        // Set controller reference for multiplayer (if not host)
        if (GameManager.gameMode == GameModeType.MULTIPLAYER)
        {
            // Determine if we're player 1 (host)
            bool isPlayer1 = false;
            if (ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.CurrentMatch != null)
            {
                var presences = ArrowduelNakamaClient.Instance.CurrentMatch.Presences;
                var sortedPresences = presences.ToList();
                sortedPresences.Sort((a, b) => string.Compare(a.UserId, b.UserId));
                isPlayer1 = sortedPresences.Count > 0 &&
                            sortedPresences[0].UserId == ArrowduelNakamaClient.Instance.Session?.UserId;
            }

            if (!isPlayer1)
            {
                GameManager.instance.SetPlayerController(this, true);
            }
        }
    }

    public bool hasRemoteSync;
    public bool hasLocalSync;

    public bool isHostPlayer;

    private void Update()
    {
        if (GameManager.instance.gameState != GameState.Gameplay || playerPowerUp.isFrozen)
            return;

        if (GameManager.gameMode == GameModeType.MULTIPLAYER)
        {
            // Check if this is a remote player (has PlayerNetworkRemoteSync but NOT PlayerNetworkLocalSync)
            // Remote players get rotation from network, not local AutoRotate
            
            //Debug.Log($"hasRemoteSync: {hasRemoteSync}, hasLocalSync: {hasLocalSync}");
            if (hasRemoteSync && !hasLocalSync)
            {
                // This is a remote player - rotation is handled by PlayerNetworkRemoteSync
                // Don't call UpdatePlayerBehavior() as it would call AutoRotate()
                return;
            }

            // Determine if we're player 2 (non-host) - Player 2 controls right player (playerID == 1)
            bool isPlayer2 = ArrowduelNakamaClient.Instance != null && !ArrowduelNakamaClient.Instance.IsHost;

            // Player 2 (non-host) controls right player (playerID == 1)
            // Only process input if this is NOT an AI game
            if (isPlayer2 && playerID == 1 )
            {
                HandleInput();
                UpdatePlayerBehavior(); // Critical: Enable bow rotation and force meter updates for Player 2
            }
            else if (hasLocalSync && playerID == 1 )
            {
                // Fallback: If we have local sync and this is player 2, allow input
                // This ensures input works even if network checks fail
                HandleInput();
                UpdatePlayerBehavior();
            } 
        }
        else 
        {
            Debug.Log($"[OpponentController] Update - GameMode: SINGLEPLAYER");
            UpdateAIBehavior();
        }
    }


    #region PLAYER :

    public void UpdatePlayerBehavior()
    {
        // Only update behavior for Player type (not AI)
        if (playerType == PlayerType.Player)
        {
            if (!isCharging)
            {
                AutoRotate();
            }
            else
            {
                UpdateForceMeter();
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerController] UpdatePlayerBehavior called but playerType is {playerType}, not Player. playerID: {playerID}");
        }
    }

    public void HandleInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE || PLATFORM_WEBGL || UNITY_WEBGL
        if (Input.GetMouseButtonDown(0)) StartCharging();
        if (Input.GetMouseButtonUp(0)) ReleaseArrow();
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) StartCharging();
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) ReleaseArrow();
#endif
    }

    #endregion


    #region AI :

    private void ResetDecisionTimer()
    {
        nextDecisionTime = Random.Range(minDecisionTime, maxDecisionTime);
        decisionTimer = 0f;
        // canShoot = false;
    }

    public void UpdateAIBehavior()
    {
        if (!isCharging)
        {
            // AI decision making when not charging
            decisionTimer += Time.deltaTime;

            if (decisionTimer >= nextDecisionTime)
            {
                canShoot = true;
                isCharging = true;
                Debug.Log($"[OpponentController] UpdateAIBehavior - canShoot: {canShoot}, isCharging: {isCharging}");
                StartCoroutine(ShootArrow());
                ResetDecisionTimer();
            }
            else
            {
                AutoRotate();
            }
        }
    }

    public int arrowHitCounter;
    public int arrowMissCounter;

    /// <summary>
    /// Enemy shoot AI.
    /// We just need to create the enemy-Arrow and feed it with initial shoot angle. It will calculate the shoot-power itself.
    /// </summary>
    /// 
    IEnumerator ShootArrow()
    {
        if (!canShoot)
            yield break;

        canShoot = false;

        //wait a little for the camera to correctly get in position
        // yield return new WaitForSeconds(0.95f);

        //we need to rotate enemy body to a random/calculated rotation angle
        float targetAngle = Random.Range(minAngle, maxAngle) /** -1*/; //important! (originate from 65)
        // //Debug.Log($"targetAngle: {targetAngle} | {currentAutoRotationAngle}");
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            // bowParent.rotation = Quaternion.Euler(0, 180f, Mathf.SmoothStep(currentRot.z, targetAngle, t));
            Quaternion rotation = Quaternion.Euler(0, 0f, Mathf.SmoothStep(currentAutoRotationAngle, targetAngle, t));
            bowParent.rotation = rotation;
            if (frontHand != null && backHand != null)
            {
                frontHand.rotation = bowParent.rotation;
                backHand.rotation = bowParent.rotation * Quaternion.Euler(0f, 0f, -15f);
            }

            yield return 0;
        }

        // //Debug.Log("Enemy Fired!");

        // float finalShootAngle = baseShootAngle + Random.Range(-shootAngleError, shootAngleError);

        bool willHit = false;
        switch (aiSkill)
        {
            case AiSkillLevels.Normal:
                willHit = Random.Range(0f, 1f) <= 0.4f; // 40% chance

                if (willHit == false && arrowMissCounter > Random.Range(4, 7))
                {
                    willHit = true;
                }

                if (arrowHitCounter >= 1)
                {
                    arrowHitCounter = 0;
                    willHit = false;
                }

                // if (willHit == false && arrowMissCounter > Random.Range(3, 6))
                // {
                //     willHit = true;
                // }

                break;
            case AiSkillLevels.Hard:
                willHit = Random.Range(0f, 1f) <= 0.8f; // 80% chance

                if (willHit == false)
                    arrowHitCounter = 0;

                if (arrowHitCounter >= 2 && willHit == true)
                {
                    arrowHitCounter = 0;
                    willHit = false;
                }

                break;
        }

        arrowMissCounter += 1;

        float finalShootAngle = willHit ? baseShootAngle : baseShootAngle + Random.Range(-5f, 5f);

        // //Debug.Log($"ENEMY - FinalShootAngle: {finalShootAngle} | willHit: {willHit} | counter: {arrowHitCounter}");
        if (gameManager.hasAutoPlay == false)
            AiReleaseArrow(finalShootAngle);
        else
            ReleaseArrow();
    }

    #endregion
}
