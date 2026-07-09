# run-tests.ps1
# Brings up the Selenium Grid, waits for all nodes to actually register with
# the hub (not just for the containers to start), runs tests, then tears down.
#
# Why this exists: `docker compose up -d` returns as soon as containers are
# created/starting -- not once the Selenium nodes have finished booting and
# registered with the hub. Chrome/Edge nodes in particular can take 10-30+
# seconds to register depending on host load. If dotnet test starts firing
# session requests before all 3 nodes (chrome/firefox/edge) are registered,
# the hub queues those requests, and if registration takes long enough the
# client-side 60s HttpClient timeout gets blown before a node ever frees up --
# which is what was causing the intermittent WebDriverException/timeout failures.

$ErrorActionPreference = "Stop"
$expectedNodeCount = 3          # chrome + firefox + edge
$timeoutSeconds = 90
$pollIntervalSeconds = 2

Write-Host "Starting Selenium Grid..."
docker compose up -d

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

Write-Host "Grid is ready. Running tests..."
dotnet test
$testExitCode = $LASTEXITCODE

Write-Host "Tearing down Selenium Grid..."
docker compose down

exit $testExitCode
