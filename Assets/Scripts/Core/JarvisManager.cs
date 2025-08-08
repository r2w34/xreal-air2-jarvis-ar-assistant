using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NRKernal;

namespace XREALJarvis.Core
{
    /// <summary>
    /// Main manager for the Jarvis AR Assistant system
    /// Coordinates all subsystems and manages the overall application state
    /// </summary>
    public class JarvisManager : MonoBehaviour
    {
        [Header("System Components")]
        [SerializeField] private VoiceInputManager voiceInputManager;
        [SerializeField] private ChatGPTManager chatGPTManager;
        [SerializeField] private VoiceOutputManager voiceOutputManager;
        [SerializeField] private MapManager mapManager;
        [SerializeField] private UIManager uiManager;

        [Header("Configuration")]
        [SerializeField] private string wakeWord = "Hey Jarvis";
        [SerializeField] private float voiceTimeoutSeconds = 5f;
        [SerializeField] private bool enableDebugLogs = true;

        // System state
        private bool isListening = false;
        private bool isProcessing = false;
        private bool isSystemReady = false;

        // Events
        public System.Action<string> OnUserSpeechDetected;
        public System.Action<string> OnAIResponseReceived;
        public System.Action<bool> OnSystemStateChanged;

        private void Start()
        {
            InitializeSystem();
        }

        private void InitializeSystem()
        {
            StartCoroutine(InitializeSystemCoroutine());
        }

        private IEnumerator InitializeSystemCoroutine()
        {
            LogDebug("Initializing Jarvis AR Assistant...");

            // Wait for NRSDK to initialize
            yield return new WaitUntil(() => NRSessionManager.Instance.NRSessionBehaviour != null);
            yield return new WaitForSeconds(1f);

            // Initialize subsystems
            if (voiceInputManager != null)
            {
                voiceInputManager.Initialize();
                voiceInputManager.OnSpeechRecognized += HandleSpeechRecognized;
                voiceInputManager.OnWakeWordDetected += HandleWakeWordDetected;
            }

            if (chatGPTManager != null)
            {
                chatGPTManager.Initialize();
                chatGPTManager.OnResponseReceived += HandleAIResponse;
                chatGPTManager.OnError += HandleAIError;
            }

            if (voiceOutputManager != null)
            {
                voiceOutputManager.Initialize();
                voiceOutputManager.OnSpeechFinished += HandleSpeechFinished;
            }

            if (mapManager != null)
            {
                mapManager.Initialize();
            }

            if (uiManager != null)
            {
                uiManager.Initialize();
                uiManager.ShowWelcomeMessage();
            }

            isSystemReady = true;
            OnSystemStateChanged?.Invoke(true);
            LogDebug("Jarvis AR Assistant initialized successfully!");

            // Start listening for wake word
            StartListening();
        }

        public void StartListening()
        {
            if (!isSystemReady || isListening) return;

            isListening = true;
            voiceInputManager?.StartListening();
            uiManager?.ShowListeningIndicator(true);
            LogDebug("Started listening for wake word...");
        }

        public void StopListening()
        {
            if (!isListening) return;

            isListening = false;
            voiceInputManager?.StopListening();
            uiManager?.ShowListeningIndicator(false);
            LogDebug("Stopped listening.");
        }

        private void HandleWakeWordDetected()
        {
            if (isProcessing) return;

            LogDebug("Wake word detected!");
            uiManager?.ShowWakeWordDetected();
            
            // Start voice command capture
            StartCoroutine(CaptureVoiceCommand());
        }

        private IEnumerator CaptureVoiceCommand()
        {
            uiManager?.ShowVoiceCapture(true);
            voiceInputManager?.StartVoiceCapture();

            float timeout = voiceTimeoutSeconds;
            while (timeout > 0 && !voiceInputManager.HasCapturedSpeech)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            voiceInputManager?.StopVoiceCapture();
            uiManager?.ShowVoiceCapture(false);

            if (timeout <= 0)
            {
                LogDebug("Voice capture timeout");
                uiManager?.ShowMessage("I didn't hear anything. Try again.");
                StartListening();
            }
        }

        private void HandleSpeechRecognized(string speechText)
        {
            if (string.IsNullOrEmpty(speechText)) return;

            LogDebug($"Speech recognized: {speechText}");
            OnUserSpeechDetected?.Invoke(speechText);

            // Show user's speech in UI
            uiManager?.AddChatMessage(speechText, true);

            // Process with AI
            ProcessUserInput(speechText);
        }

        private void ProcessUserInput(string userInput)
        {
            if (isProcessing) return;

            isProcessing = true;
            uiManager?.ShowThinkingIndicator(true);

            // Send to ChatGPT
            chatGPTManager?.SendMessage(userInput);
        }

        private void HandleAIResponse(string response)
        {
            isProcessing = false;
            uiManager?.ShowThinkingIndicator(false);

            LogDebug($"AI Response: {response}");
            OnAIResponseReceived?.Invoke(response);

            // Show AI response in UI
            uiManager?.AddChatMessage(response, false);

            // Check if response contains map/navigation request
            if (ContainsMapRequest(response))
            {
                mapManager?.ProcessMapRequest(response);
            }

            // Speak the response
            voiceOutputManager?.Speak(response);
        }

        private void HandleAIError(string error)
        {
            isProcessing = false;
            uiManager?.ShowThinkingIndicator(false);

            LogDebug($"AI Error: {error}");
            string errorMessage = "Sorry, I encountered an error. Please try again.";
            uiManager?.AddChatMessage(errorMessage, false);
            voiceOutputManager?.Speak(errorMessage);

            StartListening();
        }

        private void HandleSpeechFinished()
        {
            LogDebug("Speech finished, resuming listening...");
            StartListening();
        }

        private bool ContainsMapRequest(string text)
        {
            string[] mapKeywords = { "navigate", "directions", "map", "route", "location", "address", "where is" };
            string lowerText = text.ToLower();
            
            foreach (string keyword in mapKeywords)
            {
                if (lowerText.Contains(keyword))
                    return true;
            }
            return false;
        }

        public void ToggleListening()
        {
            if (isListening)
                StopListening();
            else
                StartListening();
        }

        public void EmergencyStop()
        {
            StopListening();
            voiceOutputManager?.StopSpeaking();
            isProcessing = false;
            uiManager?.ShowThinkingIndicator(false);
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[JarvisManager] {message}");
        }

        private void OnDestroy()
        {
            // Cleanup event subscriptions
            if (voiceInputManager != null)
            {
                voiceInputManager.OnSpeechRecognized -= HandleSpeechRecognized;
                voiceInputManager.OnWakeWordDetected -= HandleWakeWordDetected;
            }

            if (chatGPTManager != null)
            {
                chatGPTManager.OnResponseReceived -= HandleAIResponse;
                chatGPTManager.OnError -= HandleAIError;
            }

            if (voiceOutputManager != null)
            {
                voiceOutputManager.OnSpeechFinished -= HandleSpeechFinished;
            }
        }

        private void Update()
        {
            // Handle input for testing (remove in production)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleListening();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EmergencyStop();
            }
        }
    }
}