# Testing Multiplayer Screen Synchronization Guide

This guide explains how to test that both players' screens are synchronized in multiplayer mode.

## Prerequisites

### 1. Start Nakama Server

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
- Open browser: http://localhost:7350
- You should see Nakama API response (JSON or HTML)

### 2. Unity Project Setup

- Open Unity Editor
- Wait for all scripts to compile (no errors in Console)
- Ensure `ArrowduelConnectionManager` is configured:
  - Server Host: `127.0.0.1`
  - Server Port: `7350`
  - Server Key: `defaultkey`
  - Use SSL: `false`

---

## Testing Methods

### **Method 1: ParrelSync (Recommended for Testing)**

ParrelSync allows you to run two Unity Editor instances simultaneously for easy testing.

#### Setup ParrelSync:

1. **Create Clone Instance:**
   - In Unity Editor: `ParrelSync` → `Clones` → `Create New Clone`
   - Wait for clone to be created
   - A new Unity Editor window will open automatically

2. **Important Notes:**
   - **Original Editor**: Use this for making code/prefab changes
   - **Clone Editor**: Use this ONLY for testing (don't save changes here)
   - If you see "Asset modifications saving detected and blocked" - this is normal!

#### Testing Steps:

1. **Start Original Editor (Player 1):**
   - Open scene: `Assets/_Developer/_Scenes/menu.unity` (or your menu scene)
   - Enter username: **"Player1"**
   - Click **"Connect"** button
   - Status should show: "Connecting..." → "Searching for players..."
   - **Keep this window open**

2. **Start Clone Editor (Player 2):**
   - In the clone Unity Editor window
   - Open same scene: `Assets/_Developer/_Scenes/menu.unity`
   - Enter username: **"Player2"** (must be different!)
   - Click **"Connect"** button
   - Status should show: "Connecting..." → "Searching for players..."

3. **Wait for Match:**
   - Both instances should find each other within 10-30 seconds
   - Status should change to: "Match found!" → "Joining match..."
   - Both should load `GameScene` simultaneously

4. **Verify Synchronization:**
   - Both screens should show the same game state
   - See "What to Check" section below

---

### **Method 2: Unity Editor + Build**

Test with one Unity Editor instance and one built executable.

#### Steps:

1. **Build the Game:**
   - `File` → `Build Settings`
   - Select platform (Windows/Mac/Linux)
   - Click **"Build"**
   - Save to folder (e.g., `Builds/ArrowDuel`)

2. **Start Unity Editor (Player 1):**
   - Open menu scene
   - Enter username: **"Player1"**
   - Click **"Connect"**
   - Wait for "Searching for players..."

3. **Start Build (Player 2):**
   - Run the built executable
   - Enter username: **"Player2"**
   - Click **"Connect"**
   - Both should match and start game

---

### **Method 3: Two Builds**

Test with two separate build instances.

#### Steps:

1. **Build the Game:**
   - Build to a folder (e.g., `Builds/ArrowDuel`)

2. **Run Two Instances:**
   - Run the executable **twice** (double-click twice)
   - Or copy the build folder and run from different locations
   - **Note:** Some platforms may require different launch arguments

3. **Connect Both:**
   - Instance 1: Enter "Player1" → Connect
   - Instance 2: Enter "Player2" → Connect
   - Both should match and start game

---

## What to Check for Synchronization

### ✅ **1. Connection & Matchmaking**

**Check Console Logs (Both Instances):**

Look for these log messages in Unity Console:

```
[ArrowduelConnectionManager] === USERNAME INPUT DEBUG ===
[ArrowduelNakamaClient] Server configured: http://127.0.0.1:7350
[ArrowduelNakamaClient] Connecting to server: http://127.0.0.1:7350
[ArrowduelNakamaClient] Authenticated! UserID: ..., Username: ...
[ArrowduelNakamaClient] Socket connected successfully!
[ArrowduelNakamaClient] ✓ Matchmaking ticket created!
[ArrowduelNakamaClient] ⏳ Waiting for match...
[ArrowduelConnectionManager] Match found! Joining match...
[ArrowduelConnectionManager] Match joined successfully!
[ArrowduelConnectionManager] Both players ready! Transitioning to game...
[GameManager] === GAMESCENE LOADED ===
```

**Expected Behavior:**
- Both instances should show similar logs
- Match ID should be the same on both instances
- Both should transition to GameScene at the same time

---

### ✅ **2. Player Spawning**

**Check Both Screens:**

- **Player 1 (Left Side):**
  - Should see their own player on the left
  - Should see Player 2 on the right
  - Player names should be correct

- **Player 2 (Right Side):**
  - Should see Player 1 on the left
  - Should see their own player on the right
  - Player names should be correct

**Check Console Logs:**

```
[GameManager] Player order determined: isPlayer1 = true/false
[GameManager] === PLAYER NAMES DEBUG ===
[GameManager] Player 1 Name: Player1
[GameManager] Player 2 Name: Player2
[GameManager] Spawning Player 1...
[GameManager] Spawning Player 2...
```

**Expected Behavior:**
- Both players spawn correctly
- Player names match on both screens
- One player is Player 1 (left), one is Player 2 (right)

---

### ✅ **3. Game State Synchronization**

#### **Wind Events:**

**Test:**
- Wait for wind to activate (or trigger manually if host)
- Wind should appear on **both screens simultaneously**

**Check Console:**
```
[ArrowduelNetworkManager] Wind_RPC called (Host only)
[ArrowduelNetworkManager] HandleWind received
```

**Expected Behavior:**
- Wind direction and force should be identical on both screens
- Wind indicators should show the same direction
- Wind should start/stop at the same time

#### **Power-Ups:**

**Test:**
- Wait for power-up to spawn
- Power-up should appear on **both screens at the same location**

**Check Console:**
```
[ArrowduelNetworkManager] PowerUp_RPC called
[ArrowduelNetworkManager] HandlePowerUp received
```

**Expected Behavior:**
- Power-up spawns at same position on both screens
- Power-up type is the same
- Power-up disappears when collected (on both screens)

#### **Arrow Hits:**

**Test:**
- Player 1 shoots an arrow
- Arrow hits target
- Hit should register on **both screens**

**Check Console:**
```
[ArrowduelNetworkManager] OnHitTarget_RPC called
[ArrowduelNetworkManager] HandleHitTarget received
```

**Expected Behavior:**
- Hit effect appears on both screens
- Score updates on both screens
- Health decreases on both screens

#### **Level/Theme Changes:**

**Test:**
- Wait for level to change (or trigger manually if host)
- Level should change on **both screens simultaneously**

**Check Console:**
```
[ArrowduelNetworkManager] OnChangeLevel_RPC called
[ArrowduelNetworkManager] HandleLevelChange received
[ArrowduelNetworkManager] ChangeTheme_RPC called
[ArrowduelNetworkManager] HandleThemeChange received
```

**Expected Behavior:**
- Background changes on both screens
- Level number updates on both screens
- Theme changes on both screens

---

### ✅ **4. Input Synchronization**

**Test:**
- **Player 1 (Host):** Shoot an arrow
- **Player 2:** Should see Player 1's arrow fly
- **Player 2:** Shoot an arrow
- **Player 1:** Should see Player 2's arrow fly

**Expected Behavior:**
- Only host can control certain game events (wind, level changes)
- Both players can shoot arrows
- Arrow positions are synchronized (or hits are synchronized)

---

### ✅ **5. Game Completion**

**Test:**
- Play until someone wins
- Game should end on **both screens simultaneously**

**Check Console:**
```
[ArrowduelNetworkManager] GameCompleted_RPC called
[ArrowduelNetworkManager] HandleGameCompleted received
```

**Expected Behavior:**
- Winner is the same on both screens
- Final scores match on both screens
- Game over screen appears on both screens

---

## Debugging Synchronization Issues

### **Problem: Players Don't Connect**

**Check:**
1. Nakama server is running: `docker-compose ps`
2. Server is accessible: http://localhost:7350
3. Port is correct: Both should use port `7350`
4. Firewall is not blocking port `7350`

**Solution:**
```bash
# Restart server
docker-compose restart nakama

# Check logs
docker-compose logs -f nakama
```

---

### **Problem: Match Found But Game Doesn't Start**

**Check Console Logs:**
- Look for `[ArrowduelConnectionManager]` messages
- Check for `[GameManager]` messages
- Look for errors or warnings

**Common Issues:**
- Ready signal not sent/received
- Scene transition failed
- Player spawning failed

**Solution:**
- Check that both instances have `ArrowduelConnectionManager` in scene
- Verify `GameManager` exists in GameScene
- Check that player prefabs are assigned

---

### **Problem: Game State Not Syncing**

**Check:**
1. **Host Authority:**
   - Only host should control wind, level changes
   - Check console for `[ArrowduelNakamaClient] Is Host: true/false`

2. **Network Manager:**
   - Verify `ArrowduelNetworkManager` exists in scene
   - Check that it's subscribed to match state events

3. **OpCode Messages:**
   - Look for `[ArrowduelNetworkManager]` logs
   - Verify OpCode messages are being sent/received

**Debug Code:**
Add this to check host status:
```csharp
if (ArrowduelNakamaClient.Instance != null)
{
    //Debug.Log($"[DEBUG] Is Host: {ArrowduelNakamaClient.Instance.IsHost}");
    //Debug.Log($"[DEBUG] Current Match: {(ArrowduelNakamaClient.Instance.CurrentMatch != null ? "EXISTS" : "NULL")}");
}
```

---

### **Problem: Players See Different Things**

**Check:**
1. **Player Order:**
   - Player 1 should be on left, Player 2 on right
   - Check console logs for player order determination

2. **Match State:**
   - Verify both are in the same match
   - Check match ID is the same on both instances

3. **Network Events:**
   - Check that OpCode messages are being received
   - Verify handlers are executing

---

## Quick Test Checklist

Use this checklist to verify synchronization:

### Connection Phase:
- [ ] Nakama server is running (`docker-compose ps`)
- [ ] Both instances connect to server (check logs)
- [ ] Both instances authenticate successfully
- [ ] Both instances create matchmaking tickets
- [ ] Match is found on both instances
- [ ] Match ID is the same on both instances

### Game Start Phase:
- [ ] Both instances load GameScene simultaneously
- [ ] Players spawn correctly (Player 1 left, Player 2 right)
- [ ] Player names are correct on both screens
- [ ] Both players can see each other

### Gameplay Phase:
- [ ] Wind events sync (same direction, same force)
- [ ] Power-ups spawn at same location on both screens
- [ ] Arrow hits register on both screens
- [ ] Scores update on both screens
- [ ] Level changes sync (same level number)
- [ ] Theme changes sync (same background)

### Game End Phase:
- [ ] Game ends simultaneously on both screens
- [ ] Winner is the same on both screens
- [ ] Final scores match on both screens

---

## Advanced Testing

### **Test Network Latency:**

Add artificial delay to test synchronization under latency:

1. Modify `ArrowduelNakamaClient.cs`:
   ```csharp
   // Add delay before sending match state
   await Task.Delay(100); // 100ms delay
   await socket.SendMatchStateAsync(...);
   ```

2. Test with different delays (50ms, 100ms, 200ms)
3. Verify game still syncs correctly

### **Test Disconnection:**

1. Disconnect one player (close window or stop server)
2. Verify other player handles disconnection gracefully
3. Check for reconnection logic

### **Test Multiple Matches:**

1. Start 4 instances (2 matches)
2. Verify each match is independent
3. Check that players don't interfere with each other

---

## Console Log Reference

### **Successful Connection:**
```
[ArrowduelConnectionManager] Username stored: Player1
[ArrowduelNakamaClient] Server configured: http://127.0.0.1:7350
[ArrowduelNakamaClient] Authenticated! UserID: abc123...
[ArrowduelNakamaClient] Socket connected successfully!
[ArrowduelNakamaClient] ✓ Matchmaking ticket created!
[ArrowduelConnectionManager] Match found! Joining match...
[ArrowduelConnectionManager] Match joined successfully!
[ArrowduelConnectionManager] Both players ready! Transitioning to game...
```

### **Successful Synchronization:**
```
[ArrowduelNetworkManager] Wind_RPC called (Host only)
[ArrowduelNetworkManager] HandleWind received - Direction: Right, Force: 5.0
[ArrowduelNetworkManager] PowerUp_RPC called
[ArrowduelNetworkManager] HandlePowerUp received - Type: Freeze
[ArrowduelNetworkManager] OnHitTarget_RPC called
[ArrowduelNetworkManager] HandleHitTarget received - isPlayerArrow: true
```

---

## Tips for Testing

1. **Use ParrelSync** for easiest testing (no need to build)
2. **Check Console Logs** on both instances simultaneously
3. **Use Different Usernames** to easily identify which instance is which
4. **Test One Feature at a Time** (wind, then power-ups, then hits)
5. **Verify Host Authority** - only host should control certain events
6. **Check Timing** - events should happen within 100-200ms on both screens

---

## Troubleshooting Commands

```bash
# Check server status
docker-compose ps

# View server logs
docker-compose logs -f nakama

# Restart server
docker-compose restart nakama

# Check server health
curl http://localhost:7350

# Reset everything
docker-compose down -v
docker-compose up -d
```

---

## Next Steps

After verifying synchronization works:

1. **Test with Real Network Latency** (different machines)
2. **Add Reconnection Logic** (handle disconnections)
3. **Optimize Network Messages** (reduce bandwidth)
4. **Add Lag Compensation** (if needed)
5. **Test with 3+ Players** (if supported)

---

## Support

If synchronization is not working:

1. Check Unity Console for errors
2. Check Nakama server logs: `docker-compose logs nakama`
3. Verify server is accessible: http://localhost:7350
4. Check that both instances use same port (`7350`)
5. Verify network manager is subscribed to events
6. Check host authority is working correctly

For more details, see:
- `TESTING_NAKAMA.md` - General testing guide
- `QUICK_START.md` - Server setup
- `TROUBLESHOOTING.md` - Common issues
