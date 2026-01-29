using System.Collections;
using System.Linq;
using UnityEngine;
using Nakama;

public class LoopMovementObject : MonoBehaviour
{


    public bool isBird = false;
    public bool isLoopMovement = false; /// Some things move from left to right and from right to left, like birds,
    public float xBoundary = 25f;
    public float delayMoveStart = 0f;
    public bool canMove = false;
    public bool hasHurt = false;

    public float _speed = 0.05f;

    [Space(10)]
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;
    [SerializeField] private Collider[] _collider;

    [Space(05)]
    public DirectionType directionType;
    private Vector3 direction;
    private Vector3 initPosition;

    public enum DirectionType
    {
        None,
        Right,
        Left,
        Up,
        Down
    }


    void OnEnable()
    {
        if (isBird)
            return;

        GameManager.onGameStart += OnGameStart;
    }

    private void Start()
    {

        switch (directionType)
        {
            case DirectionType.None:
                direction = Vector3.zero;
                //Debug.Log($"<color=orange>- Select object move direction tyepe...! -</color>");
                break;
            case DirectionType.Right:
                direction = Vector3.right;
                break;
            case DirectionType.Left:
                direction = Vector3.left;
                break;
            case DirectionType.Up:
                direction = Vector3.up;
                break;
            case DirectionType.Down:
                direction = Vector3.down;
                break;
            default:
                break;
        }

        if (!isLoopMovement)
            initPosition = transform.position + Vector3.left * 50f;
        else
            initPosition = transform.position;

        if (GameManager.gameMode == GameModeType.SINGLEPLAYER && isBird)
        {
            StartCoroutine(DelayMoveStart());
        }

    }

    void OnDisable()
    {
        if (isBird)
            return;

        GameManager.onGameStart -= OnGameStart;
    }

    private void OnGameStart()
    {
        StartCoroutine(DelayMoveStart());

    }

    private void DelayCheck()
    {
        canMove = true;

    }

    IEnumerator DelayMoveStart()
    {
        yield return new WaitForSeconds(delayMoveStart);
        canMove = true;
    }

    private void Awake()
    {
        // Initialize for multiplayer - check if we have authority (host)
        if (GameManager.gameMode == GameModeType.MULTIPLAYER && isBird)
        {
            bool hasAuthority = false;
            if (NakamaClient.Instance != null && NakamaClient.Instance.CurrentMatch != null)
            {
                var presences = NakamaClient.Instance.CurrentMatch.Presences;
                var sortedPresences = presences.ToList();
                sortedPresences.Sort((a, b) => string.Compare(a.UserId, b.UserId));
                hasAuthority = sortedPresences.Count > 0 && sortedPresences[0].UserId == NakamaClient.Instance.UserId;
            }

            if (hasAuthority)
            {
                Invoke(nameof(DelayCheck), delayMoveStart);
            }
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.gameMode == GameModeType.MULTIPLAYER && isBird)
        {
            if (GameManager.instance.gameState != GameState.Gameplay && GameManager.instance.gameState != GameState.WaitForLevelChange)
                return;

            // Check if we have authority (host)
            bool hasAuthority = false;
            if (NakamaClient.Instance != null && NakamaClient.Instance.CurrentMatch != null)
            {
                var presences = NakamaClient.Instance.CurrentMatch.Presences;
                var sortedPresences = presences.ToList();
                sortedPresences.Sort((a, b) => string.Compare(a.UserId, b.UserId));
                hasAuthority = sortedPresences.Count > 0 && sortedPresences[0].UserId == NakamaClient.Instance.UserId;
            }

            if (!hasAuthority) return;

            if (isBird == true && Vector3.Distance(transform.position, initPosition) < 1f && _sprite.enabled == false && canMove == false)
            {
                //Debug.Log($"RESET TO START POSITION.... ...!");
                ActiveBird_RPC();
            }

            if (canMove)
            {
                transform.Translate(direction * _speed * Time.fixedDeltaTime);
                HandleBoundary();
            }
            else
            {
                if (hasHurt)
                {
                    DeactiveBird_RPC();
                }
            }
        }
    }

    void Update()
    {
        if (GameManager.gameMode == GameModeType.SINGLEPLAYER || (GameManager.gameMode == GameModeType.MULTIPLAYER && !isBird))
        {
            if (GameManager.instance.gameState != GameState.Gameplay && GameManager.instance.gameState != GameState.WaitForLevelChange)
                return;

            if (isBird == true && Vector3.Distance(transform.position, initPosition) < 1f && _sprite.enabled == false && canMove == false)
            {
                //Debug.Log($"RESET TO START POSITION....!");
                ActiveBird();
            }

            if (canMove)
            {
                transform.Translate(direction * _speed * Time.deltaTime);
                HandleBoundary();
            }
            else
            {
                if (hasHurt)
                {
                    DeactiveBird();
                }
            }

        }
    }

    void HandleBoundary()
    {

        if (transform.position.x > xBoundary && directionType == DirectionType.Right)
        {
            if (isBird)
            {
                if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
                    DeactiveBird();
                else
                    DeactiveBird_RPC();
            }
            else
            {
                transform.position = initPosition;

            }

        }

        if ((transform.position.x > xBoundary || transform.position.x < -xBoundary) && directionType == DirectionType.Left)
        {
            if (isBird)
            {
                if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
                    DeactiveBird();
                else
                    DeactiveBird_RPC();
            }
            else
            {
                transform.position = initPosition;

            }

        }

    }


    public void ActiveBird_RPC()
    {
        ActiveBird();
    }


    private void ActiveBird()
    {
        hasHurt = false;

        Invoke(nameof(SpriteActive), 1f);

        transform.position = initPosition;
        canMove = true;

    }

    private void SpriteActive()
    {
        _sprite.enabled = true;
        anim.enabled = true;

        foreach (var item in _collider)
        {
            item.enabled = true;
        }
    }


    public void DeactiveBird_RPC()
    {
        DeactiveBird();
    }


    public void DeactiveBird()
    {
        hasHurt = false;
        canMove = false;

        _sprite.enabled = false;
        anim.enabled = false;

        foreach (var item in _collider)
        {
            item.enabled = false;
        }

        transform.position = initPosition;

    }


    public void OnBirdHitDetect_RPC()
    {
       // Invoke(nameof(OnHitDetect), .05f);
        OnHitDetect();

    }

    public void OnHitDetect()
    {
        hasHurt = true;
        canMove = false;
        anim.enabled = false;

        foreach (var item in _collider)
        {
            item.enabled = false;
        }

    }



}
