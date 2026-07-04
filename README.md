# AudioConverter

Windows audio conversion tool backed by FFmpeg.

## Build

```powershell
.\build.ps1
```

The default build publishes a runtime-dependent single-file Windows executable to:

```text
artifacts\publish\win-x64\AudioConverter.exe
```

The publish folder also includes `LICENSES.md` and `THIRD_PARTY_NOTICES.md`.

## Release

Initialize the update signing key once:

```powershell
.\release.ps1 -InitSigningKey
```

Validate a release without pushing, tagging, or publishing:

```powershell
.\release.ps1 -DryRun
```

Create the GitHub release:

```powershell
.\release.ps1
```

The release script builds the app, signs the update manifest, commits and pushes pending changes, tags the version, and uploads the EXE plus manifest assets to GitHub Releases.
