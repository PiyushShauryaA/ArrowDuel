# Bow Functionality Testing Guide

This guide explains how to test the bow functionality fixes in both single-player and multiplayer modes.

## Prerequisites

### 1. Start Nakama Server (For Multiplayer Testing)

```bash
# Navigate to project root directory
cd arrow-duel-unity-main

# Start Nakama server with Docker Compose
docker-compose up -d

# Verify server is running
docker-compose ps

# Check server logs (optional)
docker-compose logs -f nakama
```

**Verify Server is Running:**
- Open browser: http://localhost:7351
- You should see Nakama API response

### 2. Unity Project Setup

- Open Unity Editor
- Wait for all scripts to compile (no errors in Console)
- Ensure `ArrowduelConnectionManager` is configured:
  - Server Host: `127.0.0.1`
  - Server Port: `7351` (HTTP API gateway)
  - Server Key: `defaultkey`
  - Use SSL: `false`

---

## Testing Methods

### **Method 1: Single Player Testing (Quick Test)**

This is the fastest way to verify basic bow functionality.

#### Steps:

1. **Open Unity Editor**
   - Load the project
   - Wait for compilation to complete

2. **Open Menu Scene**
   - Navigate to your menu scene (usually `MenuScene` or similar)
   - Press Play

3. **Start Single Player Game**
   - Click on Single Player / AI Mode button
   - Select difficulty if prompted
   - Game scene should load

4. **Test Bow Functionality:**

   **✅ Check 1: Bow Auto-Rotation**
   - Observe the left player's bow
   - **Expected:** Bow should automatically rotate up and down continuously
   - **Rotation Range:** Should rotate between max up angle (70°) and max down angle (-30°)
   - **Speed:** Should rotate smoothly at ~45 degrees/second

   **✅ Check 2: Input Response**
   - Click/Hold left mouse button (or touch on mobile)
   - **Expected:** 
     - Bow should stop auto-rotating
     - Force meter bar should appear and fill up/down
     - Force meter should oscillate between 0 and 1

   **✅ Check 3: Arrow Release**
   - Release mouse button (or lift finger)
   - **Expected:**
     - Arrow should spawn at the bow's shoot point
     - Arrow should fly in the direction the bow is pointing
     - Force meter should disappear
     - Bow should resume auto-rotation

   **✅ Check 4: Console Logs**
   - Open Unity Console (Window → General → Console)
   - **Expected:** No errors related to:
     - `BowController`
     - `PlayerController`
     - `ArrowduelNakamaClient` (should not appear in single player)

---

### **Method 2: Multiplayer Testing (ParrelSync - Recommended)**

ParrelSync allows you to run two Unity Editor instances simultaneously for easy testing.

#### Setup ParrelSync:

1. **Create Clone Instance:**
   - In Unity Editor: `ParrelSync` → `Clones` → `Create New Clone`
   - Wait for clone to be created
   - A new Unity Editor window will open automatically

2. **Configure Both Instances:**
   - **Instance 1 (Original):** Use username `Player1`
   - **Instance 2 (Clone):** Use username `Player2`

#### Testing Steps:

1. **Start Both Unity Instances**
   - Original instance: Already open
   - Clone instance: Open from ParrelSync menu

2. **Start Nakama Server** (if not already running)
   ```bash
   docker-compose up -d
   ```

3. **Connect Both Players:**

   **Instance 1 (Player1):**
   - Press Play
   - Enter username: `Player1`
   - Click "Connect" button
   - Wait for "Searching for opponent..." message

   **Instance 2 (Player2):**
   - Press Play
   - Enter username: `Player2`
   - Click "Connect" button
   - Both should connect and load GameScene

4. **Test Bow Functionality:**

   **✅ Check 1: Player 1 (Host) Bow**
   - **Expected:**
     - Bow should auto-rotate continuously
     - Mouse click should start charging
     - Force meter should appear and oscillate
     - Mouse release should shoot arrow
     - Arrow should spawn and fly correctly

   **✅ Check 2: Player 2 (Non-Host) Bow**
   - **Expected:**
     - Bow should auto-rotate continuously
     - Mouse click should start charging
     - Force meter should appear and oscillate
     - Mouse release should shoot arrow
     - Arrow should spawn and fly correctly

   **✅ Check 3: Network Synchronization**
   - Player 1 shoots an arrow
   - **Expected:** Arrow should appear on both screens
   - Player 2 shoots an arrow
   - **Expected:** Arrow should appear on both screens

   **✅ Check 4: Console Logs**
   - Check Console in both instances
   - **Expected:** Look for:
     ```
     [PlayerController] Update called
     [BowController] AutoRotate called
     [BowController] UpdateForceMeter called
     ```
   - **No errors** related to:
     - `NakamaClient` (should use `ArrowduelNakamaClient`)
     - `UpdatePlayerBehavior` not being called
     - Null reference exceptions

---

## What to Verify

### ✅ **Critical Checks:**

1. **Bow Rotation:**
   - [ ] Bow rotates automatically when not charging
   - [ ] Rotation is smooth (no stuttering)
   - [ ] Rotation stops when charging starts
   - [ ] Rotation resumes after arrow is released

2. **Force Meter:**
   - [ ] Force meter appears when clicking/holding
   - [ ] Force meter fills up and down smoothly
   - [ ] Force meter disappears after arrow release
   - [ ] Force meter value affects arrow speed

3. **Arrow Shooting:**
   - [ ] Arrow spawns at correct position (bow shoot point)
   - [ ] Arrow flies in correct direction (bow rotation angle)
   - [ ] Arrow speed matches force meter value
   - [ ] Multiple arrows can be shot sequentially

4. **Multiplayer Specific:**
   - [ ] Both players can control their bows independently
   - [ ] Bow rotation syncs across network
   - [ ] Input syncs across network
   - [ ] Arrows spawn correctly for both players

---

## Common Issues & Solutions

### ❌ **Issue: Bow Not Rotating**

**Symptoms:**
- Bow stays in one position
- No auto-rotation movement

**Debug Steps:**
1. Check Console for errors
2. Verify `UpdatePlayerBehavior()` is being called:
   - Add debug log: `//Debug.Log("[PlayerController] UpdatePlayerBehavior called");`
3. Check `playerType` is set to `Player` (not `None` or `Ai`)
4. Verify `bowParent` reference is assigned in prefab

**Solution:**
- Ensure `UpdatePlayerBehavior()` is called in `Update()` method
- Check prefab has correct references assigned

---

### ❌ **Issue: Force Meter Not Appearing**

**Symptoms:**
- Clicking doesn't show force meter
- No visual feedback when charging

**Debug Steps:**
1. Check `fillbarParentObj` is assigned in prefab
2. Check `fillbarImage` is assigned in prefab
3. Verify `StartCharging()` is being called:
   - Add debug log: `//Debug.Log("[BowController] StartCharging called");`

**Solution:**
- Ensure UI references are assigned in prefab
- Check `HandleInput()` is being called

---

### ❌ **Issue: Arrow Not Shooting**

**Symptoms:**
- Clicking charges but arrow doesn't spawn
- No arrow appears on release

**Debug Steps:**
1. Check `arrowSpawnPoint` is assigned in prefab
2. Check `arrowPrefab` is assigned in prefab
3. Verify `ReleaseArrow()` is being called:
   - Add debug log: `//Debug.Log("[BowController] ReleaseArrow called");`
4. Check Console for instantiation errors

**Solution:**
- Ensure all prefab references are assigned
- Check arrow prefab exists and is valid

---

### ❌ **Issue: Multiplayer - Only One Player Can Control Bow**

**Symptoms:**
- Player 1 can control bow, Player 2 cannot
- Or vice versa

**Debug Steps:**
1. Check Console logs for:
   ```
   [PlayerController] isPlayer1: true/false
   ```
2. Verify `ArrowduelNakamaClient.Instance` is not null
3. Check `CurrentMatch` is not null
4. Verify presence detection logic

**Solution:**
- Ensure both players are properly connected
- Check `ArrowduelNakamaClient` is initialized correctly
- Verify match is created and both players joined

---

### ❌ **Issue: Wrong Client Class Error**

**Symptoms:**
- Console shows: `NakamaClient.Instance is null`
- Or: `'NakamaClient' does not exist`

**Debug Steps:**
1. Search for `NakamaClient` in codebase
2. Verify all references use `ArrowduelNakamaClient`

**Solution:**
- Replace all `NakamaClient` with `ArrowduelNakamaClient`
- Use `Session?.UserId` instead of `UserId`

---

## Debug Logging

Add these debug logs to verify functionality:

### In PlayerController.cs:

```csharp
private void Update()
{
    if (GameManager.instance.gameState != GameState.Gameplay || playerPowerUp.isFrozen)
        return;

    //Debug.Log($"[PlayerController] Update - GameMode: {GameManager.gameMode}");
    
    if (GameManager.gameMode == GameModeType.MULTIPLAYER)
    {
        // ... existing code ...
        if (isPlayer1)
        {
            //Debug.Log("[PlayerController] Player 1 - Handling input and updating behavior");
            HandleInput();
            UpdatePlayerBehavior();
        }
    }
    else
    {
        //Debug.Log("[PlayerController] Single player - Handling input and updating behavior");
        HandleInput();
        UpdatePlayerBehavior();
    }
}

public void UpdatePlayerBehavior()
{
    //Debug.Log($"[PlayerController] UpdatePlayerBehavior - playerType: {playerType}, isCharging: {isCharging}");
    
    if (playerType == PlayerType.Player)
    {
        if (!isCharging)
        {
            //Debug.Log("[PlayerController] AutoRotate called");
            AutoRotate();
        }
        else
        {
            //Debug.Log("[PlayerController] UpdateForceMeter called");
            UpdateForceMeter();
        }
    }
}
```

### In BowController.cs:

```csharp
public void AutoRotate()
{
    //Debug.Log($"[BowController] AutoRotate - angle: {currentAutoRotationAngle}, bowParent: {(bowParent != null ? "EXISTS" : "NULL")}");
    
    // ... existing code ...
}

public void StartCharging()
{
    //Debug.Log("[BowController] StartCharging called");
    // ... existing code ...
}

public void ReleaseArrow()
{
    //Debug.Log($"[BowController] ReleaseArrow - currentForce: {currentForce}, arrowSpawnPoint: {(arrowSpawnPoint != null ? "EXISTS" : "NULL")}");
    // ... existing code ...
}
```

---

## Expected Console Output

### Single Player Mode:

```
[GameManager] === GAMESCENE LOADED ===
[GameManager] GameMode: SINGLEPLAYER
[PlayerController] Update - GameMode: SINGLEPLAYER
[PlayerController] Single player - Handling input and updating behavior
[PlayerController] UpdatePlayerBehavior - playerType: Player, isCharging: False
[PlayerController] AutoRotate called
[BowController] AutoRotate - angle: 70, bowParent: EXISTS
```

### Multiplayer Mode (Player 1):

```
[GameManager] === GAMESCENE LOADED ===
[GameManager] GameMode: MULTIPLAYER
[PlayerController] Update - GameMode: MULTIPLAYER
[PlayerController] Player 1 - Handling input and updating behavior
[PlayerController] UpdatePlayerBehavior - playerType: Player, isCharging: False
[PlayerController] AutoRotate called
[BowController] AutoRotate - angle: 70, bowParent: EXISTS
```

---

## Quick Test Checklist

### Single Player:
- [ ] Bow rotates automatically
- [ ] Click starts charging
- [ ] Force meter appears and oscillates
- [ ] Release shoots arrow
- [ ] Arrow flies correctly

### Multiplayer:
- [ ] Both players can control their bows
- [ ] Bow rotation works for both
- [ ] Force meter works for both
- [ ] Arrows spawn correctly
- [ ] Network synchronization works

---

## Next Steps

After verifying all checks pass:

1. **Remove debug logs** (optional, for production)
2. **Test edge cases:**
   - Rapid clicking
   - Holding charge for long time
   - Multiple arrows in quick succession
   - Network disconnection/reconnection

3. **Performance testing:**
   - Check frame rate during gameplay
   - Monitor network bandwidth usage
   - Test with high latency

---

## Support

If issues persist:

1. Check Unity Console for errors
2. Verify all prefab references are assigned
3. Ensure Nakama server is running (for multiplayer)
4. Check network connectivity (for multiplayer)
5. Review `TESTING_SYNCHRONIZATION.md` for network issues

---

**Last Updated:** After bow functionality fixes
**Tested With:** Unity Editor, ParrelSync, Nakama Server
