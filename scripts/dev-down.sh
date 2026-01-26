#!/bin/bash
# ===========================================
# ArdaNova Development - Tear Down Script
# ===========================================
# Supports both Docker and Podman

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
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
        echo -e "${YELLOW}Warning: podman-compose not found. Using podman directly.${NC}"
        COMPOSE_CMD=""
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
echo -e "${GREEN}Using compose: ${COMPOSE_CMD:-"direct commands"}${NC}"

# Navigate to project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
cd "$PROJECT_ROOT"

echo -e "${YELLOW}Tearing down ArdaNova development environment...${NC}"

# Parse arguments
REMOVE_VOLUMES=false
REMOVE_IMAGES=false
PRUNE=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -v|--volumes)
            REMOVE_VOLUMES=true
            shift
            ;;
        -i|--images)
            REMOVE_IMAGES=true
            shift
            ;;
        -p|--prune)
            PRUNE=true
            shift
            ;;
        -a|--all)
            REMOVE_VOLUMES=true
            REMOVE_IMAGES=true
            PRUNE=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [options]"
            echo ""
            echo "Options:"
            echo "  -v, --volumes   Remove volumes"
            echo "  -i, --images    Remove images"
            echo "  -p, --prune     Prune unused resources"
            echo "  -a, --all       Remove everything (volumes, images, prune)"
            echo "  -h, --help      Show this help"
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            exit 1
            ;;
    esac
done

# Stop and remove containers
if [ -n "$COMPOSE_CMD" ]; then
    # Try development compose first, then regular
    if [ -f "docker-compose.dev.yml" ]; then
        echo -e "${YELLOW}Stopping development containers...${NC}"
        $COMPOSE_CMD -f docker-compose.dev.yml down --remove-orphans || true
    fi

    echo -e "${YELLOW}Stopping production containers...${NC}"
    $COMPOSE_CMD down --remove-orphans || true
else
    # Manual container removal for podman without compose
    echo -e "${YELLOW}Stopping containers manually...${NC}"
    $RUNTIME stop ardanova-api ardanova-client ardanova-ai 2>/dev/null || true
    $RUNTIME rm -f ardanova-api ardanova-client ardanova-ai 2>/dev/null || true
fi

# Force remove containers by name (handles Podman edge cases)
echo -e "${YELLOW}Cleaning up any remaining containers...${NC}"
$RUNTIME rm -f ardanova-api-dev ardanova-client-dev ardanova-ai-dev 2>/dev/null || true

# For Podman: clean up pods and networks
if [ "$RUNTIME" = "podman" ]; then
    echo -e "${YELLOW}Cleaning up Podman pods...${NC}"
    for pod in $(podman pod ls --format "{{.Name}}" 2>/dev/null | grep -i ardanova); do
        podman pod rm -f "$pod" 2>/dev/null || true
    done

    echo -e "${YELLOW}Cleaning up networks...${NC}"
    podman network rm -f ardanova_default 2>/dev/null || true
fi

# Remove volumes if requested
if [ "$REMOVE_VOLUMES" = true ]; then
    echo -e "${YELLOW}Removing volumes...${NC}"
    if [ -n "$COMPOSE_CMD" ]; then
        $COMPOSE_CMD down -v 2>/dev/null || true
    fi
    $RUNTIME volume rm ardanova_minio_data 2>/dev/null || true
fi

# Remove images if requested
if [ "$REMOVE_IMAGES" = true ]; then
    echo -e "${YELLOW}Removing ArdaNova images...${NC}"
    $RUNTIME rmi ardanova-api ardanova-client ardanova-ai 2>/dev/null || true
    $RUNTIME rmi ardanova_api ardanova_client ardanova_ai 2>/dev/null || true
    # Also remove with project prefix
    $RUNTIME rmi ardanova-api:latest ardanova-client:latest ardanova-ai:latest 2>/dev/null || true
fi

# Prune if requested
if [ "$PRUNE" = true ]; then
    echo -e "${YELLOW}Pruning unused resources...${NC}"
    $RUNTIME system prune -f
fi

echo -e "${GREEN}Tear down complete!${NC}"
