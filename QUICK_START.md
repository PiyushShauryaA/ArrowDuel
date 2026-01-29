# Quick Start Guide - Nakama Server

## ✅ Server is Running!

Your Nakama server is now running with Docker Compose.

## Access Points

- **Nakama HTTP API**: http://localhost:7350
- **Nakama Console (Admin)**: http://localhost:9100
  - Username: `admin`
  - Password: `password`
- **CockroachDB Admin**: http://localhost:8080

## Quick Commands

```bash
# Start server
docker compose up -d

# Stop server
docker compose stop

# Stop and remove everything
docker compose down

# View logs
docker compose logs -f nakama

# Restart server
docker compose restart nakama

# Check status
docker compose ps
```

## Testing in Unity

1. **Open Unity Editor**
2. **Open LoginScene**
3. **Test Single Player:**
   - Enter name
   - Click "Play with AI"
   - Select difficulty
   - Game starts!

4. **Test Multiplayer:**
   - Open **two Unity Editor windows**
   - Window 1: Enter "Player1" → Click "Connect"
   - Window 2: Enter "Player2" → Click "Connect"
   - Both should match and start game!

## Server Configuration

The server is configured in `local.yml`:
- Server Key: `defaultkey`
- Console: `admin` / `password`
- Database: CockroachDB (auto-configured)

## Troubleshooting

**Server not starting?**
```bash
docker compose logs nakama
```

**Database issues?**
```bash
docker compose logs cockroachdb
```

**Reset everything:**
```bash
docker compose down -v
docker compose up -d
```

## Next Steps

1. Test single-player AI mode
2. Test multiplayer with two Unity instances
3. Check Unity Console for `[NakamaClient]` logs
4. Verify matchmaking works

Happy testing! 🎮
