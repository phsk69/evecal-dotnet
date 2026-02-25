# Deployment Guide 🔥

running EveCal without Docker on bare metal, VMs, or wherever you want bestie. the "works on my machine" era is JOVER fr fr no cap 💀

EveCal ships self-contained binaries for **6 platforms** — no .NET runtime needed, just download and run:

| Platform | RID | Archive |
|----------|-----|---------|
| Linux x64 | `linux-x64` | `.tar.gz` |
| Linux ARM64 | `linux-arm64` | `.tar.gz` |
| macOS x64 (Intel) | `osx-x64` | `.tar.gz` |
| macOS ARM64 (Apple Silicon) | `osx-arm64` | `.tar.gz` |
| Windows x64 | `win-x64` | `.zip` |
| Windows ARM64 | `win-arm64` | `.zip` |

## Environment Variables 🌍

| Variable | Required | Default | what it do |
|----------|----------|---------|------------|
| `EVE_CLIENT_ID` | **yes** | — | your EVE Developer app Client ID, no cap this is mandatory |
| `EVE_CALLBACK_URL` | for remote servers | `http://localhost:8080/callback` | OAuth callback URL — **MUST match your EVE app config EXACTLY** or it's jover |
| `EVE_DATA_PATH` | no | `/app/data` | where tokens + encryption key live. change this for bare metal (e.g., `./data`) |
| `EVE_SCOPES` | no | `esi-calendar.read_calendar_events.v1 esi-corporations.read_corporation_membership.v1 esi-characters.read_corporation_roles.v1` | space-separated OAuth scopes |
| `CALENDAR_REFRESH_MINUTES` | no | `15` | how often to poll ESI for calendar updates |
| `TOKEN_ENCRYPTION_KEY` | no | auto-generated | Base64 AES-256 key for token encryption. auto-generated on first setup if not set |
| `MATRIX_WEBHOOK_URL` | no | — | Matrix hookshot webhook URL for warning/error notifications via [LittyLogs.Webhooks](https://github.com/phsk69/litty-logs-dotnet) |

> **the `EVE_CALLBACK_URL` is the most important thing here fr fr** — if you're deploying on a remote server behind a reverse proxy, this MUST be the public URL that your browser can reach. like `https://evecal.example.com/callback`. if this doesn't match what's in your EVE Developer app settings, OAuth will be straight up jover no cap 💀

## Quick Start (any platform) 🚀

```bash
# 1. download + extract for your platform
tar xzf evecal-0.3.1-linux-x64.tar.gz   # linux/mac
# or unzip evecal-0.3.1-win-x64.zip      # windows

# 2. set up your environment
export EVE_CLIENT_ID=your_client_id_here
export EVE_DATA_PATH=./data              # store tokens locally, not /app/data

# 3. run setup (one-time OAuth flow — needs browser access)
./EveCal.Api setup

# 4. run the service
./EveCal.Api
# service is now bussin at http://localhost:8080
# calendar feed at http://localhost:8080/calendar/feed.ics
```

## Linux Deployment (systemd) 🐧

the most based way to run EveCal on a Linux server. systemd handles restarts, logging, and boot startup — it's giving reliability fr fr

### 1. install the binary

```bash
# create a dedicated user (security is the vibe)
sudo useradd -r -s /usr/sbin/nologin -m -d /opt/evecal evecal

# download and extract
cd /opt/evecal
sudo -u evecal tar xzf evecal-0.3.1-linux-x64.tar.gz

# create data + logs directories
sudo -u evecal mkdir -p data logs

# make binary executable
sudo chmod +x /opt/evecal/EveCal.Api
```

### 2. create the systemd service

```bash
sudo tee /etc/systemd/system/evecal.service << 'EOF'
[Unit]
Description=EveCal - EVE Online Corp Calendar ICS Feed 🔥
After=network-online.target
Wants=network-online.target

[Service]
Type=exec
User=evecal
Group=evecal
WorkingDirectory=/opt/evecal
ExecStart=/opt/evecal/EveCal.Api

# environment — set your EVE_CLIENT_ID here bestie
Environment=EVE_CLIENT_ID=your_client_id_here
Environment=EVE_DATA_PATH=/opt/evecal/data
Environment=EVE_CALLBACK_URL=http://localhost:8080/callback

# optional: Matrix webhook for notifications
# Environment=MATRIX_WEBHOOK_URL=https://hookshot.example.com/webhook/abc123

# restart on crash because uptime is bussin
Restart=on-failure
RestartSec=5

# security hardening (we take this seriously)
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/opt/evecal/data /opt/evecal/logs
PrivateTmp=true

[Install]
WantedBy=multi-user.target
EOF
```

### 3. run setup first (one-time)

```bash
# set env vars and run setup as the evecal user
sudo -u evecal bash -c 'EVE_CLIENT_ID=your_client_id EVE_DATA_PATH=/opt/evecal/data /opt/evecal/EveCal.Api setup'
# open the printed URL in your browser and complete OAuth
```

> see [Headless Server Setup](#headless-server-setup-) if your server doesn't have a browser

### 4. enable and start

```bash
sudo systemctl daemon-reload
sudo systemctl enable evecal
sudo systemctl start evecal

# check if it's bussin
sudo systemctl status evecal

# peep the logs
sudo journalctl -u evecal -f
```

### where the logs at

- **systemd journal**: `journalctl -u evecal` (stdout/stderr from LittyLogs console output)
- **file logs**: `/opt/evecal/logs/evecal.log` (daily rotation, 10MB max, 7-day retention via LittyLogs.File)

## Reverse Proxy Setup 🔒

EveCal has **no built-in TLS** — it listens on port 8080 HTTP only. for production deployments you NEED a reverse proxy to handle HTTPS. this is non-negotiable bestie 💅

> **important**: when using a reverse proxy, update `EVE_CALLBACK_URL` to your public HTTPS URL (e.g., `https://evecal.example.com/callback`) and update it in your EVE Developer app settings too!

### nginx

```nginx
server {
    listen 80;
    server_name evecal.example.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl;
    server_name evecal.example.com;

    ssl_certificate /etc/letsencrypt/live/evecal.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/evecal.example.com/privkey.pem;

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Caddy (the easy way)

```
evecal.example.com {
    reverse_proxy localhost:8080
}
```

that's literally it. Caddy handles TLS automatically with Let's Encrypt. bussin fr fr 🔥

## Headless Server Setup 🖥️

setup mode needs a browser to complete the EVE OAuth flow. if you're deploying on a headless server (no GUI), you've got options bestie:

### option 1: SSH tunnel (recommended) 🔥

the most based approach — tunnel the server's port 8080 to your local machine:

```bash
# on your local machine, open an SSH tunnel
ssh -L 8080:localhost:8080 user@your-server

# on the server (through the tunnel), run setup
EVE_CLIENT_ID=your_client_id EVE_DATA_PATH=/opt/evecal/data /opt/evecal/EveCal.Api setup

# open the printed URL in your LOCAL browser — it'll hit localhost:8080
# which tunnels to the server. OAuth callback hits the server through the tunnel. ez
```

this way `EVE_CALLBACK_URL` stays as `http://localhost:8080/callback` and everything just works no cap

### option 2: public URL setup

if your server already has a public URL with reverse proxy set up:

1. set `EVE_CALLBACK_URL=https://evecal.example.com/callback`
2. update the callback URL in your [EVE Developer app](https://developers.eveonline.com/) to match
3. run setup on the server — open the printed URL in any browser
4. OAuth callback goes to the public URL → reverse proxy → EveCal on the server

### option 3: transfer tokens from local machine (the sneaky way)

run setup locally, then yeet the tokens to the server:

```bash
# 1. run setup on your local machine
export EVE_CLIENT_ID=your_client_id
export EVE_DATA_PATH=./data
./EveCal.Api setup

# 2. copy the token files to the server
scp ./data/tokens.enc user@your-server:/opt/evecal/data/
scp ./data/encryption.key user@your-server:/opt/evecal/data/

# 3. make sure the evecal user owns them
ssh user@your-server 'chown evecal:evecal /opt/evecal/data/tokens.enc /opt/evecal/data/encryption.key'
```

> **heads up**: if you set `TOKEN_ENCRYPTION_KEY` env var instead of auto-generating, the key lives in the environment, not in `encryption.key`. make sure the same key is set on the server too bestie

## macOS Deployment 🍎

### launchd (auto-start on boot)

```bash
# create the plist
cat > ~/Library/LaunchAgents/com.evecal.service.plist << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.evecal.service</string>
    <key>ProgramArguments</key>
    <array>
        <string>/usr/local/bin/EveCal.Api</string>
    </array>
    <key>WorkingDirectory</key>
    <string>/usr/local/share/evecal</string>
    <key>EnvironmentVariables</key>
    <dict>
        <key>EVE_CLIENT_ID</key>
        <string>your_client_id_here</string>
        <key>EVE_DATA_PATH</key>
        <string>/usr/local/share/evecal/data</string>
    </dict>
    <key>KeepAlive</key>
    <true/>
    <key>RunAtLoad</key>
    <true/>
    <key>StandardOutPath</key>
    <string>/usr/local/share/evecal/logs/stdout.log</string>
    <key>StandardErrorPath</key>
    <string>/usr/local/share/evecal/logs/stderr.log</string>
</dict>
</plist>
EOF

# load it up
launchctl load ~/Library/LaunchAgents/com.evecal.service.plist
```

### or just use tmux/screen like a normal person

```bash
tmux new -s evecal
export EVE_CLIENT_ID=your_client_id
export EVE_DATA_PATH=./data
./EveCal.Api
# ctrl+b d to detach, tmux attach -t evecal to come back
```

## Windows Deployment 🪟

### PowerShell setup

```powershell
# extract the zip
Expand-Archive evecal-0.3.1-win-x64.zip -DestinationPath C:\evecal

# set environment variables
$env:EVE_CLIENT_ID = "your_client_id_here"
$env:EVE_DATA_PATH = "C:\evecal\data"

# run setup (one-time)
C:\evecal\EveCal.Api.exe setup

# run the service
C:\evecal\EveCal.Api.exe
```

### run as a Windows Service

use [NSSM](https://nssm.cc/) (Non-Sucking Service Manager) to wrap EveCal as a proper Windows service:

```powershell
# install nssm (via chocolatey or download from nssm.cc)
choco install nssm

# create the service
nssm install EveCal "C:\evecal\EveCal.Api.exe"
nssm set EveCal AppDirectory "C:\evecal"
nssm set EveCal AppEnvironmentExtra "EVE_CLIENT_ID=your_client_id" "EVE_DATA_PATH=C:\evecal\data"

# start it up
nssm start EveCal
```

## Data Persistence & Backup 💾

### what files matter

these two files ARE your service — lose them and you gotta re-run setup:

| File | what it is | jover if lost? |
|------|-----------|----------------|
| `{EVE_DATA_PATH}/tokens.enc` | encrypted OAuth tokens (access + refresh) | yes — re-run setup |
| `{EVE_DATA_PATH}/encryption.key` | AES-256 key that encrypts the tokens | yes — tokens become unreadable, re-run setup |

### backup strategy

```bash
# it's literally just copying the data directory bestie
cp -r /opt/evecal/data /backup/evecal-data-$(date +%Y%m%d)

# or rsync if you're fancy
rsync -av /opt/evecal/data/ /backup/evecal-data/
```

### what happens if you lose stuff

- **lost `tokens.enc`**: re-run `./EveCal.Api setup` to re-authenticate. not the end of the world
- **lost `encryption.key`**: existing `tokens.enc` becomes unreadable (encrypted with that key). delete `tokens.enc` too and re-run setup. it's jover for those tokens but you'll be back in business quick
- **lost both**: re-run setup from scratch. 2 minutes and you're back, no cap

## Upgrading 🆙

```bash
# 1. stop the service
sudo systemctl stop evecal

# 2. download new binary
cd /opt/evecal
sudo -u evecal tar xzf evecal-NEW_VERSION-linux-x64.tar.gz

# 3. start the service — tokens carry over, no re-setup needed
sudo systemctl start evecal
```

your tokens and encryption key persist across upgrades — no need to re-run setup unless there's a breaking change (we'll tell you in the [changelog](CHANGELOG.md) if that ever happens fr fr) 🔥
