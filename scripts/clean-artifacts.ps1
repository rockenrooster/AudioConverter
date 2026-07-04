param(
  [string]$RepoRoot = (Resolve-Path "$PSScriptRoot\..").Path
)

$targets = @("bin", "obj", "artifacts", "TestResults") | ForEach-Object {
  Join-Path $RepoRoot $_
}

foreach ($target in $targets) {
  $resolved = if (Test-Path -LiteralPath $target) { (Resolve-Path -LiteralPath $target).Path } else { $target }
  if ($resolved.StartsWith($RepoRoot, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolved)) {
    Remove-Item -LiteralPath $resolved -Recurse -Force
  }
}
