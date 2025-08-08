# XREAL Air 2 Jarvis AR Assistant

A complete Unity AR application for XREAL Air 2 smart glasses featuring a Jarvis-like AI voice assistant powered by OpenAI's ChatGPT API.

## Features
- 100% hands-free voice interaction
- Jarvis-style conversational AI with context retention
- Floating AR UI panels for chat, research, weather, and maps
- Map navigation overlays using GPS + compass
- Real-time information display
- Multi-language support with instant translation

## Technical Stack
- Unity 2021.3 LTS
- XREAL NRSDK (latest)
- Android API 29+, ARM64, IL2CPP
- OpenAI ChatGPT API
- Google Maps API / Mapbox
- Android Speech-to-Text & Text-to-Speech

## Project Structure
```
Assets/
├── NRSDK/                    # XREAL SDK
├── Scripts/
│   ├── Core/                 # Core system scripts
│   ├── AI/                   # ChatGPT integration
│   ├── Voice/                # STT/TTS handling
│   ├── Maps/                 # Map integration
│   ├── UI/                   # AR UI components
│   └── Utils/                # Utility scripts
├── Prefabs/                  # UI and system prefabs
├── Materials/                # AR-optimized materials
├── Scenes/                   # Main AR scene
└── StreamingAssets/          # Configuration files
```

## Setup Instructions
1. Install Unity 2021.3 LTS with Android Build Support
2. Download and import XREAL NRSDK
3. Configure project settings for XREAL Air 2
4. Set up API keys for OpenAI and Maps
5. Build and deploy via Android + Nebula

## Build Requirements
- Android SDK API 29+
- NDK for ARM64 architecture
- IL2CPP scripting backend
- OpenGL ES3 graphics API