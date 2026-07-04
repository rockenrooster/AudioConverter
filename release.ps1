param(
  [switch]$InitSigningKey,
  [switch]$DryRun,
  [switch]$PackageOnly,
  [string]$Repo = "rockenrooster/AudioConverter",
  [string]$Branch = "main",
  [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path $PSScriptRoot).Path
$privateKeyPath = Join-Path $env:USERPROFILE ".audio-converter\update-private-key.pem"
$trustPath = Join-Path $repoRoot "Services\Update\UpdateTrust.cs"
$publishDir = Join-Path $repoRoot "artifacts\publish\win-x64"
$exePath = Join-Path $publishDir "AudioConverter.exe"
$manifestPath = Join-Path $publishDir "AudioConverter.exe.manifest.json"
$signaturePath = Join-Path $publishDir "AudioConverter.exe.manifest.sig"

function Invoke-Checked {
  param([string]$FileName, [string[]]$Arguments)
  & $FileName @Arguments
  if ($LASTEXITCODE -ne 0) {
    throw "$FileName failed with exit code $LASTEXITCODE"
  }
}

function Test-Command {
  param([string]$Name)
  if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
    throw "Missing required command: $Name"
  }
}

function Set-PublicKeyInSource {
  param([string]$PublicKeyBase64)
  $content = Get-Content -LiteralPath $trustPath -Raw
  $replacement = 'internal const string PublicKeyBase64 = "' + $PublicKeyBase64 + '";'
  $updated = [regex]::Replace($content, 'internal const string PublicKeyBase64 = ".*";', $replacement)
  if ($updated -eq $content) {
    throw "Could not update PublicKeyBase64 in $trustPath"
  }
  Set-Content -LiteralPath $trustPath -Value $updated -NoNewline
}

function Initialize-SigningKey {
  $keyDir = Split-Path -Parent $privateKeyPath
  New-Item -ItemType Directory -Force -Path $keyDir | Out-Null

  $rsa = [System.Security.Cryptography.RSA]::Create(3072)
  if (Test-Path -LiteralPath $privateKeyPath) {
    $pem = Get-Content -LiteralPath $privateKeyPath -Raw
    $rsa.ImportFromPem($pem)
  } else {
    $privatePem = $rsa.ExportPkcs8PrivateKeyPem()
    Set-Content -LiteralPath $privateKeyPath -Value $privatePem -NoNewline
  }

  $publicKeyBase64 = [Convert]::ToBase64String($rsa.ExportSubjectPublicKeyInfo())
  Set-PublicKeyInSource $publicKeyBase64
  Write-Host "Signing key ready: $privateKeyPath"
}

function Get-PrivateKeyPem {
  if (-not [string]::IsNullOrWhiteSpace($env:AUDIO_CONVERTER_UPDATE_PRIVATE_KEY_PEM)) {
    return $env:AUDIO_CONVERTER_UPDATE_PRIVATE_KEY_PEM
  }

  if (-not [string]::IsNullOrWhiteSpace($env:UPDATE_PRIVATE_KEY_PEM)) {
    return $env:UPDATE_PRIVATE_KEY_PEM
  }

  if (Test-Path -LiteralPath $privateKeyPath) {
    return Get-Content -LiteralPath $privateKeyPath -Raw
  }

  throw "Missing private signing key. Run .\release.ps1 -InitSigningKey first, or set AUDIO_CONVERTER_UPDATE_PRIVATE_KEY_PEM."
}

function Get-GitHubRepoExists {
  & gh repo view $Repo *> $null
  return $LASTEXITCODE -eq 0
}

function Ensure-GitHubRepo {
  $repoExists = Get-GitHubRepoExists
  if (-not (Test-Path -LiteralPath (Join-Path $repoRoot ".git"))) {
    if ($DryRun) {
      Write-Host "DRY RUN: would initialize git repository"
    } else {
      Invoke-Checked git @("init", "-b", $Branch)
    }
  }

  & git remote get-url origin *> $null
  $hasOrigin = $LASTEXITCODE -eq 0

  if (-not $repoExists) {
    if ($DryRun) {
      Write-Host "DRY RUN: would create public GitHub repo $Repo"
    } else {
      Invoke-Checked gh @("repo", "create", $Repo, "--public", "--source", ".", "--remote", "origin")
    }
  } elseif (-not $hasOrigin -and -not $DryRun) {
    Invoke-Checked git @("remote", "add", "origin", "https://github.com/$Repo.git")
  }
}

function Get-PublishedVersion {
  if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Missing published exe: $exePath"
  }

  $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath).FileVersion
  if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Could not read file version from $exePath"
  }

  return $version
}

function Write-ManifestAndSignature {
  param([string]$Version)
  $tag = "v$Version"
  $hash = (Get-FileHash -LiteralPath $exePath -Algorithm SHA256).Hash.ToLowerInvariant()
  $size = (Get-Item -LiteralPath $exePath).Length
  $downloadUrl = "https://github.com/$Repo/releases/download/$tag/AudioConverter.exe"
  $manifest = [ordered]@{
    schemaVersion = 1
    version = $Version
    tag = $tag
    assetName = "AudioConverter.exe"
    downloadUrl = $downloadUrl
    sha256 = $hash
    sizeBytes = $size
    createdUtc = (Get-Date).ToUniversalTime().ToString("O")
  }

  $manifestJson = $manifest | ConvertTo-Json -Depth 3
  [System.IO.File]::WriteAllText($manifestPath, $manifestJson, [System.Text.UTF8Encoding]::new($false))

  $rsa = [System.Security.Cryptography.RSA]::Create()
  $rsa.ImportFromPem((Get-PrivateKeyPem))
  $manifestBytes = [System.IO.File]::ReadAllBytes($manifestPath)
  $signatureBytes = $rsa.SignData(
    $manifestBytes,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256,
    [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
  [System.IO.File]::WriteAllBytes($signaturePath, $signatureBytes)
  return $tag
}

function Assert-ReleaseDoesNotExist {
  param([string]$Tag)
  & git rev-parse -q --verify "refs/tags/$Tag" *> $null
  if ($LASTEXITCODE -eq 0) {
    throw "Local tag already exists: $Tag"
  }

  if (Get-GitHubRepoExists) {
    & gh release view $Tag -R $Repo *> $null
    if ($LASTEXITCODE -eq 0) {
      throw "GitHub release already exists: $Tag"
    }
  }
}

if ($InitSigningKey) {
  Initialize-SigningKey
  return
}

Push-Location $repoRoot
try {
  & (Join-Path $repoRoot "build.ps1") -Configuration $Configuration -BumpBuildVersion:$false
  if ($LASTEXITCODE -ne 0) {
    throw "build.ps1 failed with exit code $LASTEXITCODE"
  }
  $version = Get-PublishedVersion
  $tag = Write-ManifestAndSignature $version

  if ($PackageOnly) {
    if ($env:GITHUB_OUTPUT) {
      Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "tag=$tag"
    }
    Write-Host "PACKAGE: release $tag is ready"
    return
  }

  Test-Command git
  Test-Command gh
  Invoke-Checked gh @("auth", "status")
  Ensure-GitHubRepo
  Assert-ReleaseDoesNotExist $tag

  if ($DryRun) {
    Write-Host "DRY RUN: release $tag is ready for GitHub Actions"
    return
  }

  Invoke-Checked git @("add", "-A")
  & git diff --cached --quiet
  if ($LASTEXITCODE -ne 0) {
    Invoke-Checked git @("commit", "-m", "Release $tag")
  }

  Invoke-Checked git @("push", "-u", "origin", $Branch)
  Invoke-Checked git @("tag", $tag)
  Invoke-Checked git @("push", "origin", $tag)
  Write-Host "Pushed $tag. GitHub Actions will build and publish the release."
} finally {
  Pop-Location
}
