#Requires -Version 5.1
<#
.SYNOPSIS
    Stops the Jellyfin sandbox container.

.PARAMETER Purge
    Also deletes all Jellyfin config data (sandbox/data).
    Use this to start completely fresh on next run.
#>
param(
    [switch]$Purge
)

$ErrorActionPreference = 'Stop'
$ComposeFile = Join-Path $PSScriptRoot "sandbox\docker-compose.yml"

Write-Host "Stopping Jellyfin sandbox..." -ForegroundColor Cyan
docker compose -f $ComposeFile down

if ($Purge) {
    $DataDir = Join-Path $PSScriptRoot "sandbox\data"
    if (Test-Path $DataDir) {
        Remove-Item -Recurse -Force $DataDir
        Write-Host "Config data purged. Next start will run the setup wizard again." -ForegroundColor Yellow
    }
}

Write-Host "Stopped." -ForegroundColor Green
