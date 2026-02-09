using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Nakama;

/// <summary>
/// Syncs the local player's state across the network by sending frequent network packets 
/// containing relevant information such as position, rotation, and input.
/// Attach this component to the local player GameObject.
/// </summary>
public class PlayerNetworkLocalSync : MonoBehaviour
{
    [Header("Sync Settings")]
    [Tooltip("How often to send the player's position and rotation across the network, in seconds.")]
    public float StateFrequency = 0.1f;

    [Tooltip("Send input changes immediately when they occur.")]
    public bool SendInputImmediately = true;

    private BowController bowController;
    private PlayerController playerController;
    private OpponentController opponentController;
    private Transform bowTransform;
    private float stateSyncTimer;
    
    // Track input state to detect changes
    private bool lastIsCharging;
    private float lastCurrentForce;
    private bool inputChanged;

    private void Start()
    {
        // CRITICAL: Don't attach this component to arrows - only to player GameObjects
        if (GetComponent<Arrow>() != null)
        {
            Debug.LogWarning(
                $"[PlayerNetworkLocalSync] This component should not be on Arrow GameObject: {gameObject.name}. Removing component.");
            Destroy(this);
            return;
        }

        // Get the appropriate controller component
        // Try PlayerController first (inherits from BowController)
        playerController = GetComponent<PlayerController>();
        
        if (playerController != null)
        {
            bowController = playerController; // PlayerController IS a BowController
        }
        else
        {
            // Try OpponentController (also inherits from BowController)
            opponentController = GetComponent<OpponentController>();
            if (opponentController != null)
            {
                bowController = opponentController; // OpponentController IS a BowController
            }
            else
            {
                // Try direct BowController component
                bowController = GetComponent<BowController>();
            }
        }
        
        // If not found on this GameObject, search in parent/root
        if (bowController == null)
        {
            // Search up the hierarchy for BowController
            Transform current = transform.parent;
            while (current != null && bowController == null)
            {
                playerController = current.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    bowController = playerController;
                    break;
                }
                
                opponentController = current.GetComponent<OpponentController>();
                if (opponentController != null)
                {
                    bowController = opponentController;
                    break;
                }
                
                bowController = current.GetComponent<BowController>();
                if (bowController != null)
                {
                    break;
                }
                
                current = current.parent;
            }
            
            // If still not found, try root
            if (bowController == null && transform.root != transform)
            {
                playerController = transform.root.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    bowController = playerController;
                }
                else
                {
                    opponentController = transform.root.GetComponent<OpponentController>();
                    if (opponentController != null)
                    {
                        bowController = opponentController;
                    }
                    else
                    {
                        bowController = transform.root.GetComponent<BowController>();
                    }
                }
            }
        }
        
        if (bowController == null)
        {
            Debug.Log($"[PlayerNetworkLocalSync] BowController component not found on GameObject: {gameObject.name}!");
           enabled = false;
            return;
        }
        
        // Debug log to verify controller was found
        Debug.Log($"[LocalSync] Start - Found BowController: {bowController.GetType().Name}, " +
            $"playerID: {bowController.playerID}, " +
            $"GameObject: {bowController.gameObject.name}");

        // Get transform reference - use bowParent if available (the actual rotating transform),
        // otherwise use the root GameObject where BowController is located
        // This ensures we sync the correct transform even if this component is on a child GameObject
        if (bowController.bowParent != null)
        {
            bowTransform = bowController.bowParent;
            //Debug.Log($"[PlayerNetworkLocalSync] Using bowParent transform for rotation sync: {bowTransform.name}");
        }
        else
        {
            bowTransform = bowController.transform;
            //Debug.Log($"[PlayerNetworkLocalSync] Using BowController root transform (bowParent is null): {bowTransform.name}");
        }
        
        // Initialize input tracking
        lastIsCharging = bowController.isCharging;
        lastCurrentForce = bowController.currentForce;
        inputChanged = false;

        // Only enable in multiplayer mode - but check after a frame to ensure GameManager.gameMode is set
        StartCoroutine(CheckGameModeAndEnable());
    }

    private IEnumerator CheckGameModeAndEnable()
    {
        // Wait a frame to ensure GameManager.gameMode is set
        yield return null;
        
        // Only enable in multiplayer mode
        if (GameManager.gameMode != GameModeType.MULTIPLAYER)
        {
            //Debug.Log($"[PlayerNetworkLocalSync] Disabling - GameMode is {GameManager.gameMode}, not MULTIPLAYER");
            enabled = false;
            yield break;
        }

        // Ensure component is enabled
        enabled = true;
        //Debug.Log($"[PlayerNetworkLocalSync] Initialized for local player sync - enabled: {enabled}, playerID: {bowController.playerID}");
    }

    private void LateUpdate()
    {
        // Only sync in multiplayer mode
        if (GameManager.gameMode != GameModeType.MULTIPLAYER)
            return;

        // Check if we have authority (only sync if we control this player)
        if (!HasAuthority())
            return;

        // Send position and rotation every StateFrequency seconds
        if (stateSyncTimer <= 0)
        {
            SendPositionAndRotation();
            stateSyncTimer = StateFrequency;
        }
        stateSyncTimer -= Time.deltaTime;

        // Check for input changes
        if (SendInputImmediately)
        {
            CheckInputChanges();
            if (inputChanged)
            {
                SendInput();
                inputChanged = false;
            }
        }
    }

    /// <summary>
    /// Checks if this component has authority to sync (i.e., controls this player).
    /// </summary>
    private bool HasAuthority()
    {
        if (ArrowduelNakamaClient.Instance == null || ArrowduelNakamaClient.Instance.CurrentMatch == null)
        {
            if (Time.frameCount % 300 == 0)
            {
                Debug.LogWarning($"[LocalSync] HasAuthority - Instance or Match is NULL - playerID: {bowController?.playerID ?? -1}");
            }
            return false;
        }

        // Determine if we're player 1 (host) or player 2
        bool isPlayer1 = ArrowduelNakamaClient.Instance.IsHost;

        // Check playerID directly instead of relying on component type
        // Player 1 controls left player (playerID == 0)
        // Player 2 controls right player (playerID == 1)
        bool hasAuthority = false;
        if (isPlayer1 && bowController.playerID == 0)
            hasAuthority = true;
        else if (!isPlayer1 && bowController.playerID == 1)
            hasAuthority = true;

        // Debug log to track authority
        if (Time.frameCount % 300 == 0)
        {
            //Debug.Log($"[LocalSync] HasAuthority - playerID: {bowController.playerID}, " +
              //  $"isPlayer1: {isPlayer1}, hasAuthority: {hasAuthority}");
        }

        return hasAuthority;
    }

    /// <summary>
    /// Checks for input changes and sets inputChanged flag.
    /// </summary>
    private void CheckInputChanges()
    {
        bool currentIsCharging = bowController.isCharging;
        float currentForce = bowController.currentForce;

        if (currentIsCharging != lastIsCharging || Mathf.Abs(currentForce - lastCurrentForce) > 0.01f)
        {
            inputChanged = true;
            lastIsCharging = currentIsCharging;
            lastCurrentForce = currentForce;
        }
    }

    /// <summary>
    /// Sends the player's current position and rotation across the network.
    /// </summary>
    private void SendPositionAndRotation()
    {
        if (ArrowduelNakamaClient.Instance == null || ArrowduelNakamaClient.Instance.CurrentMatch == null)
            return;

        var matchId = ArrowduelNakamaClient.Instance.CurrentMatch.Id;
        
        // Send current position and rotation
        // Note: When charging, rotation should be frozen locally, so we send the frozen rotation value
        // The remote player will ignore rotation updates when rotationEnabled = false
        var json = MatchDataJson.PositionAndRotation(
            bowTransform.position,
            bowTransform.rotation.eulerAngles.z,
            bowController.currentAutoRotationAngle
        );

        // Debug log for rotation sync testing
        /*Debug.Log($"[LocalSync] Sending - playerID: {bowController.playerID}, " +
            $"rotationZ: {bowTransform.rotation.eulerAngles.z:F1}°, " +
            $"autoAngle: {bowController.currentAutoRotationAngle:F1}°, " +
            $"isCharging: {bowController.isCharging}");*/

        ArrowduelNakamaClient.Instance.SendMatchStateAsync(
            ArrowduelNetworkManager.OPCODE_POSITION_ROTATION,
            json
        );
    }

    /// <summary>
    /// Sends the player's current input state across the network.
    /// </summary>
    private void SendInput()
    {
        if (ArrowduelNakamaClient.Instance == null || ArrowduelNakamaClient.Instance.CurrentMatch == null)
            return;

        var matchId = ArrowduelNakamaClient.Instance.CurrentMatch.Id;
        var json = MatchDataJson.Input(
            bowController.isCharging,
            bowController.currentForce,
            bowController.fillDirection
        );

        ArrowduelNakamaClient.Instance.SendMatchStateAsync(
            ArrowduelNetworkManager.OPCODE_INPUT,
            json
        );
    }
}
