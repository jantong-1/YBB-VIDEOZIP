param(
    [Parameter(Mandatory = $true)]
    [string]$Payload,

    [string]$PrivateKeyPath = ""
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
if ([string]::IsNullOrWhiteSpace($PrivateKeyPath)) {
    $PrivateKeyPath = Join-Path $root "secrets\license-private-key.xml"
}

if (!(Test-Path -LiteralPath $PrivateKeyPath)) {
    throw "Missing private license key: $PrivateKeyPath"
}

$prefix = "YBBPRO"
$normalized = [regex]::Replace($Payload.ToUpperInvariant(), "[^A-Z0-9]", "")

if ($normalized.Length -lt 4) {
    throw "Payload must contain at least 4 letters or digits."
}

if ($normalized.Length -gt 32) {
    $normalized = $normalized.Substring(0, 32)
}

$privateKeyXml = [System.IO.File]::ReadAllText($PrivateKeyPath, [System.Text.Encoding]::UTF8)
$rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider
$rsa.FromXmlString($privateKeyXml)

$signingPayload = "YBBvideozip|pro|v2|$normalized"
$bytes = [System.Text.Encoding]::UTF8.GetBytes($signingPayload)
$hashAlgorithm = [System.Security.Cryptography.CryptoConfig]::MapNameToOID("SHA256")
$signature = $rsa.SignData($bytes, $hashAlgorithm)
$encoded = [Convert]::ToBase64String($signature).TrimEnd("=").Replace("+", "-").Replace("/", "_")

Write-Output "$prefix-$normalized-$encoded"
