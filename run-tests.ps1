# run-tests.ps1
# Brings up the Selenium Grid, waits for all nodes to actually register with
# the hub (not just for the containers to start), optionally opens a single
# noVNC dashboard page (all sessions visible side-by-side, no tab-cycling
# needed) so you can watch the sessions live, runs tests, then tears down.
#
# Why this exists: `docker compose up -d` returns as soon as containers are
# created/starting -- not once the Selenium nodes have finished booting and
# registered with the hub. Chrome/Edge nodes in particular can take 10-30+
# seconds to register depending on host load. If dotnet test starts firing
# session requests before all 3 nodes (chrome/firefox/edge) are registered,
# the hub queues those requests, and if registration takes long enough the
# client-side 60s HttpClient timeout gets blown before a node ever frees up --
# which is what was causing the intermittent WebDriverException/timeout failures.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\run-tests.ps1
#   powershell -ExecutionPolicy Bypass -File .\run-tests.ps1 -NoVnc      (skip opening the VNC dashboard)

param(
    [switch]$NoVnc
)

$ErrorActionPreference = "Stop"
$expectedNodeCount = 3          # minimum floor: chrome + firefox + edge each registering at least once
$pollIntervalSeconds = 2

# CI runners are frequently on shared/throttled hardware, so container/node
# startup can take noticeably longer than on a local dev machine. Give CI
# more runway before giving up.
$timeoutSeconds = if ($env:CI) { 180 } else { 90 }

# One noVNC port per browser type, as mapped in docker-compose.yml.
# NOTE: if you're running 2 replicas per node with a fixed host port (e.g.
# "7900:7900"), the second replica of each browser will fail to bind that
# port -- Docker Compose doesn't support two containers sharing one fixed
# host port. Check `docker compose ps` if you expect 2 chrome containers and
# only see 1. These VNC links only ever show you the single, fixed-port
# session per browser type, not any additional replicas.
$vncPorts = [ordered]@{
    chrome  = 7900
    firefox = 7901
    edge    = 7902
}

# Directory.GetCurrentDirectory() inside `dotnet test` resolves to the build
# output folder (bin/Debug/net10.0/...), not wherever you ran this script
# from -- those are two unrelated "current directory" concepts. Set this
# explicitly so ExtentReportManager (C# side) and this script agree on the
# exact same path instead of each guessing independently.
$outputRoot = (Get-Location).Path
$env:TEST_OUTPUT_ROOT = $outputRoot

Write-Host "Starting Selenium Grid..."
docker compose up -d
if ($LASTEXITCODE -ne 0) {
    Write-Host "docker compose up -d failed (exit code $LASTEXITCODE). Not proceeding to tests."
    exit $LASTEXITCODE
}

Write-Host "Waiting for $expectedNodeCount grid node(s) to register with the hub..."
$deadline = (Get-Date).AddSeconds($timeoutSeconds)
$ready = $false

while ((Get-Date) -lt $deadline) {
    try {
        $status = Invoke-RestMethod -Uri "http://localhost:4444/wd/hub/status" -TimeoutSec 5
        $nodeCount = $status.value.nodes.Count
        $allReady = $status.value.ready

        Write-Host "  hub ready=$allReady, nodes registered=$nodeCount/$expectedNodeCount"

        if ($allReady -and $nodeCount -ge $expectedNodeCount) {
            $ready = $true
            break
        }
    }
    catch {
        Write-Host "  hub not responding yet, retrying..."
    }

    Start-Sleep -Seconds $pollIntervalSeconds
}

if (-not $ready) {
    Write-Host "Grid did not become ready within $timeoutSeconds seconds. Tearing down and exiting."
    docker compose down
    exit 1
}

if (-not $NoVnc -and -not $env:CI) {
    Write-Host "Building noVNC dashboard..."

    $panes = ($vncPorts.GetEnumerator() | ForEach-Object {
        $name = $_.Key
        $port = $_.Value
        # SE_VNC_NO_PASSWORD=1 means autoconnect=1 skips the password prompt entirely.
        @"
        <div class="pane">
            <div class="label">$name</div>
            <iframe src="http://localhost:$port/?autoconnect=1&resize=scale"></iframe>
        </div>
"@
    }) -join "`n"

    $html = @"
<!DOCTYPE html>
<html>
<head>
<title>Selenium Grid - Live Sessions</title>
<style>
    html, body { margin: 0; height: 100%; background: #111; font-family: sans-serif; }
    .grid { display: grid; grid-template-columns: repeat($($vncPorts.Count), 1fr); height: 100vh; }
    .pane { display: flex; flex-direction: column; border-right: 1px solid #333; }
    .pane:last-child { border-right: none; }
    .label { color: #eee; text-align: center; padding: 6px; background: #222; text-transform: capitalize; font-size: 14px; }
    iframe { flex: 1; border: none; width: 100%; }
</style>
</head>
<body>
<div class="grid">
$panes
</div>
</body>
</html>
"@

    $dashboardPath = Join-Path $env:TEMP "selenium-vnc-dashboard.html"
    Set-Content -Path $dashboardPath -Value $html -Encoding UTF8
    Start-Process $dashboardPath
}
elseif ($env:CI) {
    Write-Host "CI environment detected (`$env:CI is set) -- skipping noVNC dashboard."
}

Write-Host "Grid is ready. Running tests..."
dotnet test
$testExitCode = $LASTEXITCODE

$reportPath = Join-Path $outputRoot "TestResults\ExtentReport\index.html"
if (-not $env:CI -and (Test-Path $reportPath)) {
    Write-Host "Opening Extent report..."
    Start-Process $reportPath
}
elseif ($env:CI) {
    Write-Host "CI environment detected (`$env:CI is set) -- skipping report auto-open. See job artifacts instead."
}

$allureResultsPath = Join-Path $outputRoot "TestResults\AllureReport"
if (Test-Path $allureResultsPath) {
    Write-Host "Allure raw results written to: $allureResultsPath"
    Write-Host "View them locally with the Allure commandline tool (not auto-launched -- it's a separate install):"
    Write-Host "  allure serve `"$allureResultsPath`""
}


Write-Host "Tearing down Selenium Grid..."
docker compose down

exit $testExitCode
