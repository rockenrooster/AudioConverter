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

Validate a release without pushing or tagging:

```powershell
.\release.ps1 -DryRun
```

Push the release tag:

```powershell
.\release.ps1
```

The release script builds the app, signs the update manifest as a local check, commits and pushes pending changes, then pushes the version tag. GitHub Actions builds the tagged source, signs the manifest with the `AUDIO_CONVERTER_UPDATE_PRIVATE_KEY_PEM` repository secret, and uploads the EXE plus manifest assets to GitHub Releases.
