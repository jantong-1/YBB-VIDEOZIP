param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$project = Join-Path $root "src\YBBvideozip.Mac.csproj"
$output = Join-Path $root "dist\osx-arm64\publish"

if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "Cannot find dotnet. Install the .NET SDK first."
}

$sdkList = & dotnet --list-sdks
if ([string]::IsNullOrWhiteSpace($sdkList)) {
    throw "No .NET SDK found. Install the .NET 8 SDK first."
}

New-Item -ItemType Directory -Force -Path $output | Out-Null

& dotnet publish $project `
    --configuration $Configuration `
    --runtime osx-arm64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    --output $output

if ($LASTEXITCODE -ne 0) {
    throw "macOS arm64 publish failed."
}

Write-Host "Published: $output"
