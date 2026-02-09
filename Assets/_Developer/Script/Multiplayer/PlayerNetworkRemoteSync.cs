using System;
using System.Collections.Generic;
using System.Linq; // ADD THIS LINE
using System.Text;
using UnityEngine;
using Nakama;
using Nakama.TinyJson;

/// <summary>
/// Syncs a remotely connected player's character using received network data.
/// Attach this component to remote player GameObjects.
/// </summary>
public class PlayerNetworkRemoteSync : MonoBehaviour
{
    [Header("Sync Settings")]
    [Tooltip(
        "The speed (in seconds) in which to smoothly interpolate to the player's actual position when receiving corrected data.")]
    public float LerpTime = 0.05f;

    public RemotePlayerNetworkData NetworkData;

    private BowController bowController;
    private Transform bowTransform;
    private float lerpTimer;
    private float lerpTimerRotation; // ADD: Separate timer for rotation
    private Vector3 lerpFromPosition;
    private Vector3 lerpToPosition;
    private float lerpFromRotation;
    private float lerpToRotation;
    private bool lerpPosition;
    private bool lerpRotation;
    public bool rotationEnabled = true; // Track if rotation should be active
    private float lastReceivedAutoRotationAngle = float.MinValue; // Track last received angle (use MinValue to indicate no previous value)
    private float lastReceivedRotationZ = 0f; // Track last received rotation Z
    private float estimatedRotationDirection = 1f; // Track estimated rotation direction (1 = up, -1 = down)

    private void Start()
    {
        // CRITICAL: Don't attach this component to arrows - only to player GameObjects
        if (GetComponent<Arrow>() != null)
        {
            //Debug.LogWarning(
               // $"[PlayerNetworkRemoteSync] This component should not be on Arrow GameObject: {gameObject.name}. Removing component.");
            DestroyImmediate(this);
            return;
        }

        // CRITICAL: Only allow this component on root GameObject or GameObject with BowController
        // If BowController is on a child, this component should be on the root, not the child
        bool hasBowControllerOnSelf = GetComponent<PlayerController>() != null ||
                                      GetComponent<OpponentController>() != null ||
                                      GetComponent<BowController>() != null;

        // If this is a child object (has parent) and doesn't have BowController, destroy it IMMEDIATELY
        // The component should only be on the root GameObject
        if (transform.parent != null && !hasBowControllerOnSelf)
        {
            //Debug.LogWarning(
               // $"[PlayerNetworkRemoteSync] Component found on child GameObject '{gameObject.name}' without BowController. " +
               // $"This component should only be on the root player GameObject. Removing from child IMMEDIATELY.");
            DestroyImmediate(this);
            return; // Don't access 'this' after DestroyImmediate
        }

        // Declare variables at method level so they're accessible throughout
        PlayerController playerController = null;
        OpponentController opponentController = null;

        // Get the appropriate controller component - search in hierarchy like PlayerNetworkLocalSync does
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

        // If not found on this GameObject, search in parent/root ONLY (not children)
        if (bowController == null)
        {
            // Search up the hierarchy for BowController (parent only, not children)
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
            Debug.LogError(
                $"[PlayerNetworkRemoteSync] BowController component not found on GameObject: {gameObject.name}!");
           // Debug.LogError(
            //    $"[PlayerNetworkRemoteSync] Available components: {string.Join(", ", GetComponents<MonoBehaviour>().Select(c => c.GetType().Name))}");
            //Debug.LogError(
            //    $"[PlayerNetworkRemoteSync] Searched parent hierarchy and root, but still not found. This component should be on the root player GameObject.");
            enabled = false;
            return;
        }

        // CRITICAL: Verify this component is on the same GameObject as BowController or its parent
        // If BowController is on a child, this component should be on the root, not the child
        // If BowController is on a parent, that's OK - component can be on child
        // But if BowController is on a sibling or unrelated object, that's wrong
        if (bowController.transform != transform &&
            !bowController.transform.IsChildOf(transform) &&
            !transform.IsChildOf(bowController.transform))
        {
            // BowController is neither on this GameObject, nor a child, nor a parent
            // This means they're siblings or unrelated - this is wrong
            //Debug.LogWarning(
               // $"[PlayerNetworkRemoteSync] BowController found on unrelated GameObject '{bowController.transform.name}' " +
               // $"but component is on '{gameObject.name}'. Component should be on the same GameObject as BowController. " +
               // $"Removing from '{gameObject.name}'.");
            Destroy(this);
            return;
        }

        // If BowController is on a child GameObject, log a warning but allow it
        // (component should ideally be on same GameObject, but parent is acceptable)
        if (bowController.transform != transform && bowController.transform.IsChildOf(transform))
        {
            //Debug.LogWarning(
               // $"[PlayerNetworkRemoteSync] BowController found on child '{bowController.transform.name}' " +
               // $"but component is on parent '{gameObject.name}'. " +
               // $"Consider moving component to '{bowController.transform.name}' for better performance.");
        }

        // Use bowParent for rotation if available (the actual rotating transform),
        // otherwise use the root GameObject transform
        if (bowController.bowParent != null)
        {
            bowTransform = bowController.bowParent;
            //Debug.Log($"[PlayerNetworkRemoteSync] Using bowParent transform for rotation sync: {bowTransform.name}");
        }
        else
        {
            bowTransform = transform;
            //Debug.Log($"[PlayerNetworkRemoteSync] Using root transform (bowParent is null): {bowTransform.name}");
        }

        // Initialize rotation as enabled
        rotationEnabled = true;
        lastReceivedAutoRotationAngle = float.MinValue; // Initialize to indicate no previous value yet
        lastReceivedRotationZ = bowTransform != null ? bowTransform.rotation.eulerAngles.z : 0f;
        estimatedRotationDirection = 1f; // Initialize direction (default: going up)

        // CRITICAL: Initialize currentAutoRotationAngle if not set
        if (bowController.currentAutoRotationAngle == 0f)
        {
            bowController.currentAutoRotationAngle = bowController.maxUpAngle;
           // Debug.Log($"[RemoteSync] Initialized currentAutoRotationAngle to maxUpAngle: {bowController.maxUpAngle}");
        }

        // Subscribe to match state events
        if (ArrowduelNakamaClient.Instance != null)
        {
            ArrowduelNakamaClient.Instance.MatchStateReceived += EnqueueOnReceivedMatchState;
        }

        // Only enable in multiplayer mode
        if (GameManager.gameMode != GameModeType.MULTIPLAYER)
        {
            enabled = false;
            return;
        }

        //Debug.Log(
           // $"[PlayerNetworkRemoteSync] Initialized for remote player sync - playerID: {bowController.playerID}, " +
          //    $"GameObject: {gameObject.name}, " +
          //    $"BowController on: {bowController.gameObject.name}, " +
          //    $"NetworkData set: {(NetworkData != null && NetworkData.User != null ? "YES" : "NO - will be set later")}");

        // If NetworkData is not set yet, log a warning (it will be set by GameManager after Start())
        if (NetworkData == null || NetworkData.User == null)
        {
            //Debug.LogWarning(
              //  $"[PlayerNetworkRemoteSync] NetworkData not set yet for playerID: {bowController.playerID}. " +
                //$"GameManager should set this after Start(). Component will wait for NetworkData to be set.");
        }
    }

    private void OnDestroy()
    {
        if (ArrowduelNakamaClient.Instance != null)
        {
            ArrowduelNakamaClient.Instance.MatchStateReceived -= EnqueueOnReceivedMatchState;
        }
    }


    private void LateUpdate()
    {
        // Add debug log every 60 frames to track state
        if (Time.frameCount % 60 == 0 && bowController != null)
        {
            /*Debug.Log($"[RemoteSync] LateUpdate - playerID: {bowController.playerID}, " +
                $"rotationEnabled: {rotationEnabled}, " +
                $"lerpRotation: {lerpRotation}, " +
                $"bowController: {(bowController != null ? "OK" : "NULL")}, " +
                $"bowTransform: {(bowTransform != null ? "OK" : "NULL")}, " +
                $"currentAngle: {bowController.currentAutoRotationAngle:F1}°, " +
                $"direction: {estimatedRotationDirection}, " +
                $"lastReceivedAngle: {(lastReceivedAutoRotationAngle == float.MinValue ? "NONE" : lastReceivedAutoRotationAngle.ToString("F1"))}");*/
        }

        // Interpolate position if needed
        if (lerpPosition)
        {
            Transform posTarget = (bowTransform == bowController.bowParent) ? transform : bowTransform;
            posTarget.position = Vector3.Lerp(lerpFromPosition, lerpToPosition, lerpTimer / LerpTime);
            lerpTimer += Time.deltaTime;

            if (lerpTimer >= LerpTime)
            {
                posTarget.position = lerpToPosition;
                lerpPosition = false;
            }
        }

        // Interpolate rotation if needed AND rotation is enabled


        // ADD THIS: Continue rotating based on last received autoRotationAngle when not lerping
        if (rotationEnabled && !lerpRotation && bowController != null && bowTransform != null)
        {
            // Check if we have a valid angle to work with
            if (lastReceivedAutoRotationAngle == float.MinValue)
            {
                // No network update received yet - use current angle from BowController
                // This ensures rotation starts even before first network update
                if (bowController.currentAutoRotationAngle == 0f)
                {
                    // Initialize to maxUpAngle if not set
                    bowController.currentAutoRotationAngle = bowController.maxUpAngle;
                }
                lastReceivedAutoRotationAngle = bowController.currentAutoRotationAngle;
                // Only log once to avoid spam
                if (Time.frameCount % 300 == 0)
                {
                    //Debug.Log($"[RemoteSync] No network update yet - using initial angle: {bowController.currentAutoRotationAngle:F1}°");
                }
            }

            // Continue auto-rotation using the last received autoRotationAngle
            // This ensures smooth rotation even between network updates
            float autoRotationSpeed = bowController.autoRotationSpeed;

            // Update the angle using estimated direction
            bowController.currentAutoRotationAngle += estimatedRotationDirection * autoRotationSpeed * Time.deltaTime;

            // Clamp and reverse direction if needed (same as BowController.AutoRotate)
            if (bowController.currentAutoRotationAngle >= bowController.maxUpAngle)
            {
                bowController.currentAutoRotationAngle = bowController.maxUpAngle;
                estimatedRotationDirection = -1f; // Reverse to go down
            }
            else if (bowController.currentAutoRotationAngle <= bowController.maxDownAngle)
            {
                bowController.currentAutoRotationAngle = bowController.maxDownAngle;
                estimatedRotationDirection = 1f; // Reverse to go up
            }

            // Apply rotation
            float zRot = bowController.currentAutoRotationAngle + bowController.rotationOffset;
            Quaternion rotation = Quaternion.Euler(0f, 0f, zRot);
            bowTransform.rotation = rotation;

            // Also update frontHand and backHand rotations
            if (bowController.frontHand != null && bowController.backHand != null)
            {
                bowController.frontHand.rotation = rotation;
                bowController.backHand.rotation = rotation * Quaternion.Euler(0f, 0f, -10f);
            }

            // Debug log every 300 frames to track continuous rotation
            if (Time.frameCount % 300 == 0)
            {
                /*Debug.Log($"[RemoteSync] Continuous rotation - playerID: {bowController.playerID}, " +
                    $"angle: {bowController.currentAutoRotationAngle:F1}°, " +
                    $"direction: {estimatedRotationDirection}, " +
                    $"zRot: {zRot:F1}°");*/
            }
        }
        else if (!rotationEnabled)
        {
            // Stop any ongoing rotation interpolation when rotation is disabled
            lerpRotation = false;
            lerpTimerRotation = 0; // Reset rotation timer

            // Ensure rotation stays frozen - don't allow any updates
            // The rotation should remain at the last frozen position
            // This prevents flickering when rotation is disabled

            // Debug log every 300 frames to avoid spam
            if (Time.frameCount % 300 == 0)
            {
                //Debug.Log($"[RemoteSync] Rotation disabled (frozen) - playerID: {bowController?.playerID ?? -1}, " +
                  //  $"Current rotation: {bowTransform?.rotation.eulerAngles.z ?? 0f:F1}°");
            }
        }
        else if (bowController == null)
        {
            Debug.LogError("[RemoteSync] bowController is NULL in LateUpdate!");
        }
    }


    /// <summary>
    /// Passes execution of the event handler to the main unity thread.
    /// </summary>
    private void EnqueueOnReceivedMatchState(IMatchState matchState)
    {
        var mainThread = UnityMainThreadDispatcher.Instance();
        mainThread.Enqueue(() => OnReceivedMatchState(matchState));
    }

    /// <summary>
    /// Called when receiving match data from the Nakama server.
    /// </summary>
    private void OnReceivedMatchState(IMatchState matchState)
    {
        // Always log when receiving messages to track network flow (but limit spam)
        if (matchState.OpCode == ArrowduelNetworkManager.OPCODE_POSITION_ROTATION || Time.frameCount % 300 == 0)
        {
            Debug.Log($"[RemoteSync] OnReceivedMatchState - playerID: {bowController?.playerID ?? -1}, " +
                $"OpCode: {matchState.OpCode}, " +
                $"Received SessionId: {matchState.UserPresence?.SessionId ?? "NULL"}, " +
                $"Expected SessionId: {NetworkData?.User?.SessionId ?? "NULL"}, " +
                $"NetworkData set: {(NetworkData != null && NetworkData.User != null ? "YES" : "NO")}"); 
        }

        // If NetworkData is not set, we can't identify which player this is for
        if (NetworkData == null || NetworkData.User == null)
        {
            // Log warning more frequently to catch setup issues
            if (Time.frameCount % 300 == 0)
            {
                //Debug.LogWarning($"[RemoteSync] NetworkData or User is NULL for playerID: {bowController?.playerID ?? -1} - ignoring message. " +
                  //  $"GameManager should set NetworkData after Start(). GameObject: {gameObject.name}");
            }
            return;
        }

        // If the incoming data is not related to this remote player, ignore it
        if (matchState.UserPresence == null || matchState.UserPresence.SessionId != NetworkData.User.SessionId)
        {
            // Log mismatch more frequently for debugging
            if (matchState.OpCode == ArrowduelNetworkManager.OPCODE_POSITION_ROTATION || Time.frameCount % 300 == 0)
            {
                //Debug.Log($"[RemoteSync] SessionId mismatch - ignoring. " +
                  //  $"Received: {matchState.UserPresence?.SessionId ?? "NULL"}, " +
                  //  $"Expected: {NetworkData.User.SessionId}, " +
                   // $"playerID: {bowController?.playerID ?? -1}, " +
                   // $"OpCode: {matchState.OpCode}");
            }
            return;
        }

        // Log when processing rotation updates - ALWAYS log to track successful receipt
        if (matchState.OpCode == ArrowduelNetworkManager.OPCODE_POSITION_ROTATION)
        {
            Debug.Log($"[RemoteSync] Processing rotation update - playerID: {bowController?.playerID ?? -1}, " +
                $"OpCode: {matchState.OpCode}, " +
                $"SessionId match: YES");
        }

        // Decide what to do based on the Operation Code
        switch (matchState.OpCode)
        {
            case ArrowduelNetworkManager.OPCODE_POSITION_ROTATION:
                UpdatePositionAndRotationFromState(matchState.State);
                break;
            case ArrowduelNetworkManager.OPCODE_INPUT:
                SetInputFromState(matchState.State);
                break;
            // REMOVED: OPCODE_ROTATION_STOP and OPCODE_ROTATION_START
            // These are now handled by ArrowduelNetworkManager which routes by playerID
            // instead of SessionId, ensuring the correct component receives the event
            default:
                // Don't log unknown opcodes to avoid spam
                break;
        }
    }

    /// <summary>
    /// Converts a byte array of a UTF8 encoded JSON string into a Dictionary.
    /// </summary>
    private IDictionary<string, string> GetStateAsDictionary(byte[] state)
    {
        return Encoding.UTF8.GetString(state).FromJson<Dictionary<string, string>>();
    }

    /// <summary>
    /// Sets the appropriate input values on the BowController based on incoming state data.
    /// </summary>
    private void SetInputFromState(byte[] state)
    {
        var stateDictionary = GetStateAsDictionary(state);

        bool isCharging = bool.Parse(stateDictionary["isCharging"]);
        float currentForce = float.Parse(stateDictionary["currentForce"]);
        int fillDirection = int.Parse(stateDictionary["fillDirection"]);

        //Debug.Log(
        // $"[PlayerNetworkRemoteSync] SetInputFromState - isCharging: {isCharging}, currentForce: {currentForce}, fillDirection: {fillDirection}");

        // Apply input to remote player
        bowController.isCharging = isCharging;
        bowController.currentForce = currentForce;
        bowController.fillDirection = fillDirection;

        // Update force meter UI if charging
        if (isCharging && bowController.fillbarParentObj != null)
        {
            bowController.fillbarParentObj.SetActive(true);
            if (bowController.fillbarImage != null)
            {
                bowController.fillbarImage.fillAmount = currentForce / bowController.maxForce;
            }
        }
        else if (!isCharging && bowController.fillbarParentObj != null)
        {
            bowController.fillbarParentObj.SetActive(false);
        }
    }

    /// <summary>
    /// Updates the player's position and rotation based on incoming state data.
    /// </summary>
    private void UpdatePositionAndRotationFromState(byte[] state)
    {
        var stateDictionary = GetStateAsDictionary(state);

        var position = new Vector3(
            float.Parse(stateDictionary["position.x"]),
            float.Parse(stateDictionary["position.y"]),
            float.Parse(stateDictionary["position.z"])
        );

        float rotationZ = float.Parse(stateDictionary["rotationZ"]);
        float autoRotationAngle = float.Parse(stateDictionary["autoRotationAngle"]);
        float networkDirection = stateDictionary.ContainsKey("autoRotationDirection")
    ? float.Parse(stateDictionary["autoRotationDirection"])
    : 1f;

        bool remoteIsCharging = stateDictionary.ContainsKey("isCharging")
    && stateDictionary["isCharging"] == "true";

        // Backup: sync rotationEnabled from charging state
        // Fixes race condition if OPCODE_ROTATION_STOP/START arrives late
        if (remoteIsCharging && rotationEnabled)
        {
            rotationEnabled = false;
            lerpRotation = false;
            lerpTimerRotation = 0;
        }
        
        // Debug log for rotation sync testing - ALWAYS log to track network updates
        /*Debug.Log($"[RemoteSync] Received - playerID: {bowController?.playerID ?? -1}, " +
                  $"rotationZ: {rotationZ:F1}°, " +
                  $"autoAngle: {autoRotationAngle:F1}°, " +
                  $"rotationEnabled: {rotationEnabled}, " +
                  $"currentRotation: {bowTransform?.rotation.eulerAngles.z ?? 0f:F1}°, " +
                  $"NetworkData set: {(NetworkData != null && NetworkData.User != null ? "YES" : "NO")}");*/

        // Begin lerping to the corrected position
        Transform positionTransform = (bowTransform == bowController.bowParent) ? transform : bowTransform;
        lerpFromPosition = positionTransform.position;
        lerpToPosition = position;
        lerpTimer = 0;
        lerpPosition = true;

        // Only process rotation updates if rotation is enabled
        if (rotationEnabled)
        {
            // Just correct the internal angle and direction — no lerp
            bowController.currentAutoRotationAngle = autoRotationAngle;
            estimatedRotationDirection = networkDirection;  // from network, not estimated
            lastReceivedAutoRotationAngle = autoRotationAngle;
            lastReceivedRotationZ = rotationZ;
            // Do NOT set lerpRotation = true
        }
        else
        {
            // When rotation is disabled (charging), completely ignore rotation updates
            lerpRotation = false;
            lerpTimerRotation = 0; // Reset rotation timer
        }
    }

    /// <summary>
    /// Handles rotation stop event from network.
    /// Called by ArrowduelNetworkManager when rotation stop event is received.
    /// </summary>
    public void HandleRotationStop(byte[] state)
    {
        string json = Encoding.UTF8.GetString(state);
        var data = JsonUtility.FromJson<ArrowduelNetworkManager.RotationControlData>(json);

        // Stop rotation for this remote player
        rotationEnabled = false;
        lerpRotation = false; // Stop any ongoing rotation interpolation
        lerpTimerRotation = 0; // Reset rotation timer

        // Freeze the current rotation - don't allow any further rotation updates
        float frozenRotation = bowTransform.rotation.eulerAngles.z;
        lerpFromRotation = frozenRotation;
        lerpToRotation = frozenRotation;

        // Ensure rotation is completely frozen
        if (bowTransform != null)
        {
            Quaternion frozenQuat = Quaternion.Euler(0f, 0f, frozenRotation);
            bowTransform.rotation = frozenQuat;

            // Also freeze hand rotations
            if (bowController.frontHand != null && bowController.backHand != null)
            {
                bowController.frontHand.rotation = frozenQuat;
                bowController.backHand.rotation = frozenQuat * Quaternion.Euler(0f, 0f, -10f);
            }
        }

        // Debug log for rotation stop
        //Debug.Log($"[RemoteSync] Rotation STOPPED - playerID: {bowController.playerID}, " +
           // $"Frozen at: {frozenRotation:F1}°, rotationEnabled: {rotationEnabled}");
    }


    /// <summary>
    /// Handles rotation start event from network.
    /// Called by ArrowduelNetworkManager when rotation start event is received.
    /// </summary>
    public void HandleRotationStart(byte[] state)
    {
        string json = Encoding.UTF8.GetString(state);
        var data = JsonUtility.FromJson<ArrowduelNetworkManager.RotationControlData>(json);

        // Start rotation for this remote player
        rotationEnabled = true;
        lerpTimerRotation = 0; // Reset rotation timer when starting
        lerpRotation = false; // Clear any pending lerp

        // Resume rotation from current position
        // Use the last received autoRotationAngle if available, otherwise use current angle
        if (lastReceivedAutoRotationAngle != float.MinValue)
        {
            bowController.currentAutoRotationAngle = lastReceivedAutoRotationAngle;
        }
        else if (bowTransform != null)
        {
            // Calculate current angle from rotation
            float currentZ = bowTransform.rotation.eulerAngles.z;
            bowController.currentAutoRotationAngle = currentZ - bowController.rotationOffset;
            // Clamp to valid range
            bowController.currentAutoRotationAngle = Mathf.Clamp(bowController.currentAutoRotationAngle,
                bowController.maxDownAngle, bowController.maxUpAngle);
        }

        // Debug log for rotation start
        //Debug.Log($"[RemoteSync] Rotation STARTED - playerID: {bowController.playerID}, " +
           // $"Resuming from: {bowTransform.rotation.eulerAngles.z:F1}°, " +
           // $"currentAutoRotationAngle: {bowController.currentAutoRotationAngle:F1}°, " +
           // $"rotationEnabled: {rotationEnabled}");
    }
}

/// <summary>
/// Data structure to identify which remote player this sync component belongs to.
/// </summary>
[Serializable]
public class RemotePlayerNetworkData
{
    public string MatchId;
    public IUserPresence User;
}
