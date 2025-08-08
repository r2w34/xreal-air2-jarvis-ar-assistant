# XREAL Air 2 Jarvis AR Assistant - Developer Guide

## Overview

This Unity project creates a complete AR assistant application for XREAL Air 2 smart glasses, featuring:
- Jarvis-like AI voice assistant powered by OpenAI ChatGPT
- 100% hands-free voice interaction
- Real-time map navigation overlays
- AR-optimized UI with floating panels
- Integration with Android speech services

## Architecture

### Core Components

#### 1. JarvisManager (Core/JarvisManager.cs)
**Purpose**: Main orchestrator for the entire system
**Key Features**:
- Coordinates all subsystems
- Manages application state
- Handles voice command flow
- Provides centralized event handling

```csharp
// Usage Example
var jarvisManager = FindObjectOfType<JarvisManager>();
jarvisManager.OnUserSpeechDetected += HandleUserSpeech;
jarvisManager.OnAIResponseReceived += HandleAIResponse;
```

#### 2. VoiceInputManager (Voice/VoiceInputManager.cs)
**Purpose**: Handles speech-to-text and wake word detection
**Key Features**:
- Android Speech Recognition integration
- Wake word detection ("Hey Jarvis")
- Continuous listening mode
- Voice command capture

```csharp
// Configuration
voiceInputManager.language = "en-US";
voiceInputManager.wakeWords = new string[] { "hey jarvis", "jarvis" };
voiceInputManager.StartListening();
```

#### 3. VoiceOutputManager (Voice/VoiceOutputManager.cs)
**Purpose**: Text-to-speech synthesis and audio output
**Key Features**:
- Android TTS integration
- Speech queue management
- XREAL Air 2 audio optimization
- Voice characteristics control

```csharp
// Usage
voiceOutputManager.Speak("Hello, how can I help you?");
voiceOutputManager.SetSpeechRate(1.2f);
voiceOutputManager.SetPitch(1.1f);
```

#### 4. ChatGPTManager (AI/ChatGPTManager.cs)
**Purpose**: OpenAI API integration and conversation management
**Key Features**:
- ChatGPT API calls with context retention
- Conversation history management
- Response processing for AR display
- Error handling and rate limiting

```csharp
// Configuration
chatGPTManager.apiKey = "your-openai-key";
chatGPTManager.model = "gpt-4o-mini";
chatGPTManager.maxTokens = 500;
chatGPTManager.SendMessage("What's the weather like?");
```

#### 5. MapManager (Maps/MapManager.cs)
**Purpose**: GPS navigation and map integration
**Key Features**:
- Google Maps/Mapbox integration
- Real-time location tracking
- Navigation arrow rendering
- Route calculation and display

```csharp
// Usage
mapManager.ProcessMapRequest("Navigate to coffee shop");
mapManager.ShowCurrentLocationMap();
```

#### 6. UIManager (UI/UIManager.cs)
**Purpose**: AR user interface management
**Key Features**:
- World-space canvas management
- Chat message display
- Status indicators
- AR-optimized layouts

```csharp
// UI Updates
uiManager.AddChatMessage("Hello Jarvis", true);  // User message
uiManager.AddChatMessage("Hi there!", false);   // AI response
uiManager.ShowThinkingIndicator(true);
```

### System Flow

```
1. User says "Hey Jarvis" → VoiceInputManager detects wake word
2. JarvisManager starts voice capture → VoiceInputManager captures command
3. Speech recognized → JarvisManager sends to ChatGPTManager
4. AI processes request → ChatGPTManager returns response
5. Response displayed in UI → UIManager shows chat bubble
6. Response spoken aloud → VoiceOutputManager synthesizes speech
7. System returns to listening → Cycle repeats
```

## Configuration

### API Keys Setup

#### Method 1: StreamingAssets (Recommended)
```json
// Assets/StreamingAssets/config.json
{
  "openai_api_key": "sk-your-key-here",
  "google_maps_api_key": "your-maps-key",
  "mapbox_api_key": "your-mapbox-key"
}
```

#### Method 2: Inspector Assignment
- Assign keys directly in Unity Inspector
- Useful for development/testing

#### Method 3: Runtime Configuration
```csharp
ConfigManager.Instance.SetAPIKey("openai", "your-key");
ConfigManager.Instance.SetAPIKey("googlemaps", "your-key");
```

### Voice Settings
```json
{
  "speech_language": "en-US",
  "speech_rate": 1.0,
  "speech_pitch": 1.0,
  "wake_words": ["hey jarvis", "jarvis"]
}
```

### AR UI Settings
```json
{
  "panel_distance": 1.5,
  "use_body_anchor": true,
  "max_chat_messages": 50
}
```

## AR UI Guidelines

### Positioning
- **Distance**: 1-2 meters from user for comfortable viewing
- **Height**: At eye level (0m offset) or slightly below
- **Angle**: Panels should face the user directly
- **FOV**: Keep content within 60° field of view

### Typography
```csharp
// Recommended settings
fontSize = 24f;           // Minimum for AR readability
fontColor = Color.white;  // High contrast
backgroundColor = new Color(0, 0, 0, 0.8f); // Semi-transparent
```

### Layout Principles
- **Chat Panel**: Center position for primary interaction
- **Map Panel**: Right side for navigation
- **Status Panel**: Top for system indicators
- **Info Panel**: Left side for contextual information

### Animation Guidelines
```csharp
// Smooth transitions
float fadeInDuration = 0.3f;
LeanTween.alpha(panel, 1f, fadeInDuration).setEase(LeanTweenType.easeOutQuad);

// Avoid jarring movements
transform.position = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * smoothness);
```

## Performance Optimization

### Memory Management
```csharp
// Limit conversation history
maxConversationHistory = 20;

// Clear unused resources
Resources.UnloadUnusedAssets();
System.GC.Collect();

// Optimize texture sizes
mapWidth = 512;
mapHeight = 512;
```

### Frame Rate Optimization
```csharp
// Target 60 FPS
Application.targetFrameRate = 60;

// Use object pooling for UI elements
ObjectPool<ChatBubble> chatBubblePool;

// Limit update frequencies
if (Time.time - lastUpdate > updateInterval)
{
    UpdateUI();
    lastUpdate = Time.time;
}
```

### Network Optimization
```csharp
// Rate limiting
const float MIN_REQUEST_INTERVAL = 1f;
if (Time.time - lastRequestTime < MIN_REQUEST_INTERVAL) return;

// Request timeout
webRequest.timeout = 30;

// Compress requests
request.SetRequestHeader("Accept-Encoding", "gzip");
```

## Testing and Debugging

### Editor Testing
```csharp
#if UNITY_EDITOR
// Simulate voice input
if (Input.GetKeyDown(KeyCode.Space))
{
    SimulateVoiceInput("Hey Jarvis, what time is it?");
}

// Mock API responses
if (useSimulatedResponses)
{
    return "This is a simulated AI response.";
}
#endif
```

### Device Testing
```bash
# Monitor Unity logs
adb logcat -s Unity

# Check app performance
adb shell dumpsys meminfo com.xreal.jarvis.ar

# Test permissions
adb shell pm list permissions com.xreal.jarvis.ar
```

### Common Issues

#### 1. Voice Recognition Not Working
```csharp
// Check permissions
if (!AndroidBridge.Instance.HasPermission("android.permission.RECORD_AUDIO"))
{
    AndroidBridge.Instance.RequestPermission("android.permission.RECORD_AUDIO");
}

// Verify microphone access
if (!Microphone.devices.Any())
{
    Debug.LogError("No microphone devices found");
}
```

#### 2. API Calls Failing
```csharp
// Validate API key
if (string.IsNullOrEmpty(apiKey))
{
    Debug.LogError("API key not configured");
    return;
}

// Check network connectivity
if (Application.internetReachability == NetworkReachability.NotReachable)
{
    Debug.LogError("No internet connection");
    return;
}
```

#### 3. AR UI Not Visible
```csharp
// Check camera reference
if (arCamera == null)
{
    arCamera = NRSessionManager.Instance.NRHMDPoseTracker.centerCamera;
}

// Verify canvas settings
canvas.renderMode = RenderMode.WorldSpace;
canvas.worldCamera = arCamera;
```

## Extending the System

### Adding New Voice Commands
```csharp
// In JarvisManager.cs
private bool ProcessSpecialCommands(string userInput)
{
    string lowerInput = userInput.ToLower();
    
    if (lowerInput.Contains("take a screenshot"))
    {
        TakeScreenshot();
        return true;
    }
    
    if (lowerInput.Contains("increase volume"))
    {
        AdjustVolume(0.1f);
        return true;
    }
    
    return false; // Let AI handle it
}
```

### Custom AI Prompts
```csharp
// Specialized system prompts
string navigationPrompt = "You are a navigation assistant. Provide clear, concise directions.";
string weatherPrompt = "You are a weather assistant. Give current conditions and forecasts.";

// Context-aware prompting
if (userInput.Contains("navigate") || userInput.Contains("directions"))
{
    chatGPTManager.SetSystemPrompt(navigationPrompt);
}
```

### New UI Panels
```csharp
public class WeatherPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI temperatureText;
    [SerializeField] private TextMeshProUGUI conditionsText;
    [SerializeField] private Image weatherIcon;
    
    public void UpdateWeather(WeatherData data)
    {
        temperatureText.text = $"{data.temperature}°C";
        conditionsText.text = data.conditions;
        weatherIcon.sprite = data.icon;
    }
}
```

## Security Best Practices

### API Key Protection
```csharp
// Never hardcode API keys
// ❌ Bad
string apiKey = "sk-1234567890abcdef";

// ✅ Good
string apiKey = ConfigManager.Instance.OpenAIAPIKey;
```

### Input Validation
```csharp
// Sanitize user input
private string SanitizeInput(string input)
{
    if (string.IsNullOrWhiteSpace(input)) return "";
    
    // Remove potentially harmful content
    input = input.Trim();
    input = Regex.Replace(input, @"[^\w\s\.\?\!]", "");
    
    // Limit length
    if (input.Length > 500)
        input = input.Substring(0, 500);
    
    return input;
}
```

### Network Security
```csharp
// Use HTTPS only
const string API_BASE_URL = "https://api.openai.com";

// Validate certificates
webRequest.certificateHandler = new CustomCertificateHandler();

// Implement timeout
webRequest.timeout = 30;
```

## Deployment Checklist

### Pre-Build
- [ ] API keys configured
- [ ] NRSDK imported and configured
- [ ] Android permissions set
- [ ] Target API level set to 29+
- [ ] IL2CPP backend selected
- [ ] ARM64 architecture selected

### Build Settings
- [ ] Development build disabled for release
- [ ] Script debugging disabled for release
- [ ] Compression method set to LZ4HC
- [ ] Graphics API set to OpenGL ES3

### Post-Build Testing
- [ ] Install on target device
- [ ] Test voice recognition
- [ ] Verify API connectivity
- [ ] Test AR UI positioning
- [ ] Check performance metrics
- [ ] Validate XREAL Air 2 compatibility

## Troubleshooting Guide

### Build Issues
```bash
# Clear Unity cache
rm -rf Library/
rm -rf Temp/

# Reimport NRSDK
# Delete NRSDK folder and reimport package

# Check Android SDK path
# Unity → Preferences → External Tools
```

### Runtime Issues
```csharp
// Enable verbose logging
Debug.unityLogger.logEnabled = true;
Debug.unityLogger.filterLogType = LogType.Log;

// Monitor memory usage
long memoryUsage = System.GC.GetTotalMemory(false);
Debug.Log($"Memory usage: {memoryUsage / 1024 / 1024} MB");

// Check frame rate
float fps = 1.0f / Time.unscaledDeltaTime;
if (fps < 30) Debug.LogWarning($"Low FPS: {fps:F1}");
```

## Contributing

### Code Style
- Use meaningful variable names
- Add XML documentation to public methods
- Follow Unity naming conventions
- Use regions to organize code sections

### Testing
- Test on multiple Android devices
- Verify XREAL Air 2 compatibility
- Test in various lighting conditions
- Validate voice recognition accuracy

### Documentation
- Update this guide when adding features
- Document API changes
- Include code examples
- Maintain changelog

This developer guide provides comprehensive information for working with the XREAL Air 2 Jarvis AR Assistant. For additional support, refer to the XREAL SDK documentation and Unity AR development resources.