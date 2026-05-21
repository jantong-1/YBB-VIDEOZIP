param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$src = Join-Path $root "src\VideoCompressorUI.cs"
$dist = Join-Path $root "dist"
$exe = Join-Path $dist "YBBvideozip.exe"
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (!(Test-Path -LiteralPath $src)) {
    throw "Missing source file: $src"
}

if (!(Test-Path -LiteralPath $csc)) {
    $csc = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (!(Test-Path -LiteralPath $csc)) {
    throw "Cannot find .NET Framework csc.exe."
}

New-Item -ItemType Directory -Force -Path $dist | Out-Null

& $csc `
    /nologo `
    /target:winexe `
    /optimize+ `
    /platform:anycpu `
    /out:$exe `
    /reference:System.Windows.Forms.dll `
    /reference:System.Drawing.dll `
    /reference:System.IO.Compression.dll `
    /reference:System.IO.Compression.FileSystem.dll `
    $src

if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

Write-Host "Built: $exe"

