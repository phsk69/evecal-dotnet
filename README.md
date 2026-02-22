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

grab the latest release for your platform from [releases](https://github.com/phsk69/evecal-dotnet/releases):

```bash
# linux example
tar xzf evecal-*.tar.gz
export EVE_CLIENT_ID=your_client_id
./EveCal.Api setup    # one-time OAuth setup
./EveCal.Api          # run the service
```

### Docker images

pull from GHCR or Forgejo registry:

```bash
docker pull ghcr.io/phsk69/evecal-dotnet:latest
# or
docker pull git.ssy.dk/public/evecal-dotnet:latest
```

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

token data lives in `/app/data` inside the container. mount a volume so tokens survive restarts:

```yaml
volumes:
  - ./data:/app/data
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

### prerequisites

you need these installed or nothing works bestie 💀

```bash
# .NET 10 SDK — the whole runtime and build toolchain
# https://dotnet.microsoft.com/download/dotnet/10.0
dotnet --version  # should be 10.x

# just — command runner (like make but actually good)
# https://github.com/casey/just
# linux
curl --proto '=https' --tlsv1.2 -sSf https://just.systems/install.sh | bash -s -- --to /usr/local/bin

# act — run CI pipelines locally without spamming commits
# https://github.com/nektos/act
# linux
curl -s https://raw.githubusercontent.com/nektos/act/master/install.sh | bash

# actionlint — static linter for workflow YAML files
# https://github.com/rhysd/actionlint
go install github.com/rhysd/actionlint/cmd/actionlint@latest
# or grab a binary from https://github.com/rhysd/actionlint/releases

# shellcheck (optional) — actionlint uses this to lint run: script blocks
# https://github.com/koalaman/shellcheck
sudo apt install shellcheck  # debian/ubuntu
# or: brew install shellcheck

# Docker + Docker Compose — for container builds and act
# https://docs.docker.com/get-docker/

# restore the litty-logs CLI tool
dotnet tool restore
```

### local dev

```bash
cd src/EveCal.Api
export EVE_CLIENT_ID=your_client_id
dotnet run
```

### building

```bash
# Docker
docker build -t evecal .

# or with litty-fied output 🔥
dotnet litty build

# publish with litty-fied output 📦
dotnet litty publish
```

### testing

```bash
# tests with litty-fied output (the ONLY way) 🧪
just test

# or use the litty CLI directly
dotnet litty test
```

tests use xUnit and Moq — they verify ICS generation and description cleaning fr fr 🧪

> **note**: always use `dotnet litty test` instead of plain `dotnet test`. the litty CLI wraps dotnet with based output that actually slaps 🔥

### smoke testing

```bash
# service must be running
just smoke
```

## release management 🚀

this project uses **git flow** with automated Forgejo CI/CD pipelines. releases trigger on version tags and produce:

- cross-platform self-contained binaries (6 platforms)
- multi-arch Docker images pushed to GHCR + Forgejo registry
- Forgejo + GitHub mirror releases with changelog notes

### release commands

```bash
# bump and release
just release patch    # 0.2.0 -> 0.3.0
just release minor    # 0.2.0 -> 0.3.0
just release major    # 0.2.0 -> 1.0.0

# pre-release
just release-dev patch         # 0.2.0 -> 0.2.1-dev
just release-dev minor beta.1  # 0.2.0 -> 0.3.0-beta.1

# release current version as-is
just release-current

# hotfix for prod issues
just hotfix patch     # starts hotfix branch
# ... make your fix, commit it ...
just finish           # finishes the branch, merges, pushes
```

### supported platforms

| Platform | Architecture | Format |
|----------|-------------|--------|
| Linux | x64, ARM64 | tar.gz |
| macOS | x64, ARM64 (Apple Silicon) | tar.gz |
| Windows | x64, ARM64 | zip |

### CI/CD setup (forgejo runner secrets + branch protection) 🔐

the pipelines need some secrets configured in your Forgejo instance or everything will be absolutely bricked no cap 💀

#### required secrets

go to your Forgejo repo → **Settings** → **Actions** → **Secrets** and add these:

| Secret | what its for | which pipeline |
|--------|-------------|----------------|
| `MATRIX_WEBHOOK_URL` | Matrix hookshot webhook URL — observability tests + runtime notifications | `ci.yml`, `release.yml` |
| `GH_PAT` | GitHub Personal Access Token — GHCR container registry login + GitHub mirror releases | `release.yml` |

> **note**: `GITHUB_TOKEN` is auto-provided by the Forgejo runner and covers Forgejo registry login + Forgejo release creation — no extra config needed bestie

`MATRIX_WEBHOOK_URL` is **required** for both CI and release pipelines — the webhook integration tests will fail hard if its not set. observability is non-negotiable fr fr 🔥

#### branch protection (merge gates like GitLab)

go to your Forgejo repo → **Settings** → **Branches** → add protection for `master`:

1. **require status checks to pass** → select the `ci / build-and-test` check
2. **required approvals** → set to 1 (nobody merges without a review bestie)
3. **block merge on rejected reviews** → enable this so requested changes actually block
4. **optionally enable "require signed commits"** for extra security vibes 🔒

this means PRs to master cant merge until CI is green AND someone approves. pipeline goes red = merge blocked. exactly like GitLab merge request approvals but on Forgejo, built different 💅

#### local CI testing (stop spamming commits to debug pipelines) 🧪

install [act](https://github.com/nektos/act) and [actionlint](https://github.com/rhysd/actionlint) to test workflows locally:

```bash
# linux
curl -s https://raw.githubusercontent.com/nektos/act/master/install.sh | bash
# actionlint — grab the binary from https://github.com/rhysd/actionlint/releases
```

then use the `just ci` commands:

```bash
just ci lint     # validate workflow YAML — catches dumb mistakes instantly 🔍
just ci local    # run the full CI pipeline locally with act 🧪
just ci check    # lint first, then full local run — the pre-push vibe check 💅
```

> **note**: `just ci local` needs Docker running and sources secrets from `.env`. first run downloads a ~1GB container image so grab a snack bestie. multi-arch docker builds cant be tested locally (QEMU is cooked for .NET) — those run on Forgejo only 🐳

## architecture (how this thing is built fr)

```
src/EveCal.Api/
├── Controllers/
│   ├── AuthController.cs        # OAuth callback handling
│   └── CalendarController.cs    # ICS feed endpoint
├── Services/
│   ├── EveAuthService.cs        # OAuth/SSO with PKCE
│   ├── EveCalendarService.cs    # ESI calendar API calls
│   └── ICalGeneratorService.cs  # ICS generation magic
├── Models/
│   ├── EveCalendarEvent.cs      # ESI event models
│   ├── EveConfiguration.cs      # app config
│   ├── EveTokens.cs             # token storage models
│   └── SsoCharacter.cs          # character identity
└── Infrastructure/
    ├── EsiHttpClientFactory.cs   # rate-limited HTTP client, stays respectful
    └── TokenStorage.cs           # encrypted token persistence, locked in
```

## how it works

### headless OAuth flow (the setup process)

1. you run `docker-compose run evecal setup`
2. container generates PKCE challenge and prints auth URL
3. you open URL in browser, log in with EVE
4. EVE redirects to `http://localhost:8080/callback`
5. container (port-mapped to 8080) catches the code
6. container exchanges code for tokens using PKCE
7. tokens get encrypted and stored in `/app/data`, secured the bag

### runtime (when its actually running)

1. container loads encrypted refresh token
2. auto-refreshes access token every ~19 minutes, stays valid
3. fetches corp calendar events from ESI
4. serves ICS feed on `/calendar/feed.ics`, absolutely eating

## security (we take this seriously fr)

- **rootless container** — runs as non-root user `evecal` (UID 1654), no root access needed 🔒
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
rm ./data/tokens.enc
docker-compose run --rm --service-ports evecal setup
```

### rate limiting

the service respects ESI rate limits automatically fr. if you see rate limit warnings in logs, it backs off appropriately. we dont spam CCP here.

## License

MIT (do whatever you want with it bestie)
