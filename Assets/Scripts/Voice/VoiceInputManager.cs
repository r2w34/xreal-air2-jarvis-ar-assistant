using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace XREALJarvis.Voice
{
    /// <summary>
    /// Manages voice input using Android Speech-to-Text
    /// Handles wake word detection and voice command capture
    /// </summary>
    public class VoiceInputManager : MonoBehaviour
    {
        [Header("Voice Settings")]
        [SerializeField] private string language = "en-US";
        [SerializeField] private float silenceTimeout = 2f;
        [SerializeField] private float maxRecordingTime = 10f;
        [SerializeField] private bool enableWakeWordDetection = true;
        [SerializeField] private string[] wakeWords = { "hey jarvis", "jarvis" };

        [Header("Audio Settings")]
        [SerializeField] private float microphoneVolume = 1f;
        [SerializeField] private int sampleRate = 44100;

        // Events
        public Action OnWakeWordDetected;
        public Action<string> OnSpeechRecognized;
        public Action OnListeningStarted;
        public Action OnListeningStopped;
        public Action<string> OnError;

        // State
        private bool isInitialized = false;
        private bool isListening = false;
        private bool isCapturing = false;
        private string lastRecognizedText = "";
        
        // Android objects
        private AndroidJavaObject speechRecognizer;
        private AndroidJavaObject currentActivity;
        private AndroidJavaClass unityPlayer;

        public bool HasCapturedSpeech { get; private set; }

        public void Initialize()
        {
            StartCoroutine(InitializeAndroidSTT());
        }

        private IEnumerator InitializeAndroidSTT()
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                // Get Unity activity
                unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                // Check for microphone permission
                if (!HasMicrophonePermission())
                {
                    RequestMicrophonePermission();
                    yield return new WaitForSeconds(2f);
                }

                // Initialize speech recognizer
                InitializeSpeechRecognizer();
#endif
                isInitialized = true;
                Debug.Log("[VoiceInputManager] Initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceInputManager] Initialization failed: {e.Message}");
                OnError?.Invoke($"Voice input initialization failed: {e.Message}");
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void InitializeSpeechRecognizer()
        {
            try
            {
                AndroidJavaClass speechRecognizerClass = new AndroidJavaClass("android.speech.SpeechRecognizer");
                speechRecognizer = speechRecognizerClass.CallStatic<AndroidJavaObject>("createSpeechRecognizer", currentActivity);
                
                // Create recognition listener
                AndroidJavaObject listener = new AndroidJavaObject("com.xreal.jarvis.SpeechRecognitionListener");
                speechRecognizer.Call("setRecognitionListener", listener);
                
                Debug.Log("[VoiceInputManager] Speech recognizer initialized");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceInputManager] Failed to initialize speech recognizer: {e.Message}");
            }
        }

        private bool HasMicrophonePermission()
        {
            AndroidJavaClass contextCompat = new AndroidJavaClass("androidx.core.content.ContextCompat");
            int permissionResult = contextCompat.CallStatic<int>("checkSelfPermission", 
                currentActivity, "android.permission.RECORD_AUDIO");
            return permissionResult == 0; // PackageManager.PERMISSION_GRANTED
        }

        private void RequestMicrophonePermission()
        {
            AndroidJavaClass activityCompat = new AndroidJavaClass("androidx.core.app.ActivityCompat");
            string[] permissions = { "android.permission.RECORD_AUDIO" };
            activityCompat.CallStatic("requestPermissions", currentActivity, permissions, 1);
        }
#endif

        public void StartListening()
        {
            if (!isInitialized || isListening) return;

            StartCoroutine(StartListeningCoroutine());
        }

        private IEnumerator StartListeningCoroutine()
        {
            isListening = true;
            OnListeningStarted?.Invoke();

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaObject intent = CreateRecognitionIntent();
                speechRecognizer.Call("startListening", intent);
                Debug.Log("[VoiceInputManager] Started listening for speech");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceInputManager] Failed to start listening: {e.Message}");
                OnError?.Invoke($"Failed to start voice recognition: {e.Message}");
                isListening = false;
            }
#else
            // Editor simulation
            yield return new WaitForSeconds(2f);
            SimulateVoiceInput("Hey Jarvis, what's the weather like today?");
#endif
            yield return null;
        }

        public void StopListening()
        {
            if (!isListening) return;

            isListening = false;
            OnListeningStopped?.Invoke();

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                speechRecognizer?.Call("stopListening");
                Debug.Log("[VoiceInputManager] Stopped listening");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceInputManager] Error stopping listening: {e.Message}");
            }
#endif
        }

        public void StartVoiceCapture()
        {
            if (!isInitialized || isCapturing) return;

            StartCoroutine(CaptureVoiceCoroutine());
        }

        private IEnumerator CaptureVoiceCoroutine()
        {
            isCapturing = true;
            HasCapturedSpeech = false;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaObject intent = CreateRecognitionIntent();
                intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.MAX_RESULTS", 1);
                speechRecognizer.Call("startListening", intent);
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceInputManager] Failed to start voice capture: {e.Message}");
                OnError?.Invoke($"Failed to capture voice: {e.Message}");
                isCapturing = false;
            }
#else
            // Editor simulation
            yield return new WaitForSeconds(1f);
            SimulateVoiceCapture("Show me directions to the nearest coffee shop");
#endif

            // Wait for capture completion or timeout
            float timeout = maxRecordingTime;
            while (isCapturing && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (timeout <= 0)
            {
                StopVoiceCapture();
            }
        }

        public void StopVoiceCapture()
        {
            if (!isCapturing) return;

            isCapturing = false;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                speechRecognizer?.Call("stopListening");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceInputManager] Error stopping voice capture: {e.Message}");
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject CreateRecognitionIntent()
        {
            AndroidJavaClass recognizerIntent = new AndroidJavaClass("android.speech.RecognizerIntent");
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", 
                recognizerIntent.GetStatic<string>("ACTION_RECOGNIZE_SPEECH"));
            
            intent.Call<AndroidJavaObject>("putExtra", 
                recognizerIntent.GetStatic<string>("EXTRA_LANGUAGE_MODEL"),
                recognizerIntent.GetStatic<string>("LANGUAGE_MODEL_FREE_FORM"));
            
            intent.Call<AndroidJavaObject>("putExtra", 
                recognizerIntent.GetStatic<string>("EXTRA_LANGUAGE"), language);
            
            intent.Call<AndroidJavaObject>("putExtra", 
                recognizerIntent.GetStatic<string>("EXTRA_PARTIAL_RESULTS"), true);
            
            return intent;
        }
#endif

        // Called from Android native code
        public void OnSpeechRecognitionResult(string result)
        {
            if (string.IsNullOrEmpty(result)) return;

            lastRecognizedText = result.ToLower();
            Debug.Log($"[VoiceInputManager] Speech recognized: {result}");

            if (enableWakeWordDetection && ContainsWakeWord(lastRecognizedText))
            {
                OnWakeWordDetected?.Invoke();
            }
            else if (isCapturing)
            {
                HasCapturedSpeech = true;
                isCapturing = false;
                OnSpeechRecognized?.Invoke(result);
            }
        }

        // Called from Android native code
        public void OnSpeechRecognitionError(string error)
        {
            Debug.LogError($"[VoiceInputManager] Speech recognition error: {error}");
            OnError?.Invoke(error);
            
            isListening = false;
            isCapturing = false;
        }

        private bool ContainsWakeWord(string text)
        {
            foreach (string wakeWord in wakeWords)
            {
                if (text.Contains(wakeWord.ToLower()))
                    return true;
            }
            return false;
        }

        // Editor simulation methods
        private void SimulateVoiceInput(string text)
        {
#if UNITY_EDITOR
            StartCoroutine(SimulateVoiceInputCoroutine(text));
#endif
        }

        private void SimulateVoiceCapture(string text)
        {
#if UNITY_EDITOR
            StartCoroutine(SimulateVoiceCaptureCoroutine(text));
#endif
        }

#if UNITY_EDITOR
        private IEnumerator SimulateVoiceInputCoroutine(string text)
        {
            yield return new WaitForSeconds(1f);
            OnSpeechRecognitionResult(text);
        }

        private IEnumerator SimulateVoiceCaptureCoroutine(string text)
        {
            yield return new WaitForSeconds(1f);
            HasCapturedSpeech = true;
            isCapturing = false;
            OnSpeechRecognized?.Invoke(text);
        }
#endif

        private void OnDestroy()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                speechRecognizer?.Call("destroy");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceInputManager] Error destroying speech recognizer: {e.Message}");
            }
#endif
        }

        // Public properties for debugging
        public bool IsListening => isListening;
        public bool IsCapturing => isCapturing;
        public string LastRecognizedText => lastRecognizedText;
    }
}