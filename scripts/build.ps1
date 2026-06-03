param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$srcDir = Join-Path $root "src"
$sources = Get-ChildItem -LiteralPath $srcDir -Filter "*.cs" | Sort-Object FullName
$dist = Join-Path $root "dist"
$exe = Join-Path $dist "YBBvideozip.exe"
$icon = Join-Path $root "assets\YBBvideozip.ico"
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if ($sources.Count -eq 0) {
    throw "No C# source files found in: $srcDir"
}

if (!(Test-Path -LiteralPath $csc)) {
    $csc = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (!(Test-Path -LiteralPath $csc)) {
    throw "Cannot find .NET Framework csc.exe."
}

if (!(Test-Path -LiteralPath $icon)) {
    & (Join-Path $root "scripts\generate-icon.ps1")
}

if (!(Test-Path -LiteralPath $icon)) {
    throw "Missing icon file: $icon"
}

New-Item -ItemType Directory -Force -Path $dist | Out-Null

& $csc `
    /nologo `
    /target:winexe `
    /optimize+ `
    /codepage:65001 `
    /platform:anycpu `
    "/win32icon:$icon" `
    /out:$exe `
    /reference:System.Windows.Forms.dll `
    /reference:System.Drawing.dll `
    /reference:System.Core.dll `
    /reference:System.IO.Compression.dll `
    /reference:System.IO.Compression.FileSystem.dll `
    /reference:Microsoft.CSharp.dll `
    /reference:System.Web.Extensions.dll `
    $sources.FullName

if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

Write-Host "Built: $exe"
