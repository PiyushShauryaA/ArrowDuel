using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("TESTING >")]
    public bool TestLevel = false;
    public int testLevel = 0;

    [System.Serializable]
    public class Level
    {
        public string levelName;
        public GameObject levelObject;
        public Transform player1Spawn;
        public Transform player2Spawn;
    }

    [Space(10)]
    [SerializeField] private GameObject fadeLevelTransitionPanel;

    [Space(10)]
    public int currentLevelIndex = 0;
    public int lastLevelIndex = 0;
    public int currentThemeIndex = 0;
    public int lastThemeIndex = 0;

    [Space(10)]
    [SerializeField] private GameObject defaultLevel;
    public Level[] levels;
    public GameObject[] availableThemes;

    public List<int> availableLevelIndices = new List<int>();
    public List<int> availableThemeIndices = new List<int>();


    void Awake()
    {
        instance = this;

    }

    void Start()
    {

        if (TestLevel)
        {
            TestLevelRealod();
            return;
        }

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].levelObject != null)
            {
                if (i == 0)
                    continue;
                levels[i].levelObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"Level {i} does not have a levelObject assigned.");
            }
        }

        if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
        {
            LoadNextShuffledLevel();
            ChangeTheme();
        }

    }

    void OnEnable()
    {

        // Initialize available levels list
        for (int i = 0; i < levels.Length; i++)
        {
            availableLevelIndices.Add(i);
        }

        for (int i = 0; i < availableThemes.Length; i++)
        {
            availableThemeIndices.Add(i);
        }

        GameManager.onGameLevelChange += LoadNextStage;

    }

    void OnDisable()
    {
        GameManager.onGameLevelChange -= LoadNextStage;
    }

    public void LevelInit()
    {
       // //Debug.Log($"[RPC]- LevelInit...!");
        LoadNextShuffledLevel();
        ChangeTheme();

    }

    [ContextMenu("-TestLevelRealod")]
    public void TestLevelRealod()
    {
        currentLevelIndex = testLevel;
        levels[currentLevelIndex].levelObject.SetActive(true);
        ResetPlayers();

    }

    private void ChangeTheme()
    {

        if (currentThemeIndex >= 0)
        {
            availableThemes[currentThemeIndex].SetActive(false);
        }

        if (availableThemeIndices.Count == 0)
        {
            for (int i = 0; i < availableThemes.Length; i++)
            {
                if (i != currentThemeIndex)
                {
                    availableThemeIndices.Add(i);
                }
            }
        }

        int index;

        if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
        {
            index = Random.Range(0, availableThemeIndices.Count);
        }
        else
        {
            // Get theme index from LevelManager's own state (synced via match state)
            index = currentThemeIndex;
            if (index < 0 || index >= availableThemeIndices.Count)
                index = 0; // Fallback
        }

       // //Debug.Log($"ChangeTheme - currentThemeIndex: {currentThemeIndex}, index: {index}, Count: {availableThemeIndices.Count}");

        // int randomListIndex = Random.Range(0, availableThemeIndices.Count);
        if (index >= 0 && index < availableThemeIndices.Count)
            currentThemeIndex = availableThemeIndices[index];
        availableThemeIndices.RemoveAt(index);

        availableThemes[currentThemeIndex].SetActive(true);

    }

    public void LoadNextStage()
    {
        // LoadNextShuffledLevel();
        StartCoroutine(LevelTransition());
        ChangeTheme();

    }

    IEnumerator LevelTransition()
    {
        // Fade out

        fadeLevelTransitionPanel.SetActive(false);
        yield return new WaitForEndOfFrame();

        fadeLevelTransitionPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(0.35f);
        GameManager.instance.gameState = GameState.Gameplay;
        LoadNextShuffledLevel();
        /*yield return new WaitForSeconds(.25f);
        fadeLevelTransitionPanel.SetActive(false);*/
        //NextLevel();
        // Fade in
    }

    public void LoadNextShuffledLevel()
    {

        if (defaultLevel.activeInHierarchy)
            defaultLevel.SetActive(false);

        if (currentLevelIndex >= 0)
        {
            levels[currentLevelIndex].levelObject.SetActive(false);
        }

        if (availableLevelIndices.Count == 0)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                if (i != currentLevelIndex)
                {
                    availableLevelIndices.Add(i);
                }
            }
        }

        // int randomListIndex = Random.Range(0, availableLevelIndices.Count);
        // lastLevelIndex = Random.Range(0, availableLevelIndices.Count);
        int index;

        if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
        {
            index = Random.Range(0, availableLevelIndices.Count);
        }
        else
        {
            // Get level index from LevelManager's own state (synced via match state)
            index = currentLevelIndex;
            if (index < 0 || index >= availableLevelIndices.Count)
                index = 0; // Fallback
        }

       // //Debug.Log($"ChangeLevel - currentLevelIndex: {currentLevelIndex}, index: {index}, Count: {availableLevelIndices.Count}");

        if (index >= 0 && index < availableLevelIndices.Count)
            currentLevelIndex = availableLevelIndices[index];
        availableLevelIndices.RemoveAt(index);

        levels[currentLevelIndex].levelObject.SetActive(true);

        ResetPlayers();

    }

    public void ResetPlayers()
    {
        GameObject player1 = GameManager.instance.playerController.gameObject;
        GameObject player2 = GameManager.instance.opponentPlayerController.gameObject;

        if (player1 == null || player2 == null)
            return;

        Vector3 player1Position = levels[currentLevelIndex].player1Spawn.position;
        Vector3 player2Position = levels[currentLevelIndex].player2Spawn.position;

        if (player1 != null && levels[currentLevelIndex].player1Spawn != null)
        {
            if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
            {
                player1.transform.position = player1Position;
            }
            else if (GameManager.gameMode == GameModeType.MULTIPLAYER)
            {
                // No NetworkTransform needed for Nakama - use regular transform
                player1.transform.position = player1Position;
            }

        }

        if (player2 != null && levels[currentLevelIndex].player2Spawn != null)
        {
            if (GameManager.gameMode == GameModeType.SINGLEPLAYER)
            {
                player2.transform.position = player2Position;
            }
            else if (GameManager.gameMode == GameModeType.MULTIPLAYER)
            {
                // No NetworkTransform needed for Nakama - use regular transform
                player2.transform.position = player2Position;
            }

        }
    }


}