# Troubleshooting Guide

## Issue 1: "The Object you want to instantiate is null"

### Problem
```
ArgumentException: The Object you want to instantiate is null.
GameManager.Bot (AiSkillLevels skillLevels)
```

### Solution
The `player1NetworkStatePrefab` and/or `player2NetworkStatePrefab` are not assigned in the Unity Inspector.

**Steps to Fix:**
1. Open Unity Editor
2. Select the **GameManager** GameObject in the scene hierarchy
3. In the Inspector, find the **GameManager** component
4. Under **"MULTIPLAYER REF."** section, you'll see:
   - `Player1 Network State Prefab`
   - `Player2 Network State Prefab`
5. **Drag and drop** the player prefabs from your Project window into these fields
6. Save the scene

**Where to find the prefabs:**
- Look in your `Assets` folder for prefabs like:
  - `Player1NetworkState` or `Player1Prefab`
  - `Player2NetworkState` or `Player2Prefab`
- They might be in folders like:
  - `Assets/Prefabs/`
  - `Assets/_Developer/Prefabs/`
  - `Assets/Resources/`

---

## Issue 2: Two Players Not Connecting to Each Other

### Problem
When testing multiplayer, two players don't connect/match with each other.

### Debugging Steps

#### 1. Check Nakama Server is Running
```bash
docker compose ps
```
Both `nakama-cockroachdb` and `nakama-server` should be running.

#### 2. Check Unity Console Logs
Look for these log messages:
- `[NakamaConnectionManager] Connecting as: PlayerName`
- `[NakamaConnectionManager] Match joined. Player count: X`
- `[NakamaConnectionManager] Player(s) joined. Total: X`

#### 3. Verify Both Players are Connecting
**Player 1:**
- Should see: `[NakamaConnectionManager] Match joined. Player count: 1`
- Should see: `[NakamaConnectionManager] Waiting for opponent...`

**Player 2:**
- Should see: `[NakamaConnectionManager] Match joined. Player count: 2`
- Should see: `[NakamaConnectionManager] Two players found! Starting game...`

#### 4. Check Matchmaking Settings
In `NakamaConnectionManager.cs` Inspector:
- **Server Host**: `127.0.0.1`
- **Server Port**: `7350`
- **Server Key**: `defaultkey`
- **Matchmaking Timeout**: `15` seconds

#### 5. Common Issues

**Issue: Players connect but don't match**
- **Cause**: Matchmaking query might be too restrictive
- **Fix**: Check `NakamaClient.cs` - ensure matchmaking query is `"*"` (matches any player)

**Issue: Timeout before matching**
- **Cause**: Players connecting too far apart in time
- **Fix**: 
  - Both players should click "Connect" within 15 seconds
  - Increase `matchmakingTimeout` in `NakamaConnectionManager.cs`

**Issue: Connection fails**
- **Cause**: Nakama server not accessible
- **Fix**: 
  - Verify server is running: `docker compose ps`
  - Check server logs: `docker compose logs nakama`
  - Verify firewall allows port 7350

**Issue: Match found but game doesn't start**
- **Cause**: `GameManager.instance` might be null or prefabs not assigned
- **Fix**: 
  - Ensure GameManager exists in GameScene
  - Assign player prefabs (see Issue 1 above)
  - Check Unity Console for errors

---

## Issue 3: Players Spawn But Game Doesn't Sync

### Problem
Both players spawn but game state (wind, powerups, hits) doesn't sync.

### Solution

1. **Check Host Authority**
   - First player (alphabetically by UserId) is the host
   - Host controls wind, powerups, level changes
   - Check logs: `[GameManager] Playing as Player 1` or `Player 2`

2. **Verify NakamaNetworkManager**
   - Should be created automatically for Player 2
   - Check Unity Hierarchy for `NakamaNetworkManager` GameObject
   - Check logs: `[NakamaNetworkManager]` messages

3. **Check OpCode Messages**
   - Look for `[NakamaNetworkManager]` logs showing OpCode messages
   - OpCodes: 1=GameStart, 3=LevelChange, 4=ThemeChange, 5=Wind, 6=PowerUp, 7=HitTarget

---

## Quick Checklist

Before testing multiplayer:

- [ ] Nakama server is running (`docker compose ps`)
- [ ] Player prefabs assigned in GameManager Inspector (**in ORIGINAL editor if using ParrelSync**)
- [ ] Scene saved (**in ORIGINAL editor if using ParrelSync**)
- [ ] NakamaConnectionManager settings correct (host, port, key)
- [ ] Both Unity instances have same server settings
- [ ] Both players click "Connect" within 15 seconds
- [ ] Check Unity Console for `[NakamaConnectionManager]` logs
- [ ] Verify match is found: `Match joined. Player count: 2`
- [ ] Check players spawn: `Playing as Player 1` / `Playing as Player 2`

### **ParrelSync Users:**
- [ ] All asset changes made in **ORIGINAL** editor instance
- [ ] Scene saved in **ORIGINAL** editor before testing
- [ ] Clone instance used only for testing (don't try to save in clone)

---

## Debug Commands

### Check Server Status
```bash
docker compose ps
docker compose logs nakama --tail 50
```

### Restart Server
```bash
docker compose restart nakama
```

### Reset Everything
```bash
docker compose down -v
docker compose up -d
```

### Check Network Connectivity
```bash
# Windows PowerShell
Test-NetConnection -ComputerName localhost -Port 7350

# Or use browser
# Open: http://localhost:7350
```

---

## Getting More Help

If issues persist:

1. **Check Unity Console** for all `[NakamaClient]`, `[NakamaConnectionManager]`, and `[NakamaNetworkManager]` logs
2. **Check Server Logs**: `docker compose logs nakama`
3. **Verify Prefabs**: Ensure player prefabs exist and are assigned
4. **Test Single Player First**: Verify AI mode works before testing multiplayer
5. **Check Network**: Ensure both Unity instances can reach `127.0.0.1:7350`

---

## Expected Log Flow for Successful Multiplayer

**Player 1:**
```
[NakamaConnectionManager] Connecting as: Player1
[NakamaClient] Authenticated. UserId: xxx
[NakamaClient] Socket connected
[NakamaClient] Matchmaking started. Ticket: xxx
[NakamaConnectionManager] Match joined. Player count: 1
[NakamaConnectionManager] Waiting for opponent...
[NakamaConnectionManager] Player(s) joined. Total: 2
[NakamaConnectionManager] Two players ready! Starting game...
[GameManager] Playing as Player 1
```

**Player 2:**
```
[NakamaConnectionManager] Connecting as: Player2
[NakamaClient] Authenticated. UserId: yyy
[NakamaClient] Socket connected
[NakamaClient] Matchmaking started. Ticket: yyy
[NakamaConnectionManager] Match joined. Player count: 2
[NakamaConnectionManager] Two players found! Starting game...
[GameManager] Playing as Player 2
```

If your logs don't match this flow, check where they differ!
