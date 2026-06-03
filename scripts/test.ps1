param()

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$dist = Join-Path $root "dist"
$testExe = Join-Path $dist "BusinessLogicTests.exe"
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (!(Test-Path -LiteralPath $csc)) {
    $csc = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (!(Test-Path -LiteralPath $csc)) {
    throw "Cannot find .NET Framework csc.exe."
}

New-Item -ItemType Directory -Force -Path $dist | Out-Null

$sources = @(
    (Join-Path $root "tests\BusinessLogicTests.cs"),
    (Join-Path $root "src\AdConfig.cs"),
    (Join-Path $root "src\LicenseManager.cs")
)

& $csc `
    /nologo `
    /target:exe `
    /optimize+ `
    /out:$testExe `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Web.Extensions.dll `
    $sources

if ($LASTEXITCODE -ne 0) {
    throw "Test compile failed."
}

& $testExe
if ($LASTEXITCODE -ne 0) {
    throw "Tests failed."
}

Remove-Item -LiteralPath $testExe -Force -ErrorAction SilentlyContinue
