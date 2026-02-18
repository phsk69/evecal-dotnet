# Changelog

all the fire updates go here bestie, no cap

## [0.1.2] - 2026-02-18

### fixed fr
- yeeted EVE showinfo anchor tags from calendar descriptions (they were acting sus in ICS output)
- descriptions now clean but keep the text content, very readable

### new stuff
- added unit test project with xUnit and Moq, testing is bussin now
- 7 tests covering ICS generation and description cleaning

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
