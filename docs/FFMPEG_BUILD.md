# FFmpeg runtime

The app ships FFmpeg shared libraries and uses them through `FFmpeg.AutoGen`.

Current runtime location:

```text
FFMPEG_AUDIO/
```

Current core libraries:

```text
avcodec-62.dll
avformat-62.dll
avutil-60.dll
swresample-6.dll
```

Important bundled dependencies include:

```text
libmp3lame-0.dll
libopus-0.dll
libvorbis-0.dll
libvorbisenc-2.dll
libogg-0.dll
libspeex-1.dll
libspeexdsp-1.dll
liblzma-5.dll
libbz2-1.dll
zlib1.dll
libiconv-2.dll
libintl-8.dll
libgcc_s_seh-1.dll
libstdc++-6.dll
libwinpthread-1.dll
```

The inspected build was GPL/version3-enabled. Keep that as an explicit release decision and ship the matching notices/source offer.

Use `scripts/verify-ffmpeg-runtime.ps1` after changing the runtime folder.
