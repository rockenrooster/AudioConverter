param(
  [string]$RuntimeDir = (Join-Path (Resolve-Path "$PSScriptRoot\..").Path "FFMPEG_AUDIO")
)

$required = @(
  "avcodec-62.dll",
  "avformat-62.dll",
  "avutil-60.dll",
  "swresample-6.dll"
)

$missing = @()
foreach ($dll in $required) {
  $path = Join-Path $RuntimeDir $dll
  if (-not (Test-Path -LiteralPath $path)) {
    $missing += $dll
  }
}

if ($missing.Count -gt 0) {
  throw "Missing FFmpeg runtime DLL(s): $($missing -join ', ')"
}

Get-ChildItem -LiteralPath $RuntimeDir -Filter *.dll | Sort-Object Name | ForEach-Object {
  [pscustomobject]@{
    Name = $_.Name
    Sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash
  }
}
