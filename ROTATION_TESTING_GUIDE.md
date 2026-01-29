# Rotation Testing Guide

## Debug Logs Added

### 1. PlayerNetworkLocalSync.cs
- Added debug log in `SendPositionAndRotation()` method
- Logs: playerID, rotationZ, autoAngle, isCharging
- Format: `[LocalSync] Sending - playerID: X, rotationZ: Y°, autoAngle: Z°, isCharging: bool`

### 2. PlayerNetworkRemoteSync.cs
- Added debug log in `UpdatePositionAndRotationFromState()` method
- Logs: playerID, rotationZ, autoAngle, rotationEnabled, currentRotation
- Format: `[RemoteSync] Received - playerID: X, rotationZ: Y°, autoAngle: Z°, rotationEnabled: bool, currentRotation: W°`

- Added debug log in `LateUpdate()` method (every 60 frames)
- Logs: playerID, From angle, To angle, Progress, Current rotation
- Format: `[RemoteSync] Lerping - playerID: X, From: Y°, To: Z°, Progress: W, Current: V°`

- Added debug log in `HandleRotationStop()` method
- Logs: playerID, Frozen rotation angle
- Format: `[RemoteSync] Rotation STOPPED - playerID: X, Frozen at: Y°`

- Added debug log in `HandleRotationStart()` method
- Logs: playerID, Resuming rotation angle
- Format: `[RemoteSync] Rotation STARTED - playerID: X, Resuming from: Y°`

## Testing Script Created

### RotationTestDisplay.cs
- Location: `Assets/_Developer/Script/Testing/RotationTestDisplay.cs`
- Purpose: Displays real-time rotation status for both players on screen
- Shows: Sync type (LOCAL/REMOTE), Rotation angle, AutoAngle, Rotation enabled status, Charging status

### How to Use RotationTestDisplay:
1. Create a GameObject in your scene (e.g., "RotationTestDisplay")
2. Add the `RotationTestDisplay` component to it
3. Assign `GameManager.instance` to the `gameManager` field
4. Create two UI Text components (TextMeshProUGUI) for player1StatusText and player2StatusText
5. Assign the Text components to the script fields
6. The script will automatically update the display every frame

## Expected Console Output

### Screen 1 (Player 1 - Host):

**When rotating:**
```
[LocalSync] Sending - playerID: 0, rotationZ: 45.2°, autoAngle: -44.8°, isCharging: False
[RemoteSync] Received - playerID: 1, rotationZ: 135.5°, autoAngle: 45.5°, rotationEnabled: True, currentRotation: 135.0°
[RemoteSync] Lerping - playerID: 1, From: 135.0°, To: 135.5°, Progress: 0.80, Current: 135.4°
```

**When Screen 2 charges:**
```
[RemoteSync] Received - playerID: 1, rotationZ: 135.5°, autoAngle: 45.5°, rotationEnabled: False, currentRotation: 135.5°
[RemoteSync] Rotation STOPPED - playerID: 1, Frozen at: 135.5°
```

**When Screen 2 releases:**
```
[RemoteSync] Rotation STARTED - playerID: 1, Resuming from: 135.5°
```

### Screen 2 (Player 2 - Non-Host):

**When rotating:**
```
[LocalSync] Sending - playerID: 1, rotationZ: 135.5°, autoAngle: 45.5°, isCharging: False
[RemoteSync] Received - playerID: 0, rotationZ: 45.2°, autoAngle: -44.8°, rotationEnabled: True, currentRotation: 45.0°
[RemoteSync] Lerping - playerID: 0, From: 45.0°, To: 45.2°, Progress: 0.80, Current: 45.1°
```

**When Screen 1 charges:**
```
[RemoteSync] Received - playerID: 0, rotationZ: 45.2°, autoAngle: -44.8°, rotationEnabled: False, currentRotation: 45.2°
[RemoteSync] Rotation STOPPED - playerID: 0, Frozen at: 45.2°
```

**When Screen 1 releases:**
```
[RemoteSync] Rotation STARTED - playerID: 0, Resuming from: 45.2°
```

## Testing Checklist

### Screen 1 (Player 1 - Host):
- [ ] Left player bow rotates automatically
- [ ] Rotation stops when clicking/holding (charging)
- [ ] Rotation resumes after releasing arrow
- [ ] Rotation oscillates between up and down angles
- [ ] Right player bow rotates automatically (synced with Screen 2's right player)
- [ ] Right player rotation stops when Screen 2's right player starts charging
- [ ] Right player rotation resumes when Screen 2's right player releases arrow
- [ ] Right player rotation oscillates between up and down angles (synced with Screen 2's right player)

### Screen 2 (Player 2 - Non-Host):
- [ ] Right player bow rotates automatically
- [ ] Rotation stops when clicking/holding (charging)
- [ ] Rotation resumes after releasing arrow
- [ ] Rotation oscillates between up and down angles
- [ ] Left player bow rotates automatically (synced with Screen 1's left player)
- [ ] Left player rotation stops when Screen 1's left player starts charging
- [ ] Left player rotation resumes when Screen 1's left player releases arrow
- [ ] Left player rotation oscillates between up and down angles (synced with Screen 1's left player)

## Troubleshooting

### If rotation is not syncing:
1. Check Unity Console for `[LocalSync]` and `[RemoteSync]` logs
2. Verify `PlayerNetworkLocalSync` is attached to local player
3. Verify `PlayerNetworkRemoteSync` is attached to remote player
4. Check `NetworkData.User.SessionId` matches opponent's SessionId
5. Verify `rotationEnabled` is `true` in console logs

### If rotation is jittery:
1. Increase `StateFrequency` in `PlayerNetworkLocalSync` (try 0.15s)
2. Increase `LerpTime` in `PlayerNetworkRemoteSync` (try 0.1s)
3. Check network latency/ping

### If rotation not stopping when opponent charges:
1. Check `[RemoteSync] Rotation STOPPED` log appears
2. Verify `OPCODE_ROTATION_STOP` is being sent
3. Check `HandleRotationStop()` is being called

### If rotation not resuming:
1. Check `[RemoteSync] Rotation STARTED` log appears
2. Verify `OPCODE_ROTATION_START` is being sent
3. Check `rotationEnabled` is set back to `true`
