# 🎙️ TurboScribe

**Transcribe entire drives of audio and video — locally, privately, and for free.**

TurboScribe is a GPU-accelerated desktop app that transcribes meetings, interviews, voice memos, and lectures using **Whisper Large-v3** — without ever uploading your data to the cloud.

> **🌟 Exceptional Noise Handling:** Works great with noisy outdoor recordings — car traffic, wind, lawn mowers, barking dogs, you name it.

---

## 📥 Download

**[⬇ Download TurboScribe v1.6.0 (Windows x64)](https://github.com/dparksports/turboscribe/releases/download/v1.6.0/TurboScribe-v1.6.0.zip)**

Extract the zip and run `TurboScribe.exe`. That's it.

**Requirements:** Windows 10/11 with an **NVIDIA GPU** (CUDA). Python 3.10+ is installed automatically.

---

## 🔑 Key Features

### 🔒 100% Private & Offline
All transcription happens on your local machine. Your audio files never leave your computer — perfect for confidential meetings, legal interviews, and sensitive recordings.

### 💾 Drive-Based Transcription
Check one or more drives (local, USB, network, or mapped) and hit Transcribe. TurboScribe recursively finds and transcribes every media file across all selected drives.

### ⚡ GPU-Accelerated Speed
Built on [faster-whisper](https://github.com/SYSTRAN/faster-whisper) with CUDA acceleration. Transcribe hours of audio in minutes, not hours.

### 🔎 Search Across All Transcripts
- **Exact Match** — instant in-process keyword search across all transcript files
- **Similar Meaning** — semantic search powered by sentence-transformers (runs independently, even during transcription)

### 📊 AI-Powered Analysis
Right-click any transcript to **Summarize** or generate an **Outline** using:
- Local models (Phi-3, LLaMA)
- Cloud APIs (Gemini, OpenAI, Claude)

### 🗂️ Transcript Management
- Browse, sort, and filter transcripts
- View transcripts inline with search highlighting
- Compare different transcription versions side-by-side
- Delete unwanted transcripts from the context menu
- Open source media files directly from the transcript list

---

## 🚀 Getting Started

### Option 1: Download the Release (Recommended)
1. Download the [latest release](https://github.com/dparksports/turboscribe/releases/latest)
2. Extract the zip
3. Run `TurboScribe.exe`
4. Click **⚙ Settings → Install AI Libraries** to set up the Python environment
5. Check the drives you want to transcribe and click **▶ Transcribe All Files**

### Option 2: Build from Source
```bash
git clone https://github.com/dparksports/turboscribe.git
cd turboscribe

# Build and run
dotnet run --project LongAudioApp
```

---

## 🛠️ Tech Stack

| Component | Technology |
|---|---|
| Transcription Engine | [faster-whisper](https://github.com/SYSTRAN/faster-whisper) (Whisper Large-v3) |
| Voice Detection | Silero VAD |
| Semantic Search | sentence-transformers |
| Desktop App | WPF (.NET 8, C#) |
| GPU Acceleration | CUDA via CTranslate2 |
| AI Analysis | Local (llama-cpp-python) or Cloud (Gemini, OpenAI, Claude) |

---

## ⚙️ Settings

All settings persist across app launches:

| Setting | Description |
|---|---|
| **Selected Drives** | Which drives to scan for media files |
| **No VAD Mode** | Disable voice activity detection (better for noisy outdoor audio) |
| **Skip Existing** | Don't re-transcribe files that already have transcripts |
| **English Only** | Filter Whisper model list to English-optimized models |
| **Device** | Choose between CUDA (GPU) or CPU |
| **Start Engine on Launch** | Auto-start the Python transcription engine |
| **GPU Refresh Interval** | How often to poll GPU usage stats |

Settings are stored in `%AppData%/LongAudioApp/app_settings.json`.

---

## 📁 Project Structure

```
turboscribe/
├── fast_engine.py              # Python transcription engine
├── LongAudioApp/               # WPF desktop application
│   ├── MainWindow.xaml          # UI layout (dark theme)
│   ├── MainWindow.xaml.cs       # Application logic
│   ├── PythonRunner.cs          # Python process manager
│   ├── PipInstaller.cs          # Automated library installer
│   ├── AnalyticsService.cs      # Optional GA4 analytics
│   └── App.xaml                 # Styles and themes
└── LICENSE                      # Apache 2.0
```

---

## 📄 License

Licensed under the [Apache License 2.0](LICENSE).

---

**Made for people who value privacy and productivity.** 🔐
