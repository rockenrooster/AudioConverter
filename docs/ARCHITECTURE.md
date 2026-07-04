# AudioConverter architecture

AudioConverter is a Windows WinForms app targeting `net10.0-windows7.0`.

Conversion uses FFmpeg shared libraries through `FFmpeg.AutoGen`; normal conversion does not shell out to `ffmpeg.exe`.

Startup calls `FFmpegConverter.Initialize()`, sets `ffmpeg.RootPath` to `AppDomain.CurrentDomain.BaseDirectory`, preloads the core runtime DLLs, verifies required exports, and logs FFmpeg library versions.

Required core DLLs:

- `avcodec-62.dll`
- `avformat-62.dll`
- `avutil-60.dll`
- `swresample-6.dll`

The project currently keeps runtime DLLs under `FFMPEG_AUDIO/` and copies them beside the app at build/publish time.

Output path safety is centralized in `OutputPathResolver`. Conversions write to a temporary file in the destination folder whose filename still ends with the target media extension, then move that temp file to the final path only after a successful conversion.

Input import uses extension prefiltering only to avoid probing obvious non-media files. FFmpeg probing is the source of truth: accepted files must open, contain an audio stream, and have a decoder.
