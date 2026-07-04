# Release checklist

- Run `dotnet test .\tests\AudioConverter.Tests\AudioConverter.Tests.csproj`.
- Run `.\scripts\verify-ffmpeg-runtime.ps1`.
- Run `.\scripts\publish.ps1 -SelfContained` for a portable build.
- Confirm output presets shown by the app match `docs/FORMAT_SUPPORT.md`.
- Confirm failed/cancelled conversions do not leave final output files.
- Confirm same-extension conversion never overwrites the source.
- Include `docs/LICENSES.md`, `docs\THIRD_PARTY_NOTICES.md`, and FFmpeg source-offer material in release artifacts.
- Do not ship signing keys, generated build trees, backup archives, or unlicensed media samples.
