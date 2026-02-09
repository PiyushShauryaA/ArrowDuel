using System;
using System.Collections;

using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class HitTargetData
{
    public bool isPlayerArrow;
}

public class Arrow : MonoBehaviour
{

    public enum Type
    {
        Arrow,
        Bomb
    }
    [SerializeField] private Type type;

    private BowController bowController;
    public BowController bowControllerNetwork { get; set; }
    private PlayerPowerUp playerPowerUp;
    public PlayerPowerUp playerPowerUpNetwork { get; set; }

    private int arrowPlayerID = -1;
    public int arrowPlayerIDNetwork { get; set; }
    private string arrowPlayerTag;
    public string arrowPlayerTagNetwork { get; set; }

    private Rigidbody2D rb;
    public SpriteRenderer arrowSprite;

    [SerializeField] private GameObject bombEffect;
    [SerializeField] private GameObject bloodParticle;
    [SerializeField] private GameObject grassParticle; // Ground hit particle
    [SerializeField] private GameObject featherExplosionParticle; // bird hit particle
    public GameObject trail;
    
    [SerializeField]private bool isPlayerArrow;
    [SerializeField] public bool isPlayerArrowNetwork { get; set; }
    [SerializeField] private CircleCollider2D[] colliders; // Ground hit particle

    public AudioSource audioSource;

    private Vector2 windForce;

    public bool hasHit = false;
    private bool isFading = false;
    private bool isBomb = false;
    private bool isWindActive = false;

    public float enemyShootAngle;

    public bool isReturningArrow = false;
    public float returnDelay = .5f;


    public void Initialize(BowController bowController, PlayerPowerUp playerPowerUp, string playerTag)
    {

        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();

        this.bowController = bowController;
        if (GameManager.gameMode == GameModeType.MULTIPLAYER)
            bowControllerNetwork = bowController;

        this.playerPowerUp = playerPowerUp;
        if (GameManager.gameMode == GameModeType.MULTIPLAYER)
            playerPowerUpNetwork = playerPowerUp;

        if (GameManager.gameMode == GameModeType.MULTIPLAYER)
        {
            arrowPlayerIDNetwork = bowControllerNetwork.playerID;
            this.isPlayerArrowNetwork = arrowPlayerIDNetwork == 0 ? true : false;   

            arrowPlayerTagNetwork = playerTag;
        }
        else
        {
        }
        arrowPlayerID = bowController.playerID;
        this.isPlayerArrow = (arrowPlayerID == 0);

        arrowPlayerTag = playerTag;


        if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
            isWindActive = WindManager.instance.isWindActive;
        if (GameManager.gameMode == GameModeType.MULTIPLAYER)
            isWindActive = WindManager.instance != null ? WindManager.instance.isWindActive : false;

        windForce = WindManager.instance.windForce;

        isBomb = playerPowerUp.playerPowerUpType == PowerUpType.Bomb;
        if (GameManager.gameMode == GameModeType.MULTIPLAYER)
            isBomb = playerPowerUpNetwork.playerPowerUpType == PowerUpType.Bomb;

        if (type == Type.Bomb)
        {
            if (GameManager.gameMode == GameModeType.MULTIPLAYER)
                playerPowerUpNetwork.ResetPower();
            else
                playerPowerUp.ResetPower();

            Bomb bomb = bombEffect.GetComponent<Bomb>();
            bomb.bombPlayerTag = playerTag;
            //if (GameManager.gameMode == GameModeType.MULTIPLAYER)
           // {
               // bomb.isPlayerBomb = isPlayerArrowNetwork;
           // }
            //else
              //  bomb.isPlayerBomb = isPlayerArrow;
        }

        if (GameManager.gameMode == GameModeType.MULTIPLAYER)
            //Debug.Log($"tag: {arrowPlayerTag} | {isPlayerArrow} | {arrowPlayerID} \n {arrowPlayerTagNetwork} | {isPlayerArrowNetwork} | {arrowPlayerIDNetwork}");

        if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
        {
            if (arrowPlayerID == 0)
            {
                AudioManager.instance.PlayShootSfx();
            }
            ArrowIndicatorSystem.instance.TrackArrow(gameObject, isPlayerArrow);
        }
        else if (GameManager.gameMode == GameModeType.MULTIPLAYER)
        {
            // Check if this is local player's arrow (playerID 0 is local player)
            if (arrowPlayerIDNetwork == 0)
            {
                AudioManager.instance.PlayShootSfx();
            }
            ArrowIndicatorSystem.instance.TrackArrow(gameObject, isPlayerArrowNetwork);
        }


        if (arrowPlayerID == 1 && GameManager.gameMode == GameModeType.SINGLEPLAYER)
        {
            EnemyShoot();
        }

        Invoke(nameof(SetAsReturning), returnDelay);

    }

    void SetAsReturning()
    {
        isReturningArrow = true;
        // Add returning logic (e.g., reverse direction)
    }

    private void Start()
    {
        // Initialize arrow after spawn
        StartCoroutine(WairForEnable());
    }

    IEnumerator WairForEnable()
    {
        yield return new WaitForSeconds(.2f);
        arrowSprite.enabled = true;
        trail.GetComponent<TrailRenderer>().enabled = true;
    }

    private void EnemyShoot()
    {
        // Safety: prevent division by zero if angle is 0
        if (Mathf.Approximately(enemyShootAngle, 0f))
        {
            enemyShootAngle = 61.5f; // fallback to baseShootAngle
        }

        Vector3 pos = transform.position;
        Vector3 target = GameManager.instance.playerController.transform.position;

        float dist = Vector3.Distance(pos, target);

        float Vi = Mathf.Sqrt(dist * -Physics.gravity.y / (Mathf.Sin(Mathf.Deg2Rad * enemyShootAngle * 1.05f)));
        float Vy, Vx;

        Vx = Vi * Mathf.Cos(Mathf.Deg2Rad * enemyShootAngle);
        Vy = Vi * Mathf.Sin(Mathf.Deg2Rad * enemyShootAngle);

        // create the velocity vector
        Vector3 localVelocity = new Vector3(-Vx, Vy, 0);
        Vector3 globalVelocity = transform.TransformVector(localVelocity);

        rb.linearVelocity = globalVelocity;

        rb.AddForce(new Vector3(EnemyController.fakeWindPower, 0, 0), ForceMode2D.Force);

    }

    private void Update()
    {

        if (isFading)
        {
            float fadeSpeed = 1f * Time.deltaTime;
            arrowSprite.color = new Color(1, 1, 1, arrowSprite.color.a - fadeSpeed);

            if (arrowSprite.color.a <= 0f)
            {
                isFading = false;
                Destroy(gameObject);
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
        {
            //if (isWindActive)
            {
                // Apply continuous wind force
                if (GameManager.gameMode == GameModeType.SINGLEPLAYER && isWindActive)
                    rb.AddForce(WindManager.instance.windForce * Time.fixedDeltaTime, ForceMode2D.Force);
                else if (GameManager.gameMode == GameModeType.MULTIPLAYER)
                {
                    if (WindManager.instance != null && WindManager.instance.isWindActive)
                    {
                        rb.AddForce(WindManager.instance.windForce * Time.fixedDeltaTime, ForceMode2D.Force);
                    }
                }
            }

            // Rotate arrow to face movement direction
            if (rb.linearVelocity.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        if (transform.position.y < -25f)
        {
            Destroy(this.gameObject);
        }
    }


    public string hitObjectName { get; set; }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ADD THIS DEBUG LOG AT THE START
        Debug.Log($"[Arrow] OnTriggerEnter2D called! Collision: {collision.gameObject.name}, Tag: {collision.tag}, hasHit: {hasHit}, arrowPlayerIDNetwork: {arrowPlayerIDNetwork}");

        if (hasHit)
        {
            Debug.Log($"RETURN FROM HIT !!!!!!!!!!!!!!");
            return;
        }

        if (collision.CompareTag("Player") || collision.CompareTag("Opponent"))
        {
            OnTargetDetect(collision);

        }
        else if (collision.CompareTag("PowerUp"))
        {
            OnPowerUpDetect(collision);

        }
        else if (collision.CompareTag("Ground"))
        {
            OnGroundDetect(collision);

        }
        else if (collision.CompareTag("Bird"))
        {
            Debug.Log($"OnObstacleDetect >> collision: {collision.gameObject.name}");
            OnObstacleDetect(collision);

        }
        else if (collision.CompareTag("arrow"))
        {
            TwoArrowHitDetect(collision);
        }

    }

    private void OnObstacleDetect(Collider2D collision)
    {
        Debug.Log($"OnObstacleDetect ENTERED for: {collision.gameObject.name}");

        // Check if LoopMovementObject exists
        LoopMovementObject loopMovement = collision.GetComponent<LoopMovementObject>();
        Debug.Log($"LoopMovementObject found: {loopMovement != null}");


        hasHit = true;
        _collision = collision;

        AudioManager.instance.PlayBirdHitSfx();

        Debug.Log($"OnObstacleDetect >> Collision: {collision.gameObject.name}");
        RigidbodyFreeze();
        if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
        {
            if (collision.TryGetComponent(out LoopMovementObject bird))
            {
                BirdHitEffect(collision.transform);
                bird.OnHitDetect();
            }
            Destroy(gameObject, .1f);
        }
        else
        {
            Debug.Log($"OnObstacleDetect >> GameManager.hitObjectName: {hitObjectName}");
            if (String.IsNullOrEmpty(hitObjectName))
                hitObjectName = "OBJECT";

            // In multiplayer, only process hits for local arrows
           // if (GameManager.gameMode == GameModeType.SINGLEPLAYER || arrowPlayerIDNetwork == 0)
           // {
                if (collision.TryGetComponent(out LoopMovementObject bird))
                {

                    trail.SetActive(false);
                    arrowSprite.gameObject.SetActive(false);
                    //GetComponent<CircleCollider2D>().enabled = false;

                    bird.GetComponent<SpriteRenderer>().enabled = false;

                    BirdHitEffect(collision.transform);
                    bird.OnBirdHitDetect_RPC();
                    Invoke(nameof(NetworkDespawn), 0.25f);
                }
            Destroy(gameObject, .1f);
            
            //}
        }

    }

    public void BirdHitEffect(Transform _transform)
    {
        if (type == Type.Bomb)
            featherExplosionParticle.GetComponent<AudioSource>().enabled = true;

        featherExplosionParticle.transform.SetParent(null, false);
        featherExplosionParticle.SetActive(true);
        featherExplosionParticle.transform.position = _transform.position;
        Destroy(_transform.gameObject, 0.3f);
    }

    private void TwoArrowHitDetect(Collider2D collision)
    {

        //Debug.Log($"hitObj >> Me: {this.gameObject.name} | Other : {collision.gameObject.name}", this);

        Arrow arrow = collision.GetComponent<Arrow>();

        if (arrowPlayerTag == arrow.arrowPlayerTag)
        {
            // //Debug.Log($"hitObj >> Arrow collision with owener arrow RETURN...");
            return;
        }

        if (!hasHit && !arrow.hasHit)
        {

            // //Debug.Log($"Arrow DESTROY DESTROY DESTROY.....");
            if (type == Type.Arrow && arrow.type == Type.Arrow)
            {
                GameManager.instance.TwoArrowHitEffectActive(collision.transform.position);
            }
            else if (type == Type.Arrow && arrow.type == Type.Bomb)
            {
                bombEffect.transform.SetParent(null, false);
                bombEffect.gameObject.SetActive(true);
                bombEffect.transform.position = transform.position;
            }

            if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
            {
                Destroy(collision.gameObject);
                Destroy(gameObject);
            }
            else
            {
                if (String.IsNullOrEmpty(hitObjectName))
                    hitObjectName = "ARROW";
                ////Debug.Log($"hitObject >>> hasAuth : {HasStateAuthority == true} | {hitObjectName}", this);
                // if (Object.HasStateAuthority == true)
                {

                    trail.SetActive(false);
                    arrowSprite.gameObject.SetActive(false);
                    //GetComponent<CircleCollider2D>().enabled = false;

                    RigidbodyFreeze();

                    arrow.ArrowHitEachOther_RPC();
                    ArrowHitEachOther_RPC();

                }
            }
        }

    }

    public void ArrowHitEachOther_RPC()
    {

        GameManager.instance.TwoArrowHitEffectActive(transform.position);

        trail.SetActive(false);
        arrowSprite.gameObject.SetActive(false);
        //GetComponent<CircleCollider2D>().enabled = false;
        RigidbodyFreeze();

        NetworkDespawn();

    }

    public void DelayToCall()
    {
        arrowSprite.gameObject.SetActive(false);
        //GetComponent<CircleCollider2D>().enabled = false;

    }

    public Collider2D _collision;

    private void OnTargetDetect(Collider2D collision)
    {

        if (arrowPlayerTag == collision.gameObject.tag)
        {
            Debug.Log($"Arrow collision with Shooter...");
            if (!isReturningArrow)
                return;

            if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
            {

                isPlayerArrow = false;
            }
            else
            {
                if (arrowPlayerIDNetwork == 0)
                {
                    isPlayerArrow = false;
                    isPlayerArrowNetwork = false;
                }
                else
                {
                    isPlayerArrow = true;
                    isPlayerArrowNetwork = true;
                }
            }
        }

        Debug.Log($"player_AUTH: ");

        _collision = collision;

        if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
        {
            if (collision.CompareTag("Player"))
            {
                OpponentController opponentController = bowController.GetComponent<OpponentController>();
                if (opponentController != null)
                {
                    opponentController.arrowHitCounter += 1;
                    opponentController.arrowMissCounter = 0;
                }

            }
            
            ArrowHitEffect();
            Destroy(this.gameObject, 0.001f);
        }
        else
        {

            // if (String.IsNullOrEmpty(hitObjectName))
            hitObjectName = "PLAYER";
            Debug.Log($"hitObjectName: arrowPlayerIDNetwork--- {arrowPlayerIDNetwork}");

            // In multiplayer, only process hits for local arrows
            if (GameManager.gameMode == GameModeType.SINGLEPLAYER || arrowPlayerIDNetwork == 0)
            {
                ArrowHitEffect();

                arrowSprite.gameObject.SetActive(false);
                //GetComponent<CircleCollider2D>().enabled = false;

                Invoke(nameof(NetworkDespawn), 0.25f);

            }else if (GameManager.gameMode == GameModeType.MULTIPLAYER && arrowPlayerIDNetwork != 0)
            {
                ArrowHitEffect();

                arrowSprite.gameObject.SetActive(false);
                //GetComponent<CircleCollider2D>().enabled = false;

                Invoke(nameof(NetworkDespawn), 0.25f);
            }
        }

        // Destroy(this.gameObject, 0.001f);

    }

    private void ArrowHitEffect()
    {

        AudioManager.instance.PlayHitSfx();
        AudioManager.instance.PlayHurtSfx();

        hasHit = true;

        RigidbodyFreeze();

        if (type == Type.Bomb)
        {
            // OnBombActive(collision.transform);

            if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
                playerPowerUp.OnBombActive();
            if (GameManager.gameMode == GameModeType.MULTIPLAYER)
                playerPowerUpNetwork.OnBombActive();

            bombEffect.transform.SetParent(null, false);
            bombEffect.transform.position = transform.position;
            bombEffect.gameObject.SetActive(true);
        }
        else if (type == Type.Arrow)
        {
            bloodParticle.transform.SetParent(null, true);
            bloodParticle.gameObject.SetActive(true);

            if (_collision != null)
            {
                _collision.GetComponent<FloatingText>().ShowDamageEffect();

                GameObject heartBreakEffect = _collision.GetComponent<BowController>().heartBreakEffect;

                if (heartBreakEffect.activeInHierarchy)
                    heartBreakEffect.SetActive(false);

                heartBreakEffect.SetActive(true);
            }

            if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
                GameManager.onHitTarget?.Invoke(isPlayerArrow);
            else if (GameManager.gameMode == GameModeType.MULTIPLAYER)
            {
                if (ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.CurrentMatch != null)
                {
                    const long OPCODE_HIT_TARGET = 7;
                    var data = new HitTargetData { isPlayerArrow = bowControllerNetwork.playerID==0?true:false };
                    string json = JsonUtility.ToJson(data);
                    Debug.Log($"[Arrow] OnTargetDetect: HitTargetData>>>>>>>>>>>> - isPlayerArrow: {json}");
                    ArrowduelNakamaClient.Instance.SendMatchStateAsync(OPCODE_HIT_TARGET, json);
                }
                // GameManager.onHitTarget?.Invoke(isPlayerArrowNetwork);
            }

        }

    }

    private void NetworkDespawn()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Cleanup if needed

        // Only cleanup if this is not the local player's arrow in multiplayer
        if (GameManager.gameMode == GameModeType.MULTIPLAYER && arrowPlayerIDNetwork != 0)
        {

            //     //Debug.Log($"INDEX ::::::: hitObjectName >> {hitObjectName}, {this.gameObject.name}", this);

            //if (hitObjectName == "PLAYER")
            //{
                //ArrowHitEffect();

           // }
            //else
            if (hitObjectName == "Ground")
            {

                if (type == Type.Bomb)
                {
                    bombEffect.transform.SetParent(null, false);
                    bombEffect.transform.position = transform.position;
                    bombEffect.gameObject.SetActive(true);

                    if (GameManager.gameMode == GameModeType.MULTIPLAYER)
                    {
                        playerPowerUpNetwork.OnBombActive();
                    }

                }

            }
            else if (hitObjectName == "OBJECT")
            {
                BirdHitEffect(this.transform);

            }

        }

    }

    private void OnPowerUpDetect(Collider2D collision)
    {
        //Debug.Log($"OnPowerUpDetect >> arrowPlayerID: {arrowPlayerID} || PLAYER > {arrowPlayerIDNetwork}");
        hasHit = true;
        RigidbodyFreeze();

        _collision = collision;

        if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
        {
            if (collision.TryGetComponent(out PowerUp powerUp))
            {
                if (arrowPlayerID == 0)
                {
                    AudioManager.instance.PlayPowerCollectSfx();
                }

                powerUp.Collect(bowController);
                Destroy(gameObject, 0.001f);

            }
        }
        else
        {
            if (String.IsNullOrEmpty(hitObjectName))
                hitObjectName = "POWER";

            if (collision.TryGetComponent(out PowerUp powerUp))
            {
                // Check if this is local player's arrow (playerID 0 is local player)
            if (arrowPlayerIDNetwork == 0)
                {
                    AudioManager.instance.PlayPowerCollectSfx();
                }

                powerUp.Collect(bowControllerNetwork);

            }

            // In multiplayer, only process hits for local arrows
            if (GameManager.gameMode == GameModeType.SINGLEPLAYER || arrowPlayerIDNetwork == 0)
            {
                arrowSprite.gameObject.SetActive(false);
                //GetComponent<CircleCollider2D>().enabled = false;

                Invoke(nameof(NetworkDespawn), 0.25f);

            }
        }

    }

    private void OnGroundDetect(Collider2D collision)
    {
        hasHit = true;
        RigidbodyFreeze();
        arrowSprite.sortingLayerName = "Default";

        if (GameManager.gameMode == GameModeType.MULTIPLAYER)
            hitObjectName = "GROUND";

        AudioManager.instance.PlayGroundHitSfx();

        if (type == Type.Bomb)
        {

            bombEffect.transform.SetParent(null, false);
            bombEffect.transform.position = transform.position;
            bombEffect.gameObject.SetActive(true);

            if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
            {
                playerPowerUp.OnBombActive();
                Destroy(gameObject, 0.001f);
            }
            else if (GameManager.gameMode == GameModeType.MULTIPLAYER)
            {
                playerPowerUpNetwork.OnBombActive();
                Invoke(nameof(NetworkDespawn), .25f);
            }

        }
        else if (type == Type.Arrow)
        {

            grassParticle.transform.SetParent(null, true);
            grassParticle.gameObject.SetActive(true);

            Vector3 screenPos = Camera.main.WorldToViewportPoint(transform.position);

            // Arrow is on screen
            if (screenPos.x >= 0 && screenPos.x <= 1 && screenPos.y >= 0 && screenPos.y <= 1)
            {
                Invoke(nameof(FadeArrowDisplay), 10f);
            }
            else
            {
                Invoke(nameof(FadeArrowDisplay), 1f);
            }

        }

    }

    private void RigidbodyFreeze()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void FadeArrowDisplay()
    {
        isFading = true;
    }

}
