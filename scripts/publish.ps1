param(
  [string]$Configuration = "Release",
  [string]$RuntimeIdentifier = "win-x64",
  [switch]$SelfContained,
  [bool]$SingleFile = $true
)

$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
$project = Join-Path $repoRoot "AudioConverter.csproj"
$publishRoot = Join-Path $repoRoot "artifacts\publish"
$publishDir = Join-Path $publishRoot $RuntimeIdentifier
$publishRoot = [System.IO.Path]::GetFullPath($publishRoot)
$publishDir = [System.IO.Path]::GetFullPath($publishDir)

if (-not $publishDir.StartsWith($publishRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
  throw "Publish directory escaped artifacts root: $publishDir"
}

if (Test-Path -LiteralPath $publishDir) {
  Remove-Item -LiteralPath $publishDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

$args = @(
  "publish", $project,
  "-c", $Configuration,
  "-r", $RuntimeIdentifier,
  "-o", $publishDir,
  "--self-contained", $SelfContained.IsPresent.ToString().ToLowerInvariant(),
  "/p:PublishSingleFile=$($SingleFile.ToString().ToLowerInvariant())",
  "/p:IncludeNativeLibrariesForSelfExtract=true"
)

dotnet @args

Copy-Item -LiteralPath (Join-Path $repoRoot "docs\LICENSES.md") -Destination $publishDir -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "docs\THIRD_PARTY_NOTICES.md") -Destination $publishDir -Force
