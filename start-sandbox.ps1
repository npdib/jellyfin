#Requires -Version 5.1
<#
.SYNOPSIS
    Starts the Jellyfin sandbox container for local plugin testing.
    Config persists in sandbox/data between restarts.
#>

$ErrorActionPreference = 'Stop'
$ComposeFile = Join-Path $PSScriptRoot "sandbox\docker-compose.yml"

Write-Host "Starting Jellyfin sandbox..." -ForegroundColor Cyan
docker compose -f $ComposeFile up -d

if ($LASTEXITCODE -ne 0) { throw "docker compose failed." }

Write-Host ""
Write-Host "Sandbox running at http://localhost:8096" -ForegroundColor Green
Write-Host ""
Write-Host "First run: complete the setup wizard, then go to" -ForegroundColor Yellow
Write-Host "  Dashboard -> Plugins -> Repositories -> + Add"
Write-Host "  and paste your manifest URL."
Write-Host ""
Write-Host "Run .\stop-sandbox.ps1 to stop." -ForegroundColor Gray
