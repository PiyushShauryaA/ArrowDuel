using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;

public class IFrameBridge : MonoBehaviour
{
    public static IFrameBridge instance;

    public static string MatchId { get; private set; }
    public static string PlayerId { get; private set; }
    public static string OpponentId { get; private set; }
    public static string region { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void SendMatchResult(string matchId, string playerId, string opponentId, string outcome, int score, int opponentScore, int averagePing, string region);
    [DllImport("__Internal")] private static extern void SendMatchAbort(string message, string error, string errorCode);
    [DllImport("__Internal")] private static extern int GetDeviceType();
    [DllImport("__Internal")] private static extern void SendBuildVersion(string version);
#endif

    [Serializable]
    public class MatchParams
    {
        public string matchId;
        public string playerId;
        public string opponentId;
        public string region;
    }

    public void Awake()
    {
        instance = this;
        // base.Awake();
#if !UNITY_EDITOR
        Debug.unityLogger.logEnabled = false;
#endif
    }

    void Start()
    {
#if UNITY_EDITOR
       // string json = "{\"matchId\":\"match_abc123\",\"playerId\":\"b912345678\",\"opponentId\":\"player_442\",\"region\":\"in\"}";
        //  string json = "{\"matchId\":\"match_abc12345\",\"playerId\":\"player_541\",\"opponentId\":\"player_789\",\"region\":\"in\"}";
        //InitParamsFromJS(json);
        //PostBuildVersion("1.0.0");
#endif
    }

    void OnButtonStart()
    {
#if UNITY_EDITOR
        string json = "{\"matchId\":\"match_abc123\",\"playerId\":\"b912345678\",\"opponentId\":\"player_442\",\"region\":\"in\"}";
        //  string json = "{\"matchId\":\"match_abc12345\",\"playerId\":\"player_541\",\"opponentId\":\"player_789\",\"region\":\"in\"}";
        InitParamsFromJS(json);
        //PostBuildVersion("1.0.0");
#endif
    }

    public void InitParamsFromJS(string json)
    {
        var data = JsonUtility.FromJson<MatchParams>(json);
        MatchId = data.matchId;
        PlayerId = data.playerId;
        OpponentId = data.opponentId;
        region = data.region;

        //Debug.Log($"[IFrame] Match ID: {MatchId}, Player ID: {PlayerId}, Opponent ID: {OpponentId}, region: {region}");

        // Set player name from PlayerId if not already set
        if (string.IsNullOrEmpty(PlayerData.playerName))
        {
            PlayerData.playerName = PlayerId;
        }

        // Check if we need to load GameScene first
        if (SceneManager.GetActiveScene().name != "GameScene")
        {
            //Debug.Log("[IFrame] Loading GameScene first...");
            SceneManager.LoadScene("GameScene");
            StartCoroutine(WaitForGameSceneAndInit());
            return;
        }

        // Initialize game mode
        InitializeGameMode();
    }

    private IEnumerator WaitForGameSceneAndInit()
    {
        // Wait until GameScene is loaded
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "GameScene");
        
        // Wait a frame for GameManager to initialize
        yield return null;
        
        // Now initialize game mode
        InitializeGameMode();
    }

    private void InitializeGameMode()
    {
        // Ensure GameManager exists
        if (GameManager.instance == null)
        {
            Debug.LogError("[IFrame] GameManager.instance is null! Cannot initialize game.");
            return;
        }

        if (PlayerUtils.IsBot(PlayerId) || PlayerUtils.IsBot(OpponentId))
        {
            //Debug.Log("[IFrame] Bot detected, skipping multiplayer connection.");
            
            // Determine AI difficulty
            AiSkillLevels difficulty = AiSkillLevels.Normal;
            if (PlayerId.StartsWith("a9") || OpponentId.StartsWith("a9"))
            {
                difficulty = AiSkillLevels.Easy;
            }
            else if (PlayerId.StartsWith("b9") || OpponentId.StartsWith("b9"))
            {
                difficulty = AiSkillLevels.Hard;
            }

            // Set PlayerData flags - GameManager.Start() will handle spawning
            PlayerData.gameMode = GameModeType.SINGLEPLAYER;
            PlayerData.isAIMode = true;
            PlayerData.aiDifficulty = difficulty;

            // Don't call Bot() directly - let GameManager.Start() handle it
            // This prevents double spawning
            //Debug.Log($"[IFrame] AI mode set. GameManager will spawn players on Start().");
        }
        else
        {
            // Set PlayerData flags for multiplayer
            PlayerData.gameMode = GameModeType.MULTIPLAYER;
            PlayerData.isAIMode = false;

            // Start multiplayer connection with Nakama
            GameManager.instance.WaitingScreenActive();
            //Debug.Log($"Initiating Nakama multiplayer connection in region: {region}");
            if (string.IsNullOrEmpty(region))
                region = "in";
            
            // Ensure NakamaConnectionManager exists
            if (NakamaConnectionManager.Instance == null)
            {
                var managerObj = new GameObject("NakamaConnectionManager");
                managerObj.AddComponent<NakamaConnectionManager>();
            }
            
            NakamaConnectionManager.Instance.ConnectToServer(MatchId, region);
        }
    }

    public void PostMatchResult(string outcome, int score = 10, int opponentScore = 10)
    {
        //Debug.Log($"[IFrame] PostMatchResult outcome: {outcome}, score: {score}, opponentScore: {opponentScore}");
#if UNITY_WEBGL && !UNITY_EDITOR
        SendMatchResult(MatchId, PlayerId, OpponentId, outcome, score, opponentScore, 0, region);
#endif
    }

    public void PostMatchAbort(string message, string error = "", string errorCode = "")
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SendMatchAbort(message, error, errorCode);
#endif
    }

    public static void PostBuildVersion(string version)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SendBuildVersion(version);
#endif
        //Debug.Log($"[IFrame] Sent build version: {version}");
    }

    internal bool IsMobile()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return GetDeviceType() == 1;
#else
        return false;
#endif
    }
}

public static class PlayerUtils
{
    public static bool IsBot(string playerId)
    {
        return playerId.StartsWith("a9") || playerId.StartsWith("b9");
    }

}