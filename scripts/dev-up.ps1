# ===========================================
# ArdaNova Development - Build & Start Script
# ===========================================
# Supports both Docker and Podman

param(
    [switch]$Prod,
    [switch]$Build,
    [switch]$Foreground,
    [string]$Service,
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
    Write-Host "Usage: .\dev-up.ps1 [options]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Prod        Use production compose (default: development)"
    Write-Host "  -Build       Force rebuild images"
    Write-Host "  -Foreground  Run in foreground (default: detached)"
    Write-Host "  -Service     Start specific service only"
    Write-Host "  -Help        Show this help"
    Write-Host ""
    Write-Host "Development mode (default):"
    Write-Host "  - Mounts source code for hot-reload"
    Write-Host "  - .NET backend uses 'dotnet watch' for live changes"
    Write-Host "  - Next.js client uses 'npm run dev' with Turbopack"
    Write-Host "  - No need to rebuild for code changes"
    Write-Host ""
    Write-Host "Production mode (-Prod):"
    Write-Host "  - Uses pre-built images"
    Write-Host "  - Optimized for deployment"
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

if (-not $ComposeCmd) {
    Write-ColorOutput $Red "Error: Compose command not found. Please install docker-compose or podman-compose."
    exit 1
}

Write-ColorOutput $Green "Using runtime: $Runtime"
Write-ColorOutput $Green "Using compose: $ComposeCmd"

# Navigate to project root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
Set-Location $ProjectRoot

# Check for .env file
if (-not (Test-Path ".env")) {
    Write-ColorOutput $Yellow "Warning: .env file not found. Copying from .env.example..."
    if (Test-Path ".env.example") {
        Copy-Item ".env.example" ".env"
        Write-ColorOutput $Yellow "Please edit .env with your configuration before continuing."
        exit 1
    } else {
        Write-ColorOutput $Red "Error: .env.example not found."
        exit 1
    }
}

# Select compose file
$DevMode = -not $Prod
if ($DevMode) {
    $ComposeFile = "docker-compose.dev.yml"
    Write-ColorOutput $Blue "Starting in DEVELOPMENT mode (hot-reload enabled)"
} else {
    $ComposeFile = "docker-compose.yml"
    Write-ColorOutput $Blue "Starting in PRODUCTION mode"
}

# Check if compose file exists
if (-not (Test-Path $ComposeFile)) {
    Write-ColorOutput $Red "Error: $ComposeFile not found."
    exit 1
}

# Build command arguments
$Args = @("-f", $ComposeFile, "up")

if ($Build) {
    $Args += "--build"
}

if (-not $Foreground) {
    $Args += "-d"
}

if ($Service) {
    $Args += $Service
}

Write-ColorOutput $Yellow "Running: $ComposeCmd $($Args -join ' ')"

# Execute command
if ($ComposeCmd -eq "podman compose" -or $ComposeCmd -eq "docker compose") {
    $parts = $ComposeCmd.Split(" ")
    & $parts[0] $parts[1] @Args
} else {
    & $ComposeCmd @Args
}

if (-not $Foreground) {
    Write-Host ""
    Write-ColorOutput $Green "Services started successfully!"
    Write-Host ""
    Write-ColorOutput $Blue "Service URLs:"
    Write-Host "  API:        http://localhost:8080"
    Write-Host "  Client:     http://localhost:3000"
    Write-Host "  AI Service: http://localhost:8081"
    Write-Host ""
    Write-ColorOutput $Blue "Useful commands:"
    Write-Host "  View logs:    $ComposeCmd -f $ComposeFile logs -f"
    Write-Host "  Stop:         $ComposeCmd -f $ComposeFile down"
    Write-Host "  Restart API:  $ComposeCmd -f $ComposeFile restart api"

    if ($DevMode) {
        Write-Host ""
        Write-ColorOutput $Green "Development mode active:"
        Write-Host "  - .NET backend changes will auto-reload (dotnet watch)"
        Write-Host "  - Next.js client changes will auto-reload (Turbopack)"
        Write-Host "  - No rebuild needed for code changes"
    }
}
