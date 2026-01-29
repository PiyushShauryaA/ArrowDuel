# Quick Start: Debug Two Player Connection

## ✅ What's Been Done

1. **Comprehensive Debug Logging Added**
   - Every step of the connection process is now logged
   - Easy to identify where connection fails
   - Logs show: scene loading → connection → authentication → matchmaking → match found → player spawning

2. **Automatic Connection Trigger**
   - GameScene now automatically starts multiplayer connection when loaded
   - No manual connection calls needed

3. **Manager Auto-Creation**
   - NakamaConnectionManager and NakamaClient are created automatically if missing

## 🚀 Quick Test Steps

### Step 1: Start Nakama Server
```bash
docker compose up -d
```

Verify it's running:
```bash
docker compose ps
```
You should see both `cockroachdb` and `nakama` containers running.

### Step 2: Setup Unity (Original Editor)
1. Open Unity Editor
2. Open **GameScene**
3. Select **GameManager** GameObject in Hierarchy
4. In Inspector, assign:
   - `player1NetworkStatePrefab` → Your Player 1 prefab
   - `player2NetworkStatePrefab` → Your Player 2 prefab
5. **Save the scene** (Ctrl+S)

### Step 3: Test Multiplayer

#### Option A: Using ParrelSync (Recommended)
1. **Original Editor (Player 1):**
   - Open **LoginScene**
   - Enter name: `Player1`
   - Click **"Connect"**
   - Watch Console logs

2. **Clone Editor (Player 2):**
   - Use ParrelSync to open clone
   - Open **LoginScene**
   - Enter name: `Player2`
   - Click **"Connect"** (within 15 seconds)
   - Watch Console logs

#### Option B: Two Separate Builds
1. Build the game (File → Build Settings → Build)
2. Run the executable twice
3. Both instances connect via Nakama

## 📊 What to Watch in Console

### Success Flow:
```
[GameManager] === GAMESCENE LOADED ===
[GameManager] Multiplayer mode detected. Initiating connection...
[NakamaConnectionManager] === ConnectToServer CALLED ===
[NakamaClient] === AUTHENTICATION SUCCESS ===
[NakamaClient] === MATCHMAKING STARTED ===
[NakamaClient] === MATCH FOUND ===
[NakamaConnectionManager] === TWO PLAYERS FOUND! ===
[GameManager] === SpawnPlayerNakama CALLED ===
```

### If Connection Fails:
The logs will show exactly where it stops:
- **No connection start?** → Check GameMode/IsAIMode values
- **Authentication fails?** → Check Nakama server is running
- **No match found?** → Check both players clicked Connect
- **Players don't spawn?** → Check prefabs are assigned

## 🔍 Debug Checklist

Before testing:
- [ ] Nakama server running (`docker compose ps`)
- [ ] Player prefabs assigned in GameManager Inspector
- [ ] Scene saved (in original editor if using ParrelSync)
- [ ] Unity Console open to see logs

During testing:
- [ ] Both players enter different names
- [ ] Both players click "Connect" within 15 seconds
- [ ] Watch Console logs in both instances
- [ ] Check for any error messages

## 🐛 Common Issues

### "NakamaConnectionManager.Instance: NULL"
- **Fix:** Managers are created automatically, but if you see this, wait a frame

### "Prefabs not assigned"
- **Fix:** Assign prefabs in GameManager Inspector and save scene

### "Matchmaking timeout"
- **Fix:** Ensure both players click Connect within 15 seconds

### "Socket closed" or connection errors
- **Fix:** Check Nakama server is running: `docker compose ps`
- **Fix:** Check server logs: `docker compose logs nakama`

## 📝 Next Steps

1. **Start Nakama server** (if not running)
2. **Assign prefabs** in GameManager
3. **Test with two players**
4. **Check Console logs** to see connection flow
5. **Share logs** if connection fails

## 📚 Full Documentation

- **DEBUG_CONNECTION.md** - Complete debug guide with all log explanations
- **TESTING_NAKAMA.md** - Comprehensive testing guide
- **TROUBLESHOOTING.md** - Common issues and solutions
- **PARRRELSYNC_NOTES.md** - ParrelSync-specific guide

---

**Ready to test!** Start the server, assign prefabs, and connect two players. The debug logs will show you exactly what's happening! 🎮
