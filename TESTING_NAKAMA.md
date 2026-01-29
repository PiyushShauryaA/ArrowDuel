# Testing Nakama Integration Guide

## Prerequisites

### 1. Install Nakama Server

**Option A: Using Docker (Recommended)**
```bash
docker run -p 7349:7349 -p 7350:7350 -p 7351:7351 heroiclabs/nakama:latest
```

**Option B: Download Binary**
- Download from: https://github.com/heroiclabs/nakama/releases
- Extract and run: `nakama.exe` (Windows) or `./nakama` (Linux/Mac)
- Server will start on `127.0.0.1:7350`

**Verify Server is Running:**
- Open browser: http://127.0.0.1:7350
- You should see Nakama server status page

### 2. Unity Setup
- Open Unity Editor
- Wait for Nakama package to import (check Packages folder)
- Ensure no compilation errors in Console

---

## Testing Steps

### **Test 1: Single Player (AI) Mode**

1. **Open Unity Editor**
   - Open the project
   - Wait for compilation to complete

2. **Open LoginScene**
   - File → Open Scene → `Assets/Scenes/LoginScene.unity`
   - Or use the scene dropdown at top of Unity window

3. **Test AI Game**
   - Enter player name (e.g., "TestPlayer")
   - Click **"Play with AI"** button
   - Select AI difficulty from dropdown:
     - Easy
     - Normal
     - Hard
     - Robinhood
   - Game should load and start with AI opponent

4. **Expected Behavior:**
   - GameScene loads
   - Player spawns on left side
   - AI opponent spawns on right side
   - You can shoot arrows and play against AI
   - Game ends when someone wins

---

### **Test 2: Multiplayer Mode (Local Testing)**

#### **Method A: Using ParrelSync (Recommended)**

**Important:** If you're using ParrelSync, make all asset changes (prefab assignments, scene saves) in the **ORIGINAL** editor instance, not the clone!

1. **Setup (Do this FIRST in Original Editor):**
   - Assign player prefabs in GameManager Inspector (if not done)
   - Save the scene
   - **Close the original editor** (or keep it open but don't use it for testing)

2. **First Instance - Original Editor (Player 1):**
   - Open LoginScene
   - Enter name: **"Player1"**
   - Click **"Connect"** button
   - Wait for "Searching for players..." or "Waiting for opponent..." message
   - **Keep this instance running**

3. **Second Instance - ParrelSync Clone (Player 2):**
   - Use ParrelSync to open a clone instance
   - Open LoginScene in the clone
   - Enter name: **"Player2"**
   - Click **"Connect"** button
   - Both instances should connect and game should start
   - **Note:** If you see "Asset modifications saving detected and blocked" - this is normal! Don't try to save in the clone.

#### **Method B: Two Separate Unity Editor Windows (Without ParrelSync)**

1. **First Instance (Player 1):**
   - Open Unity Editor
   - Open LoginScene
   - Enter name: **"Player1"**
   - Click **"Connect"** button
   - Wait for "Searching for players..." or "Waiting for opponent..." message
   - **Keep this instance running**

2. **Second Instance (Player 2):**
   - Open **NEW Unity Editor window** (File → New Window)
   - Open same project
   - Open LoginScene
   - Enter name: **"Player2"**
   - Click **"Connect"** button
   - Both instances should connect and game should start

3. **Expected Behavior:**
   - Both players connect to Nakama server
   - Matchmaking finds match
   - GameScene loads on both instances
   - Player 1 spawns on left, Player 2 on right
   - Both can shoot arrows
   - Game state syncs (wind, powerups, hits)

#### **Method B: Build and Run Two Instances**

1. **Build the Game:**
   - File → Build Settings
   - Select platform (Windows/Mac/Linux)
   - Click **"Build"**
   - Save to a folder (e.g., `Builds/ArrowDuel`)

2. **Run Two Instances:**
   - Run the built game **twice** (double-click executable twice)
   - Or copy the build folder and run from different locations
   - Both should connect via Nakama server

3. **Test Multiplayer:**
   - Instance 1: Enter "Player1" → Connect
   - Instance 2: Enter "Player2" → Connect
   - Both should match and start game

---

### **Test 3: WebGL Testing (IFrameBridge)**

1. **Build for WebGL:**
   - File → Build Settings
   - Select **WebGL** platform
   - Click **"Build"**
   - Save to a folder

2. **Host on Web Server:**
   - Use any web server (IIS, Apache, Node.js, etc.)
   - Or use Unity's built-in server for testing

3. **Call from Parent Page:**
   ```javascript
   // Example JavaScript call
   gameInstance.SendMessage('MULTIPLAYER', 'InitParamsFromJS', JSON.stringify({
       matchId: "test-match-123",
       playerId: "player1",
       opponentId: "player2",
       region: "us"
   }));
   ```

4. **Expected Behavior:**
   - Game loads GameScene
   - Connects to Nakama with provided match info
   - Initializes multiplayer mode

---

## Configuration

### **Nakama Server Settings**

Edit `NakamaConnectionManager.cs` Inspector settings or modify code:

```csharp
// Default settings in NakamaConnectionManager.cs
serverHost = "127.0.0.1"  // Change to your server IP
serverPort = 7350         // Default Nakama port
serverKey = "defaultkey"  // Default key
useSSL = false            // Set to true for production
```

### **Matchmaking Settings**

In `NakamaClient.cs`:
- Matchmaking query: `"*"` (matches any player)
- Min players: `2`
- Max players: `2`
- Timeout: `15 seconds` (default)

---

## Troubleshooting

### **Connection Issues**

**Problem: "Connection failed" or "Cannot connect to server"**

**Solutions:**
1. **Check Nakama Server is Running**
   ```bash
   # Check if server is running
   curl http://127.0.0.1:7350
   # Should return server status
   ```

2. **Check Firewall**
   - Ensure port 7350 is open
   - Windows: Check Windows Firewall settings
   - Allow Unity/Nakama through firewall

3. **Check Server Logs**
   - Look at Nakama server console output
   - Check for connection errors

4. **Verify Server Settings**
   - Check `NakamaConnectionManager.cs` Inspector
   - Ensure server host/port are correct

### **Matchmaking Issues**

**Problem: "No match found" or timeout**

**Solutions:**
1. **Ensure Two Clients are Connecting**
   - Matchmaking requires at least 2 players
   - Both must click "Connect" within timeout period

2. **Increase Timeout**
   - Edit `NakamaConnectionManager.cs`
   - Increase `matchmakingTimeout` value

3. **Check Console Logs**
   - Look for `[NakamaClient]` and `[NakamaConnectionManager]` messages
   - Verify matchmaking started successfully

### **Game Sync Issues**

**Problem: Game state not syncing between players**

**Solutions:**
1. **Check Host Authority**
   - First player (alphabetically by UserId) is host
   - Host controls wind, powerups, level changes
   - Check console for authority messages

2. **Verify Network Manager**
   - Ensure `NakamaNetworkManager` is created
   - Check `GameManager.nakamaNetworkManagerRef` is set

3. **Check OpCode Messages**
   - Look for `[NakamaNetworkManager]` logs
   - Verify OpCode messages are being sent/received

### **Common Errors**

**Error: "NakamaClient.Instance is null"**
- Ensure `NakamaClient` GameObject exists in scene
- Check `DontDestroyOnLoad` is working

**Error: "Cannot resolve symbol 'Nakama'"**
- Ensure Nakama package is imported
- Check `Packages/manifest.json` has Nakama entry
- Reimport package if needed

**Error: "Socket not connected"**
- Check Nakama server is running
- Verify connection settings
- Check network connectivity

---

## Debugging Tips

### **Enable Debug Logging**

All Nakama scripts use `//Debug.Log` for important events. Check Unity Console for:

- `[NakamaClient]` - Connection, authentication, matchmaking
- `[NakamaConnectionManager]` - Connection flow, match joining
- `[NakamaNetworkManager]` - Game state sync, RPC calls

### **Check Match State**

Add this to any script to check match status:

```csharp
if (NakamaClient.Instance != null && NakamaClient.Instance.CurrentMatch != null)
{
    var match = NakamaClient.Instance.CurrentMatch;
    //Debug.Log($"Match ID: {match.Id}");
    //Debug.Log($"Players: {match.Presences.Count() + 1}");
    foreach (var presence in match.Presences)
    {
        //Debug.Log($"Player: {presence.UserId}");
    }
}
```

### **Test Authority**

Check if current player is host:

```csharp
bool isHost = false;
if (NakamaClient.Instance != null && NakamaClient.Instance.CurrentMatch != null)
{
    var presences = NakamaClient.Instance.CurrentMatch.Presences;
    var sortedPresences = presences.ToList();
    sortedPresences.Sort((a, b) => string.Compare(a.UserId, b.UserId));
    isHost = sortedPresences.Count > 0 && 
             sortedPresences[0].UserId == NakamaClient.Instance.UserId;
}
//Debug.Log($"Is Host: {isHost}");
```

---

## Expected Network Messages

The game uses these OpCodes for synchronization:

- **OpCode 1**: Game Start
- **OpCode 3**: Level Change
- **OpCode 4**: Theme Change
- **OpCode 5**: Wind
- **OpCode 6**: PowerUp
- **OpCode 7**: Hit Target

Check `NakamaNetworkManager.cs` for OpCode definitions.

---

## Performance Notes

- Nakama uses WebSocket for real-time communication
- Match data is sent as JSON (can be optimized to binary later)
- Network updates happen on game events (not every frame)
- Arrow physics are local-only (not synced) - only hits are synced
- Host controls deterministic events (wind, powerups, level changes)

---

## Next Steps After Testing

1. **Add Arrow Position Sync** (if needed)
   - Currently arrows are local-only
   - Add OpCode for arrow spawn/position updates

2. **Add Reconnection Logic**
   - Handle disconnections gracefully
   - Rejoin match on reconnect

3. **Add Match History**
   - Store match results in Nakama storage
   - Display match history

4. **Add Leaderboards**
   - Use Nakama leaderboards
   - Track wins/losses

5. **Production Deployment**
   - Set up production Nakama server
   - Configure SSL/TLS
   - Set up proper authentication
   - Configure CORS for WebGL

---

## Quick Test Checklist

- [ ] Nakama server is running
- [ ] No compilation errors in Unity
- [ ] LoginScene opens correctly
- [ ] AI mode works (Play with AI button)
- [ ] Multiplayer connects (two instances)
- [ ] Players spawn correctly
- [ ] Arrows can be shot
- [ ] Hits are registered
- [ ] Wind syncs between players
- [ ] Powerups spawn and sync
- [ ] Game ends correctly
- [ ] Scores are tracked

---

## Support

If you encounter issues:
1. Check Unity Console for error messages
2. Check Nakama server logs
3. Verify server is accessible
4. Test with single-player AI first
5. Check network connectivity

For Nakama documentation: https://heroiclabs.com/docs/
