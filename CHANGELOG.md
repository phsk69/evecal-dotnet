# Changelog

all the fire updates go here bestie, no cap

## [0.3.0] - 2026-02-21

### the great litty-logs migration 🔥💅

#### logging glow up
- yeeted the homegrown `LittyConsoleFormatter` and replaced it with the official [`LittyLogs`](https://github.com/phsk69/litty-logs-dotnet) NuGet package — same vibes but maintained as a proper library now no cap 📦
- framework log messages now get auto-rewritten into gen alpha style (e.g. "Application started" becomes "app is bussin and ready to slay bestie 💅") 🔥
- added `LittyLogs.File` for persistent file logging with daily rolling rotation and 10MB max file size 📝
- installed `LittyLogs.Tool` as a local dotnet tool — `just test` now shows litty-fied test output with emojis for pass/fail ✅💀

#### Matrix webhook notifications 📨
- new `MatrixWebhookService` sends notifications to a Matrix room via webhook
- fires on service startup (with character name) and token refresh failures
- fire-and-forget design — Matrix being down never crashes the app, resilience is bussin 💪
- configurable via `MATRIX_ENABLED`, `MATRIX_WEBHOOK_URL`, `MATRIX_API_KEY` env vars
- 8 new tests covering disabled state, enabled posting, HTTP failure resilience

#### rootless Docker container 🔒
- container now runs as non-root user `evecal` (UID 1654), security going crazy
- data and logs directories created with proper ownership
- named Docker volume `evecal-logs` for persistent log storage

#### full release pipeline 🚀
- adopted gitflow release automation from litty-logs — `just release`, `just hotfix`, `just finish` all work
- Forgejo CI pipeline: build + test on every push to develop/master
- Forgejo release pipeline: triggered by `v*` tags, produces:
  - 6 cross-platform self-contained binaries (linux-x64, linux-arm64, win-x64, win-arm64, osx-x64, osx-arm64)
  - multi-arch Docker images (amd64 + arm64) pushed to GHCR + Forgejo registry
  - Forgejo + GitHub mirror releases with changelog notes
- `Directory.Build.props` as single source of truth for versioning
- version sanity check in pipeline — tag must match props or it fails
- 15 tests total, all passing no cap ✅

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
