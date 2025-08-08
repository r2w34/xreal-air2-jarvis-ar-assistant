# APK Build Instructions - XREAL Air 2 Jarvis AR Assistant

## Quick Start

### Prerequisites Checklist
- [ ] Unity 2021.3 LTS installed with Android Build Support
- [ ] Android SDK (API 29+) and NDK configured
- [ ] XREAL NRSDK downloaded and imported
- [ ] API keys configured in `Assets/StreamingAssets/config.json`
- [ ] Android device with Developer Options enabled

### Method 1: Automated Build Scripts

#### Windows Users:
```cmd
# Double-click or run from command prompt
build_apk.bat
```

#### Mac/Linux Users:
```bash
# Make executable and run
chmod +x build_apk.sh
./build_apk.sh
```

### Method 2: Unity Editor Build

1. **Open Unity Project**
   - Launch Unity Hub
   - Open the XREAL_Jarvis_AR_App project
   - Wait for project to load and compile

2. **Configure Build Settings**
   - Go to `Build → Configure for XREAL Air 2` (menu item)
   - Go to `Build → Validate Build Requirements`
   - Fix any issues reported

3. **Build APK**
   - Go to `Build → Build Android APK` for release build
   - Or `Build → Build Android APK (Development)` for debug build
   - Choose build location when prompted
   - Wait for build to complete

## Detailed Setup Instructions

### 1. Unity Installation

#### Download Unity Hub
- Visit: https://unity3d.com/get-unity/download
- Install Unity Hub
- Sign in with Unity account

#### Install Unity 2021.3 LTS
```
Unity Hub → Installs → Add
Select: Unity 2021.3.33f1 (LTS)
Modules to include:
  ✓ Android Build Support
  ✓ Android SDK & NDK Tools
  ✓ OpenJDK
```

### 2. Android SDK Configuration

#### Automatic Setup (Recommended)
Unity will automatically download and configure Android tools.

#### Manual Setup (Advanced)
```
Unity → Preferences → External Tools
Android SDK Root: /path/to/android-sdk
Android NDK Root: /path/to/android-ndk
JDK Root: /path/to/openjdk
```

### 3. XREAL NRSDK Setup

#### Download NRSDK
1. Visit: https://docs.xreal.com/Getting%20Started%20with%20XREAL%20SDK
2. Download latest NRSDK Unity package
3. Create account if required

#### Import NRSDK
```
Unity → Assets → Import Package → Custom Package
Select: NRSDK_X.X.X.unitypackage
Import All
```

#### Configure NRSDK
```
Unity → NRKernal → NRProjectConfig
✓ Support XREAL Air
✓ Multi Resume
Target Device: XREAL Air 2
```

### 4. API Keys Configuration

#### Required API Keys
- **OpenAI API Key**: For ChatGPT integration
  - Get from: https://platform.openai.com/api-keys
  - Format: `sk-...`

- **Google Maps API Key**: For map functionality
  - Get from: https://console.cloud.google.com/
  - Enable: Maps Static API, Geocoding API
  - Format: `AIza...`

- **Mapbox API Key** (Optional): Alternative to Google Maps
  - Get from: https://account.mapbox.com/access-tokens/
  - Format: `pk.eyJ1...`

#### Configuration Methods

**Method 1: StreamingAssets (Recommended)**
```json
// Edit: Assets/StreamingAssets/config.json
{
  "openai_api_key": "sk-your-actual-openai-key-here",
  "google_maps_api_key": "AIza-your-actual-google-maps-key",
  "mapbox_api_key": "pk.eyJ1-your-mapbox-key-here"
}
```

**Method 2: Inspector Assignment**
- Select JarvisManager in scene
- Paste API keys directly in inspector fields
- Not recommended for version control

### 5. Project Configuration

#### Player Settings
```
Unity → File → Build Settings → Player Settings

Company Name: XREAL Jarvis
Product Name: Jarvis AR Assistant
Package Name: com.xreal.jarvis.ar

Android Settings:
  Minimum API Level: Android 10.0 (API level 29)
  Target API Level: Android 13.0 (API level 33)
  Scripting Backend: IL2CPP
  Target Architectures: ARM64
  
Graphics:
  Graphics APIs: OpenGL ES3, Vulkan
  Color Space: Linear
  
Other Settings:
  Scripting Define Symbols: XREAL_AR
  Configuration: Release (for production)
```

#### Build Settings
```
Unity → File → Build Settings

Platform: Android
Scenes in Build:
  ✓ Assets/Scenes/JarvisARScene.unity

Development Build: ☐ (uncheck for release)
Script Debugging: ☐ (uncheck for release)
```

### 6. Building the APK

#### Using Build Script (Recommended)
```csharp
// Unity → Build → Build Android APK
// This uses the automated BuildScript.cs
```

#### Manual Build Process
```
1. File → Build Settings
2. Select Android platform
3. Click "Switch Platform" if needed
4. Click "Build"
5. Choose output location
6. Wait for build completion
```

#### Build Output
```
Location: Builds/JarvisAR_YYYYMMDD_HHMMSS.apk
Size: ~50-100 MB (depending on configuration)
```

## Installation and Testing

### 1. Enable Developer Mode
```
Android Device:
Settings → About Phone → Tap "Build Number" 7 times
Settings → Developer Options → Enable USB Debugging
```

### 2. Install APK
```bash
# Connect device via USB
adb devices

# Install APK
adb install -r "path/to/JarvisAR.apk"

# Launch app
adb shell am start -n com.xreal.jarvis.ar/.UnityPlayerActivity
```

### 3. XREAL Air 2 Setup
```
1. Install XREAL Nebula app from Play Store
2. Connect XREAL Air 2 glasses to Android device
3. Launch Nebula app
4. Find "Jarvis AR Assistant" in app list
5. Launch through Nebula interface
```

### 4. Testing Checklist
- [ ] App launches without crashes
- [ ] Voice recognition works ("Hey Jarvis")
- [ ] AI responses are generated
- [ ] Text-to-speech works
- [ ] Map functionality works (if API keys configured)
- [ ] AR UI is visible and positioned correctly

## Troubleshooting

### Common Build Errors

#### "NRSDK not found"
```
Solution:
1. Download NRSDK from XREAL website
2. Import .unitypackage into project
3. Configure NRProjectConfig settings
```

#### "Android SDK not found"
```
Solution:
1. Unity → Preferences → External Tools
2. Set Android SDK Root path
3. Or reinstall Unity with Android Build Support
```

#### "IL2CPP build failed"
```
Solution:
1. Check Android NDK is installed
2. Unity → Preferences → External Tools → Android NDK Root
3. Try switching to Mono scripting backend temporarily
```

#### "Gradle build failed"
```
Solution:
1. Clear Unity cache: Build → Clean Build Cache
2. Reimport all assets
3. Check Android SDK/NDK versions compatibility
```

### Runtime Issues

#### "Microphone permission denied"
```
Solution:
1. Android Settings → Apps → Jarvis AR → Permissions
2. Enable Microphone permission
3. Or grant when app prompts
```

#### "No internet connection"
```
Solution:
1. Check WiFi/mobile data connection
2. Verify API keys are correct
3. Check firewall/proxy settings
```

#### "AR UI not visible"
```
Solution:
1. Ensure XREAL Air 2 is connected properly
2. Check Nebula app is running
3. Restart app through Nebula
```

### Performance Issues

#### "Low frame rate"
```
Solutions:
1. Reduce map image resolution in config
2. Limit conversation history length
3. Close other apps running on device
```

#### "High battery usage"
```
Solutions:
1. Reduce location update frequency
2. Limit API call frequency
3. Optimize UI update intervals
```

## Advanced Configuration

### Custom Build Configuration
```csharp
// Edit BuildScript.cs for custom build settings
private static void ConfigureBuildSettings(bool developmentBuild)
{
    // Add custom configuration here
    PlayerSettings.Android.bundleVersionCode = 1;
    PlayerSettings.bundleVersion = "1.0.0";
    // ... other settings
}
```

### Signing APK for Distribution
```
Unity → Publishing Settings → Keystore Manager
Create New Keystore:
  - Keystore Name: jarvis_ar_keystore
  - Password: [secure password]
  - Alias: jarvis_ar_key
  - Validity: 25 years

Build Settings:
  ✓ Use Custom Keystore
  Select created keystore
```

### Optimization Settings
```json
// config.json optimizations
{
  "max_tokens": 300,           // Reduce for faster responses
  "map_width": 256,           // Reduce for better performance
  "map_height": 256,          // Reduce for better performance
  "location_update_interval": 10.0,  // Reduce frequency
  "max_chat_messages": 20     // Limit memory usage
}
```

## Distribution

### Internal Testing
1. Build development APK with debug symbols
2. Test on multiple Android devices
3. Verify XREAL Air 2 compatibility
4. Test in various environments

### Production Release
1. Build release APK without debug symbols
2. Sign with production keystore
3. Test thoroughly before distribution
4. Consider Google Play Store or direct distribution

### App Store Preparation
- Create app icons (48x48 to 512x512)
- Write app description
- Take screenshots of AR interface
- Prepare privacy policy
- Set up app store listing

This guide provides complete instructions for building and deploying the XREAL Air 2 Jarvis AR Assistant APK. Follow the steps carefully and test thoroughly before production use.