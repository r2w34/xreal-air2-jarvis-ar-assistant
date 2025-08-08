# XREAL Air 2 Jarvis AR Assistant - Build & Deployment Guide

## Prerequisites

### Software Requirements
- Unity 2021.3 LTS or later
- Android SDK (API level 29+)
- Android NDK (for ARM64 builds)
- XREAL NRSDK (latest version)
- Android Studio (recommended for debugging)
- Git (for version control)

### Hardware Requirements
- XREAL Air 2 smart glasses
- Compatible Android device (Android 10+)
- USB-C cable for device connection
- Development computer (Windows/Mac/Linux)

## Setup Instructions

### 1. Unity Installation
```bash
# Download Unity Hub
# Install Unity 2021.3 LTS with Android Build Support
# Include Android SDK & NDK tools
```

### 2. XREAL NRSDK Setup
1. Download NRSDK from: https://docs.xreal.com/Getting%20Started%20with%20XREAL%20SDK
2. Import NRSDK package into Unity project
3. Configure NRProjectConfig:
   - Enable "Support XREAL Air"
   - Enable "Multi Resume" mode
   - Set target device to "XREAL Air 2"

### 3. Project Configuration

#### Unity Player Settings
```
Platform: Android
Target API Level: 33 (Android 13)
Minimum API Level: 29 (Android 10)
Scripting Backend: IL2CPP
Architecture: ARM64
Graphics API: OpenGL ES3
```

#### Build Settings
```
Compression Method: LZ4HC
Development Build: Enabled (for debugging)
Script Debugging: Enabled (for debugging)
```

### 4. API Keys Configuration

#### Method 1: StreamingAssets (Recommended)
1. Edit `Assets/StreamingAssets/config.json`
2. Add your API keys:
```json
{
  "openai_api_key": "sk-your-openai-key-here",
  "google_maps_api_key": "your-google-maps-key-here",
  "mapbox_api_key": "your-mapbox-key-here"
}
```

#### Method 2: PlayerPrefs
```csharp
PlayerPrefs.SetString("OPENAI_API_KEY", "your-key-here");
PlayerPrefs.SetString("GOOGLE_MAPS_API_KEY", "your-key-here");
```

### 5. Android Permissions
Ensure these permissions are in AndroidManifest.xml:
- `RECORD_AUDIO` - Voice input
- `INTERNET` - API calls
- `ACCESS_FINE_LOCATION` - GPS navigation
- `ACCESS_COARSE_LOCATION` - Location services
- `VIBRATE` - Haptic feedback

## Build Process

### 1. Pre-Build Checklist
- [ ] NRSDK imported and configured
- [ ] API keys configured
- [ ] Android SDK/NDK paths set in Unity
- [ ] Target device set to Android
- [ ] All required permissions added

### 2. Unity Build
```bash
# In Unity Editor:
1. File → Build Settings
2. Select Android platform
3. Add all scenes to build
4. Configure Player Settings
5. Click "Build" or "Build and Run"
```

### 3. Command Line Build (Optional)
```bash
# Unity command line build
Unity.exe -batchmode -quit -projectPath "path/to/project" \
  -buildTarget Android -executeMethod BuildScript.BuildAndroid
```

### 4. Build Script Example
```csharp
public class BuildScript
{
    public static void BuildAndroid()
    {
        BuildPlayerOptions buildOptions = new BuildPlayerOptions();
        buildOptions.scenes = new[] { "Assets/Scenes/JarvisARScene.unity" };
        buildOptions.locationPathName = "Builds/JarvisAR.apk";
        buildOptions.target = BuildTarget.Android;
        buildOptions.options = BuildOptions.None;
        
        BuildPipeline.BuildPlayer(buildOptions);
    }
}
```

## Deployment

### 1. Direct Installation
```bash
# Enable Developer Options on Android device
# Enable USB Debugging
# Connect device via USB

# Install APK
adb install -r JarvisAR.apk

# Launch app
adb shell am start -n com.xreal.jarvis.ar/.UnityPlayerActivity
```

### 2. XREAL Nebula Integration
1. Install XREAL Nebula app on Android device
2. Connect XREAL Air 2 glasses to device
3. Launch Nebula
4. Install Jarvis AR app through Nebula
5. Launch app from Nebula interface

### 3. Testing Setup
```bash
# Check device connection
adb devices

# Monitor logs
adb logcat -s Unity

# Check app permissions
adb shell dumpsys package com.xreal.jarvis.ar | grep permission
```

## Performance Optimization

### 1. Graphics Settings
- Use OpenGL ES3 for best performance
- Enable GPU Instancing where possible
- Optimize texture sizes for mobile
- Use compressed texture formats

### 2. Memory Management
```csharp
// Limit conversation history
maxConversationHistory = 20;

// Optimize texture memory
mapWidth = 512;
mapHeight = 512;

// Clear unused assets
Resources.UnloadUnusedAssets();
System.GC.Collect();
```

### 3. Battery Optimization
- Limit API call frequency
- Use efficient UI update cycles
- Implement proper sleep/wake handling
- Optimize location update intervals

## Troubleshooting

### Common Issues

#### 1. NRSDK Not Found
```
Error: NRKernal namespace not found
Solution: Import NRSDK package and restart Unity
```

#### 2. Build Failures
```
Error: IL2CPP build failed
Solution: Check Android NDK path in Unity preferences
```

#### 3. Permission Denied
```
Error: Microphone permission denied
Solution: Grant permissions in Android settings
```

#### 4. API Connection Issues
```
Error: OpenAI API call failed
Solution: Check API key and internet connection
```

### Debug Commands
```bash
# Check Unity logs
adb logcat -s Unity

# Monitor system resources
adb shell top -p com.xreal.jarvis.ar

# Check network connectivity
adb shell ping google.com

# Restart ADB if connection issues
adb kill-server && adb start-server
```

## AR UI Guidelines

### 1. Text Placement
- Keep text within 60° field of view
- Use minimum 14pt font size
- Maintain 1-2 meter distance from user
- Ensure high contrast for readability

### 2. Panel Layout
```csharp
// Comfortable viewing distances
float panelDistance = 1.5f; // meters
float panelHeight = 0f;     // eye level
float maxPanelWidth = 1.2f; // meters
```

### 3. Color Guidelines
- Use high contrast colors
- Avoid pure white (use off-white)
- Test in various lighting conditions
- Consider colorblind accessibility

### 4. Animation
- Keep animations smooth (60fps target)
- Use easing for comfortable transitions
- Avoid rapid flashing or strobing
- Implement fade-in/out for new content

## Performance Monitoring

### 1. Frame Rate
```csharp
// Monitor FPS
float fps = 1.0f / Time.deltaTime;
if (fps < 30) {
    // Reduce quality settings
}
```

### 2. Memory Usage
```csharp
// Check memory usage
long memoryUsage = System.GC.GetTotalMemory(false);
if (memoryUsage > maxMemoryThreshold) {
    // Clean up resources
}
```

### 3. Network Performance
```csharp
// Monitor API response times
float responseTime = Time.time - requestStartTime;
if (responseTime > 5f) {
    // Show timeout warning
}
```

## Distribution

### 1. Internal Testing
- Use Android App Bundle (.aab) format
- Test on multiple Android devices
- Verify XREAL Air 2 compatibility
- Test in various environments

### 2. App Store Preparation
- Create app icons and screenshots
- Write app description
- Set up app store listing
- Configure in-app purchases if needed

### 3. Updates
- Use incremental builds for faster deployment
- Implement auto-update checking
- Maintain backward compatibility
- Test updates thoroughly

## Security Considerations

### 1. API Key Protection
- Never commit API keys to version control
- Use secure storage for production keys
- Implement key rotation if compromised
- Monitor API usage for anomalies

### 2. User Privacy
- Request minimal permissions
- Explain permission usage to users
- Implement data encryption
- Follow GDPR/privacy regulations

### 3. Network Security
- Use HTTPS for all API calls
- Implement certificate pinning
- Validate server responses
- Handle network errors gracefully

## Support and Maintenance

### 1. Logging
```csharp
// Implement comprehensive logging
Debug.Log($"[{GetType().Name}] {message}");

// Use different log levels
Debug.LogWarning("Warning message");
Debug.LogError("Error message");
```

### 2. Crash Reporting
- Implement crash reporting system
- Monitor app stability metrics
- Collect user feedback
- Regular bug fix releases

### 3. Analytics
- Track feature usage
- Monitor performance metrics
- Analyze user behavior
- Optimize based on data

This guide provides a complete workflow for building and deploying the XREAL Air 2 Jarvis AR Assistant. Follow each step carefully and test thoroughly before production deployment.