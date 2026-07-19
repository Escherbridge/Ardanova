#!/bin/bash
# ===========================================
# ArdaNova Development Environment Utility
# ===========================================
# Comprehensive local dev setup for ArdaNova
# Supports: local mode (default) and Docker mode
# Auto-checks prerequisites, kills stale ports, validates .env

set -e

# --- Colors ---
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
WHITE='\033[1;37m'
NC='\033[0m'

# --- Config ---
MODE="local"
API_ONLY=false
CLIENT_ONLY=false
DO_INSTALL=false
CHECK_ONLY=false
KILL_ONLY=false

# --- Paths ---
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
API_PROJECT_DIR="$PROJECT_ROOT/ardanova-backend-api-mcp/api-server/src/ArdaNova.API"
CLIENT_DIR="$PROJECT_ROOT/ardanova-client"
ENV_FILE="$PROJECT_ROOT/.env"

# --- Ports ---
API_HTTP_PORT=5147
API_HTTPS_PORT=7160
CLIENT_PORT=3000
DOCKER_API_PORT=8080

# --- Parse Args ---
while [[ $# -gt 0 ]]; do
    case $1 in
        --mode)
            MODE="$2"
            shift 2
            ;;
        --docker)
            MODE="docker"
            shift
            ;;
        --api-only)
            API_ONLY=true
            shift
            ;;
        --client-only)
            CLIENT_ONLY=true
            shift
            ;;
        --install)
            DO_INSTALL=true
            shift
            ;;
        --check)
            CHECK_ONLY=true
            shift
            ;;
        --kill)
            KILL_ONLY=true
            shift
            ;;
        -h|--help)
            echo ""
            echo -e "${BLUE}ArdaNova Dev Environment Utility${NC}"
            echo -e "${BLUE}=================================${NC}"
            echo ""
            echo "Usage: $0 [options]"
            echo ""
            echo -e "${GREEN}Modes:${NC}"
            echo "  --mode local    Run .NET API + Next.js locally (default)"
            echo "  --mode docker   Run via Docker Compose containers"
            echo "  --docker        Shorthand for --mode docker"
            echo ""
            echo -e "${GREEN}Service Flags:${NC}"
            echo "  --api-only      Start only the .NET backend API"
            echo "  --client-only   Start only the Next.js client"
            echo ""
            echo -e "${GREEN}Utility Flags:${NC}"
            echo "  --install       Fresh lockfile install (npm ci, dotnet restore)"
            echo "  --check         Check prerequisites only, don't start anything"
            echo "  --kill          Kill all ArdaNova dev processes and free ports"
            echo "  -h, --help      Show this help"
            echo ""
            echo -e "${YELLOW}Local Mode URLs:${NC}"
            echo "  API (HTTPS):  https://localhost:$API_HTTPS_PORT"
            echo "  API (HTTP):   http://localhost:$API_HTTP_PORT"
            echo "  Swagger:      https://localhost:$API_HTTPS_PORT/swagger"
            echo "  Client:       http://localhost:$CLIENT_PORT"
            echo ""
            echo -e "${YELLOW}Docker Mode URLs:${NC}"
            echo "  API:          http://localhost:$DOCKER_API_PORT"
            echo "  Swagger:      http://localhost:$DOCKER_API_PORT/swagger"
            echo "  Client:       http://localhost:$CLIENT_PORT"
            echo ""
            echo -e "${GREEN}Examples:${NC}"
            echo "  $0                      # Start everything locally"
            echo "  $0 --api-only           # Start only the API"
            echo "  $0 --install            # Install deps then start"
            echo "  $0 --docker             # Start via Docker Compose"
            echo "  $0 --kill               # Kill all dev processes"
            echo "  $0 --check              # Verify prerequisites"
            echo ""
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            echo "Use -h or --help for usage"
            exit 1
            ;;
    esac
done

# --- Utility Functions ---
step()  { echo -e "${GREEN}[*] $1${NC}"; }
warn()  { echo -e "${YELLOW}[!] $1${NC}"; }
err()   { echo -e "${RED}[X] $1${NC}"; }
info()  { echo -e "    ${WHITE}$1${NC}"; }

load_dotenv() {
    while IFS='=' read -r key value || [ -n "$key" ]; do
        key="${key#${key%%[![:space:]]*}}"
        key="${key%${key##*[![:space:]]}}"
        [[ "$key" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]] || continue
        # Security overrides must never be inherited from a shared dotenv file.
        case "$key" in
            ALLOW_REMOTE_DEV_DATABASE|NODE_TLS_REJECT_UNAUTHORIZED) continue ;;
        esac

        value="${value#${value%%[![:space:]]*}}"
        value="${value%${value##*[![:space:]]}}"
        if [[ "$value" == \"*\" && "$value" == *\" ]] ||
           [[ "$value" == \'*\' && "$value" == *\' ]]; then
            value="${value:1:${#value}-2}"
        fi

        # Explicit process variables take precedence over the shared local file.
        if [ -z "${!key+x}" ]; then
            export "$key=$value"
        fi
    done < "$1"
}

assert_secure_tls_configuration() {
    if [ "${NODE_TLS_REJECT_UNAUTHORIZED:-}" = "0" ]; then
        err "Refusing to start with NODE_TLS_REJECT_UNAUTHORIZED=0."
        info "Use a trusted local certificate or the HTTP loopback API origin instead."
        exit 1
    fi

    # Pin the secure value so Next.js dotenv loading cannot override it.
    export NODE_TLS_REJECT_UNAUTHORIZED=1
}

assert_local_database_target() {
    [ -n "${DATABASE_URL:-}" ] || return 0

    local database_host
    if ! database_host="$(node -e 'try { console.log(new URL(process.env.DATABASE_URL).hostname) } catch { process.exit(1) }')"; then
        err "DATABASE_URL is not a valid absolute PostgreSQL URL."
        exit 1
    fi

    case "$database_host" in
        localhost|127.0.0.1|::1|\[::1\])
            return 0
            ;;
    esac

    if [ "${ALLOW_REMOTE_DEV_DATABASE:-}" = "true" ]; then
        warn "Remote development database explicitly allowed for host '$database_host'."
        return 0
    fi

    err "Refusing to start local services against remote database host '$database_host'."
    info "Set DATABASE_URL to a dedicated loopback database."
    info "For an intentional remote session only, set ALLOW_REMOTE_DEV_DATABASE=true."
    exit 1
}

banner() {
    echo ""
    echo -e "${BLUE}==========================================${NC}"
    echo -e "${BLUE}  ArdaNova Development Environment${NC}"
    echo -e "${BLUE}==========================================${NC}"
    echo ""
}

kill_port() {
    local port=$1
    local pids=$(lsof -ti :$port 2>/dev/null || true)
    if [ -n "$pids" ]; then
        for pid in $pids; do
            local name=$(ps -p $pid -o comm= 2>/dev/null || echo "unknown")
            warn "Killing $name (PID $pid) on port $port"
            kill -9 $pid 2>/dev/null || true
        done
        sleep 1
    fi
}

# --- Track PIDs for cleanup ---
PIDS=()

cleanup() {
    echo ""
    step "Stopping all services..."
    for pid in "${PIDS[@]}"; do
        if kill -0 "$pid" 2>/dev/null; then
            info "Stopping PID $pid"
            kill "$pid" 2>/dev/null || true
        fi
    done
    # Kill any remaining dotnet processes for this project
    pkill -f "dotnet.*ArdaNova" 2>/dev/null || true
    wait 2>/dev/null || true
    step "All services stopped."
    exit 0
}

trap cleanup SIGINT SIGTERM

# ===========================================
# Kill Mode
# ===========================================
if [ "$KILL_ONLY" = true ]; then
    banner
    step "Killing all ArdaNova dev processes..."
    kill_port $API_HTTP_PORT
    kill_port $API_HTTPS_PORT
    kill_port $CLIENT_PORT
    kill_port $DOCKER_API_PORT
    pkill -f "dotnet.*ArdaNova" 2>/dev/null || true
    pkill -f "next dev" 2>/dev/null || true
    step "All dev processes stopped."
    exit 0
fi

# ===========================================
# Prerequisite Checks
# ===========================================
check_prerequisites() {
    step "Checking prerequisites..."
    local all_good=true

    # .NET SDK
    if command -v dotnet &>/dev/null; then
        info "dotnet SDK: $(dotnet --version 2>/dev/null)"
    else
        err "dotnet SDK not found. Install from https://dotnet.microsoft.com/download"
        all_good=false
    fi

    # Node.js
    if command -v node &>/dev/null; then
        info "Node.js: $(node --version 2>/dev/null)"
    else
        err "Node.js not found. Install from https://nodejs.org"
        all_good=false
    fi

    # npm
    if command -v npm &>/dev/null; then
        info "npm: $(npm --version 2>/dev/null)"
    else
        err "npm not found."
        all_good=false
    fi

    # Project directories
    if [ -d "$API_PROJECT_DIR" ]; then
        info "API project: Found"
    else
        err "API project not found at: $API_PROJECT_DIR"
        all_good=false
    fi

    if [ -d "$CLIENT_DIR" ]; then
        info "Client project: Found"
    else
        err "Client project not found at: $CLIENT_DIR"
        all_good=false
    fi

    # .env file
    if [ -f "$ENV_FILE" ]; then
        info ".env file: Found"
        local required_vars=(
            "DATABASE_URL"
            "AUTH_SECRET"
            "GOOGLE_CLIENT_ID"
            "GOOGLE_CLIENT_SECRET"
            "API_KEY"
            "ADMIN_API_KEY"
            "ACTOR_ASSERTION_HMAC_KEY"
        )
        for var in "${required_vars[@]}"; do
            if ! grep -q "^${var}=.\+" "$ENV_FILE" 2>/dev/null; then
                warn "Missing or empty: $var in .env"
            fi
        done
    else
        err ".env file not found at: $ENV_FILE"
        info "Copy .env.example to .env and fill in values"
        all_good=false
    fi

    # node_modules
    if [ -d "$CLIENT_DIR/node_modules" ]; then
        info "node_modules: Installed"
    else
        warn "node_modules not found. Run with --install or 'npm ci' in ardanova-client/"
    fi

    # Docker (if docker mode)
    if [ "$MODE" = "docker" ]; then
        if command -v docker &>/dev/null; then
            info "Docker: $(docker --version 2>/dev/null)"
        elif command -v podman &>/dev/null; then
            info "Podman: $(podman --version 2>/dev/null)"
        else
            err "Docker/Podman required for docker mode."
            all_good=false
        fi
    fi

    echo ""
    [ "$all_good" = true ]
}

banner

if ! check_prerequisites; then
    if [ "$CHECK_ONLY" = true ]; then
        err "Some prerequisites missing. Fix the issues above."
        exit 1
    fi
    err "Prerequisites check failed. Use --check for details."
    exit 1
fi

if [ "$CHECK_ONLY" = true ]; then
    step "All prerequisites met!"
    exit 0
fi

if [ "$MODE" = "local" ]; then
    step "Loading shared local environment..."
    load_dotenv "$ENV_FILE"
    info "Loaded repository .env without overriding process variables"
    assert_secure_tls_configuration
    assert_local_database_target
    echo ""
fi

# ===========================================
# Install Dependencies
# ===========================================
if [ "$DO_INSTALL" = true ]; then
    if [ "$CLIENT_ONLY" != true ]; then
        step "Restoring .NET packages..."
        dotnet restore "$PROJECT_ROOT/ardanova-backend-api-mcp/api-server" >/dev/null 2>&1
        info "dotnet restore complete"
    fi

    if [ "$API_ONLY" != true ]; then
        step "Installing npm packages from package-lock.json..."
        (cd "$CLIENT_DIR" && npm ci) >/dev/null 2>&1
        info "npm ci complete (including npm run generate:prisma postinstall)"
    fi
    echo ""
fi

# ===========================================
# Docker Mode
# ===========================================
if [ "$MODE" = "docker" ]; then
    step "Starting in DOCKER mode..."

    COMPOSE_CMD=""
    if command -v docker &>/dev/null; then
        if docker compose version &>/dev/null 2>&1; then
            COMPOSE_CMD="docker compose"
        elif command -v docker-compose &>/dev/null; then
            COMPOSE_CMD="docker-compose"
        fi
    fi
    if [ -z "$COMPOSE_CMD" ] && command -v podman-compose &>/dev/null; then
        COMPOSE_CMD="podman-compose"
    fi

    if [ -z "$COMPOSE_CMD" ]; then
        err "No compose command found."
        exit 1
    fi

    info "Using: $COMPOSE_CMD"

    COMPOSE_FILE="$PROJECT_ROOT/docker-compose.dev.yml"
    if [ ! -f "$COMPOSE_FILE" ]; then
        err "docker-compose.dev.yml not found"
        exit 1
    fi

    SERVICES=""
    if [ "$API_ONLY" = true ]; then
        SERVICES="api"
    elif [ "$CLIENT_ONLY" = true ]; then
        SERVICES="client"
    else
        SERVICES="api client"
    fi

    step "Running compose up..."
    echo ""
    $COMPOSE_CMD -f "$COMPOSE_FILE" up --build --force-recreate --remove-orphans $SERVICES
    exit $?
fi

# ===========================================
# Local Mode
# ===========================================
step "Starting in LOCAL mode..."
echo ""

# Kill stale processes
step "Freeing ports..."
if [ "$CLIENT_ONLY" != true ]; then
    kill_port $API_HTTP_PORT
    kill_port $API_HTTPS_PORT
fi
if [ "$API_ONLY" != true ]; then
    kill_port $CLIENT_PORT
fi
echo ""

# Start .NET API
if [ "$CLIENT_ONLY" != true ]; then
    step "Starting .NET API (dotnet watch run)..."
    (cd "$API_PROJECT_DIR" && dotnet watch run) &
    PIDS+=($!)
    info "PID: ${PIDS[-1]}"
    info "HTTPS: https://localhost:$API_HTTPS_PORT"
    info "HTTP:  http://localhost:$API_HTTP_PORT"
    info "Swagger: https://localhost:$API_HTTPS_PORT/swagger"
    echo ""

    sleep 3
fi

# Start Next.js Client
if [ "$API_ONLY" != true ]; then
    step "Starting Next.js client (npm run dev)..."
    (cd "$CLIENT_DIR" && npm run dev) &
    PIDS+=($!)
    info "PID: ${PIDS[-1]}"
    info "URL: http://localhost:$CLIENT_PORT"
    echo ""
fi

echo -e "${GREEN}==========================================${NC}"
echo -e "${GREEN}  All services started!${NC}"
echo -e "${GREEN}  Press Ctrl+C to stop everything${NC}"
echo -e "${GREEN}==========================================${NC}"
echo ""

wait
