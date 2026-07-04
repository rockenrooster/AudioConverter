param(
  [string]$Repo = "rockenrooster/AudioConverter",
  [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
$publishDir = Join-Path $repoRoot "artifacts\publish\win-x64"
$exePath = Join-Path $publishDir "AudioConverter.exe"
$manifestPath = Join-Path $publishDir "AudioConverter.exe.manifest.json"
$signaturePath = Join-Path $publishDir "AudioConverter.exe.manifest.sig"

function Get-PrivateKeyPem {
  if (-not [string]::IsNullOrWhiteSpace($env:AUDIO_CONVERTER_UPDATE_PRIVATE_KEY_PEM)) {
    return $env:AUDIO_CONVERTER_UPDATE_PRIVATE_KEY_PEM
  }

  throw "Missing AUDIO_CONVERTER_UPDATE_PRIVATE_KEY_PEM."
}

& (Join-Path $repoRoot "build.ps1") -Configuration $Configuration -BumpBuildVersion:$false
if ($LASTEXITCODE -ne 0) {
  throw "build.ps1 failed with exit code $LASTEXITCODE"
}

$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath).FileVersion
if ([string]::IsNullOrWhiteSpace($version)) {
  throw "Could not read file version from $exePath"
}

$tag = "v$version"
$hash = (Get-FileHash -LiteralPath $exePath -Algorithm SHA256).Hash.ToLowerInvariant()
$size = (Get-Item -LiteralPath $exePath).Length
$manifest = [ordered]@{
  schemaVersion = 1
  version = $version
  tag = $tag
  assetName = "AudioConverter.exe"
  downloadUrl = "https://github.com/$Repo/releases/download/$tag/AudioConverter.exe"
  sha256 = $hash
  sizeBytes = $size
  createdUtc = (Get-Date).ToUniversalTime().ToString("O")
}

$manifestJson = $manifest | ConvertTo-Json -Depth 3
[System.IO.File]::WriteAllText($manifestPath, $manifestJson, [System.Text.UTF8Encoding]::new($false))

$rsa = [System.Security.Cryptography.RSA]::Create()
$rsa.ImportFromPem((Get-PrivateKeyPem))
$signatureBytes = $rsa.SignData(
  [System.IO.File]::ReadAllBytes($manifestPath),
  [System.Security.Cryptography.HashAlgorithmName]::SHA256,
  [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
[System.IO.File]::WriteAllBytes($signaturePath, $signatureBytes)

if ($env:GITHUB_OUTPUT) {
  Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "tag=$tag"
}

Write-Host "PACKAGE: release $tag is ready"
