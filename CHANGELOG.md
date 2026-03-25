# Changelog

all the fire updates go here bestie, no cap

## [0.3.6] - 2026-03-25

### dependency glow up — keeping the packages fresh 📦

#### dependency upgrades
- bumped `Microsoft.AspNetCore.OpenApi` 10.0.3 → 10.0.5 — staying current with the OpenAPI drip no cap 🔥
- bumped `System.IdentityModel.Tokens.Jwt` 8.16.0 → 8.17.0 — JWT handling stays bussin with the latest fixes fr fr 🔐

## [0.3.5] - 2026-02-26

### hotfix — GHCR linking + Docker perms fix + setup glow up 🏷️🐳

#### bug fixes
- GHCR wasn't auto-linking Docker packages to the repo because multi-platform builds need OCI annotations at the **manifest index level**, not image-level labels. swapped `labels:` → `annotations:` with `index:` prefix on the GHCR build step — Forgejo was already bussin with labels so that stays 💅
- reordered Docker build steps: Forgejo registry first (home first bestie 🏠), then GHCR
- Docker container was bricking on first OAuth setup — `Permission denied` writing `/app/data/encryption.key` 💀 bind mount (`./data:/app/data`) creates host dir as root but container runs rootless (UID 10001). swapped to named volume `evecal-data` — named volumes respect Dockerfile ownership, bind mounts don't no cap 🐳

#### improvements
- `just setup` now accepts `local` or `tag` mode (like `just up`) — `just setup tag` pulls the GHCR image instead of forcing a local build 🔐
- `just clean` now yeets the `evecal-data` Docker volume instead of trying to `rm` files from a bind mount that doesn't exist anymore 🧹
- documented GHCR package visibility gotcha in README — packages are private by default even on public repos, gotta flip it manually in GitHub settings (one-time thing) 💀

#### dependency upgrade
- bumped `actions/checkout` v4 → v5 across both CI and release pipelines — v6 is [bricked on Forgejo](https://github.com/actions/checkout/issues/2321) with hardcoded GitHub paths 💀 v5 uses universal HTTP auth that works everywhere no cap

## [0.3.4] - 2026-02-26

### hotfix — OCI labels so Docker packages link to the repo automatically 🏷️

#### bug fix
- split Docker build-push into two steps (GHCR + Forgejo) with registry-specific `org.opencontainers.image.source` OCI labels — each registry auto-links the package to its own repo now. `image.source` only takes one value so a single build couldn't serve both 💀
- labels set at build time via `build-push-action` `labels:` input instead of hardcoding in Dockerfile

## [0.3.3] - 2026-02-25

### hotfix — Forgejo registry auth fix 🔐

#### bug fix
- swapped Forgejo registry + release API auth from built-in `GITHUB_TOKEN` → dedicated `FORGEJO_TOKEN` PAT — runner token lacked package write perms, Docker push was getting `401 reqPackageAccess` 💀
- also: `GH_PAT` for GHCR must be a **classic** token with `write:packages` scope — fine-grained tokens [don't support GHCR](https://github.com/docker/login-action/issues/331) and that's on GitHub fr fr

## [0.3.2] - 2026-02-25

### hotfix — simplified release pipeline, yeeted the artifact matrix 🔥

#### pipeline simplification
- consolidated release pipeline from 4 jobs → 2 jobs — `build-assets` matrix + `create-release` merged into single `release` job that builds all 6 RIDs in a loop, same pattern as litty-logs 💅
- yeeted `upload-artifact@v4` and `download-artifact@v4` entirely — v4 uses GitHub's artifact API that doesn't work on Forgejo/GHES, and we don't need artifacts when it's all one job no cap 🔧
- Docker build + push merged into the same `release` job — no more separate `build-docker` job

## [0.3.1] - 2026-02-25

### hotfix — upload-artifact revert + docs that actually slay 🔥

#### bug fixes
- reverted `if: ${{ !env.ACT }}` on upload-artifact step — Forgejo runner is based on act so it also sets `ACT=true`, which bricked artifact uploads on real CI 💀

#### docs glow up 📖
- split developer-facing content from README into [CONTRIBUTING.md](CONTRIBUTING.md) — README stays clean for users, dev guide lives in its own file
- added "known act limitations" section documenting expected local CI failures (upload-artifact, multi-arch Docker, create-release)
- README slimmed from 391 to ~190 lines — user-facing only, links to CONTRIBUTING.md for dev stuff

#### DEPLOYMENT.md — the "works on my machine" era is JOVER 💀
- NEW [DEPLOYMENT.md](DEPLOYMENT.md) — full bare metal deployment guide for all 6 platforms, the works on my machine era is literally over no cap
- environment variables reference table with required/optional/defaults for every var
- Linux systemd service file with security hardening (`NoNewPrivileges`, `ProtectSystem=strict`, auto-restart) — the most based way to run EveCal fr fr 🐧
- reverse proxy configs for nginx + Caddy (auto-TLS) — no built-in TLS so this is non-negotiable bestie 🔒
- headless server OAuth setup with 3 options: SSH tunnel (recommended), public URL, or token transfer from local machine 🖥️
- macOS deployment with launchd plist (or just tmux like a normal person) 🍎
- Windows deployment with PowerShell + NSSM service manager 🪟
- data persistence, backup strategy, and upgrade guide — lose your encryption key and it's jover 💾
- README pre-built binary section expanded with linux + windows quick start examples and link to DEPLOYMENT.md

## [0.3.0] - 2026-02-22

### the full glow up — LittyLogs 0.2.3 + security + CI/CD + observability 🔥💅

#### LittyLogs upgrade 0.1.4 → 0.2.3 📦
- yeeted the homegrown `LittyConsoleFormatter` and replaced with the official [`LittyLogs`](https://github.com/phsk69/litty-logs-dotnet) NuGet package no cap
- `LittyLogs.File` for persistent file logging with daily rolling rotation and 10MB max size 📝
- `LittyLogs.Webhooks` for Matrix hookshot notifications — warnings and errors go straight to the chat fr fr 📨
- `LittyLogs.Tool` 0.2.3 as local dotnet tool — `dotnet litty test`, `dotnet litty build`, `dotnet litty publish` all bussin
- `.config/dotnet-tools.json` now tracked in git so `dotnet tool restore` works for everyone

#### Matrix webhook notifications (LittyLogs.Webhooks) 📨
- YEETED the old custom `MatrixWebhookService` and `MatrixConfiguration` — replaced by `LittyLogs.Webhooks` package
- `MATRIX_ENABLED` and `MATRIX_API_KEY` env vars are GONE — just `MATRIX_WEBHOOK_URL` now, simple as
- conditional registration in Program.cs — only activates if URL is configured
- batched delivery (10 msgs / 2s), Polly retry + circuit breaker, best-effort (never crashes the app) 💪

#### security hardening 🔒
- ReDoS-safe regex with `[GeneratedRegex]` + `RegexOptions.NonBacktracking` for showinfo tag cleaning
- OAuth callback parameters truncated to prevent log injection
- API error responses truncated to 1000 chars — no more leaking internal error details to clients
- `ex.Message` no longer exposed to HTTP responses — generic error messages only
- `appsettings.Development.json` removed from git, excluded from `dotnet publish`, blocked by `.dockerignore`

#### duplicate logging fix 🔇
- `ClearProviders()` called before `AddLittyLogs()` to prevent double console output
- setup mode shutdown changed from `RunAsync()` + `StopAsync()` to `CancellationTokenSource` pattern — no more duplicate Host lifecycle messages

#### webhook integration tests 🧪
- 4 new integration tests that actually HIT Matrix — observability is non-negotiable
- tests FAIL HARD if `MATRIX_WEBHOOK_URL` not configured — no silent skips
- `just test` sources `.env` so webhook URL is available locally
- CI pipeline passes `MATRIX_WEBHOOK_URL` from Forgejo secrets

#### full release pipeline 🚀
- Forgejo CI pipeline (`ci.yml`): build + test on every push to develop/master
- Forgejo release pipeline (`release.yml`): triggered by `v*` tags, produces:
  - 6 cross-platform self-contained binaries (linux-x64, linux-arm64, win-x64, win-arm64, osx-x64, osx-arm64)
  - multi-arch Docker images (amd64 + arm64) pushed to GHCR + Forgejo registry
  - Forgejo + GitHub mirror releases with changelog notes
- `Directory.Build.props` as single source of truth for versioning
- version sanity check in pipeline — tag must match props or it fails
- `FORGEJO_TOKEN` yeeted — uses built-in `GITHUB_TOKEN` for Forgejo registry + releases

#### local CI testing with act 🧪
- `just ci lint` — validate workflow YAML with actionlint
- `just ci local` — run BOTH ci.yml AND multi-target release build (6 RIDs) locally with act
- `just ci check` — lint then full local CI run
- `just ci release` — test just the multi-target release build (6 RID dotnet publish)
- `.actrc` maps `runs-on: linux` to catthehacker/ubuntu:full-latest

#### docker + justfile improvements 🐳
- docker-compose now uses published GHCR image by default (`ghcr.io/phsk69/evecal-dotnet:latest`)
- `just up local` — build from local Dockerfile and start
- `just up tag` — pull latest GHCR image and start
- rootless container (UID 10001), named volume for logs
- Dockerfile now copies `Directory.Build.props` for correct version in builds
- gitflow release automation — `just release`, `just hotfix`, `just finish`

#### docs 📖
- README: CI/CD secrets table, branch protection setup, local CI testing, full dev prerequisites
- CLAUDE.md: updated with all litty commands and webhook logging info
- 12 tests total (8 ICalGenerator + 4 webhook integration), all passing no cap ✅

## [0.2.0] - 2026-02-18

### new stuff 🆕
- `LittyConsoleFormatter` just dropped and it EATS 🔥 custom console log formatter that hijacks ALL logs - framework AND app
- every log level got its own emoji drip: 👀 Trace, 🔍 Debug, 🔥 Info, 😤 Warn, 💀 Error, ☠️ Critical
- ANSI color-coded output in terminal - green info, yellow warns, red errors, the terminal looks absolutely bussin now 🎨
- short category names so `Microsoft.Hosting.Lifetime` just shows as `Lifetime`, no namespace bloat
- dim timestamps and categories so the actual message stays the main character
- existing tests upgraded from silent mock loggers to real litty loggers, test output is fire now too 🧪
- 10 new unit tests for the formatter, 26 total tests all passing no cap ✅

## [0.1.2] - 2026-02-18

### fixed fr 🔧
- yeeted EVE showinfo anchor tags from calendar descriptions (they were acting sus in ICS output)
- descriptions now clean but keep the text content, very readable
- feed URL in logs now parsed from config instead of hardcoded localhost 🔥

### new stuff 🆕
- added unit test project with xUnit and Moq, testing is bussin now
- 7 tests covering ICS generation and description cleaning
- justfile updated: `just test` runs unit tests, `just smoke` does the endpoint check 🧪
- emojis in ALL the logs now fr fr 💀🔥✨😤
- HTTP error responses got emojis too, users gonna love it no cap

## [0.1.1-2] - 2026-02-18

### its giving transformation
- converted all code comments to gen alpha style fr fr
- log messages now absolutely slaying
- added asterisk prefix to calendar event titles (for the boomers who import instead of subscribe, we got you)

## [0.1.1-1] - 2026-01-31

### fixed that stuff
- `EVE_CALLBACK_URL` env var was acting sus, now it actually overrides the default like its supposed to
- yeeted some missing using statements back into AuthController after the linter did its thing

### glow up
- all classes converted to C# 12 primary constructors, they hit different now

## [0.1.0] - 2026-01-31

### the beginning fr fr
- initial release dropped, lets gooo
- PKCE OAuth flow for EVE SSO auth (no client secret needed, we move smarter not harder)
- headless setup mode for Docker environments, very convenient bestie
- ICS/iCal feed generation for corp calendar events, your outlook gonna eat this up
- encrypted token storage with AES-256, security is bussin
- automatic token refresh so you dont gotta worry bout nothing
- rate-limited ESI API client with retry logic, we respectful to CCP fr
- Docker and docker-compose config included, deploy with ease
- human-like request delays so ESI doesnt think were a bot lmao
