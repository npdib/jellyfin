#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the plugin, packages it into a zip, calculates the MD5 checksum,
    and patches manifest.json ready for a GitHub release.

.PARAMETER GitHubUser
    Your GitHub username (e.g. "npdib")

.PARAMETER GitHubRepo
    Your GitHub repository name (e.g. "jellyfin-plugin-passwordstrength")

.PARAMETER Version
    Plugin version to package. Defaults to 1.0.0.0

.EXAMPLE
    .\package-plugin.ps1 -GitHubUser npdib -GitHubRepo jellyfin-plugin-passwordstrength
#>
param(
    [Parameter(Mandatory)]
    [string]$GitHubUser,

    [Parameter(Mandatory)]
    [string]$GitHubRepo,

    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
$ProjectFile = "Jellyfin.Plugin.Template.csproj"
$AssemblyName = "Jellyfin.Plugin.Template"
$ZipName = "${AssemblyName}_${Version}.zip"
$DistDir = Join-Path $PSScriptRoot "dist"
$PublishDir = Join-Path $DistDir "publish"
$ZipPath = Join-Path $DistDir $ZipName

# ── 1. Build ─────────────────────────────────────────────────────────────────
Write-Host "`n[1/4] Building Release..." -ForegroundColor Cyan
dotnet publish $ProjectFile -c Release -o $PublishDir --nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

# ── 2. Package ───────────────────────────────────────────────────────────────
Write-Host "[2/4] Packaging zip..." -ForegroundColor Cyan
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }

$DllPath = Join-Path $PublishDir "${AssemblyName}.dll"
if (-not (Test-Path $DllPath)) { throw "DLL not found at $DllPath" }

Compress-Archive -Path $DllPath -DestinationPath $ZipPath
Write-Host "    -> $ZipPath"

# ── 3. Checksum ───────────────────────────────────────────────────────────────
Write-Host "[3/4] Calculating MD5 checksum..." -ForegroundColor Cyan
$Checksum = (Get-FileHash -Algorithm MD5 $ZipPath).Hash.ToUpper()
Write-Host "    -> $Checksum"

# ── 4. Patch manifest.json ────────────────────────────────────────────────────
Write-Host "[4/4] Patching manifest.json..." -ForegroundColor Cyan
$ManifestPath = Join-Path $PSScriptRoot "manifest.json"
$Manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json

$ReleaseUrl = "https://github.com/${GitHubUser}/${GitHubRepo}/releases/download/v${Version}/${ZipName}"

$Manifest[0].versions[0].version   = $Version
$Manifest[0].versions[0].sourceUrl = $ReleaseUrl
$Manifest[0].versions[0].checksum  = $Checksum
$Manifest[0].versions[0].timestamp = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")

$Manifest | ConvertTo-Json -Depth 10 | Set-Content $ManifestPath -Encoding utf8
Write-Host "    -> manifest.json updated"

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Done. Next steps:" -ForegroundColor Green
Write-Host ""
Write-Host "  1. Commit and push:" -ForegroundColor Yellow
Write-Host "       git add manifest.json"
Write-Host "       git commit -m `"Release v${Version}`""
Write-Host "       git push"
Write-Host ""
Write-Host "  2. Create a GitHub release tagged v${Version} and attach:" -ForegroundColor Yellow
Write-Host "       $ZipPath"
Write-Host ""
Write-Host "  3. In Jellyfin (sandbox or live), go to:" -ForegroundColor Yellow
Write-Host "       Dashboard -> Plugins -> Repositories -> + Add"
Write-Host "       URL: https://raw.githubusercontent.com/${GitHubUser}/${GitHubRepo}/main/manifest.json"
Write-Host ""
