param(
    [Parameter(Mandatory = $false)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '0.2.0'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root 'dist'
$publish = Join-Path $dist 'FeizaiTracker-win-x64'
$archive = Join-Path $dist "FeizaiTracker-v$Version-win-x64.zip"
$checksum = "$archive.sha256"

Set-Location $root

dotnet restore FeizaiTracker.sln --nologo
dotnet test FeizaiTracker.sln -c Release --no-restore --nologo

if (Test-Path $publish) {
    Remove-Item -Path $publish -Recurse -Force
}

if (Test-Path $archive) {
    Remove-Item -Path $archive -Force
}

if (Test-Path $checksum) {
    Remove-Item -Path $checksum -Force
}

dotnet publish src\FeizaiTracker.App\FeizaiTracker.App.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    --nologo `
    -o $publish `
    -p:Version=$Version `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

Copy-Item README.md, LICENSE, THIRD_PARTY_NOTICES.md -Destination $publish
Compress-Archive -Path "$publish\*" -DestinationPath $archive -CompressionLevel Optimal

$hash = (Get-FileHash -Path $archive -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -Path $checksum -Value "$hash  $(Split-Path -Leaf $archive)"
Write-Output "Created $archive"
Write-Output "Created $checksum"
Write-Output "SHA256 $hash"
