# Format support

Visible output presets are capability-checked at startup:

| Id | Output | Encoder | Muxer |
| --- | --- | --- | --- |
| `mp3` | MP3 | MP3 | mp3 |
| `aac` | AAC ADTS | AAC | adts |
| `flac` | FLAC | FLAC | flac |
| `wav` | WAV PCM | PCM 16/24 | wav |
| `ogg` | Ogg Vorbis | Vorbis | ogg |
| `opus` | Opus | Opus | opus |
| `m4a` | M4A AAC | AAC | ipod |

Unsupported outputs are hidden if the runtime FFmpeg build lacks the encoder or muxer.

Input support is probe-based. A file is accepted when FFmpeg can open it, find stream info, find at least one audio stream, and find a decoder for that stream.
