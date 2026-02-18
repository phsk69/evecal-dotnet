# EVE Calendar Service

a .NET 10 service that serves up your **corporation** EVE Online calendar events as a subscribable calendar feed (iCal/ICS format). its giving organization bestie.

## what this bad boy does

- corp calendar events only (not your personal stuff, we respect privacy fr)
- ICS feed that slaps with Outlook, Google Calendar, Apple Calendar, etc
- headless OAuth setup - prints URL for browser auth, very convenient
- encrypted token storage (security is bussin)
- automatic token refresh so it stays working
- human-like ESI requests with rate limit respect (we dont get banned here)

## what you need first

- Docker and Docker Compose
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

1. **set up the environment**

   create a `.env` file:
   ```bash
   EVE_CLIENT_ID=your_client_id_here
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
   docker-compose up -d
   ```

4. **subscribe to the calendar**

   add this URL to your calendar app:
   ```
   http://localhost:8080/calendar/feed.ics
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

### data persistence

token data lives in `/app/data` inside the container. mount a volume so tokens survive restarts:

```yaml
volumes:
  - ./data:/app/data
```

## development

### local dev

```bash
cd src/EveCal.Api
export EVE_CLIENT_ID=your_client_id
dotnet run
```

### building

```bash
docker build -t evecal .
```

## architecture (how this thing is built fr)

```
src/EveCal.Api/
├── Controllers/
│   ├── AuthController.cs      # OAuth callback handling
│   └── CalendarController.cs  # ICS feed endpoint
├── Services/
│   ├── EveAuthService.cs      # OAuth/SSO with PKCE, very secure
│   ├── EveCalendarService.cs  # ESI calendar API calls
│   └── ICalGeneratorService.cs # ICS generation magic
├── Models/
│   ├── EveCalendarEvent.cs    # ESI event models
│   ├── EveConfiguration.cs    # app config
│   ├── EveTokens.cs           # token storage models
│   └── SsoCharacter.cs        # character identity
└── Infrastructure/
    ├── EsiHttpClientFactory.cs # rate-limited HTTP client, stays respectful
    └── TokenStorage.cs         # encrypted token persistence, locked in
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
