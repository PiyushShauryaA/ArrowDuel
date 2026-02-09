using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;      
using UnityEngine.SceneManagement;
using Nakama; 
using UnityEngine.UI;
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    [SerializeField] private int pointsToWin = 10;
    [SerializeField] private int themeChangeScore = 5;

    internal bool hasEventSend = false;

    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private GameObject gameOverPanel;

    [SerializeField] private Button homeButton  ;

    private string matchResultStr;


    [Header("Heart Display")]
    public float player1Hearts = 5f; // Now using float for partial hearts
    public float player2Hearts = 5f;
    public float damagePerHit = 0.5f; // Each hit removes 0.5 hearts (2 hits = 1 full heart)

    public HeartDisplay[] heartDisplays; // Array of UI heart displays


    private int playerScore;
    private int opponentScore;



    private void Awake()
    {
        instance = this;

    }

    private void Start()
    {
        // Initialize scores
        playerScore = 0;
        opponentScore = 0;

        UpdateScoreDisplay();
        gameOverPanel.SetActive(false);

        GameManager.onHitTarget += OnHitTarget; 
        homeButton.onClick.AddListener(OnHomeButtonClick);
    }

    async private void OnHomeButtonClick()
    {
        Debug.Log("OnHomeButtonClick");

        // Pehle match leave karo, phir disconnect karo
        if (ArrowduelNakamaClient.Instance != null)
        {
            await ArrowduelNakamaClient.Instance.LeaveMatchAsync();
            await ArrowduelNakamaClient.Instance.DisconnectAsync();
        }

        SceneManager.LoadScene("menu");  
    }

    private void OnDestroy()
    {
        GameManager.onHitTarget -= OnHitTarget;

    }

    private void OnHitTarget(bool isPlayerArrow)
    {
        RegisterHit(isPlayerArrow);
    }

    [ContextMenu("-TestRegisterHit")]
    public void TestRegisterHit()
    {
        RegisterHit(Random.value > .5f);
    }

    public void RegisterHit(bool isPlayerArrow)
    {
        if (GameManager.instance.gameState == GameState.GameOver)
            return;

        Debug.Log($"playerScore: {playerScore} || opponentScore: {opponentScore} || {isPlayerArrow}");

        if (isPlayerArrow)
        {
            playerScore++;
            if (playerScore == themeChangeScore)
            {
                GameManager.instance.gameState = GameState.WaitForLevelChange;
                Invoke(nameof(ChangeTheme), 1f);
            }
        }
        else
        {
            opponentScore++;
            if (opponentScore == themeChangeScore)
            {
                GameManager.instance.gameState = GameState.WaitForLevelChange;
                Invoke(nameof(ChangeTheme), 1f);
            }
        }

        // UpdateScoreDisplay();
        StartCoroutine(ScorePlush());

        PlayerHit(isPlayerArrow ? 1 : 0);

        // Check win condition
        if (playerScore >= pointsToWin || opponentScore >= pointsToWin)
        {
            EndGame();
        }

    }

    private void ChangeTheme()
    {
        //Debug.Log($"442 - ChangeTheme");
        if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
            GameManager.onGameLevelChange?.Invoke();
        else if (GameManager.gameMode == GameModeType.MULTIPLAYER)
        {
            // Use ArrowduelNetworkManager for theme change synchronization
            if (ArrowduelNetworkManager.Instance != null && ArrowduelNetworkManager.Instance.HasStateAuthorityGameData)
            {
                ArrowduelNetworkManager.Instance.ChangeTheme_RPC();
            }
            else
            {
                //Debug.Log($"442 - ChangeTheme - Multiplayer theme change");
                GameManager.onGameLevelChange?.Invoke();
            }
        }
    }

    private void UpdateScoreDisplay()
    {
        scoreText.text = $"{playerScore} - {opponentScore}";
    }

    private IEnumerator ScorePlush()
    {

        for (float i = 1f; i <= 1.2f; i += 0.05f)
        {
            scoreText.rectTransform.localScale = new Vector3(i, i, i);
            yield return new WaitForEndOfFrame();
        }

        scoreText.rectTransform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

        UpdateScoreDisplay();

        for (float i = 1.2f; i <= 1f; i -= 0.05f)
        {
            scoreText.rectTransform.localScale = new Vector3(i, i, i);
            yield return new WaitForEndOfFrame();
        }

        scoreText.rectTransform.localScale = new Vector3(1f, 1f, 1f);

    }

    public void EndGame(bool forceToWon = false)
    {
        Debug.Log($"EndGame > forceToWon: {forceToWon}");
        GameManager.instance.gameState = GameState.GameOver;
        gameOverPanel.SetActive(true);

        string resultStr = "GAME OVER!";
        // Determine if we're player 1 (host) in multiplayer
        bool isPlayer1Local = true;
        if (GameManager.gameMode == GameModeType.MULTIPLAYER && ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.CurrentMatch != null)
        {
            var presences = ArrowduelNakamaClient.Instance.CurrentMatch.Presences;
            var sortedPresences = presences.ToList();
            sortedPresences.Sort((a, b) => string.Compare(a.UserId, b.UserId));
            isPlayer1Local = sortedPresences.Count > 0 && sortedPresences[0].UserId == ArrowduelNakamaClient.Instance.Session?.UserId;
        }

        if (forceToWon)
        {
            IFrameBridge.instance.PostMatchResult("won", isPlayer1Local ? playerScore : opponentScore, isPlayer1Local ? opponentScore : playerScore);
            gameOverText.text = "GAME ABORTED!";

            return;
        }

        if (playerScore >= pointsToWin)
        {
            if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
            {
                resultStr = "YOU WIN!";
            }
            else
            {
                if (isPlayer1Local)
                    resultStr = "YOU WIN!";
                else
                    resultStr = "OPPONENT WIN!";
            }
        }
        else if (opponentScore >= pointsToWin)
        {
            if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
            {
                resultStr = "OPPONENT WIN!";
            }
            else
            {
                if (!isPlayer1Local)
                    resultStr = "YOU WIN!";
                else
                    resultStr = "OPPONENT WIN!";
            }
        }
        else
        {
            resultStr = "GAME DRAW!";
        }
        // After line 216 (end of the if/else block), before setting gameOverText.text:
        string winnerName = "N/A";
        if (playerScore >= pointsToWin)
            winnerName = GameManager.instance.player1NameText.text;
        else if (opponentScore >= pointsToWin)
            winnerName = GameManager.instance.player2NameText.text;
        else
            winnerName = "DRAW";

        Debug.Log($"[ScoreManager] Game Over! Winner: {winnerName} | Result: {resultStr} | Score: {playerScore}-{opponentScore} | isPlayer1Local: {isPlayer1Local}");
        gameOverText.text = resultStr+" "+winnerName+" "+playerScore+" "+opponentScore;
        // Invoke(nameof(ActiveGameOverPanel), 5f);

        if (GameManager.gameMode == GameModeType.MULTIPLAYER)
        {
            Invoke(nameof(OnSendGameComplete), 1f);
        }
        else
        {
            EndGameWaitForSomeTime();
        }

    }

    private void OnSendGameComplete()
    {
        // Check if we're player 1 (host)
        bool isPlayer1 = true;
        if (GameManager.gameMode == GameModeType.MULTIPLAYER && NakamaClient.Instance != null && NakamaClient.Instance.CurrentMatch != null)
        {
            var presences = NakamaClient.Instance.CurrentMatch.Presences;
            var sortedPresences = presences.ToList();
            sortedPresences.Sort((a, b) => string.Compare(a.UserId, b.UserId));
            isPlayer1 = sortedPresences.Count > 0 && sortedPresences[0].UserId == NakamaClient.Instance.UserId;
        }
        if (isPlayer1)
            GameManager.instance.playerController.SetGameCompleted();
        else
            GameManager.instance.opponentPlayerController.SetGameCompleted();
    }

    private void ActiveGameOverPanel()
    {
        gameOverPanel.SetActive(true);
    }

    public void EndGameWaitForSomeTime()
    {
        if (hasEventSend)
            return;
        hasEventSend = true;
        Invoke(nameof(SendGameFinishEvent), 5f);
    }

    private void SendGameFinishEvent()
    {
        // IFrameBridge.instance.PostMatchResult(matchResultStr, playerScore, opponentScore);

        // Determine if we're player 1 (host) in multiplayer
        bool isPlayer1Local = true;
        if (GameManager.gameMode == GameModeType.MULTIPLAYER && NakamaClient.Instance != null && NakamaClient.Instance.CurrentMatch != null)
        {
            var presences = NakamaClient.Instance.CurrentMatch.Presences;
            var sortedPresences = presences.ToList();
            sortedPresences.Sort((a, b) => string.Compare(a.UserId, b.UserId));
            isPlayer1Local = sortedPresences.Count > 0 && sortedPresences[0].UserId == NakamaClient.Instance.UserId;
        }
        bool isMultiplayerMode = GameManager.gameMode == GameModeType.MULTIPLAYER;

        //Debug.Log($"GameFinished > isPlayer1Local: {isPlayer1Local}, isMultiplayerMode: {isMultiplayerMode}");
        //Debug.Log($"GameFinished > Player 1: {playerScore}, Player 2: {opponentScore} Sending event");

        if (playerScore >= pointsToWin)
        {
            if (!isMultiplayerMode || (isMultiplayerMode && isPlayer1Local))
                IFrameBridge.instance.PostMatchResult("won", playerScore, opponentScore);
            else
                IFrameBridge.instance.PostMatchResult("lost", opponentScore, playerScore);
        }
        else if (opponentScore >= pointsToWin)
        {
            if (!isMultiplayerMode)
                IFrameBridge.instance.PostMatchResult("lost", playerScore, opponentScore);
            else if (isMultiplayerMode && !isPlayer1Local)
                IFrameBridge.instance.PostMatchResult("won", opponentScore, playerScore);
            else
                IFrameBridge.instance.PostMatchResult("lost", playerScore, opponentScore);

        }
        else
        {
            // Draw case
            // IFrameBridge.instance.PostMatchResult("draw", playerScore, opponentScore);
            IFrameBridge.instance.PostMatchResult("draw", isPlayer1Local ? playerScore : opponentScore, isPlayer1Local ? opponentScore : playerScore);

        }

        //Debug.Log($"GameFinished > MatchResult: {matchResultStr}");
        SceneManager.LoadScene("menu");  

    }


    public void PlayerHit(int playerNumber)
    {

        if (playerNumber == 0)
        {
            player1Hearts = Mathf.Max(0, player1Hearts - damagePerHit);
            Debug.Log($"Player 1 hit! Hearts remaining: {player1Hearts:F1} | {player1Hearts:F0}");
            heartDisplays[0].UpdateHearts(player1Hearts);
        }
        else if (playerNumber == 1)
        {
            player2Hearts = Mathf.Max(0, player2Hearts - damagePerHit);
            Debug.Log($"Player 2 hit! Hearts remaining: {player2Hearts:F1} | {player2Hearts:F0}");
            heartDisplays[1].UpdateHearts(player2Hearts);
        }

        // UpdateHeartDisplays();

    }

    private void UpdateHeartDisplays()
    {
        foreach (HeartDisplay display in heartDisplays)
        {
            if (display.playerNumber == 0)
                display.UpdateHearts(player1Hearts);
            else if (display.playerNumber == 1)
                display.UpdateHearts(player2Hearts);
        }
    }

}
