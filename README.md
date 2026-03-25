# EVE Calendar Service 🔥

a .NET 10 service that serves up your **corporation** EVE Online calendar events as a subscribable calendar feed (iCal/ICS format). its giving organization bestie. now powered by [LittyLogs](https://github.com/phsk69/litty-logs-dotnet) for maximum bussin console output no cap 💅

## what this bad boy does

- corp calendar events only (not your personal stuff, we respect privacy fr)
- ICS feed that slaps with Outlook, Google Calendar, Apple Calendar, etc
- headless OAuth setup - prints URL for browser auth, very convenient
- encrypted token storage (security is bussin)
- automatic token refresh so it stays working
- human-like ESI requests with rate limit respect (we dont get banned here)
- cleans up EVE showinfo tags from descriptions (calendar apps cant handle those)
- Matrix webhook notifications for warnings and errors via [LittyLogs.Webhooks](https://github.com/phsk69/litty-logs-dotnet) 📨
- file logging with daily rotation and 7-day persistence 📝
- rootless Docker container because security is the vibe 🔒
- cross-platform releases (Linux, macOS, Windows — x64 + ARM64) 🏗️

## what you need first

- Docker and Docker Compose (or grab a [pre-built binary](https://github.com/phsk69/evecal-dotnet/releases))
- an EVE Online account (obviously lmao)
- an EVE Developer application (keep reading bestie)

## setting up your EVE Developer App

1. hit up https://developers.eveonline.com/
2. log in with your EVE account
3. go to **Applications** → **Create New Application**
4. fill in the form no cap:
   - **Name**: whatever you want fam (e.g., "Corp Calendar Service")
   - **Description**: brief description, keep it real
   - **Connection Type**: "Authentication & API Access"
   - **Permissions**: grab these scopes:
     - `esi-calendar.read_calendar_events.v1`
     - `esi-corporations.read_corporation_membership.v1`
     - `esi-characters.read_corporation_roles.v1`
   - **Callback URL**: `http://localhost:8080/callback`
5. smash that **Create Application** button
6. copy the **Client ID** (no secret needed - we use PKCE flow, its built different)

## quick start (lets get this bread)

### Docker (recommended)

1. **set up the environment**

   ```bash
   cp .env.example .env
   # edit .env and set your EVE_CLIENT_ID
   ```

2. **run setup** (one time thing, needs browser)

   ```bash
   docker-compose run --rm --service-ports evecal setup
   ```

   this will:
   - print an auth URL
   - you open it in your browser
   - log in with EVE and authorize
   - container catches the callback and stores tokens, ez

3. **start the service**

   ```bash
   just up tag      # pull latest image from GHCR
   # or: just up local  # build from source
   ```

4. **subscribe to the calendar**

   add this URL to your calendar app:
   ```
   http://localhost:8080/calendar/feed.ics
   ```

### Pre-built binary

grab the latest release for your platform from [releases](https://github.com/phsk69/evecal-dotnet/releases) — self-contained binaries, no .NET runtime needed fr fr:

```bash
# linux/macOS
tar xzf evecal-*-linux-x64.tar.gz
export EVE_CLIENT_ID=your_client_id
export EVE_DATA_PATH=./data
./EveCal.Api setup    # one-time OAuth setup (needs browser)
./EveCal.Api          # run the service on port 8080
```

```powershell
# windows
Expand-Archive evecal-*-win-x64.zip -DestinationPath .\evecal
$env:EVE_CLIENT_ID = "your_client_id"
$env:EVE_DATA_PATH = ".\data"
.\evecal\EveCal.Api.exe setup
.\evecal\EveCal.Api.exe
```

available for: `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`, `win-x64`, `win-arm64`

> deploying on a remote server? need systemd, reverse proxy (nginx/caddy), or headless setup? check the full [DEPLOYMENT](DEPLOYMENT.md) guide bestie 🔥

### Docker images

pull from GHCR or Forgejo registry:

```bash
docker pull ghcr.io/phsk69/evecal-dotnet:latest
# or
docker pull git.ssy.dk/public/evecal-dotnet:latest
```

> **note**: GHCR packages are private by default even on public repos 💀 after the first push you gotta [flip the package to public](https://docs.github.com/en/packages/learn-github-packages/configuring-a-packages-access-control-and-visibility) in GitHub package settings (one-time thing). Forgejo packages inherit repo visibility automatically so that's already bussin 💅

## endpoints

| Endpoint | what it does |
|----------|-------------|
| `GET /calendar/feed.ics` | the ICS calendar feed, the main event fr |
| `GET /calendar/status` | service status and character info |
| `GET /callback` | OAuth callback (setup uses this) |

## config

### environment variables

| Variable | what it do | Default |
|----------|-------------|---------|
| `EVE_CLIENT_ID` | your EVE Developer app Client ID | Required bestie |
| `EVE_CALLBACK_URL` | OAuth callback URL | `http://localhost:8080/callback` |
| `EVE_SCOPES` | space-separated OAuth scopes | (calendar scopes) |
| `TOKEN_ENCRYPTION_KEY` | Base64 encryption key for tokens | Auto-generated |
| `CALENDAR_REFRESH_MINUTES` | how often to poll ESI for updates | `15` |
| `MATRIX_WEBHOOK_URL` | Matrix hookshot webhook URL for notifications | — (disabled) |

### data persistence

token data lives in `/app/data` inside the container. a named Docker volume keeps them around (named volumes respect rootless container permissions, bind mounts don't 💀):

```yaml
volumes:
  - evecal-data:/app/data
```

### log persistence

file logs live in `/app/logs` with daily rotation. a named Docker volume keeps them around:

```yaml
volumes:
  - evecal-logs:/app/logs
```

logs use [LittyLogs.File](https://github.com/phsk69/litty-logs-dotnet) with daily rolling and 10MB max file size. old log files stick around for 7 days before getting yeeted 🔥

### Matrix webhook notifications 📨

optional Matrix room notifications powered by [LittyLogs.Webhooks](https://github.com/phsk69/litty-logs-dotnet). any warning or error log automatically gets sent to your Matrix room — no manual webhook calls needed, it's just a logging provider fr fr

set up a [matrix-hookshot](https://matrix-org.github.io/matrix-hookshot/) generic webhook and pass the URL:

```bash
MATRIX_WEBHOOK_URL=https://hookshot.example.com/webhook/abc123
```

features: batched delivery (10 msgs / 2s), Polly retry + circuit breaker, best-effort (never crashes the app), gen alpha formatted messages with emojis 🔥

## development

see [CONTRIBUTING](CONTRIBUTING.md) for the full dev guide — prerequisites, building, testing, local CI, release management, architecture, and more 🔥

## security (we take this seriously fr)

- **rootless container** — runs as non-root user `evecal` (UID 10001), no root access needed 🔒
- tokens encrypted with AES-256 before storage, no cap
- PKCE flow means no client secret needed, built different
- encryption key auto-generated or you can provide one via environment
- only corp calendar events exposed (not personal stuff)

## troubleshooting (when things act up)

### "No valid tokens available"

run setup again bestie:
```bash
docker-compose run --rm --service-ports evecal setup
```

### "Token refresh failed"

your refresh token probably got yeeted. delete tokens and re-auth:
```bash
docker compose exec evecal rm /app/data/tokens.enc
docker compose run --rm --service-ports evecal setup
```

### rate limiting

the service respects ESI rate limits automatically fr. if you see rate limit warnings in logs, it backs off appropriately. we dont spam CCP here.

## License

MIT (do whatever you want with it bestie)
