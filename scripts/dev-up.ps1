# ===========================================
# ArdaNova Development Environment Utility
# ===========================================
# Comprehensive local dev setup for ArdaNova
# Supports: local mode (default) and Docker mode
# Auto-checks prerequisites, kills stale ports, validates .env

param(
    [ValidateSet("local", "docker")]
    [string]$Mode = "local",
    [switch]$ApiOnly,
    [switch]$ClientOnly,
    [switch]$Install,
    [switch]$Check,
    [switch]$Kill,
    [switch]$Help
)

# --- Colors ---
$Red = "Red"
$Green = "Green"
$Yellow = "Yellow"
$Blue = "Cyan"
$White = "White"

# --- Paths ---
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$ApiProjectDir = Join-Path $ProjectRoot "ardanova-backend-api-mcp\api-server\src\ArdaNova.API"
$ClientDir = Join-Path $ProjectRoot "ardanova-client"
$EnvFile = Join-Path $ProjectRoot ".env"

# --- Ports ---
$ApiHttpPort = 5147
$ApiHttpsPort = 7160
$ClientPort = 3000
$DockerApiPort = 8080

# ===========================================
# Help
# ===========================================
if ($Help) {
    Write-Host ""
    Write-Host "ArdaNova Dev Environment Utility" -ForegroundColor $Blue
    Write-Host "=================================" -ForegroundColor $Blue
    Write-Host ""
    Write-Host "Usage: .\dev-up.ps1 [options]" -ForegroundColor $White
    Write-Host ""
    Write-Host "Modes:" -ForegroundColor $Green
    Write-Host "  -Mode local     Run .NET API + Next.js locally (default)"
    Write-Host "  -Mode docker    Run via Docker Compose containers"
    Write-Host ""
    Write-Host "Service Flags:" -ForegroundColor $Green
    Write-Host "  -ApiOnly        Start only the .NET backend API"
    Write-Host "  -ClientOnly     Start only the Next.js client"
    Write-Host ""
    Write-Host "Utility Flags:" -ForegroundColor $Green
    Write-Host "  -Install        Fresh lockfile install (npm ci, dotnet restore)"
    Write-Host "  -Check          Check prerequisites only, don't start anything"
    Write-Host "  -Kill           Kill all ArdaNova dev processes and free ports"
    Write-Host "  -Help           Show this help"
    Write-Host ""
    Write-Host "Local Mode URLs:" -ForegroundColor $Yellow
    Write-Host "  API (HTTPS):  https://localhost:$ApiHttpsPort"
    Write-Host "  API (HTTP):   http://localhost:$ApiHttpPort"
    Write-Host "  Swagger:      https://localhost:$ApiHttpsPort/swagger"
    Write-Host "  Client:       http://localhost:$ClientPort"
    Write-Host ""
    Write-Host "Docker Mode URLs:" -ForegroundColor $Yellow
    Write-Host "  API:          http://localhost:$DockerApiPort"
    Write-Host "  Swagger:      http://localhost:$DockerApiPort/swagger"
    Write-Host "  Client:       http://localhost:$ClientPort"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor $Green
    Write-Host "  .\dev-up.ps1                    # Start everything locally"
    Write-Host "  .\dev-up.ps1 -ApiOnly           # Start only the API"
    Write-Host "  .\dev-up.ps1 -Install           # Install deps then start"
    Write-Host "  .\dev-up.ps1 -Mode docker       # Start via Docker Compose"
    Write-Host "  .\dev-up.ps1 -Kill              # Kill all dev processes"
    Write-Host "  .\dev-up.ps1 -Check             # Verify prerequisites"
    Write-Host ""
    exit 0
}

# ===========================================
# Utility Functions
# ===========================================

function Write-Banner {
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor $Blue
    Write-Host "  ArdaNova Development Environment" -ForegroundColor $Blue
    Write-Host "==========================================" -ForegroundColor $Blue
    Write-Host ""
}

function Write-Step($Message) {
    Write-Host "[*] $Message" -ForegroundColor $Green
}

function Write-Warn($Message) {
    Write-Host "[!] $Message" -ForegroundColor $Yellow
}

function Write-Err($Message) {
    Write-Host "[X] $Message" -ForegroundColor $Red
}

function Write-Info($Message) {
    Write-Host "    $Message" -ForegroundColor $White
}

function Test-Command($Name) {
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Import-DotEnv($Path) {
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -notmatch '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
            continue
        }

        $name = $Matches[1]
        # Security overrides must never be inherited from a shared dotenv file.
        if ($name -in @("ALLOW_REMOTE_DEV_DATABASE", "NODE_TLS_REJECT_UNAUTHORIZED")) {
            continue
        }

        $value = $Matches[2].Trim()
        if (($value.StartsWith('"') -and $value.EndsWith('"')) -or
            ($value.StartsWith("'") -and $value.EndsWith("'"))) {
            $value = $value.Substring(1, $value.Length - 2)
        }

        # Explicit process variables take precedence over the shared local file.
        if ([string]::IsNullOrEmpty([Environment]::GetEnvironmentVariable($name, "Process"))) {
            [Environment]::SetEnvironmentVariable($name, $value, "Process")
        }
    }
}

function Assert-SecureTlsConfiguration {
    if ([Environment]::GetEnvironmentVariable("NODE_TLS_REJECT_UNAUTHORIZED", "Process") -eq "0") {
        Write-Err "Refusing to start with NODE_TLS_REJECT_UNAUTHORIZED=0."
        Write-Info "Use a trusted local certificate or the HTTP loopback API origin instead."
        exit 1
    }

    # Pin the secure value so Next.js dotenv loading cannot override it.
    [Environment]::SetEnvironmentVariable("NODE_TLS_REJECT_UNAUTHORIZED", "1", "Process")
}

function Assert-LocalDatabaseTarget {
    $databaseUrl = [Environment]::GetEnvironmentVariable("DATABASE_URL", "Process")
    if ([string]::IsNullOrWhiteSpace($databaseUrl)) {
        return
    }

    try {
        $databaseHost = ([Uri]$databaseUrl).Host
    } catch {
        Write-Err "DATABASE_URL is not a valid absolute PostgreSQL URL."
        exit 1
    }

    $loopbackHosts = @("localhost", "127.0.0.1", "::1", "[::1]")
    if ($loopbackHosts -contains $databaseHost) {
        return
    }

    if ([Environment]::GetEnvironmentVariable("ALLOW_REMOTE_DEV_DATABASE", "Process") -eq "true") {
        Write-Warn "Remote development database explicitly allowed for host '$databaseHost'."
        return
    }

    Write-Err "Refusing to start local services against remote database host '$databaseHost'."
    Write-Info "Set DATABASE_URL to a dedicated loopback database."
    Write-Info "For an intentional remote session only, set ALLOW_REMOTE_DEV_DATABASE=true."
    exit 1
}

function Stop-PortProcesses($Port) {
    $conns = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if ($conns) {
        foreach ($c in $conns) {
            $proc = Get-Process -Id $c.OwningProcess -ErrorAction SilentlyContinue
            if ($proc) {
                Write-Warn "Killing $($proc.ProcessName) (PID $($proc.Id)) on port $Port"
                Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            }
        }
        Start-Sleep -Seconds 1
    }
}

# ===========================================
# Kill Mode
# ===========================================
if ($Kill) {
    Write-Banner
    Write-Step "Killing all ArdaNova dev processes..."
    Stop-PortProcesses $ApiHttpPort
    Stop-PortProcesses $ApiHttpsPort
    Stop-PortProcesses $ClientPort
    Stop-PortProcesses $DockerApiPort

    # Kill any remaining dotnet watch processes for this project
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
        $_.CommandLine -like "*ArdaNova*" -or $_.MainWindowTitle -like "*ArdaNova*"
    } | ForEach-Object {
        Write-Warn "Killing dotnet PID $($_.Id)"
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }

    Write-Step "All dev processes stopped."
    exit 0
}

# ===========================================
# Prerequisite Checks
# ===========================================
function Test-Prerequisites {
    Write-Step "Checking prerequisites..."
    $allGood = $true

    # .NET SDK
    if (Test-Command "dotnet") {
        $dotnetVersion = (dotnet --version 2>$null)
        Write-Info "dotnet SDK: $dotnetVersion"
    } else {
        Write-Err "dotnet SDK not found. Install from https://dotnet.microsoft.com/download"
        $allGood = $false
    }

    # Node.js
    if (Test-Command "node") {
        $nodeVersion = (node --version 2>$null)
        Write-Info "Node.js: $nodeVersion"
    } else {
        Write-Err "Node.js not found. Install from https://nodejs.org"
        $allGood = $false
    }

    # npm
    if (Test-Command "npm") {
        $npmVersion = (npm --version 2>$null)
        Write-Info "npm: $npmVersion"
    } else {
        Write-Err "npm not found."
        $allGood = $false
    }

    # Project directories
    if (Test-Path $ApiProjectDir) {
        Write-Info "API project: Found"
    } else {
        Write-Err "API project not found at: $ApiProjectDir"
        $allGood = $false
    }

    if (Test-Path $ClientDir) {
        Write-Info "Client project: Found"
    } else {
        Write-Err "Client project not found at: $ClientDir"
        $allGood = $false
    }

    # .env file
    if (Test-Path $EnvFile) {
        Write-Info ".env file: Found"

        # Check critical env vars
        $envContent = Get-Content $EnvFile -Raw
        $requiredVars = @(
            "DATABASE_URL",
            "AUTH_SECRET",
            "GOOGLE_CLIENT_ID",
            "GOOGLE_CLIENT_SECRET",
            "API_KEY",
            "ADMIN_API_KEY",
            "ACTOR_ASSERTION_HMAC_KEY"
        )
        foreach ($var in $requiredVars) {
            $processValue = [Environment]::GetEnvironmentVariable($var, "Process")
            if ([string]::IsNullOrWhiteSpace($processValue) -and $envContent -notmatch "$var=.+") {
                Write-Warn "Missing or empty: $var in .env"
            }
        }
    } else {
        Write-Err ".env file not found at: $EnvFile"
        Write-Info "Copy .env.example to .env and fill in values"
        $allGood = $false
    }

    # node_modules
    $nodeModules = Join-Path $ClientDir "node_modules"
    if (Test-Path $nodeModules) {
        Write-Info "node_modules: Installed"
    } else {
        Write-Warn "node_modules not found. Run with -Install or 'npm ci' in ardanova-client/"
    }

    # Docker (optional)
    if ($Mode -eq "docker") {
        if (Test-Command "docker") {
            $dockerVersion = (docker --version 2>$null)
            Write-Info "Docker: $dockerVersion"
        } elseif (Test-Command "podman") {
            $podmanVersion = (podman --version 2>$null)
            Write-Info "Podman: $podmanVersion"
        } else {
            Write-Err "Docker/Podman required for docker mode."
            $allGood = $false
        }
    }

    Write-Host ""
    return $allGood
}

Write-Banner

$prereqsPassed = Test-Prerequisites

if ($Check) {
    if ($prereqsPassed) {
        Write-Step "All prerequisites met!"
    } else {
        Write-Err "Some prerequisites missing. Fix the issues above."
    }
    exit ([int](-not $prereqsPassed))
}

if (-not $prereqsPassed) {
    Write-Err "Prerequisites check failed. Use -Check for details or fix issues above."
    exit 1
}

if ($Mode -eq "local") {
    Write-Step "Loading shared local environment..."
    Import-DotEnv $EnvFile
    Write-Info "Loaded repository .env without overriding process variables"
    Assert-SecureTlsConfiguration
    Assert-LocalDatabaseTarget
    Write-Host ""
}

# ===========================================
# Install Dependencies
# ===========================================
if ($Install) {
    if (-not $ClientOnly) {
        Write-Step "Restoring .NET packages..."
        $slnDir = Join-Path $ProjectRoot "ardanova-backend-api-mcp\api-server"
        & dotnet.exe restore "$slnDir" 2>&1 | Out-Null
        Write-Info "dotnet restore complete"
    }

    if (-not $ApiOnly) {
        Write-Step "Installing npm packages from package-lock.json..."
        Push-Location $ClientDir
        & cmd.exe /c "npm ci" 2>&1 | Out-Null
        $npmExitCode = $LASTEXITCODE
        if ($npmExitCode -ne 0) {
            Pop-Location
            Write-Err "npm ci failed"
            exit $npmExitCode
        }
        Write-Info "npm ci complete (including npm run generate:prisma postinstall)"
        Pop-Location
    }
    Write-Host ""
}

# ===========================================
# Docker Mode
# ===========================================
if ($Mode -eq "docker") {
    Write-Step "Starting in DOCKER mode..."

    # Detect compose command
    $ComposeCmd = $null
    if (Test-Command "docker") {
        try {
            & docker compose version 2>$null | Out-Null
            if ($LASTEXITCODE -eq 0) { $ComposeCmd = "docker compose" }
        } catch {}
        if (-not $ComposeCmd -and (Test-Command "docker-compose")) {
            $ComposeCmd = "docker-compose"
        }
    }
    if (-not $ComposeCmd -and (Test-Command "podman")) {
        if (Test-Command "podman-compose") { $ComposeCmd = "podman-compose" }
    }

    if (-not $ComposeCmd) {
        Write-Err "No compose command found."
        exit 1
    }

    Write-Info "Using: $ComposeCmd"

    $ComposeFile = Join-Path $ProjectRoot "docker-compose.dev.yml"
    if (-not (Test-Path $ComposeFile)) {
        Write-Err "docker-compose.dev.yml not found"
        exit 1
    }

    $Services = @()
    if ($ApiOnly) { $Services = @("api") }
    elseif ($ClientOnly) { $Services = @("client") }
    else { $Services = @("api", "client") }

    $ComposeArgs = @("-f", $ComposeFile, "up", "--build", "--force-recreate", "--remove-orphans")
    $ComposeArgs += $Services

    Write-Step "Running: $ComposeCmd $($ComposeArgs -join ' ')"
    Write-Host ""

    if ($ComposeCmd -match " ") {
        $parts = $ComposeCmd.Split(" ")
        & $parts[0] $parts[1] @ComposeArgs
    } else {
        & $ComposeCmd @ComposeArgs
    }
    exit $LASTEXITCODE
}

# ===========================================
# Local Mode
# ===========================================
Write-Step "Starting in LOCAL mode..."
Write-Host ""

# Kill stale processes on our ports
Write-Step "Freeing ports..."
if (-not $ClientOnly) {
    Stop-PortProcesses $ApiHttpPort
    Stop-PortProcesses $ApiHttpsPort
}
if (-not $ApiOnly) {
    Stop-PortProcesses $ClientPort
}
Write-Host ""

# Track child processes
$jobs = @()
$apiJob = $null
$clientJob = $null
$serviceExitCode = 0

try {
    # Start .NET API
    if (-not $ClientOnly) {
        Write-Step "Starting .NET API (dotnet watch run)..."
        $apiJob = Start-Process -FilePath "dotnet.exe" `
            -ArgumentList "watch", "run", "--project", $ApiProjectDir `
            -PassThru -NoNewWindow
        $jobs += $apiJob
        Write-Info "PID: $($apiJob.Id)"
        Write-Info "HTTPS: https://localhost:$ApiHttpsPort"
        Write-Info "HTTP:  http://localhost:$ApiHttpPort"
        Write-Info "Swagger: https://localhost:$ApiHttpsPort/swagger"
        Write-Host ""

        Start-Sleep -Seconds 3
    }

    # Start Next.js Client
    if (-not $ApiOnly) {
        Write-Step "Starting Next.js client (npm run dev)..."
        $clientJob = Start-Process -FilePath "cmd.exe" `
            -ArgumentList "/c", "npm run dev" `
            -WorkingDirectory $ClientDir `
            -PassThru -NoNewWindow
        $jobs += $clientJob
        Write-Info "PID: $($clientJob.Id)"
        Write-Info "URL: http://localhost:$ClientPort"
        Write-Host ""
    }

    Write-Host "==========================================" -ForegroundColor $Green
    Write-Host "  All services started!" -ForegroundColor $Green
    Write-Host "  Press Ctrl+C to stop everything" -ForegroundColor $Green
    Write-Host "==========================================" -ForegroundColor $Green
    Write-Host ""

    # Wait for processes
    while ($true) {
        $exited = $jobs | Where-Object { $_.HasExited }
        if ($exited) {
            $exitedProcess = @($exited)[0]
            $exitedName = if ($apiJob -and $exitedProcess.Id -eq $apiJob.Id) { "API" } else { "Client" }
            $serviceExitCode = $exitedProcess.ExitCode
            if ($serviceExitCode -eq 0) { $serviceExitCode = 1 }
            Write-Warn "$exitedName process exited with code $serviceExitCode. Shutting down..."
            break
        }
        Start-Sleep -Seconds 2
    }
}
finally {
    Write-Host ""
    Write-Step "Stopping all services..."
    foreach ($job in $jobs) {
        if ($job -and -not $job.HasExited) {
            Stop-Process -Id $job.Id -Force -ErrorAction SilentlyContinue
            Write-Info "Stopped PID $($job.Id)"
        }
    }
    # Also kill any child dotnet processes that may have spawned
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
        try { $_.MainModule.FileName -like "*ArdaNova*" } catch { $false }
    } | Stop-Process -Force -ErrorAction SilentlyContinue

    Write-Step "All services stopped."
}

if ($serviceExitCode -ne 0) {
    exit $serviceExitCode
}
