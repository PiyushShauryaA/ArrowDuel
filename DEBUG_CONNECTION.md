# Debug Guide: Two Player Connection Issue

## Overview

This guide explains the comprehensive debug logging added to troubleshoot why two players are not connecting to each other.

## Debug Logs Added

### 1. **GameScene Loading** (`GameManager.Start()`)
When GameScene loads, you'll see:
```
[GameManager] === GAMESCENE LOADED ===
[GameManager] GameMode: MULTIPLAYER, IsAIMode: False, PlayerName: Player1
[GameManager] Current Scene: GameScene
[GameManager] NakamaConnectionManager.Instance: EXISTS/NULL
[GameManager] NakamaClient.Instance: EXISTS/NULL
```

**What to check:**
- Is `GameMode` set to `MULTIPLAYER`?
- Is `IsAIMode` set to `False`?
- Is `PlayerName` set correctly?
- Do both managers exist?

### 2. **LoginMenu Connection** (`LoginMenu.OnConnectButtonClicked()`)
When Connect button is clicked:
```
[LoginMenu] === CONNECT BUTTON CLICKED ===
[LoginMenu] PlayerName: Player1
[LoginMenu] GameMode set to MULTIPLAYER
[LoginMenu] Loading GameScene...
```

**What to check:**
- Is the button click registered?
- Is name validation passing?

### 3. **Multiplayer Game Start** (`GameManager.StartMultiplayerGame()`)
When multiplayer mode is detected:
```
[GameManager] === StartMultiplayerGame CALLED ===
[GameManager] Waiting for NakamaConnectionManager to be ready...
[GameManager] NakamaConnectionManager created ✓ (or already exists ✓)
[GameManager] NakamaClient created ✓ (or already exists ✓)
[GameManager] === INITIATING CONNECTION ===
[GameManager] PlayerName: Player1
```

**What to check:**
- Are managers being created?
- Is connection being initiated?

### 4. **Connection Initiation** (`NakamaConnectionManager.ConnectToServer()`)
When connection starts:
```
[NakamaConnectionManager] === ConnectToServer CALLED ===
[NakamaConnectionManager] MatchId: , Region: 
[NakamaConnectionManager] Server: http://127.0.0.1:7350
[NakamaConnectionManager] ServerKey: defaultkey, UseSSL: False
[NakamaConnectionManager] Configuring server settings...
[NakamaConnectionManager] === STARTING CONNECTION ===
[NakamaConnectionManager] PlayerName: Player1
[NakamaConnectionManager] Calling ConnectAndStartMatchmakingAsync...
```

**What to check:**
- Is server address correct?
- Is server key correct?
- Is connection method being called?

### 5. **Nakama Client Connection** (`NakamaClient.ConnectAndStartMatchmakingAsync()`)
During authentication and socket setup:
```
[NakamaClient] === ConnectAndStartMatchmakingAsync START ===
[NakamaClient] Username: Player1
[NakamaClient] Server: http://127.0.0.1:7350
[NakamaClient] Creating client with URI: http://127.0.0.1:7350
[NakamaClient] Client created successfully
[NakamaClient] Authenticating with device ID: [device-id]
[NakamaClient] === AUTHENTICATION SUCCESS ===
[NakamaClient] Username: Player1
[NakamaClient] UserId: [user-id]
[NakamaClient] SessionId: [session-id]
[NakamaClient] Creating socket...
[NakamaClient] Socket created. Setting up event handlers...
[NakamaClient] Event handlers registered
[NakamaClient] Connecting socket...
[NakamaClient] === SOCKET CONNECTED ===
[NakamaClient] Socket.IsConnected: True
[NakamaClient] Starting matchmaking...
[NakamaClient] Matchmaking query: *, Min: 2, Max: 2
[NakamaClient] === MATCHMAKING STARTED ===
[NakamaClient] Matchmaking Ticket: [ticket-id]
[NakamaClient] Waiting for match...
```

**What to check:**
- Is authentication successful?
- Is socket connecting?
- Is matchmaking starting?
- Is ticket ID generated?

### 6. **Match Found** (`NakamaClient.OnMatchmakerMatched()`)
When a match is found:
```
[NakamaClient] === MATCH FOUND ===
[NakamaClient] MatchId: [match-id]
[NakamaClient] Token: [token]
[NakamaClient] Users in match: 2
[NakamaClient] User 0: UserId=[user-id-1], Username=Player1
[NakamaClient] User 1: UserId=[user-id-2], Username=Player2
[NakamaClient] Joining match...
[NakamaClient] === MATCH JOINED ===
[NakamaClient] MatchId: [match-id]
[NakamaClient] Presences count: 1
[NakamaClient] Total players (including self): 2
[NakamaClient] Presence 0: UserId=[user-id], Username=[username], SessionId=[session-id]
[NakamaClient] Current UserId: [current-user-id]
[NakamaClient] Invoking MatchJoined event...
```

**What to check:**
- Is match found?
- Are both users in the match?
- Is match joined successfully?
- Are presences correct?

### 7. **Match Joined Handler** (`NakamaConnectionManager.HandleMatchJoined()`)
When match joined event is processed:
```
[NakamaConnectionManager] === HandleMatchJoined CALLED ===
[NakamaConnectionManager] === MATCH JOINED ===
[NakamaConnectionManager] Match ID: [match-id]
[NakamaConnectionManager] Presences count: 1
[NakamaClient] Total player count (including self): 2
[NakamaConnectionManager] === PRESENCES LIST ===
[NakamaConnectionManager] Presence[0] - UserId: [user-id], Username: [username], SessionId: [session-id]
[NakamaConnectionManager] Current UserId: [current-user-id]
[NakamaConnectionManager] GameManager.instance: EXISTS/NULL
```

**What to check:**
- Is player count >= 2?
- Are presences correct?
- Is GameManager available?

### 8. **Player Spawning** (`GameManager.SpawnPlayerNakama()`)
When players are spawned:
```
[GameManager] === SpawnPlayerNakama CALLED ===
[GameManager] Prefabs are assigned ✓
[GameManager] No existing players found ✓
[GameManager] NakamaClient and CurrentMatch exist ✓
[GameManager] Presences count: 1
[GameManager] Sorted presences:
[GameManager]   [0] UserId: [user-id-1]
[GameManager] Current UserId: [current-user-id]
[GameManager] Is Player 1: True/False
[GameManager] === SPAWNING AS PLAYER 1 === (or PLAYER 2)
[GameManager] Player 1 prefab instantiated: SUCCESS
[GameManager] Player 1 setup complete ✓
[GameManager] === SpawnPlayerNakama COMPLETE ===
```

**What to check:**
- Are prefabs assigned?
- Is player role determined correctly?
- Are players spawning?

## Common Issues and Solutions

### Issue 1: Connection Never Starts
**Symptoms:**
- No `[NakamaConnectionManager] === ConnectToServer CALLED ===` log

**Possible Causes:**
- `GameManager.StartMultiplayerGame()` not being called
- `PlayerData.gameMode` not set to `MULTIPLAYER`
- `PlayerData.isAIMode` set to `True`

**Solution:**
- Check `PlayerData` values in `LoginMenu.OnConnectButtonClicked()`
- Ensure GameScene loads after LoginMenu

### Issue 2: Authentication Fails
**Symptoms:**
- `[NakamaClient] === CONNECTION ERROR ===` appears
- Error mentions authentication

**Possible Causes:**
- Nakama server not running
- Wrong server address/port
- Wrong server key

**Solution:**
- Check `docker compose ps` - both containers should be running
- Verify server settings in `NakamaConnectionManager` Inspector
- Check `local.yml` configuration

### Issue 3: Matchmaking Never Finds Match
**Symptoms:**
- `[NakamaClient] === MATCHMAKING STARTED ===` appears
- But no `[NakamaClient] === MATCH FOUND ===` log
- Timeout after 15 seconds

**Possible Causes:**
- Only one player connected
- Matchmaking query incorrect
- Nakama server matchmaking not working

**Solution:**
- Ensure BOTH players click Connect within 15 seconds
- Check Nakama server logs: `docker compose logs nakama`
- Verify matchmaking query: `"*", 2, 2` (any query, min 2, max 2)

### Issue 4: Match Found But Players Don't Spawn
**Symptoms:**
- `[NakamaClient] === MATCH FOUND ===` appears
- `[NakamaConnectionManager] === TWO PLAYERS FOUND! ===` appears
- But no `[GameManager] === SpawnPlayerNakama CALLED ===` log

**Possible Causes:**
- `GameManager.instance` is null
- `BeginMultiplayerGame()` not calling spawn

**Solution:**
- Check `GameManager` exists in scene
- Verify `NakamaConnectionManager.BeginMultiplayerGame()` is called

### Issue 5: Prefabs Not Assigned
**Symptoms:**
- `[GameManager] === PREFAB ASSIGNMENT ERROR ===` appears
- `player1NetworkStatePrefab: NULL` or `player2NetworkStatePrefab: NULL`

**Solution:**
- Open GameScene
- Select GameManager GameObject
- Assign `player1NetworkStatePrefab` and `player2NetworkStatePrefab` in Inspector
- Save scene (in ORIGINAL editor if using ParrelSync)

## Testing Checklist

1. **Before Testing:**
   - [ ] Nakama server running (`docker compose ps`)
   - [ ] Player prefabs assigned in GameManager Inspector
   - [ ] Scene saved (in original editor if using ParrelSync)

2. **Player 1:**
   - [ ] Open LoginScene
   - [ ] Enter name: "Player1"
   - [ ] Click "Connect"
   - [ ] Check logs for connection flow
   - [ ] Wait for match

3. **Player 2:**
   - [ ] Open LoginScene (in clone or second instance)
   - [ ] Enter name: "Player2"
   - [ ] Click "Connect" within 15 seconds
   - [ ] Check logs for connection flow
   - [ ] Wait for match

4. **Expected Flow:**
   - Both players see "Matchmaking started"
   - Both players see "Match found"
   - Both players see "Match joined"
   - Both players see "Two players found"
   - Both players spawn
   - Game starts

## Debug Commands

### Check Nakama Server Status
```bash
docker compose ps
```

### View Nakama Server Logs
```bash
docker compose logs nakama -f
```

### View Database Logs
```bash
docker compose logs cockroachdb -f
```

### Restart Nakama Server
```bash
docker compose restart nakama
```

## Next Steps

If connection still fails after checking all logs:

1. **Share the complete log output** from both players
2. **Check Nakama server logs** for server-side errors
3. **Verify network connectivity** between Unity and Docker
4. **Test with a single player** first (should timeout and fallback to AI)
5. **Check Unity Console** for any errors or warnings

The debug logs will help identify exactly where the connection process is failing!
