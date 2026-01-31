# EVE Calendar Service

# Build the Docker image
build:
    docker-compose build

# Run setup (rebuild first)
setup: build
    docker-compose run --rm --service-ports evecal setup

# Start the service
up:
    docker-compose up -d

# Stop the service
down:
    docker-compose down

# View logs
logs:
    docker-compose logs -f

# Rebuild and start
restart: build
    docker-compose up -d

# Quick test endpoints
test:
    @echo "Status:"
    @curl -s http://localhost:8080/calendar/status | jq .
    @echo "\nFeed (first 20 lines):"
    @curl -s http://localhost:8080/calendar/feed.ics | head -20

# Clean tokens and start fresh
clean:
    rm -f data/tokens.enc data/encryption.key
