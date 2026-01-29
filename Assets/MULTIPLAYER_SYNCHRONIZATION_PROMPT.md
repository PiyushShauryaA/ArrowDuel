# Multiplayer Synchronization System - Comprehensive Technical Documentation

## Overview
This Unity project implements a real-time multiplayer arrow duel game using **Photon Fusion** networking framework. The system uses a **Shared Mode** architecture where all clients participate in deterministic simulation with state authority management.

---

## 1. NETWORK ARCHITECTURE

### 1.1 Framework: Photon Fusion
- **Game Mode**: `GameMode.Shared` (P2P with deterministic simulation)
- **Network Model**: State Synchronization + Remote Procedure Calls (RPCs)
- **Player Count**: Fixed at 2 players per session
- **Connection Method**: Region-based connection via Photon Cloud

### 1.2 Core Components

#### FusionConnector.cs
**Location**: `Assets/_Developer/Script/Multiplayer/FusionConnector.cs`

**Responsibilities**:
- Manages NetworkRunner lifecycle
- Handles connection to Photon servers
- Implements `INetworkRunnerCallbacks` interface
- Manages player join/leave events
- Region-based server selection

**Key Functions**:
```csharp
ConnectToServer(string sessionName, string region)
// - Creates/retrieves NetworkRunner component
// - Sets ProvideInput = true for client prediction
// - Configures PhotonAppSettings with region
// - Starts game session with NetworkSceneManagerDefault
// - Triggers player spawning on success
```

**Network Callbacks Implemented**:
- `OnPlayerJoined(NetworkRunner, PlayerRef)` - Logs player connection
- `OnPlayerLeft(NetworkRunner, PlayerRef)` - Handles disconnect, aborts match
- `OnConnectedToServer(NetworkRunner)` - Connection confirmation
- `OnConnectFailed(NetworkRunner, NetAddress, NetConnectFailedReason)` - Error handling
- `OnShutdown(NetworkRunner, ShutdownReason)` - Cleanup on disconnect

---

## 2. NETWORKED OBJECTS & STATE AUTHORITY

### 2.1 State Authority Model

**Concept**: Only the object's State Authority can modify its `[Networked]` properties. Other clients receive synchronized updates.

**Key Classes**:
1. **GameNetworkManager** - Central game state authority
2. **BowController** - Player/opponent controller
3. **Arrow** - Projectile synchronization
4. **LoopMovementObject** - Birds/obstacles synchronization

### 2.2 GameNetworkManager.cs
**Location**: `Assets/_Developer/Script/Multiplayer/GameNetworkManager.cs`

**State Authority**: Managed via `HasStateAuthorityGameData` flag
- **Player 1 (Master)**: Spawns GameNetworkManager with State Authority
- **Player 2 (Client)**: Receives reference, no State Authority initially
- Authority can be transferred via `TakeStateAuthority()` method

**Networked Properties**:
```csharp
[Networked] public GameState gameState { get; set; }
[Networked] public int currentLevelIndex { get; set; }
[Networked] public int lastLevelIndex { get; set; }
[Networked] public int currentThemeIndex { get; set; }
[Networked] public int lastThemeIndex { get; set; }
[Networked] public int powerUpSpawnPointIndex { get; set; }
[Networked] public int powerUpDataIndex { get; set; }
[Networked] public bool isWindActive { get; set; }
[Networked] public bool isWindDirectionRight { get; set; }
[Networked] public float windEndTime { get; set; }
[Networked] public Vector2 windForce { get; set; }
[Networked] public Vector2 windDirection { get; set; }
```

**Lifecycle**:
- `Spawned()`: Called when object spawns on network
  - Client (non-authority) registers with GameManager
  - Sets `HasStateAuthorityGameData` flag
- `TakeStateAuthority()`: Requests state authority transfer

---

## 3. PLAYER SYNCHRONIZATION

### 3.1 Player Spawning Logic
**Location**: `GameManager.cs` → `SpawnPlayer(NetworkRunner)`

**Spawn Rules**:
- **Player ID 1 (Master)**: Spawns `player1NetworkStatePrefab` (PlayerController)
- **Player ID 2 (Client)**: Spawns `player2NetworkStatePrefab` (OpponentController)
- Client also spawns `GameNetworkManager` prefab

**Coroutine Flow**:
```csharp
SpawnPlayer(runner)
├── Player 1: Spawn player1NetworkStatePrefab → Set as playerController
└── Player 2: 
    ├── Spawn player2NetworkStatePrefab → Set as opponentPlayerController
    ├── Spawn gameNetworkManager → Set as gameNetworkManagerRef
    └── DelayToCall() → OnChangeLevel_RPC() + GameStart_RPC()
```

### 3.2 BowController.cs (Base Class)
**Location**: `Assets/_Developer/Script/BowController.cs`

**Inheritance**: `NetworkBehaviour` (Photon Fusion base class)

**Networked Properties**:
```csharp
[Networked] public int playerIndex { get; set; } = -1;
[Networked, OnChangedRender(nameof(OnGameCompletedValueChanged))] 
public int isGameCompleted { get; set; }
```

**Key Methods**:
- `FixedUpdateNetwork()`: Network tick update (runs every network tick)
- `Spawned()`: Called when object spawns on network
- `OnGameCompletedValueChanged()`: Callback when game completion changes

**Arrow Spawning**:
```csharp
SpawnArrowNetwork(GameObject prefab, float shootForce)
// - Uses Runner.Spawn() for networked arrow creation
// - Only State Authority can spawn
// - Initializes arrow with velocity based on playerID direction
// - Arrow inherits NetworkBehaviour for synchronization
```

**Time Synchronization**:
- Singleplayer: Uses `Time.deltaTime`
- Multiplayer: Uses `Runner.DeltaTime` (network-synchronized time)

### 3.3 PlayerController.cs
**Location**: `Assets/_Developer/Script/PlayerController.cs`

**Player 1 (Left Side)**:
- Handles input only if `LocalPlayer.PlayerId == 1`
- Input: Mouse/Touch → StartCharging() / ReleaseArrow()
- Behavior updates in `FixedUpdateNetwork()` for multiplayer sync

**Key Methods**:
```csharp
FixedUpdateNetwork()
// - Only updates during Gameplay/WaitForLevelChange states
// - Skips if frozen (power-up effect)
// - Calls base.FixedUpdateNetwork()
// - Calls UpdatePlayerBehavior() in multiplayer mode

UpdatePlayerBehavior()
// - Auto-rotation when not charging
// - Force meter update when charging

HandleInput()
// - Detects mouse/touch input
// - Triggers StartCharging() / ReleaseArrow()
```

### 3.4 OpponentController.cs
**Location**: `Assets/_Developer/Script/OpponentController.cs`

**Player 2 (Right Side)**:
- Handles input only if `LocalPlayer.PlayerId == 2`
- Can operate as AI (singleplayer) or Player (multiplayer)
- Same input handling as PlayerController

**AI Behavior** (Singleplayer only):
- Decision-based shooting with skill levels (Easy/Normal/Hard/Robinhood)
- Angle error calculation based on skill
- Hit/miss counter tracking for adaptive difficulty

**Multiplayer Mode**:
- Same input/behavior as PlayerController
- Synchronized via FixedUpdateNetwork()

---

## 4. ARROW SYNCHRONIZATION

### 4.1 Arrow.cs Network Implementation
**Location**: `Assets/_Developer/Script/Arrow.cs`

**Networked Properties**:
```csharp
[Networked] public BowController bowControllerNetwork { get; set; }
[Networked] public PlayerPowerUp playerPowerUpNetwork { get; set; }
[Networked] public int arrowPlayerIDNetwork { get; set; }
[Networked] public string arrowPlayerTagNetwork { get; set; }
[Networked] public bool isPlayerArrowNetwork { get; set; }
[Networked] public string hitObjectName { get; set; }
```

### 4.2 Arrow Initialization Flow
**Method**: `Initialize(BowController, PlayerPowerUp, string)`

**Multiplayer Initialization**:
```csharp
if (GameManager.gameMode == GameModeType.MULTIPLAYER)
{
    bowControllerNetwork = bowController;
    playerPowerUpNetwork = playerPowerUp;
    arrowPlayerIDNetwork = bowControllerNetwork.playerID;
    isPlayerArrowNetwork = (arrowPlayerIDNetwork == 0);
    arrowPlayerTagNetwork = playerTag;
    
    // Wind data from GameNetworkManager
    isWindActive = GameNetworkManager.instance.isWindActive;
    windForce = GameNetworkManager.instance.windForce;
}
```

### 4.3 Arrow Physics Synchronization

**Physics Update** (`FixedUpdate()`):
- Applies wind force continuously (multiplier mode)
- Wind source: `GameNetworkManager.instance.windForce` (multiplayer)
- Rotation sync: Arrow faces movement direction
- All clients simulate physics identically

**Wind Application**:
```csharp
if (GameManager.gameMode == GameModeType.MULTIPLAYER && 
    GameNetworkManager.instance.isWindActive)
    rb.AddForce(GameNetworkManager.instance.windForce * Time.fixedDeltaTime, 
                ForceMode2D.Force);
```

### 4.4 Arrow Collision & Hit Detection

**State Authority Model**:
- Only State Authority processes collisions
- Non-authority clients receive hit results via networked properties

**Collision Types**:

1. **Target Hit** (`OnTargetDetect`):
   ```csharp
   if (HasStateAuthority == true)
   {
       ArrowHitEffect();
       // Disable visuals
       Invoke(nameof(NetworkDespawn), 0.25f);
   }
   ```
   - Sets `hitObjectName = "PLAYER"`
   - Triggers `OnHitTarget_RPC()` for score update

2. **Ground Hit** (`OnGroundDetect`):
   ```csharp
   if (GameManager.gameMode == GameModeType.MULTIPLAYER)
       hitObjectName = "GROUND";
   ```
   - State Authority despawns after delay
   - Non-authority applies effects in `Despawned()` callback

3. **Bird/Obstacle Hit** (`OnObstacleDetect`):
   ```csharp
   if (HasStateAuthority)
   {
       bird.OnBirdHitDetect_RPC(); // RPC to bird object
       Invoke(nameof(NetworkDespawn), 0.25f);
   }
   ```
   - Sets `hitObjectName = "OBJECT"`
   - Triggers bird despawn RPC

4. **Arrow-to-Arrow Collision** (`TwoArrowHitDetect`):
   ```csharp
   arrow.ArrowHitEachOther_RPC();
   ArrowHitEachOther_RPC();
   ```
   - Both arrows despawn via RPC
   - Visual effects synchronized

### 4.5 Arrow RPC Methods

```csharp
[Rpc(RpcSources.All, RpcTargets.All)]
public void ArrowHitEachOther_RPC()
// - Called when two arrows collide
// - Disables visuals on all clients
// - Despawns arrow on network
```

### 4.6 Arrow Despawn Synchronization

**Despawned() Callback**:
- Called on all clients when arrow despawns
- Non-authority clients apply visual effects based on `hitObjectName`:
  - `"PLAYER"`: Apply hit effects
  - `"Ground"`: Apply ground hit effects
  - `"OBJECT"`: Apply bird hit effects
  - `"POWER"`: Power-up collection (handled on authority)

**Delayed Despawn Pattern**:
```csharp
Invoke(nameof(NetworkDespawn), 0.25f);
// - Allows effects to play before despawn
// - NetworkDespawn() calls Runner.Despawn(Object)
```

---

## 5. RPC (REMOTE PROCEDURE CALL) SYSTEM

### 5.1 RPC Types Used
**Attribute**: `[Rpc(RpcSources.All, RpcTargets.All)]`
- **Sources**: All clients can invoke
- **Targets**: All clients receive
- **Reliability**: Reliable (guaranteed delivery)

### 5.2 GameNetworkManager RPCs

#### GameStart_RPC()
```csharp
[Rpc(RpcSources.All, RpcTargets.All)]
public void GameStart_RPC()
// - Invoked by Player 2 after spawning
// - All clients call GameManager.instance.OnGameStart()
// - Starts game countdown and transitions to Gameplay state
```

#### SetGameState_RPC(GameState)
```csharp
[Rpc(RpcSources.All, RpcTargets.All)]
public void SetGameState_RPC(GameState _gameState)
// - Synchronizes game state across clients
// - Updates both Networked property and local GameManager state
```

#### OnChangeLevel_RPC()
```csharp
[Rpc(RpcSources.All, RpcTargets.All)]
public void OnChangeLevel_RPC()
// - State Authority generates random level/theme indices
// - All clients receive indices and call LevelManager.instance.LevelInit()
// - Destroys existing arrows before level change
```

#### ChangeTheme_RPC()
```csharp
[Rpc(RpcSources.All, RpcTargets.All)]
public void ChangeTheme_RPC()
// - Triggered when score reaches theme change threshold
// - State Authority generates new level/theme
// - Invokes GameManager.onGameLevelChange event
```

#### Wind_RPC()
```csharp
[Rpc(RpcSources.All, RpcTargets.All)]
public void Wind_RPC()
// - State Authority generates random wind direction/force
// - Updates Networked wind properties
// - All clients apply wind via WindManager.instance.ChangeWind()
```

#### PowerUp_RPC()
```csharp
[Rpc(RpcSources.All, RpcTargets.All)]
public void PowerUp_RPC()
// - State Authority generates random power-up spawn indices
// - All clients receive indices and spawn power-up at same location
```

#### OnHitTarget_RPC(bool isPlayerArrow)
```csharp
[Rpc(RpcSources.All, RpcTargets.All)]
public void OnHitTarget_RPC(bool isPlayerArrow)
// - Invoked by arrow when hitting target (State Authority only)
// - Triggers GameManager.onHitTarget event
// - ScoreManager registers hit and updates score/hearts
```

### 5.3 Arrow RPCs

#### ArrowHitEachOther_RPC()
```csharp
[Rpc(RpcSources.All, RpcTargets.All)]
public void ArrowHitEachOther_RPC()
// - Disables visuals on all clients
// - Despawns arrow on network
// - Synchronizes arrow-to-arrow collision effects
```

### 5.4 LoopMovementObject (Bird) RPCs

#### ActiveBird_RPC()
```csharp
[Rpc(RpcSources.All, RpcTargets.All)]
public void ActiveBird_RPC()
// - Resets bird to initial position
// - Enables sprite and colliders
// - Synchronizes bird respawn
```

#### DeactiveBird_RPC()
```csharp
[Rpc(RpcSources.All, RpcTargets.All)]
public void DeactiveBird_RPC()
// - Disables bird visuals and colliders
// - Stops movement
// - Synchronizes bird hit/despawn
```

#### OnBirdHitDetect_RPC()
```csharp
[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
public void OnBirdHitDetect_RPC()
// - Called by arrow when hitting bird
// - Only State Authority receives
// - Marks bird as hit, disables movement
```

---

## 6. GAME STATE SYNCHRONIZATION

### 6.1 GameState Enum
**States**:
- `WaitforOtherPlayer` - Waiting for opponent to join
- `Gameplay` - Active game state
- `WaitForLevelChange` - Transitioning between levels
- `GameOver` - Match ended

### 6.2 State Authority & Synchronization

**GameNetworkManager** manages game state:
- `gameState` property is `[Networked]`
- Changes propagate automatically to all clients
- RPC methods ensure state transitions are synchronized

**State Transition Flow**:
```
GameStart_RPC() 
→ GameManager.OnGameStart() 
→ WaitForSeconds(1) 
→ gameState = GameState.Gameplay
→ onGameStart event
```

### 6.3 Score Synchronization

**ScoreManager.cs**:
- **NOT networked directly** - calculated locally on each client
- Synchronized via hit events:
  1. Arrow (State Authority) detects hit
  2. Calls `GameNetworkManager.instance.OnHitTarget_RPC(isPlayerArrow)`
  3. All clients receive RPC → `GameManager.onHitTarget?.Invoke()`
  4. ScoreManager.RegisterHit() called on all clients
  5. Score/hearts updated identically (deterministic)

**Score Update Flow**:
```csharp
Arrow.OnTargetDetect() [State Authority]
  ↓
GameNetworkManager.OnHitTarget_RPC(isPlayerArrow) [RPC]
  ↓
GameManager.onHitTarget event [All Clients]
  ↓
ScoreManager.RegisterHit(isPlayerArrow) [All Clients]
  ↓
playerScore++ or opponentScore++ [Deterministic]
```

### 6.4 Game Completion Synchronization

**Completion Check**:
```csharp
BowController.isGameCompleted [Networked property]
  ↓
OnGameCompletedValueChanged() [OnChangedRender callback]
  ↓
GameManager.CheckForGameCompletion()
  ↓
if (playerController.isGameCompleted == 1 && 
    opponentPlayerController.isGameCompleted == 1)
  → EndGameWaitForSomeTime()
```

**Completion Trigger**:
- Player 1 (Master): `playerController.SetGameCompleted()`
- Player 2 (Client): `opponentPlayerController.SetGameCompleted()`
- Both must complete → EndGame()

---

## 7. LEVEL & THEME SYNCHRONIZATION

### 7.1 Level Change Flow

**Trigger**: Score reaches `themeChangeScore` (default: 5)

**Singleplayer**:
```csharp
ScoreManager.ChangeTheme()
→ GameManager.onGameLevelChange?.Invoke()
→ LevelManager.LevelInit()
```

**Multiplayer**:
```csharp
ScoreManager.ChangeTheme()
→ Check: HasStateAuthorityGameData == true
→ GameNetworkManager.ChangeTheme_RPC()
→ State Authority generates random indices
→ All clients receive indices
→ GameManager.onGameLevelChange?.Invoke()
→ LevelManager.LevelInit() [same indices on all clients]
```

### 7.2 Level Initialization Synchronization

**GameNetworkManager.OnChangeLevel_RPC()**:
```csharp
if (HasStateAuthority)
{
    // Only State Authority generates random values
    lastLevelIndex = currentLevelIndex;
    currentLevelIndex = Random.Range(0, availableLevelIndices.Count);
    lastThemeIndex = currentThemeIndex;
    currentThemeIndex = Random.Range(0, availableThemeIndices.Count);
}

// All clients receive Networked properties
LevelManager.instance.currentLevelIndex = currentLevelIndex;
LevelManager.instance.currentThemeIndex = currentThemeIndex;
LevelManager.instance.LevelInit(); // Same level on all clients
```

**Arrow Cleanup**:
```csharp
GameManager.OnGameLevelChange()
→ FindObjectsByType<Arrow>()
→ Destroy all arrows before level change
```

---

## 8. WIND SYSTEM SYNCHRONIZATION

### 8.1 Wind Properties

**Networked in GameNetworkManager**:
```csharp
[Networked] public bool isWindActive { get; set; }
[Networked] public bool isWindDirectionRight { get; set; }
[Networked] public Vector2 windForce { get; set; }
[Networked] public Vector2 windDirection { get; set; }
[Networked] public float windEndTime { get; set; }
```

### 8.2 Wind Activation Flow

**Trigger**: Wind event RPC

**State Authority** (HasStateAuthorityGameData == true):
```csharp
isWindActive = true;
isWindDirectionRight = Random.value > 0.5f;
windDirection = isWindDirectionRight ? Vector2.right : Vector2.left;
windForce = windDirection * WindManager.instance.maxWindStrength;
```

**All Clients**:
```csharp
WindManager.instance.isWindDirectionRight = isWindDirectionRight;
WindManager.instance.windDirection = windDirection;
WindManager.instance.windForce = windForce;
WindManager.instance.ChangeWind(); // Apply visual/physics changes
```

### 8.3 Wind Application to Arrows

**Arrow FixedUpdate()**:
```csharp
if (GameManager.gameMode == GameModeType.MULTIPLAYER && 
    GameNetworkManager.instance.isWindActive)
    rb.AddForce(GameNetworkManager.instance.windForce * Time.fixedDeltaTime, 
                ForceMode2D.Force);
```

**Synchronization**: All clients apply same wind force → deterministic arrow trajectory

---

## 9. POWER-UP SYNCHRONIZATION

### 9.1 Power-Up Spawn Synchronization

**GameNetworkManager.PowerUp_RPC()**:

**State Authority**:
```csharp
powerUpSpawnPointIndex = Random.Range(0, powerUpDatas.Count);
powerUpDataIndex = Random.Range(0, powerUpSpawnPoints.Length);
```

**All Clients**:
```csharp
PowerUpManager.instance.powerUpSpawnPointIndex = powerUpSpawnPointIndex;
PowerUpManager.instance.powerUpDataIndex = powerUpDataIndex;
StartCoroutine(PowerUpManager.instance.InitSpawnPowerUp());
// Same spawn location and power-up type on all clients
```

### 9.2 Power-Up Collection

**Arrow.OnPowerUpDetect()**:
- State Authority detects collision
- Calls `powerUp.Collect(bowControllerNetwork)`
- Sets `hitObjectName = "POWER"`
- Despawns arrow after delay

**Non-Authority**:
- Receives despawn event
- Power-up already collected by authority (handled locally)

---

## 10. NETWORK OBJECT LIFECYCLE

### 10.1 Object Spawning

**Spawn Method**: `Runner.Spawn(NetworkObject prefab, Vector3 position, Quaternion rotation)`

**Spawned Objects**:
1. **Players**: `player1NetworkStatePrefab`, `player2NetworkStatePrefab`
2. **GameNetworkManager**: Spawned by Player 2
3. **Arrows**: Spawned by BowController when shooting
4. **Birds/Obstacles**: Pre-spawned in scene (with NetworkObject component)

### 10.2 Object Despawning

**Despawn Method**: `Runner.Despawn(NetworkObject)`

**Despawn Scenarios**:
1. **Arrow Hit**: Despawned after 0.25s delay (visual effects)
2. **Arrow Out of Bounds**: Destroyed when y < -25f (local only)
3. **Level Change**: All arrows destroyed before level transition

**Despawned() Callback**:
- Called on all clients when object despawns
- Non-authority clients use this for visual effect synchronization

### 10.3 Network Scene Management

**NetworkSceneManagerDefault**:
- Manages scene loading across network
- Automatically synchronizes scene changes
- Ensures all clients load same scene

**Callbacks**:
- `OnSceneLoadStart(NetworkRunner)` - Scene load initiated
- `OnSceneLoadDone(NetworkRunner)` - Scene load completed

---

## 11. INPUT SYNCHRONIZATION

### 11.1 Input Model

**ProvideInput Setting**: `networkRunner.ProvideInput = true`
- Enables client-side input prediction
- Input sent to server and applied locally

### 11.2 Input Handling

**Player 1**:
```csharp
if (FusionConnector.instance.NetworkRunner.LocalPlayer.PlayerId == 1)
    HandleInput();
```

**Player 2**:
```csharp
if (FusionConnector.instance.NetworkRunner.LocalPlayer.PlayerId == 2)
    HandleInput();
```

**Input Methods**:
- Mouse: `Input.GetMouseButtonDown/Up(0)`
- Touch: `Input.GetTouch(0).phase == TouchPhase.Began/Ended`

**Actions**:
- `StartCharging()` - Local only (visual feedback)
- `ReleaseArrow()` - Spawns networked arrow (State Authority only)

### 11.3 Input Validation

**State Authority Check**:
- Only State Authority can spawn arrows
- Input processed locally, but arrow spawn requires authority
- Non-authority input ignored for arrow spawning

---

## 12. TIME SYNCHRONIZATION

### 12.1 Network Time vs Unity Time

**Singleplayer**:
- Uses `Time.deltaTime` for all time-based calculations
- `Time.fixedDeltaTime` for physics

**Multiplayer**:
- Uses `Runner.DeltaTime` for network-synchronized time
- Ensures deterministic simulation across clients

### 12.2 Usage Examples

**BowController**:
```csharp
// Force meter update
if (GameManager.gameMode == GameModeType.MULTIPLAYER)
    currentForce += fillDirection * fillSpeed * Runner.DeltaTime;
else
    currentForce += fillDirection * fillSpeed * Time.deltaTime;

// Auto rotation
if (GameManager.gameMode == GameModeType.MULTIPLAYER)
    currentAutoRotationAngle += autoRotationDirection * autoRotationSpeed * Runner.DeltaTime;
```

**LoopMovementObject**:
```csharp
// Bird movement
if (GameManager.gameMode == GameModeType.MULTIPLAYER)
    transform.Translate(direction * _speed * Runner.DeltaTime);
```

---

## 13. PHYSICS SYNCHRONIZATION

### 13.1 Physics Simulation Mode

**Fusion Physics**:
- Uses `RunnerSimulatePhysics` component
- Physics simulation controlled by network tick
- Deterministic physics across all clients

### 13.2 Arrow Physics

**Rigidbody2D Synchronization**:
- Arrows use `Rigidbody2D` for physics
- Position/rotation synchronized via NetworkTransform (if present)
- Velocity set on spawn: `arrowRb.linearVelocity = shootDirection * shootForce`

**Wind Force Application**:
- Applied in `FixedUpdate()` every frame
- Same force applied on all clients → deterministic trajectory
- Force source: `GameNetworkManager.instance.windForce`

### 13.3 Physics Time Step

**FixedUpdate()** vs **FixedUpdateNetwork()**:
- `FixedUpdate()`: Unity physics update (local)
- `FixedUpdateNetwork()`: Network tick update (synchronized)
- Physics forces applied in `FixedUpdate()` for smooth simulation
- Network state updates in `FixedUpdateNetwork()`

---

## 14. EVENT SYNCHRONIZATION

### 14.1 C# Events vs Network Events

**Local Events** (Singleplayer):
```csharp
GameManager.onGameStart?.Invoke();
GameManager.onHitTarget?.Invoke(isPlayerArrow);
GameManager.onGameLevelChange?.Invoke();
```

**Network Events** (Multiplayer):
- Events triggered via RPC
- RPC methods invoke local events on all clients
- Ensures all clients receive event callbacks

### 14.2 Event Flow Example

**Hit Target Event**:
```
Arrow.OnTargetDetect() [State Authority]
  ↓
GameNetworkManager.OnHitTarget_RPC(isPlayerArrow) [RPC]
  ↓
All Clients: GameManager.onHitTarget?.Invoke(isPlayerArrow)
  ↓
All Clients: ScoreManager.RegisterHit(isPlayerArrow)
```

**Game Start Event**:
```
Player 2: GameNetworkManager.GameStart_RPC() [RPC]
  ↓
All Clients: GameManager.instance.OnGameStart()
  ↓
All Clients: GameManager.onGameStart?.Invoke()
```

---

## 15. BIRD/OBSTACLE SYNCHRONIZATION

### 15.1 LoopMovementObject (Birds)

**Networked Component**: `NetworkBehaviour`

**State Authority**:
- Scene-placed objects have State Authority on spawner
- Movement synchronized via `FixedUpdateNetwork()`

### 15.2 Bird Movement Synchronization

**Movement Update**:
```csharp
public override void FixedUpdateNetwork()
{
    if (canMove)
        transform.Translate(direction * _speed * Runner.DeltaTime);
    HandleBoundary();
}
```

**Synchronization**:
- Position updated every network tick
- All clients apply same movement (deterministic)
- Boundary checks synchronized

### 15.3 Bird Hit Synchronization

**Hit Detection Flow**:
```
Arrow.OnObstacleDetect() [State Authority]
  ↓
bird.OnBirdHitDetect_RPC() [RPC to State Authority]
  ↓
LoopMovementObject.OnBirdHitDetect_RPC()
  ↓
OnHitDetect() [Marks bird as hit, disables movement]
```

**Respawn Synchronization**:
```
Bird reaches boundary [State Authority]
  ↓
DeactiveBird_RPC() [RPC to all clients]
  ↓
All Clients: DeactiveBird() [Disable visuals, reset position]
```

**Reset Synchronization**:
```
Bird position reset [State Authority detects reset condition]
  ↓
ActiveBird_RPC() [RPC to all clients]
  ↓
All Clients: ActiveBird() [Enable visuals, start movement]
```

---

## 16. ERROR HANDLING & EDGE CASES

### 16.1 Connection Failures

**OnConnectFailed**:
- Logs error reason
- Does not automatically retry
- UI should handle retry logic

### 16.2 Player Disconnection

**OnPlayerLeft**:
```csharp
if (GameManager.instance.gameState == GameState.WaitforOtherPlayer || 
    GameManager.instance.gameState == GameState.Gameplay)
{
    IFrameBridge.instance.PostMatchAbort("Opponent left the game.", "", "");
    ScoreManager.instance.EndGame(true);
}
```

**Handling**:
- Immediately ends game if opponent leaves
- Posts match abort result
- Forces game over state

### 16.3 State Authority Edge Cases

**Missing State Authority**:
- Operations requiring authority check `HasStateAuthority` before execution
- Non-authority operations ignored or queued

**Authority Transfer**:
- `TakeStateAuthority()` requests authority transfer
- Used when original authority disconnects (host migration)

### 16.4 Network Despawn Delays

**Visual Effect Synchronization**:
- 0.25s delay before despawn allows effects to play
- Non-authority clients apply effects in `Despawned()` callback
- Prevents premature visual cleanup

---

## 17. PERFORMANCE CONSIDERATIONS

### 17.1 Network Object Count

**Optimizations**:
- Arrows despawned after hit (not pooled)
- Birds pre-spawned (not dynamically created)
- Minimal Networked properties (only essential data)

### 17.2 Update Frequency

**FixedUpdateNetwork()**:
- Runs every network tick (typically 60Hz)
- Only active objects update during gameplay
- Paused during non-gameplay states

### 17.3 RPC Frequency

**RPCs Used Sparingly**:
- Only for state changes, not continuous updates
- Hit detection RPCs only when collisions occur
- Level changes infrequent

---

## 18. TESTING & DEBUGGING

### 18.1 Multiplayer Testing

**Tools**:
- ParrelSync: Multiple Unity editor instances
- Photon Dashboard: Monitor connections/sessions
- Unity Editor: Simulate multiplayer locally

### 18.2 Debug Logging

**Key Log Points**:
- Connection events: `OnPlayerJoined`, `OnPlayerLeft`
- RPC calls: All RPC methods log invocations
- State changes: GameState transitions logged
- Authority checks: State authority changes logged

### 18.3 Common Issues

**Desync Causes**:
- Using `Time.deltaTime` instead of `Runner.DeltaTime`
- Non-deterministic calculations (local Random instead of networked)
- Missing State Authority checks
- Physics differences between clients

**Solutions**:
- Always use `Runner.DeltaTime` in multiplayer mode
- Use Networked Random or State Authority for random values
- Validate State Authority before modifying Networked properties
- Ensure physics settings identical on all clients

---

## 19. CODE STRUCTURE SUMMARY

### 19.1 Network Components Hierarchy

```
FusionConnector (MonoBehaviour)
├── NetworkRunner
│   ├── NetworkSceneManagerDefault
│   │
│   └── Networked Objects:
│       ├── GameNetworkManager (NetworkBehaviour)
│       ├── PlayerController (NetworkBehaviour)
│       ├── OpponentController (NetworkBehaviour)
│       ├── Arrow (NetworkBehaviour)
│       └── LoopMovementObject (NetworkBehaviour)
```

### 19.2 Key Files & Responsibilities

**Connection & Setup**:
- `FusionConnector.cs` - Network connection management
- `IFrameBridge.cs` - External system integration
- `GameManager.cs` - Player spawning, game flow

**Game State**:
- `GameNetworkManager.cs` - Central state synchronization
- `ScoreManager.cs` - Score tracking (event-based sync)

**Player Control**:
- `BowController.cs` - Base player controller
- `PlayerController.cs` - Player 1 implementation
- `OpponentController.cs` - Player 2 / AI implementation

**Projectiles**:
- `Arrow.cs` - Arrow physics & collision

**Environment**:
- `LoopMovementObject.cs` - Birds/obstacles
- `LevelManager.cs` - Level loading
- `WindManager.cs` - Wind effects

---

## 20. BEST PRACTICES IMPLEMENTED

### 20.1 State Authority Pattern
✅ Only State Authority modifies Networked properties
✅ Non-authority uses RPCs to request changes
✅ Authority validation before all state changes

### 20.2 Deterministic Simulation
✅ Networked time (`Runner.DeltaTime`) for all time-based calculations
✅ Same random seeds or authority-generated random values
✅ Identical physics settings across clients

### 20.3 Efficient Networking
✅ Minimal Networked properties (only essential state)
✅ RPCs for infrequent events (not continuous updates)
✅ Despawn objects when no longer needed

### 20.4 Error Handling
✅ Connection failure logging
✅ Player disconnect handling
✅ State authority validation
✅ Graceful degradation

---

## 21. EXTENSIBILITY NOTES

### 21.1 Adding New Networked Features

**Steps**:
1. Add `[Networked]` property to appropriate NetworkBehaviour
2. Implement State Authority checks
3. Add RPC method if cross-client communication needed
4. Update both Singleplayer and Multiplayer code paths
5. Test with multiple clients

### 21.2 Adding New RPCs

**Pattern**:
```csharp
[Rpc(RpcSources.All, RpcTargets.All)]
public void NewFeature_RPC(/* parameters */)
{
    // State Authority: Generate/modify state
    if (HasStateAuthority)
    {
        // Modify Networked properties
    }
    
    // All Clients: Apply effects
    // Call local methods/events
}
```

### 21.3 Adding New Networked Objects

**Requirements**:
- Inherit from `NetworkBehaviour`
- Attach `NetworkObject` component
- Register prefab in NetworkProjectConfig
- Spawn via `Runner.Spawn()`
- Despawn via `Runner.Despawn()`

---

## CONCLUSION

This multiplayer synchronization system provides:
- **Deterministic gameplay** via Photon Fusion Shared Mode
- **State authority management** for consistent game state
- **Efficient networking** with minimal bandwidth usage
- **Robust error handling** for edge cases
- **Scalable architecture** for future enhancements

The system successfully synchronizes:
- Player actions and arrows
- Game state and scoring
- Level/theme changes
- Environmental effects (wind, power-ups)
- Collision detection and physics
- Visual effects and animations

All synchronization logic is centralized in key components, making the system maintainable and extensible.
