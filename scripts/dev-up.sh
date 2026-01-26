#!/bin/bash
# ===========================================
# ArdaNova Development - Build & Start Script
# ===========================================
# Supports both Docker and Podman

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Detect container runtime
detect_runtime() {
    if command -v podman &> /dev/null; then
        echo "podman"
    elif command -v docker &> /dev/null; then
        echo "docker"
    else
        echo ""
    fi
}

RUNTIME=$(detect_runtime)
COMPOSE_CMD=""

if [ -z "$RUNTIME" ]; then
    echo -e "${RED}Error: Neither Docker nor Podman found. Please install one of them.${NC}"
    exit 1
fi

# Detect compose command
if [ "$RUNTIME" = "podman" ]; then
    if command -v podman-compose &> /dev/null; then
        COMPOSE_CMD="podman-compose"
    elif podman compose version &> /dev/null 2>&1; then
        COMPOSE_CMD="podman compose"
    else
        echo -e "${RED}Error: podman-compose not found. Please install it.${NC}"
        exit 1
    fi
else
    if command -v docker-compose &> /dev/null; then
        COMPOSE_CMD="docker-compose"
    elif docker compose version &> /dev/null 2>&1; then
        COMPOSE_CMD="docker compose"
    else
        echo -e "${RED}Error: docker-compose not found.${NC}"
        exit 1
    fi
fi

echo -e "${GREEN}Using runtime: ${RUNTIME}${NC}"
echo -e "${GREEN}Using compose: ${COMPOSE_CMD}${NC}"

# Navigate to project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
cd "$PROJECT_ROOT"

# Parse arguments
DEV_MODE=true
BUILD=false
DETACHED=true
SERVICE=""

while [[ $# -gt 0 ]]; do
    case $1 in
        -p|--prod)
            DEV_MODE=false
            shift
            ;;
        -b|--build)
            BUILD=true
            shift
            ;;
        -f|--foreground)
            DETACHED=false
            shift
            ;;
        -s|--service)
            SERVICE="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [options]"
            echo ""
            echo "Options:"
            echo "  -p, --prod        Use production compose (default: development)"
            echo "  -b, --build       Force rebuild images"
            echo "  -f, --foreground  Run in foreground (default: detached)"
            echo "  -s, --service     Start specific service only"
            echo "  -h, --help        Show this help"
            echo ""
            echo "Development mode (default):"
            echo "  - Mounts source code for hot-reload"
            echo "  - .NET backend uses 'dotnet watch' for live changes"
            echo "  - Next.js client uses 'npm run dev' with Turbopack"
            echo "  - No need to rebuild for code changes"
            echo ""
            echo "Production mode (-p):"
            echo "  - Uses pre-built images"
            echo "  - Optimized for deployment"
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            exit 1
            ;;
    esac
done

# Check for .env file
if [ ! -f ".env" ]; then
    echo -e "${YELLOW}Warning: .env file not found. Copying from .env.example...${NC}"
    if [ -f ".env.example" ]; then
        cp .env.example .env
        echo -e "${YELLOW}Please edit .env with your configuration before continuing.${NC}"
        exit 1
    else
        echo -e "${RED}Error: .env.example not found.${NC}"
        exit 1
    fi
fi

# Select compose file
if [ "$DEV_MODE" = true ]; then
    COMPOSE_FILE="docker-compose.dev.yml"
    echo -e "${BLUE}Starting in DEVELOPMENT mode (hot-reload enabled)${NC}"
else
    COMPOSE_FILE="docker-compose.yml"
    echo -e "${BLUE}Starting in PRODUCTION mode${NC}"
fi

# Check if compose file exists
if [ ! -f "$COMPOSE_FILE" ]; then
    echo -e "${RED}Error: $COMPOSE_FILE not found.${NC}"
    exit 1
fi

# Build command
CMD="$COMPOSE_CMD -f $COMPOSE_FILE"

if [ "$BUILD" = true ]; then
    CMD="$CMD up --build"
else
    CMD="$CMD up"
fi

if [ "$DETACHED" = true ]; then
    CMD="$CMD -d"
fi

if [ -n "$SERVICE" ]; then
    CMD="$CMD $SERVICE"
fi

echo -e "${YELLOW}Running: $CMD${NC}"
$CMD

if [ "$DETACHED" = true ]; then
    echo ""
    echo -e "${GREEN}Services started successfully!${NC}"
    echo ""
    echo -e "${BLUE}Service URLs:${NC}"
    echo -e "  API:        http://localhost:8080"
    echo -e "  Client:     http://localhost:3000"
    echo -e "  AI Service: http://localhost:8081"
    echo ""
    echo -e "${BLUE}Useful commands:${NC}"
    echo -e "  View logs:    $COMPOSE_CMD -f $COMPOSE_FILE logs -f"
    echo -e "  Stop:         $COMPOSE_CMD -f $COMPOSE_FILE down"
    echo -e "  Restart API:  $COMPOSE_CMD -f $COMPOSE_FILE restart api"

    if [ "$DEV_MODE" = true ]; then
        echo ""
        echo -e "${GREEN}Development mode active:${NC}"
        echo -e "  - .NET backend changes will auto-reload (dotnet watch)"
        echo -e "  - Next.js client changes will auto-reload (Turbopack)"
        echo -e "  - No rebuild needed for code changes"
    fi
fi
