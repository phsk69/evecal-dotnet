# evecal — EVE Online corp calendar as ICS feed, absolutely bussin no cap 🔥

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

# Run unit tests with litty-fied output fr fr 🔥
# sources .env so MATRIX_WEBHOOK_URL is available for webhook integration tests 📨
test:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ -f .env ]; then
        set -a
        source .env
        set +a
    fi
    dotnet litty test

# Build through the litty tool for that gen alpha build output 🏗️
litty-build:
    dotnet litty build

# Quick smoke test endpoints (service must be running)
smoke:
    @echo "Status:"
    @curl -s http://localhost:8080/calendar/status | jq .
    @echo "\nFeed (first 20 lines):"
    @curl -s http://localhost:8080/calendar/feed.ics | head -20

# local CI vibes — test pipelines without spamming commits no cap 🧪🔥
# usage: just ci local | lint | check
ci action:
    #!/usr/bin/env bash
    set -euo pipefail
    case "{{action}}" in
        local)
            if [ -f .env ]; then
                act -W .forgejo/workflows/ci.yml push --secret-file .env
            else
                echo "💀 no .env file found — create one from .env.example first bestie"
                exit 1
            fi
            ;;
        lint)
            actionlint -verbose .forgejo/workflows/*.yml
            ;;
        check)
            echo "🔍 linting workflows first..."
            actionlint -verbose .forgejo/workflows/*.yml
            echo ""
            echo "🧪 running CI pipeline locally with act..."
            if [ -f .env ]; then
                act -W .forgejo/workflows/ci.yml push --secret-file .env
            else
                echo "💀 no .env file found — create one from .env.example first bestie"
                exit 1
            fi
            ;;
        *)
            echo "fam thats not a valid ci action — use local, lint, or check no cap 😤"
            exit 1
            ;;
    esac

# yeet all build artifacts and token data
clean:
    dotnet clean
    rm -f data/tokens.enc data/encryption.key

# bump the version bestie — usage: just bump major|minor|patch 🔥
bump part:
    #!/usr/bin/env bash
    set -euo pipefail
    props="Directory.Build.props"
    current=$(grep -oP '(?<=<Version>)[^<]+' "$props")
    if [ -z "$current" ]; then
        echo "bruh cant find <Version> in $props thats not bussin 💀"
        exit 1
    fi
    base="${current%%-*}"
    IFS='.' read -r major minor patch <<< "$base"
    case "{{part}}" in
        major) major=$((major + 1)); minor=0; patch=0 ;;
        minor) minor=$((minor + 1)); patch=0 ;;
        patch) patch=$((patch + 1)) ;;
        *) echo "fam thats not a valid bump part — use major, minor, or patch no cap 😤"; exit 1 ;;
    esac
    new_version="${major}.${minor}.${patch}"
    sed -i "s|<Version>${current}</Version>|<Version>${new_version}</Version>|" "$props"
    echo "version went from ${current} -> ${new_version} lets gooo 🔥"

# slap a pre-release label on the current version — usage: just bump-pre dev.1 🧪
bump-pre label:
    #!/usr/bin/env bash
    set -euo pipefail
    props="Directory.Build.props"
    current=$(grep -oP '(?<=<Version>)[^<]+' "$props")
    if [ -z "$current" ]; then
        echo "bruh cant find <Version> in $props thats not bussin 💀"
        exit 1
    fi
    base="${current%%-*}"
    new_version="${base}-{{label}}"
    sed -i "s|<Version>${current}</Version>|<Version>${new_version}</Version>|" "$props"
    echo "version went from ${current} -> ${new_version} (pre-release mode activated) 🧪"

# gitflow release — start branch clean, bump on the branch, finish 🚀
# usage: just release patch (or minor, or major)
release part:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ -n "$(git status --porcelain)" ]; then
        echo "fam your working tree is dirty, commit or stash first no cap 😤"
        exit 1
    fi
    props="Directory.Build.props"
    current=$(grep -oP '(?<=<Version>)[^<]+' "$props")
    base="${current%%-*}"
    IFS='.' read -r major minor patch <<< "$base"
    case "{{part}}" in
        major) major=$((major + 1)); minor=0; patch=0 ;;
        minor) minor=$((minor + 1)); patch=0 ;;
        patch) patch=$((patch + 1)) ;;
        *) echo "fam thats not a valid bump part — use major, minor, or patch no cap 😤"; exit 1 ;;
    esac
    new_version="${major}.${minor}.${patch}"
    echo "starting the gitflow release ritual bestie 🕯️"
    echo "  ${current} -> ${new_version}"
    echo ""
    git flow release start "v${new_version}"
    sed -i "s|<Version>${current}</Version>|<Version>${new_version}</Version>|" "$props"
    git add "$props"
    git commit -m "bump: v${new_version} incoming no cap 🔥"
    GIT_MERGE_AUTOEDIT=no git flow release finish "v${new_version}" -m "v${new_version} dropped no cap 🔥"
    echo ""
    echo "=========================================="
    echo "  release v${new_version} complete 🔥"
    echo "=========================================="
    echo ""
    echo "pushing develop, main, and tag to origin 📤"
    git push origin develop master "v${new_version}"
    echo "everything is pushed — pipeline go brrr 🚀🔥"

# release the current version as-is without bumping 🚀
release-current:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ -n "$(git status --porcelain)" ]; then
        echo "fam your working tree is dirty, commit or stash first no cap 😤"
        exit 1
    fi
    version=$(grep -oP '(?<=<Version>)[^<]+' Directory.Build.props)
    echo "releasing v${version} as-is bestie 🕯️"
    echo ""
    git flow release start "v${version}"
    GIT_MERGE_AUTOEDIT=no git flow release finish "v${version}" -m "v${version} dropped no cap 🔥"
    echo ""
    echo "=========================================="
    echo "  release v${version} complete 🔥"
    echo "=========================================="
    echo ""
    echo "pushing develop, master, and tag to origin 📤"
    git push origin develop master "v${version}"
    echo "everything is pushed — pipeline go brrr 🚀🔥"

# dev/pre-release — bump + slap a label on it and ship the whole thing 🧪
# usage: just release-dev patch [label] — label defaults to "dev"
release-dev part label="dev":
    #!/usr/bin/env bash
    set -euo pipefail
    if [ -n "$(git status --porcelain)" ]; then
        echo "fam your working tree is dirty, commit or stash first no cap 😤"
        exit 1
    fi
    props="Directory.Build.props"
    current=$(grep -oP '(?<=<Version>)[^<]+' "$props")
    base="${current%%-*}"
    IFS='.' read -r major minor patch <<< "$base"
    case "{{part}}" in
        major) major=$((major + 1)); minor=0; patch=0 ;;
        minor) minor=$((minor + 1)); patch=0 ;;
        patch) patch=$((patch + 1)) ;;
        *) echo "fam thats not a valid bump part — use major, minor, or patch no cap 😤"; exit 1 ;;
    esac
    new_version="${major}.${minor}.${patch}-{{label}}"
    echo "starting dev release bestie 🧪"
    echo "  ${current} -> ${new_version}"
    echo ""
    git flow release start "v${new_version}"
    sed -i "s|<Version>${current}</Version>|<Version>${new_version}</Version>|" "$props"
    git add "$props"
    git commit -m "bump: v${new_version} dev release incoming 🧪"
    GIT_MERGE_AUTOEDIT=no git flow release finish "v${new_version}" -m "v${new_version} dropped no cap 🔥"
    echo ""
    echo "=========================================="
    echo "  dev release v${new_version} complete 🧪🔥"
    echo "=========================================="
    echo ""
    echo "pushing develop, master, and tag to origin 📤"
    git push origin develop master "v${new_version}"
    echo "everything is pushed — pipeline go brrr 🚀🔥"

# start a hotfix — for when something is bricked in prod 🚑
# usage: just hotfix patch (or minor, or major)
hotfix part:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ -n "$(git status --porcelain)" ]; then
        echo "fam your working tree is dirty, commit or stash first no cap 😤"
        exit 1
    fi
    props="Directory.Build.props"
    current=$(grep -oP '(?<=<Version>)[^<]+' "$props")
    base="${current%%-*}"
    IFS='.' read -r major minor patch <<< "$base"
    case "{{part}}" in
        major) major=$((major + 1)); minor=0; patch=0 ;;
        minor) minor=$((minor + 1)); patch=0 ;;
        patch) patch=$((patch + 1)) ;;
        *) echo "fam thats not a valid bump part — use major, minor, or patch no cap 😤"; exit 1 ;;
    esac
    new_version="${major}.${minor}.${patch}"
    echo "starting hotfix — something in prod is not bussin 🚑"
    echo "  ${current} -> ${new_version}"
    git flow hotfix start "v${new_version}"
    sed -i "s|<Version>${current}</Version>|<Version>${new_version}</Version>|" "$props"
    git add "$props"
    git commit -m "bump: v${new_version} hotfix incoming 🚑"
    echo ""
    echo "hotfix/v${new_version} branch created and version bumped 🔥"
    echo "now make your fix, commit it, then run:"
    echo "  just finish"

# finish whatever gitflow branch youre on — hotfix, release, or support 🏁
finish:
    #!/usr/bin/env bash
    set -euo pipefail
    branch=$(git rev-parse --abbrev-ref HEAD)
    if [ -n "$(git status --porcelain)" ]; then
        echo "fam your working tree is dirty, commit or stash first no cap 😤"
        exit 1
    fi
    if [[ "$branch" == hotfix/* ]]; then
        version="${branch#hotfix/}"
        kind="hotfix"
        emoji="🚑"
    elif [[ "$branch" == release/* ]]; then
        version="${branch#release/}"
        kind="release"
        emoji="🚀"
    elif [[ "$branch" == support/* ]]; then
        version="${branch#support/}"
        kind="support"
        emoji="🛠️"
    else
        echo "bruh youre on '${branch}' — thats not a hotfix, release, or support branch 💀"
        echo "get on the right branch first bestie"
        exit 1
    fi
    version_clean="${version#v}"
    echo "finishing ${kind} v${version_clean} ${emoji}🏁"
    GIT_MERGE_AUTOEDIT=no git flow "${kind}" finish "${version}" -m "v${version_clean} ${kind} dropped no cap 🔥"
    echo ""
    echo "=========================================="
    echo "  v${version_clean} complete ${emoji}🔥"
    echo "=========================================="
    echo ""
    echo "pushing develop, master, and tag to origin 📤"
    git push origin develop master "${version}"
    echo ""
    echo "everything is pushed — pipeline go brrr 🚀🔥"
