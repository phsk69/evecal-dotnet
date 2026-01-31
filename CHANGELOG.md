# Changelog

All notable changes to this project will be documented in this file.

## [0.1.1] - 2026-01-31

### Fixed
- `EVE_CALLBACK_URL` environment variable now correctly overrides the default value
- Added missing using statements in AuthController after linter refactoring

### Changed
- Converted all classes to use C# 12 primary constructors

## [0.1.0] - 2026-01-31

### Added
- Initial release
- PKCE OAuth flow for EVE SSO authentication
- Headless setup mode for Docker environments
- ICS/iCal feed generation for corporation calendar events
- Encrypted token storage with AES-256
- Automatic token refresh
- Rate-limited ESI API client with retry logic
- Docker and docker-compose configuration
- Human-like request delays to respect ESI rate limits