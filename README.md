# 🎙️ TurboScribe

**Fast, private, GPU-accelerated transcription for Windows.**

TurboScribe transcribes audio and video files entirely on your machine using OpenAI's Whisper models. No cloud services, no subscriptions, no data leaves your computer.

---

## 📥 Download

**[⬇ Download Latest Release](https://github.com/dparksports/turboscribe/releases/latest)**

Extract the zip → run `TurboScribe.exe` → done.

**Requirements:** Windows 10/11 (x64), .NET 8 Runtime. NVIDIA GPU recommended for speed.

---

## ✨ What It Does

### Transcription Engine
- **12 Whisper models** — tiny, base, small, medium, large-v1/v2/v3, turbo (+ English variants)
- **GPU acceleration** — CUDA support for 4× faster transcription on NVIDIA GPUs
- **Voice Activity Detection** — fast VAD scan to find files with speech before transcribing
- **Multi-model comparison** — run different models on the same file and compare outputs
- **Batch processing** — transcribe entire drives, folders, or USB devices
- **Smart skip** — automatically skip files that already have transcripts

### Media Player
- **Embedded playback** — play audio/video directly in the app
- **Transcript sync** — click transcript lines to seek video, or scrub timeline to highlight text
- **Full controls** — play/pause, stop, volume, timeline scrubbing

### AI Analysis
- **Summarize & Outline** — generate summaries or structured outlines for transcripts
- **Local or Cloud** — use local LLMs (LLaMA, Mistral, Phi-3, Qwen2, Gemma) or cloud APIs (Gemini, OpenAI, Claude)
- **Batch analysis** — process all transcripts at once

### Search
- **Keyword search** — find exact matches across all transcripts
- **Semantic search** — find content by meaning using sentence-transformers
- **5 embedding models** — MiniLM, mpnet, GTE, Qwen3-Embedding, Gemma-Embedding

---

## 🚀 Quick Start

1. **Download** the [latest release](https://github.com/dparksports/turboscribe/releases/latest)
2. **Extract** and run `TurboScribe.exe`
3. **Install AI Libraries** (one-time):
   - Go to **Settings** → **Install AI Libraries**
   - Downloads Python + faster-whisper (~2GB)
4. **Select folders** to scan using the checkboxes
5. **Click "🔍 Scan for Voice"** to find files with speech
6. **Click "▶ Transcribe All Files"** to start

---

## 🎯 Key Features

### Voice Duration Column
The file list shows detected speech duration for each file (e.g., "2.3m", "45s"). Click the column header to sort by voice duration — perfect for finding actual meetings vs silent recordings.

### Untranscribed Files List
Toggle the "📋 Untranscribed files" checkbox to see files with detected voice but no transcript yet. Files are sorted by voice duration (most promising first).

### Model Badges
Each file shows checkmarks for which Whisper models have been used. Compare turbo vs medium.en side-by-side.

### Current Folder Filter
Toggle "📂 Current folder" to show only files from checked drives/folders, or uncheck to see all previously scanned files.

---

## 🔧 Tech Stack

| Component | Technology |
|---|---|
| Transcription | [faster-whisper](https://github.com/SYSTRAN/faster-whisper) with CUDA |
| Voice Detection | Silero VAD |
| Semantic Search | sentence-transformers |
| AI Analysis | llama-cpp-python (local) or cloud APIs |
| Desktop App | WPF, .NET 8, C# |
| Media Player | NAudio, FFmpeg |

---

## 🛠️ Build from Source

```bash
git clone https://github.com/dparksports/turboscribe.git
cd turboscribe
dotnet restore
dotnet run --project LongAudioApp
```

---

## 📝 Recent Updates

### Latest
- **Voice duration column** — shows speech seconds detected by VAD, sortable
- **Untranscribed files list** — toggle to show/hide files without transcripts
- **Model names in filenames** — transcripts now include model name (e.g., `meeting_transcript_turbo.txt`)
- **Fixed directory handling** — VAD scan now correctly targets selected folders
- **Delete all transcripts** — new button in Settings to clear all transcript files

---

## 📄 License

[Apache License 2.0](LICENSE)
