<#
.SYNOPSIS
    Boots (if needed) a local AZOA node, obtains an nft:mint-scoped API key, and
    runs the gated ArdaNova <-> AZOA cross-process golden-path e2e.

.DESCRIPTION
    This is the developer entry point for the LIVE e2e that lives in
    tests/ArdaNova.Application.Tests/E2E/AzoaGoldenPathE2ETests.cs. That test is
    GATED -- it skips cleanly during a normal `dotnet test` run and only executes
    when AZOA_E2E=1, the node is reachable at http://localhost:5000, and
    AZOA_E2E_API_KEY is set. This script arranges all three, then invokes
    `dotnet test --filter Category=E2E`, then tears down anything it started.

    It does NOT modify the AZOA (oasis-sleek) repo or ArdaNova production code.
    It only: starts a `dotnet run` process (optional), calls AZOA's public HTTP
    API to mint a dev key, sets env vars for the child test process, and cleans up.

    Runs under Windows PowerShell 5.1 and PowerShell 7+. ASCII-only on purpose so
    the 5.1 parser (which reads unmarked files as the ANSI codepage) does not choke.

.PREREQUISITES
    - .NET SDK on PATH (`dotnet`).
    - SurrealDB running at http://127.0.0.1:8000 (ns/db/user/pass = azoa/azoa/root/root
      per oasis-sleek appsettings.Development.json). AZOA persists to SurrealDB;
      register/allocation will fail without it. Start it however you normally do,
      e.g.:  surreal start --user root --pass root --bind 127.0.0.1:8000 memory
      (or file/rocksdb). This script does NOT install or start SurrealDB.
    - oasis-sleek checkout at the path in -AzoaProjectPath below. Its dev config
      already has Blockchain:Mode=Simulated.

.PARAMETER AzoaProjectPath
    Path to AZOA.WebAPI.csproj. Defaults to the known local checkout.

.PARAMETER NoStartAzoa
    Do not attempt to start AZOA; assume it is already running at localhost:5000.

.PARAMETER ApiKey
    Use this pre-existing nft:mint-scoped X-Api-Key instead of minting one. If
    omitted and AZOA_E2E_API_KEY is already set in the environment, that is used.
    If both are absent, the script mints a fresh dev key via the AZOA HTTP API
    (register avatar -> login -> POST /api/apikey with scopes "nft:mint").

.PARAMETER SolutionPath
    Path to ardanova.sln. Defaults to the known local checkout.

.EXAMPLE
    pwsh ./run-azoa-e2e.ps1
    # Starts AZOA if needed, mints a dev key, runs the e2e, tears AZOA down.

.EXAMPLE
    $env:AZOA_E2E_API_KEY = "azoa_deadbeef..."; pwsh ./run-azoa-e2e.ps1 -NoStartAzoa
    # Reuse a running node + an existing key.
#>
[CmdletBinding()]
param(
    [string]$AzoaProjectPath = "C:\Users\atooz\Programming\Projects\oasis-sleek\AZOA.WebAPI.csproj",
    [string]$SolutionPath    = "C:\Users\atooz\Documents\Escherbridge\ardanova\ardanova-backend-api-mcp\ardanova.sln",
    [switch]$NoStartAzoa,
    [string]$ApiKey
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$BaseUrl = "http://localhost:5000"
$startedAzoa = $false
$azoaProc = $null

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }
function Write-Warn2($msg) { Write-Host "!!  $msg" -ForegroundColor Yellow }

function Test-NodeReachable {
    # Any HTTP answer (even 404/401) means the process is up. -SkipHttpErrorCheck
    # exists only on PS7+, so fall back to catching the WebException on 5.1.
    try {
        Invoke-WebRequest -Uri "$BaseUrl/" -Method Head -TimeoutSec 3 -UseBasicParsing | Out-Null
        return $true
    } catch [System.Net.WebException] {
        # A protocol error (non-2xx) still proves the socket answered.
        if ($null -ne $_.Exception.Response) { return $true }
        return $false
    } catch {
        return $false
    }
}

function Wait-ForNode([int]$timeoutSec = 60) {
    Write-Step "Waiting for AZOA at $BaseUrl (timeout ${timeoutSec}s)..."
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    while ((Get-Date) -lt $deadline) {
        if (Test-NodeReachable) {
            Write-Step "AZOA is up."
            return $true
        }
        Start-Sleep -Seconds 2
    }
    return $false
}

function Get-Result($response) {
    # AZOA envelope shape: isError / message / result.
    if ($null -eq $response) { throw "Empty AZOA response." }
    if ($response.isError) { throw "AZOA error: $($response.message)" }
    return $response.result
}

function New-DevApiKey {
    # Mints an nft:mint-scoped API key using ONLY AZOA's public HTTP API:
    #   1. POST /api/avatar/register  (anonymous)                     -> avatar
    #   2. POST /api/avatar/login     (anonymous)                     -> JWT (in .result)
    #   3. POST /api/apikey           (Bearer JWT, scopes=nft:mint)   -> raw key (once)
    # Returns the raw key string (prefix "azoa_...").
    $suffix = [guid]::NewGuid().ToString("N").Substring(0, 10)
    $email = "e2e_runner_$suffix@ardanova-e2e.test"
    $password = "E2e!$($suffix)Pw9"

    Write-Step "Minting a dev nft:mint API key via AZOA HTTP API (avatar=$email)..."

    $registerBody = @{
        username  = "e2e_runner_$suffix"
        email     = $email
        password  = $password
        firstName = "Ardanova"
        lastName  = "E2ERunner"
    } | ConvertTo-Json

    $regResp = Invoke-RestMethod -Uri "$BaseUrl/api/avatar/register" -Method Post -ContentType "application/json" -Body $registerBody
    $avatar = Get-Result $regResp
    Write-Step "  registered avatar id=$($avatar.id)"

    $loginBody = @{ email = $email; password = $password } | ConvertTo-Json
    $loginResp = Invoke-RestMethod -Uri "$BaseUrl/api/avatar/login" -Method Post -ContentType "application/json" -Body $loginBody
    $jwt = Get-Result $loginResp
    if ([string]::IsNullOrWhiteSpace($jwt)) { throw "Login returned no JWT." }
    Write-Step "  obtained JWT (login ok)"

    $keyBody = @{
        name   = "ardanova-e2e $suffix"
        scopes = "nft:mint"
    } | ConvertTo-Json

    $keyResp = Invoke-RestMethod -Uri "$BaseUrl/api/apikey" -Method Post -ContentType "application/json" -Headers @{ Authorization = "Bearer $jwt" } -Body $keyBody
    $key = Get-Result $keyResp
    if ([string]::IsNullOrWhiteSpace($key.key)) { throw "API key creation returned no raw key." }
    Write-Step "  minted key (prefix=$($key.keyPrefix), scopes=nft:mint)"
    return $key.key
}

try {
    # 1. Ensure the node is up (start it only if we must).
    if (Test-NodeReachable) {
        Write-Step "AZOA already running at $BaseUrl -- not starting a second instance."
    }
    elseif ($NoStartAzoa) {
        throw "AZOA is not reachable at $BaseUrl and -NoStartAzoa was set."
    }
    else {
        if (-not (Test-Path $AzoaProjectPath)) {
            throw "AZOA project not found at '$AzoaProjectPath'. Pass -AzoaProjectPath."
        }
        Write-Warn2 "Reminder: AZOA needs SurrealDB at http://127.0.0.1:8000. If register/login"
        Write-Warn2 "fails, start SurrealDB first (see the .PREREQUISITES block in this script)."
        Write-Step "Starting AZOA: dotnet run --project $AzoaProjectPath (Development)..."

        # Set ASPNETCORE_ENVIRONMENT for the child via the current process env
        # (inherited by Start-Process) so this works on PS 5.1 too (-Environment is PS7+).
        $prevEnv = $env:ASPNETCORE_ENVIRONMENT
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        try {
            $azoaProc = Start-Process -FilePath "dotnet" `
                -ArgumentList @("run", "--project", "`"$AzoaProjectPath`"", "--launch-profile", "http") `
                -PassThru -WindowStyle Minimized
        }
        finally {
            $env:ASPNETCORE_ENVIRONMENT = $prevEnv
        }
        $startedAzoa = $true

        if (-not (Wait-ForNode -timeoutSec 90)) {
            throw "AZOA did not become reachable within timeout. Check SurrealDB + the AZOA console window."
        }
    }

    # 2. Obtain the nft:mint API key.
    $effectiveKey = $ApiKey
    if ([string]::IsNullOrWhiteSpace($effectiveKey)) {
        $effectiveKey = $env:AZOA_E2E_API_KEY
    }
    if ([string]::IsNullOrWhiteSpace($effectiveKey)) {
        $effectiveKey = New-DevApiKey
    }
    else {
        Write-Step "Using pre-supplied API key (from -ApiKey or AZOA_E2E_API_KEY)."
    }

    # 3. Set the gate env vars for the child `dotnet test` process.
    $env:AZOA_E2E = "1"
    $env:AZOA_E2E_API_KEY = $effectiveKey

    # 4. Run ONLY the E2E category against the solution.
    Write-Step "Running: dotnet test `"$SolutionPath`" --filter Category=E2E"
    dotnet test "$SolutionPath" --filter "Category=E2E" --logger "console;verbosity=detailed"
    $testExit = $LASTEXITCODE
    Write-Step "dotnet test exit code: $testExit"
    exit $testExit
}
finally {
    # 5. Tear down ONLY what we started.
    if ($startedAzoa -and $null -ne $azoaProc) {
        Write-Step "Stopping the AZOA process we started (PID $($azoaProc.Id))..."
        try {
            # Kill the process tree -- `dotnet run` spawns a child that hosts Kestrel.
            Stop-Process -Id $azoaProc.Id -Force -ErrorAction SilentlyContinue
            Get-CimInstance Win32_Process -Filter "ParentProcessId=$($azoaProc.Id)" -ErrorAction SilentlyContinue |
                ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
        } catch {
            Write-Warn2 "Could not fully stop AZOA (PID $($azoaProc.Id)): $($_.Exception.Message)"
        }
    }
    # Do not clear AZOA_E2E_API_KEY if the caller pre-set it; these are process-local anyway.
}
