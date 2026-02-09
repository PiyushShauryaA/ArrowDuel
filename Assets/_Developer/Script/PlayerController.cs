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
            // Remote players are fully controlled by PlayerNetworkRemoteSync — skip all local input/rotation
            if (isRemotePlayer)
            {
                return;
            }

            // Only the host controls the left player (playerID == 0)
            bool isPlayer1 = ArrowduelNakamaClient.Instance != null && ArrowduelNakamaClient.Instance.IsHost;
            if (isPlayer1 && playerID == 0)
            {
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
