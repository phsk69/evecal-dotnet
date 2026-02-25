# Contributing to EveCal 🔥

dev guide for the homies who wanna build, test, and ship this thing. if you just wanna USE evecal, check out the [README](README.md) instead bestie 💅

## prerequisites

you need these installed or nothing works 💀

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

## local dev

```bash
cd src/EveCal.Api
export EVE_CLIENT_ID=your_client_id
dotnet run
```

## building

```bash
# Docker
docker build -t evecal .

# or with litty-fied output 🔥
dotnet litty build

# publish with litty-fied output 📦
dotnet litty publish
```

## testing

```bash
# tests with litty-fied output (the ONLY way) 🧪
just test

# or use the litty CLI directly
dotnet litty test
```

tests use xUnit and Moq — 12 tests total (8 ICalGenerator unit + 4 webhook integration). webhook tests FAIL HARD if `MATRIX_WEBHOOK_URL` not set — observability is non-negotiable fr fr 🔥

`just test` sources `.env` automatically so the webhook URL is available without manual exports.

> **note**: always use `dotnet litty test` instead of plain `dotnet test`. the litty CLI wraps dotnet with based output that actually slaps 🔥

## smoke testing

```bash
# service must be running
just smoke
```

## local CI testing (stop spamming commits to debug pipelines) 🧪

install [act](https://github.com/nektos/act) and [actionlint](https://github.com/rhysd/actionlint) to test workflows locally:

```bash
just ci lint     # validate workflow YAML — catches dumb mistakes instantly 🔍
just ci local    # run full CI pipeline + multi-target release build (6 RIDs) 🧪
just ci check    # lint first, then full local run — the pre-push vibe check 💅
just ci release  # test only the multi-target release build (6 RID dotnet publish) 🏗️
```

> **note**: `just ci local` needs Docker running and sources secrets from `.env`. first run downloads a ~1GB container image so grab a snack bestie 🐳

### known act limitations (expected failures, not bugs)

when running pipelines locally with `act`, some steps will fail — this is normal and expected no cap:

| step | what happens | why |
|------|-------------|-----|
| `build-docker` (multi-arch) | can't be tested | QEMU cross-compilation is cooked for .NET — multi-arch Docker builds run on Forgejo only |
| `create-release` | can't be tested | needs real Forgejo/GitHub API endpoints |

**what DOES work locally**: `dotnet build`, `dotnet test`, `dotnet publish` for all 6 RIDs (linux-x64, linux-arm64, osx-x64, osx-arm64, win-x64, win-arm64), and `zip`/`tar.gz` packaging. if the builds and packaging succeed, you're golden bestie 💅

## release management 🚀

this project uses **git flow** with automated Forgejo CI/CD pipelines. releases trigger on version tags and produce:

- cross-platform self-contained binaries (6 platforms)
- multi-arch Docker images pushed to GHCR + Forgejo registry
- Forgejo + GitHub mirror releases with changelog notes

the release pipeline is fully idempotent — you can re-run it all day without manual cleanup. if something bricks, use `just re-release vX.X.X` to nuke everything and start fresh 💣

### release commands

```bash
# bump and release
just release patch    # 0.2.0 -> 0.2.1
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

# nuclear option — nuke a bricked release everywhere and re-tag
just re-release v0.3.0
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
