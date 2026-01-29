# Network Synchronization Implementation

## Overview

Implemented FishGame-style network synchronization pattern for Arrow Duel multiplayer. This provides smooth position/rotation and input synchronization between players.

## Components Created

### 1. `UnityMainThreadDispatcher.cs`
- **Location**: `Assets/_Developer/Script/Multiplayer/UnityMainThreadDispatcher.cs`
- **Purpose**: Dispatches Nakama callbacks to Unity's main thread
- **Usage**: Required for thread-safe network event handling

### 2. `PlayerNetworkLocalSync.cs`
- **Location**: `Assets/_Developer/Script/Multiplayer/PlayerNetworkLocalSync.cs`
- **Purpose**: Syncs local player's state to network
- **Attached to**: Local player GameObject (the one you control)
- **Features**:
  - Sends position/rotation every `StateFrequency` seconds (default: 0.1s)
  - Sends input changes immediately when they occur
  - Uses `LateUpdate()` for timing
  - Only syncs if component has authority (controls this player)

### 3. `PlayerNetworkRemoteSync.cs`
- **Location**: `Assets/_Developer/Script/Multiplayer/PlayerNetworkRemoteSync.cs`
- **Purpose**: Syncs remote player's state from network
- **Attached to**: Remote player GameObject (opponent's character)
- **Features**:
  - Receives match state and filters by `SessionId`
  - Interpolates position using `Lerp` for smooth movement
  - Interpolates rotation smoothly
  - Applies input directly to remote player

### 4. `MatchDataJson.cs`
- **Location**: `Assets/_Developer/Script/Multiplayer/MatchDataJson.cs`
- **Purpose**: Helper class for creating JSON strings for match state
- **Methods**:
  - `PositionAndRotation()` - Position and rotation data
  - `Input()` - Input state data
  - `HitTarget()` - Hit event data
  - `Wind()` - Wind data
  - `PowerUp()` - Power-up data

## OpCodes Added

Added to `ArrowduelNetworkManager.cs`:
- `OPCODE_POSITION_ROTATION = 12` - Player position and rotation sync
- `OPCODE_INPUT = 13` - Player input sync

## How It Works

### Player Spawning (GameManager.cs)

When players spawn in multiplayer:

**Player 1 (Host):**
- Left player (Player 1) gets `PlayerNetworkLocalSync` component
- Right player (Player 2) gets `PlayerNetworkRemoteSync` component with opponent's `SessionId`

**Player 2 (Non-Host):**
- Left player (Player 1) gets `PlayerNetworkRemoteSync` component with opponent's `SessionId`
- Right player (Player 2) gets `PlayerNetworkLocalSync` component

### Local Sync Flow

1. `PlayerNetworkLocalSync` checks if it has authority (controls this player)
2. Every `StateFrequency` seconds, sends position/rotation via `OPCODE_POSITION_ROTATION`
3. When input changes (charging, force), sends immediately via `OPCODE_INPUT`
4. Data is sent through `ArrowduelNakamaClient.SendMatchStateAsync()`

### Remote Sync Flow

1. `PlayerNetworkRemoteSync` subscribes to match state events
2. Filters incoming messages by `SessionId` (only processes opponent's data)
3. On `OPCODE_POSITION_ROTATION`: Interpolates position and rotation smoothly
4. On `OPCODE_INPUT`: Applies input directly to remote player's `BowController`

## Configuration

### PlayerNetworkLocalSync Settings
- `StateFrequency`: How often to send position/rotation (default: 0.1s)
- `SendInputImmediately`: Send input changes immediately (default: true)

### PlayerNetworkRemoteSync Settings
- `LerpTime`: Interpolation time for position/rotation (default: 0.05s)

## Integration Points

### GameManager.cs Changes
- Added component attachment in `SpawnPlayerNakama()`
- Sets up `RemotePlayerNetworkData` for remote players
- Identifies opponent's `SessionId` for filtering

### ArrowduelNetworkManager.cs Changes
- Added new OpCodes for position/rotation and input sync

## Testing

To test the synchronization:

1. **Start two instances** (Unity Editor + Build, or ParrelSync)
2. **Connect both** to Nakama server
3. **Join match** - players should spawn
4. **Move/rotate** local player - should sync to opponent
5. **Charge bow** - opponent should see charging animation
6. **Release arrow** - opponent should see arrow release

## Troubleshooting

### Players Not Syncing
- Check that `PlayerNetworkLocalSync` is attached to local player
- Check that `PlayerNetworkRemoteSync` is attached to remote player
- Verify `NetworkData.User.SessionId` matches opponent's `SessionId`
- Check Unity Console for sync messages

### Position Jitter
- Increase `StateFrequency` in `PlayerNetworkLocalSync` (send less frequently)
- Increase `LerpTime` in `PlayerNetworkRemoteSync` (smoother interpolation)

### Input Not Syncing
- Verify `SendInputImmediately` is enabled
- Check that input changes are being detected
- Verify OpCode `OPCODE_INPUT` is being sent/received

## Notes

- This pattern is based on FishGame's implementation
- Uses `UnityMainThreadDispatcher` for thread-safe callbacks
- Position sync is optional - you can disable it if you only need input sync
- Input sync is critical for multiplayer gameplay
- Both players spawn both characters locally, but only sync their own

## Future Enhancements

- Add velocity sync for smoother movement prediction
- Add arrow position sync (if needed)
- Add health sync
- Add power-up state sync
- Optimize network bandwidth (reduce frequency if needed)
