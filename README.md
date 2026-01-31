# EVE Calendar Service

A .NET 10 service that exposes **corporation** EVE Online calendar events as a subscribable public calendar (iCal/ICS format).

## Features

- Corporation calendar events only (not character personal calendar)
- Subscribable ICS feed compatible with Outlook, Google Calendar, Apple Calendar, etc.
- Headless OAuth setup - prints URL for browser authentication
- Encrypted token storage
- Automatic token refresh
- Human-like ESI request handling with rate limit respect

## Prerequisites

- Docker and Docker Compose
- An EVE Online account
- An EVE Developer application (see setup below)

## EVE Developer App Registration

1. Go to https://developers.eveonline.com/
2. Log in with your EVE account
3. Go to **Applications** → **Create New Application**
4. Fill in the form:
   - **Name**: Your app name (e.g., "Corp Calendar Service")
   - **Description**: Brief description of your app
   - **Connection Type**: "Authentication & API Access"
   - **Permissions**: Select these scopes:
     - `esi-calendar.read_calendar_events.v1`
     - `esi-corporations.read_corporation_membership.v1`
     - `esi-characters.read_corporation_roles.v1`
   - **Callback URL**: `http://localhost:8080/callback`
5. Click **Create Application**
6. Copy the **Client ID** (no secret needed - we use PKCE flow)

## Quick Start

1. **Configure environment**

   Create a `.env` file:
   ```bash
   EVE_CLIENT_ID=your_client_id_here
   ```

2. **Run setup** (one-time, requires browser interaction)

   ```bash
   docker-compose run --rm --service-ports evecal setup
   ```

   This will:
   - Print an authorization URL
   - Open the URL in your browser
   - Log in with your EVE account and authorize the app
   - The container receives the callback and stores tokens

3. **Start the service**

   ```bash
   docker-compose up -d
   ```

4. **Subscribe to the calendar**

   Add this URL to your calendar client:
   ```
   http://localhost:8080/calendar/feed.ics
   ```

## Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /calendar/feed.ics` | ICS calendar feed |
| `GET /calendar/status` | Service status and character info |
| `GET /callback` | OAuth callback (used during setup) |

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `EVE_CLIENT_ID` | EVE Developer application Client ID | Required |
| `EVE_CALLBACK_URL` | OAuth callback URL | `http://localhost:8080/callback` |
| `EVE_SCOPES` | Space-separated OAuth scopes | (calendar scopes) |
| `TOKEN_ENCRYPTION_KEY` | Base64 encryption key for tokens | Auto-generated |
| `CALENDAR_REFRESH_MINUTES` | How often to poll ESI for updates | `15` |

### Data Persistence

Token data is stored in `/app/data` inside the container. Mount a volume to persist tokens across container restarts:

```yaml
volumes:
  - ./data:/app/data
```

## Development

### Local Development

```bash
cd src/EveCal.Api
export EVE_CLIENT_ID=your_client_id
dotnet run
```

### Building

```bash
docker build -t evecal .
```

## Architecture

```
src/EveCal.Api/
├── Controllers/
│   ├── AuthController.cs      # OAuth callback handling
│   └── CalendarController.cs  # ICS feed endpoint
├── Services/
│   ├── EveAuthService.cs      # OAuth/SSO with PKCE
│   ├── EveCalendarService.cs  # ESI calendar API
│   └── ICalGeneratorService.cs # ICS generation
├── Models/
│   ├── EveCalendarEvent.cs    # ESI event models
│   ├── EveConfiguration.cs    # App configuration
│   ├── EveTokens.cs           # Token storage models
│   └── SsoCharacter.cs        # Character identity
└── Infrastructure/
    ├── EsiHttpClientFactory.cs # Rate-limited HTTP client
    └── TokenStorage.cs         # Encrypted token persistence
```

## How It Works

### Headless OAuth Flow

1. User runs `docker-compose run evecal setup`
2. Container generates PKCE challenge and prints auth URL
3. User opens URL in browser, logs in with EVE
4. EVE redirects to `http://localhost:8080/callback`
5. Container (port-mapped to 8080) receives the code
6. Container exchanges code for tokens using PKCE
7. Tokens are encrypted and stored in `/app/data`

### Runtime

1. Container loads encrypted refresh token
2. Auto-refreshes access token every ~19 minutes
3. Fetches corporation calendar events from ESI
4. Serves ICS feed on `/calendar/feed.ics`

## Security Notes

- Tokens are encrypted with AES-256 before storage
- PKCE flow means no client secret is needed
- Encryption key is auto-generated or can be provided via environment
- Only corporation calendar events are exposed (not personal)

## Troubleshooting

### "No valid tokens available"

Run setup again:
```bash
docker-compose run --rm --service-ports evecal setup
```

### "Token refresh failed"

Your refresh token may have been revoked. Delete tokens and re-authenticate:
```bash
rm ./data/tokens.enc
docker-compose run --rm --service-ports evecal setup
```

### Rate limiting

The service respects ESI rate limits automatically. If you see rate limit warnings in logs, the service will back off appropriately.

## License

MIT
