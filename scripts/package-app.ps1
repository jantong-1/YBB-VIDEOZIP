param(
    [string]$Output = "YBBvideozip-green.zip"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$dist = Join-Path $root "dist"
$exe = Join-Path $dist "YBBvideozip.exe"
$outputPath = Join-Path $dist $Output
$stageRoot = Join-Path $dist "release-stage"
$stageAppDir = Join-Path $stageRoot "YBBvideozip"
$license = Join-Path $root "LICENSE"
$thirdPartyNotices = Join-Path $root "THIRD_PARTY_NOTICES.md"

if (!(Test-Path -LiteralPath $exe)) {
    throw "Missing built exe: $exe. Run scripts\build.ps1 first."
}

if (!(Test-Path -LiteralPath $license)) {
    throw "Missing license file: $license"
}

if (!(Test-Path -LiteralPath $thirdPartyNotices)) {
    throw "Missing third party notices file: $thirdPartyNotices"
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (Test-Path -LiteralPath $outputPath) {
    Remove-Item -LiteralPath $outputPath -Force
}

if (Test-Path -LiteralPath $stageRoot) {
    Remove-Item -LiteralPath $stageRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $stageAppDir | Out-Null
Copy-Item -LiteralPath $exe -Destination (Join-Path $stageAppDir "YBBvideozip.exe") -Force
Copy-Item -LiteralPath $license -Destination (Join-Path $stageAppDir "LICENSE") -Force
Copy-Item -LiteralPath $thirdPartyNotices -Destination (Join-Path $stageAppDir "THIRD_PARTY_NOTICES.md") -Force

[System.IO.Compression.ZipFile]::CreateFromDirectory(
    (Resolve-Path -LiteralPath $stageRoot),
    $outputPath,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $false
)

Remove-Item -LiteralPath $stageRoot -Recurse -Force

Write-Host "Created: $outputPath"

