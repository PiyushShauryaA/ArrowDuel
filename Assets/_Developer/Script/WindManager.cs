using UnityEngine;

public class WindManager : MonoBehaviour
{
    public static WindManager instance;

    [Header("Wind Settings")]
    public float maxWindStrength = 5f;
    // [SerializeField] private GameObject[] windIndicators;

    public float currentTime;
    public float maxWaitTime;

    public Vector2 windDirection;
    public Vector2 windForce;

    public bool isWindActive = false;
    public bool isWindDirectionRight = false;

    public AudioSource windAudioSource;


    private void Awake()
    {
        instance = this;

    }

    private void Start()
    {
        GameManager.instance.windIndicators[0].gameObject.SetActive(false);
        GameManager.instance.windIndicators[1].gameObject.SetActive(false);

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
        isWindActive = false;
        currentTime = maxWaitTime;

    }

    private void Update()
    {

        if (GameManager.instance.gameState != GameState.Gameplay || isWindActive)
            return;

        if (GameManager.gameMode == GameModeType.MULTIPLAYER && 
            (NakamaNetworkManager.Instance != null && !NakamaNetworkManager.Instance.HasStateAuthorityGameData))
            return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime * 1f;
        }
        else
        {
            // ChangeWind();
            InitWind();
        }

    }

    public void InitWind()
    {
       // //Debug.Log($"442 - WindInit - WindInit - WindInit");
        // Random wind direction (left or right)

        if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
        {
            isWindDirectionRight = Random.value > 0.5f;
            windDirection = isWindDirectionRight ? Vector2.right : Vector2.left;
            windForce = windDirection * maxWindStrength;

            ChangeWind();
        }
        else
        {
            if (NakamaNetworkManager.Instance != null)
            {
                NakamaNetworkManager.Instance.Wind_RPC();
            }
        }

    }


    public void ChangeWind()
    {

        isWindActive = true;
        currentTime = 0;

        windAudioSource.Play();

       // //Debug.Log($"442 - WIND - {isWindDirectionRight} | {isWindActive} | {windForce}");

        GameManager.instance.windIndicators[0].gameObject.SetActive(isWindDirectionRight);
        GameManager.instance.windIndicators[1].gameObject.SetActive(!isWindDirectionRight);

        Invoke(nameof(StopWind), 10f);

    }

    public void StopWind()
    {
        if (GameManager.gameMode == GameModeType.MULTIPLAYER)
        {
            if (NakamaNetworkManager.Instance != null && NakamaNetworkManager.Instance.HasStateAuthorityGameData)
            {
                NakamaNetworkManager.Instance.isWindActive = false;
                NakamaNetworkManager.Instance.windForce = Vector2.zero;
            }
        }
            
        windAudioSource.Stop();

        GameManager.instance.windIndicators[0].gameObject.SetActive(false);
        GameManager.instance.windIndicators[1].gameObject.SetActive(false);

        windDirection = Vector2.zero;
        windForce = windDirection;

        currentTime = maxWaitTime;
        isWindActive = false;

        // Invoke(nameof(NextWindTimeStart), Random.Range(3, 5));
    }

    public void OnGameLevelChange()
    {
        if (isWindActive)
        {
            StopWind();
        }
        else
        {
            currentTime = maxWaitTime;
        }
    }


}
