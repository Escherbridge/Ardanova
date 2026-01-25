# ===========================================
# ArdaNova Development - Tear Down Script
# ===========================================
# Supports both Docker and Podman

param(
    [switch]$Volumes,
    [switch]$Images,
    [switch]$Prune,
    [switch]$All,
    [switch]$Help
)

# Colors
$Red = "Red"
$Green = "Green"
$Yellow = "Yellow"
$Blue = "Cyan"

function Write-ColorOutput($ForegroundColor, $Message) {
    Write-Host $Message -ForegroundColor $ForegroundColor
}

# Show help
if ($Help) {
    Write-Host "Usage: .\dev-down.ps1 [options]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Volumes    Remove volumes"
    Write-Host "  -Images     Remove images"
    Write-Host "  -Prune      Prune unused resources"
    Write-Host "  -All        Remove everything (volumes, images, prune)"
    Write-Host "  -Help       Show this help"
    exit 0
}

# Detect container runtime
function Get-ContainerRuntime {
    if (Get-Command "podman" -ErrorAction SilentlyContinue) {
        return "podman"
    }
    elseif (Get-Command "docker" -ErrorAction SilentlyContinue) {
        return "docker"
    }
    return $null
}

# Detect compose command
function Get-ComposeCommand($Runtime) {
    if ($Runtime -eq "podman") {
        if (Get-Command "podman-compose" -ErrorAction SilentlyContinue) {
            return "podman-compose"
        }
        # Try podman compose
        try {
            $null = & podman compose version 2>$null
            if ($LASTEXITCODE -eq 0) {
                return "podman compose"
            }
        } catch {}
        return $null
    }
    else {
        if (Get-Command "docker-compose" -ErrorAction SilentlyContinue) {
            return "docker-compose"
        }
        # Try docker compose
        try {
            $null = & docker compose version 2>$null
            if ($LASTEXITCODE -eq 0) {
                return "docker compose"
            }
        } catch {}
        return $null
    }
}

$Runtime = Get-ContainerRuntime

if (-not $Runtime) {
    Write-ColorOutput $Red "Error: Neither Docker nor Podman found. Please install one of them."
    exit 1
}

$ComposeCmd = Get-ComposeCommand $Runtime

Write-ColorOutput $Green "Using runtime: $Runtime"
if ($ComposeCmd) {
    Write-ColorOutput $Green "Using compose: $ComposeCmd"
} else {
    Write-ColorOutput $Yellow "Warning: Compose not found. Using direct commands."
}

# Navigate to project root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
Set-Location $ProjectRoot

Write-ColorOutput $Yellow "Tearing down ArdaNova development environment..."

# Apply -All flag
if ($All) {
    $Volumes = $true
    $Images = $true
    $Prune = $true
}

# Stop and remove containers
if ($ComposeCmd) {
    # Try development compose first, then regular
    if (Test-Path "docker-compose.dev.yml") {
        Write-ColorOutput $Yellow "Stopping development containers..."
        if ($ComposeCmd -eq "podman compose" -or $ComposeCmd -eq "docker compose") {
            $parts = $ComposeCmd.Split(" ")
            & $parts[0] $parts[1] -f docker-compose.dev.yml down 2>$null
        } else {
            & $ComposeCmd -f docker-compose.dev.yml down 2>$null
        }
    }

    Write-ColorOutput $Yellow "Stopping production containers..."
    if ($ComposeCmd -eq "podman compose" -or $ComposeCmd -eq "docker compose") {
        $parts = $ComposeCmd.Split(" ")
        & $parts[0] $parts[1] down 2>$null
    } else {
        & $ComposeCmd down 2>$null
    }
} else {
    # Manual container removal
    Write-ColorOutput $Yellow "Stopping containers manually..."
    & $Runtime stop ardanova-api ardanova-client ardanova-ai 2>$null
    & $Runtime rm ardanova-api ardanova-client ardanova-ai 2>$null
}

# Remove volumes if requested
if ($Volumes) {
    Write-ColorOutput $Yellow "Removing volumes..."
    if ($ComposeCmd) {
        if ($ComposeCmd -eq "podman compose" -or $ComposeCmd -eq "docker compose") {
            $parts = $ComposeCmd.Split(" ")
            & $parts[0] $parts[1] down -v 2>$null
        } else {
            & $ComposeCmd down -v 2>$null
        }
    }
    & $Runtime volume rm ardanova_minio_data 2>$null
}

# Remove images if requested
if ($Images) {
    Write-ColorOutput $Yellow "Removing ArdaNova images..."
    & $Runtime rmi ardanova-api ardanova-client ardanova-ai 2>$null
    & $Runtime rmi ardanova_api ardanova_client ardanova_ai 2>$null
    & $Runtime rmi ardanova-api:latest ardanova-client:latest ardanova-ai:latest 2>$null
}

# Prune if requested
if ($Prune) {
    Write-ColorOutput $Yellow "Pruning unused resources..."
    & $Runtime system prune -f
}

Write-ColorOutput $Green "Tear down complete!"
