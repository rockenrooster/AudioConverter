param(
  [string]$Configuration = "Release",
  [switch]$SelfContained,
  [bool]$SingleFile = $true,
  [bool]$BumpBuildVersion = $true
)

$script = Join-Path $PSScriptRoot "scripts\publish.ps1"
& $script -Configuration $Configuration -RuntimeIdentifier "win-x64" -SelfContained:$SelfContained -SingleFile:$SingleFile -BumpBuildVersion:$BumpBuildVersion
