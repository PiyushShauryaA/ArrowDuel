# Running Nakama Server with Docker Compose

This Docker Compose setup runs Nakama server with CockroachDB database.

## Quick Start

### 1. Start the Server

```bash
docker-compose up -d
```

This will start:
- **CockroachDB** (database) on ports 26257 and 8080
- **Nakama Server** on ports 7349, 7350, 7351, and 9100

### 2. Verify Server is Running

**Nakama HTTP API:**
- http://localhost:7350

**Nakama Console (Admin UI):**
- http://localhost:9100
- Username: `admin`
- Password: `password`

**CockroachDB Admin UI:**
- http://localhost:8080

### 3. Stop the Server

```bash
docker-compose down
```

### 4. Stop and Remove All Data

```bash
docker-compose down -v
```

**Warning:** This will delete all game data!

---

## Configuration

### Server Settings

Edit `local.yml` to configure:
- Server key
- Console username/password
- Ports
- Database settings
- Log levels

### Unity Client Settings

In Unity, configure `NakamaConnectionManager.cs`:
- **Server Host**: `127.0.0.1` (or `localhost`)
- **Server Port**: `7350`
- **Server Key**: `defaultkey`
- **Use SSL**: `false`

---

## Ports

| Service | Port | Description |
|---------|------|-------------|
| Nakama HTTP | 7350 | HTTP API endpoint |
| Nakama gRPC | 7349 | gRPC API endpoint |
| Nakama WebSocket | 7351 | WebSocket for real-time |
| Nakama Console | 9100 | Admin web UI |
| CockroachDB SQL | 26257 | Database SQL port |
| CockroachDB Admin | 8080 | Database admin UI |

---

## Data Persistence

- **Database data**: Stored in Docker volume `cockroachdb-data`
- **Nakama data**: Stored in `./nakama-data/` directory
- **Nakama modules**: Stored in `./nakama-modules/` directory

---

## Troubleshooting

### Check Logs

```bash
# View all logs
docker-compose logs

# View Nakama logs only
docker-compose logs nakama

# Follow logs in real-time
docker-compose logs -f nakama
```

### Restart Services

```bash
# Restart Nakama only
docker-compose restart nakama

# Restart everything
docker-compose restart
```

### Check Service Status

```bash
docker-compose ps
```

### Access Database

```bash
# Connect to CockroachDB
docker exec -it nakama-cockroachdb ./cockroach sql --insecure

# Show databases
SHOW DATABASES;

# Use nakama database
USE nakama;
```

### Reset Everything

```bash
# Stop and remove containers, networks, and volumes
docker-compose down -v

# Remove data directories
rm -rf nakama-data nakama-modules

# Start fresh
docker-compose up -d
```

---

## Production Deployment

For production, you should:

1. **Change default passwords** in `local.yml`:
   - Console username/password
   - Server key
   - Signing keys

2. **Enable SSL/TLS**:
   - Configure SSL certificates
   - Set `useSSL: true` in Unity client

3. **Use environment variables**:
   - Move sensitive data to `.env` file
   - Use Docker secrets for production

4. **Set up backups**:
   - Backup CockroachDB data regularly
   - Use persistent volumes

5. **Configure firewall**:
   - Only expose necessary ports
   - Use reverse proxy (nginx/traefik)

---

## Example Production docker-compose.yml

```yaml
version: '3.8'

services:
  cockroachdb:
    image: cockroachdb/cockroach:v23.1.11
    command: start-single-node --insecure
    volumes:
      - cockroachdb-data:/cockroach/cockroach-data
    environment:
      - COCKROACH_DATABASE=nakama
    networks:
      - nakama-internal

  nakama:
    image: heroiclabs/nakama:3.24.0
    depends_on:
      - cockroachdb
    volumes:
      - ./nakama-data:/nakama/data
      - ./local.yml:/nakama/data/local.yml
    ports:
      - "7350:7350"
      - "7351:7351"
    environment:
      - NAKAMA_DATABASE_URL=root@cockroachdb:26257/nakama?sslmode=disable
      - NAKAMA_CONSOLE_USERNAME=${NAKAMA_ADMIN_USER}
      - NAKAMA_CONSOLE_PASSWORD=${NAKAMA_ADMIN_PASS}
    networks:
      - nakama-internal
    restart: unless-stopped

volumes:
  cockroachdb-data:

networks:
  nakama-internal:
    driver: bridge
```

---

## Useful Commands

```bash
# Start in background
docker-compose up -d

# Start and view logs
docker-compose up

# Stop services
docker-compose stop

# Stop and remove containers
docker-compose down

# Rebuild and start
docker-compose up -d --build

# View logs
docker-compose logs -f nakama

# Execute command in container
docker-compose exec nakama sh

# Check resource usage
docker stats
```

---

## Health Checks

The compose file includes health checks. Verify services are healthy:

```bash
docker-compose ps
```

All services should show "healthy" status.

---

## Next Steps

1. Start the server: `docker-compose up -d`
2. Verify it's running: http://localhost:7350
3. Configure Unity client to connect
4. Test multiplayer functionality

For Unity testing, see `TESTING_NAKAMA.md`
