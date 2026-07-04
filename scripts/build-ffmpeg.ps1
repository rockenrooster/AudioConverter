param(
  [string]$RepoRoot = (Resolve-Path "$PSScriptRoot\..").Path,
  [string]$MsysRoot = "C:\msys64",
  [switch]$Clean,
  [switch]$LgplOnly
)

$sourceDir = Join-Path $RepoRoot "ffmpeg_source"
$buildDir = Join-Path $RepoRoot "ffmpeg_audio_build"
$buildBin = Join-Path $buildDir "bin"
$dest = Join-Path $RepoRoot "FFMPEG_AUDIO"
$msysBin = Join-Path $MsysRoot "mingw64\bin"
$bash = Join-Path $MsysRoot "usr\bin\bash.exe"

if ($Clean -and (Test-Path -LiteralPath $dest)) {
  Remove-Item -LiteralPath $dest -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $dest | Out-Null

function Convert-ToMsysPath([string]$Path) {
  (& $bash -lc "cygpath -u '$($Path -replace '\\', '\\')'").Trim()
}

if (-not (Test-Path -LiteralPath (Join-Path $buildBin "avcodec-62.dll"))) {
  if (-not (Test-Path -LiteralPath $bash)) {
    throw "MSYS2 bash not found at $bash. Install MSYS2 or provide -MsysRoot."
  }
  if (-not (Test-Path -LiteralPath (Join-Path $sourceDir "configure"))) {
    throw "FFmpeg source not found at $sourceDir."
  }

  $source = Convert-ToMsysPath $sourceDir
  $prefix = Convert-ToMsysPath $buildDir
  $gplFlags = if ($LgplOnly) { "" } else { "--enable-gpl --enable-version3" }
  $configure = @(
    "./configure",
    "--prefix=$prefix",
    "--disable-static",
    "--enable-shared",
    "--disable-programs",
    "--disable-doc",
    "--enable-encoder=aac,flac,pcm_s16le,pcm_s24le,libmp3lame,libopus,libvorbis",
    "--enable-muxer=adts,flac,ipod,mov,mp3,mp4,ogg,opus,wav",
    "--enable-libmp3lame",
    "--enable-libopus",
    "--enable-libvorbis",
    $gplFlags
  ) -join " "

  & $bash -lc "cd '$source' && $configure && make -j`$(nproc) && make install"
  if ($LASTEXITCODE -ne 0) {
    throw "FFmpeg build failed."
  }
}

$core = @("avcodec-62.dll", "avformat-62.dll", "avutil-60.dll", "swresample-6.dll")
$extras = @(
  "libgcc_s_seh-1.dll",
  "libstdc++-6.dll",
  "libwinpthread-1.dll",
  "libogg-0.dll",
  "libvorbis-0.dll",
  "libvorbisenc-2.dll",
  "libvorbisfile-3.dll",
  "libopus-0.dll",
  "libmp3lame-0.dll",
  "libspeex-1.dll",
  "libspeexdsp-1.dll",
  "libiconv-2.dll",
  "libintl-8.dll",
  "libbz2-1.dll",
  "liblzma-5.dll",
  "zlib1.dll"
)

foreach ($dll in $core) {
  Copy-Item -LiteralPath (Join-Path $buildBin $dll) -Destination $dest -Force
}

foreach ($dll in $extras) {
  $src = Join-Path $msysBin $dll
  if (Test-Path -LiteralPath $src) {
    Copy-Item -LiteralPath $src -Destination $dest -Force
  }
}

& (Join-Path $PSScriptRoot "verify-ffmpeg-runtime.ps1") -RuntimeDir $dest
