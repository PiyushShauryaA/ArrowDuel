using System.Linq;
using UnityEngine;
using Nakama;

public class PlayerController : BowController
{
    private void Start()
    {
        hasRemoteSync = GetComponent<PlayerNetworkRemoteSync>() != null;
        hasLocalSync = GetComponent<PlayerNetworkLocalSync>() != null;
        // Set controller reference for multiplayer
        if (GameManager.gameMode == GameModeType.MULTIPLAYER)
        {
            // Determine if we're player 1 (host)
            // NAYA (lagao):
            bool isPlayer1 = ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.IsHost;

            if (!isPlayer1)
            {
                GameManager.instance.SetPlayerController(this, false);
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
                //Debug.Log($"hasRemoteSync111111: {hasRemoteSync}, hasLocalSync: {hasLocalSync}");
                // This is a remote player - rotation is handled by PlayerNetworkRemoteSync
                // Don't call UpdatePlayerBehavior() as it would call AutoRotate()
                return;
            }
            
            isHostPlayer = ArrowduelNakamaClient.Instance.IsHost;
            // Determine if we're player 1 (host) - only host handles input in multiplayer
            bool isPlayer1 = ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.IsHost;
            //Debug.Log($"isPlayer1: {isPlayer1}");
            // Use the same UserId-based logic from SpawnPlayerNakama:
                // bool isPlayer1 = false;
                // if (ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.CurrentMatch != null)
                // {
                //     var presences = ArrowduelNakamaClient.Instance.CurrentMatch.Presences;
                //     var allUserIds = presences.Select(p => p.UserId).ToList();
                //     string currentUserId = ArrowduelNakamaClient.Instance.Session?.UserId;
                //     if (!string.IsNullOrEmpty(currentUserId) && !allUserIds.Contains(currentUserId))
                //         allUserIds.Add(currentUserId);
                //     allUserIds.Sort();
                //     isPlayer1 = allUserIds.Count > 0 && allUserIds[0] == currentUserId;
                // }

            // Player 1 (host) controls left player (playerID == 0)
            // Player 2 (non-host) controls right player (playerID == 1)
            if (isPlayer1 && playerID == 0)
            {
                //Debug.Log($"isPlayer122222: {isPlayer1}, playerID: {playerID}");
                HandleInput();
                UpdatePlayerBehavior(); // Critical: Enable bow rotation and force meter updates
            }
            
            else if (hasLocalSync && playerID == 0)
            {
                //Debug.Log($"hasLocalSync333333: {hasLocalSync}, playerID: {playerID}");
                // Fallback: If we have local sync and this is player 1, allow rotation
                // This ensures rotation works even if network checks fail
                HandleInput();
                UpdatePlayerBehavior();
            } 
            
        }
        else
        {
            //Debug.Log($"else444444: {playerID}");
            HandleInput();
            UpdatePlayerBehavior();
        }
    }

    public void UpdatePlayerBehavior()
    {
        //Debug.Log($"UpdatePlayerBehavior555555: {playerType}, playerID: {playerID}");
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

}
