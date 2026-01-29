# Port Configuration Fix

## Issue
The Unity client was trying to connect to port `7350` for HTTP API calls, but Nakama 3.24.0+ uses port `7351` for the HTTP API gateway. Port `7350` is now used for gRPC API.

**Error:** `Curl error 1: Received HTTP/0.9 when not allowed`

## Root Cause
Nakama 3.24.0 changed the default port configuration:
- **Port 7350**: gRPC API server
- **Port 7351**: HTTP API gateway & WebSocket

The Unity client was configured to use port `7350` for HTTP API calls, which caused connection failures.

## Fix Applied

### 1. Updated `ArrowduelConnectionManager.cs`
Changed default port from `7350` to `7351`:
```csharp
[SerializeField] private int serverPort = 7351; // Port 7351 for HTTP API gateway
```

### 2. Updated `ArrowduelNakamaClient.cs`
Updated comments to reflect correct port usage:
- Port 7351: HTTP API gateway (for Unity HTTP adapter)
- Port 7350: gRPC API (not used by Unity HTTP adapter)
- Port 7351: WebSocket (same port as HTTP gateway)

### 3. Updated `docker-compose.yml`
Updated port comments to reflect actual usage:
```yaml
ports:
  - "7349:7349"  # Nakama gRPC API (legacy)
  - "7350:7350"  # Nakama gRPC API (Nakama 3.24.0+)
  - "7351:7351"  # Nakama HTTP API Gateway & WebSocket (Nakama 3.24.0+)
  - "9100:9100"  # Nakama Console UI
```

## Verification

### Check Server Logs
```bash
docker logs nakama-server --tail 20
```

Look for:
```
"Starting API server for gRPC requests","port":7350
"Starting API server gateway for HTTP requests","port":7351
```

### Test Connection
1. Start Nakama server: `docker-compose up -d`
2. Open Unity Editor
3. Enter username and click "Connect"
4. Check Unity Console for: `[ArrowduelNakamaClient] Server configured: http://127.0.0.1:7351`
5. Should see: `[ArrowduelNakamaClient] Authenticated!` (no more HTTP/0.9 errors)

## Port Reference

| Port | Service | Used By |
|------|---------|---------|
| 7349 | gRPC API (legacy) | gRPC clients |
| 7350 | gRPC API | gRPC clients |
| 7351 | HTTP API Gateway & WebSocket | Unity HTTP adapter, WebSocket clients |
| 9100 | Console UI | Browser (admin panel) |

## Important Notes

1. **Unity HTTP Adapter**: Always uses port `7351` for HTTP API calls
2. **WebSocket**: Also uses port `7351` (same as HTTP gateway)
3. **gRPC Clients**: Use port `7350` (not used by Unity)
4. **Port Mapping**: Docker maps host ports to container ports, so `127.0.0.1:7351` connects to container port `7351`

## Testing

After applying this fix:
1. Restart Unity Editor (to reload configuration)
2. Connect to server
3. Should see successful authentication without HTTP/0.9 errors
4. Matchmaking should work correctly

## Related Files

- `Assets/ArrowduelConnectionManager.cs` - Connection manager with port configuration
- `Assets/ArrowduelNakamaClient.cs` - Nakama client wrapper
- `docker-compose.yml` - Docker port mappings
- `local.yml` - Nakama server configuration
