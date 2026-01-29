# Player Spawn Synchronization Fix

## Problem

When two players connect:
- **Player 1**: Both players spawn correctly and are visible immediately
- **Player 2**: Both players spawn but with a delay (sometimes only one player visible initially)

## Root Cause

The `WaitAndSpawnPlayers()` coroutine in `GameManager.cs` was spawning players immediately when it detected 2 players in the match, without waiting for both clients to fully load the GameScene. This caused:

1. Player 1 joins match → loads GameScene → spawns immediately
2. Player 2 joins match → loads GameScene → but Player 1 already spawned
3. Player 2 spawns with a delay because it's still loading the scene

## Solution

Added a **synchronization delay** in `WaitAndSpawnPlayers()` that ensures:

1. Both players are detected in the match (2+ players)
2. **Wait for synchronization delay** (matches scene transition delay + buffer)
3. Both players have fully loaded the GameScene
4. **Then** spawn players simultaneously

### Changes Made

**File**: `Assets/_Developer/Script/GameManager.cs`

**Modified Method**: `WaitAndSpawnPlayers()`

**Key Changes**:
- Added `synchronizationDelay` variable that matches scene transition delay
- Added `matchReady` flag to track when match is ready
- Added `matchReadyTime` to track when match became ready
- Wait for synchronization delay **after** detecting 2 players before spawning
- This ensures both players are in the scene before spawning starts

### Code Flow

```
1. Scene loads → WaitAndSpawnPlayers() starts
2. Check for 2+ players in match
3. When 2 players detected:
   - Set matchReady = true
   - Record matchReadyTime
4. Wait for synchronizationDelay (1.5-2.5s depending on build type)
5. After delay completes → Spawn players
```

## Synchronization Delay

The delay is calculated as:
```csharp
float synchronizationDelay = TicTacToeConfig.GetSceneTransitionDelay() + 0.5f;
```

- **Development Build**: 1.0s + 0.5s = **1.5s**
- **Production Build**: 2.0s + 0.5s = **2.5s**

This matches the scene transition delay in `ArrowduelConnectionManager.BeginMultiplayerGameTransition()` which is:
- **When waiting for opponent**: 1.5s
- **Otherwise**: 1.0s (dev) or 2.0s (production)

## Expected Behavior After Fix

### Player 1 Timeline
1. Joins match
2. Receives ready signal from Player 2
3. Transitions to GameScene (1.5s delay)
4. Scene loads → `WaitAndSpawnPlayers()` starts
5. Detects 2 players → waits 1.5-2.5s
6. Spawns players

### Player 2 Timeline
1. Joins match
2. Receives ready signal from Player 1
3. Transitions to GameScene (1.5s delay)
4. Scene loads → `WaitAndSpawnPlayers()` starts
5. Detects 2 players → waits 1.5-2.5s
6. Spawns players

**Result**: Both players spawn at approximately the same time, ensuring synchronization.

## Testing

To verify the fix:

1. **Start two instances** (Unity Editor + Build, or ParrelSync)
2. **Connect both** to Nakama server
3. **Join match** - both players should connect
4. **Observe spawn timing**:
   - Both players should spawn within ~0.5s of each other
   - No delay on Player 2's screen
   - Both players visible on both screens simultaneously

## Debug Logs

The fix adds detailed logging:
- `[GameManager] ✓ Match is ready with 2+ players! Waiting for synchronization...`
- `[GameManager] Waiting for synchronization... (X.XXs / Y.YYs)`
- `[GameManager] ✓ Synchronization delay (Y.YYs) complete! Spawning players...`

Check Unity Console to verify synchronization timing.

## Notes

- The synchronization delay ensures both clients are ready before spawning
- The delay is slightly longer than scene transition to account for network latency
- If timeout occurs (15s), players will spawn anyway (fallback behavior)
- This fix works in conjunction with the ready signal synchronization in `ArrowduelConnectionManager`

## Related Files

- `Assets/_Developer/Script/GameManager.cs` - Spawn synchronization
- `Assets/ArrowduelConnectionManager.cs` - Ready signal synchronization
- `Assets/TicTacToeConfig.cs` - Scene transition delay configuration
