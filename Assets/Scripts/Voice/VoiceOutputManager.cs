using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace XREALJarvis.Voice
{
    /// <summary>
    /// Manages voice output using Android Text-to-Speech
    /// Handles speech synthesis and audio playback through XREAL Air 2 speakers
    /// </summary>
    public class VoiceOutputManager : MonoBehaviour
    {
        [Header("TTS Settings")]
        [SerializeField] private string language = "en-US";
        [SerializeField] private float speechRate = 1.0f;
        [SerializeField] private float pitch = 1.0f;
        [SerializeField] private float volume = 0.8f;

        [Header("Voice Characteristics")]
        [SerializeField] private bool useNeuralVoice = true;
        [SerializeField] private string preferredVoice = ""; // Leave empty for default

        // Events
        public Action OnSpeechStarted;
        public Action OnSpeechFinished;
        public Action<string> OnSpeechError;

        // State
        private bool isInitialized = false;
        private bool isSpeaking = false;
        private Queue<string> speechQueue = new Queue<string>();
        private bool isProcessingQueue = false;

        // Android TTS objects
        private AndroidJavaObject textToSpeech;
        private AndroidJavaObject currentActivity;
        private AndroidJavaClass unityPlayer;

        public bool IsSpeaking => isSpeaking;

        public void Initialize()
        {
            StartCoroutine(InitializeAndroidTTS());
        }

        private IEnumerator InitializeAndroidTTS()
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                // Get Unity activity
                unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                // Initialize TTS
                InitializeTextToSpeech();
                
                // Wait for TTS initialization
                yield return new WaitForSeconds(1f);
                
                // Configure TTS settings
                ConfigureTTSSettings();
#endif
                isInitialized = true;
                Debug.Log("[VoiceOutputManager] Initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceOutputManager] Initialization failed: {e.Message}");
                OnSpeechError?.Invoke($"Voice output initialization failed: {e.Message}");
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void InitializeTextToSpeech()
        {
            try
            {
                // Create TTS instance
                AndroidJavaClass ttsClass = new AndroidJavaClass("android.speech.tts.TextToSpeech");
                AndroidJavaObject listener = new AndroidJavaObject("com.xreal.jarvis.TTSInitListener");
                
                textToSpeech = new AndroidJavaObject("android.speech.tts.TextToSpeech", 
                    currentActivity, listener);
                
                Debug.Log("[VoiceOutputManager] TextToSpeech initialized");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceOutputManager] Failed to initialize TTS: {e.Message}");
            }
        }

        private void ConfigureTTSSettings()
        {
            try
            {
                // Set language
                AndroidJavaClass locale = new AndroidJavaClass("java.util.Locale");
                AndroidJavaObject languageLocale = locale.CallStatic<AndroidJavaObject>("forLanguageTag", language);
                textToSpeech.Call<int>("setLanguage", languageLocale);

                // Set speech rate
                textToSpeech.Call<int>("setSpeechRate", speechRate);

                // Set pitch
                textToSpeech.Call<int>("setPitch", pitch);

                // Configure audio attributes for XREAL Air 2
                ConfigureAudioAttributes();

                Debug.Log("[VoiceOutputManager] TTS settings configured");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceOutputManager] Failed to configure TTS: {e.Message}");
            }
        }

        private void ConfigureAudioAttributes()
        {
            try
            {
                // Create AudioAttributes for optimal XREAL Air 2 playback
                AndroidJavaClass audioAttributesBuilder = new AndroidJavaClass("android.media.AudioAttributes$Builder");
                AndroidJavaObject builder = new AndroidJavaObject("android.media.AudioAttributes$Builder");
                
                // Set usage for media/assistant
                AndroidJavaClass audioAttributes = new AndroidJavaClass("android.media.AudioAttributes");
                int usageAssistant = audioAttributes.GetStatic<int>("USAGE_ASSISTANT");
                builder.Call<AndroidJavaObject>("setUsage", usageAssistant);
                
                // Set content type for speech
                int contentTypeSpeech = audioAttributes.GetStatic<int>("CONTENT_TYPE_SPEECH");
                builder.Call<AndroidJavaObject>("setContentType", contentTypeSpeech);
                
                AndroidJavaObject audioAttrs = builder.Call<AndroidJavaObject>("build");
                
                // Apply to TTS
                textToSpeech.Call<int>("setAudioAttributes", audioAttrs);
                
                Debug.Log("[VoiceOutputManager] Audio attributes configured for XREAL Air 2");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VoiceOutputManager] Could not configure audio attributes: {e.Message}");
            }
        }
#endif

        public void Speak(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            Debug.Log($"[VoiceOutputManager] Queuing speech: {text}");
            speechQueue.Enqueue(text);

            if (!isProcessingQueue)
            {
                StartCoroutine(ProcessSpeechQueue());
            }
        }

        private IEnumerator ProcessSpeechQueue()
        {
            isProcessingQueue = true;

            while (speechQueue.Count > 0)
            {
                string textToSpeak = speechQueue.Dequeue();
                yield return StartCoroutine(SpeakText(textToSpeak));
                
                // Small delay between queued speeches
                yield return new WaitForSeconds(0.2f);
            }

            isProcessingQueue = false;
        }

        private IEnumerator SpeakText(string text)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[VoiceOutputManager] TTS not initialized, cannot speak");
                yield break;
            }

            isSpeaking = true;
            OnSpeechStarted?.Invoke();

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // Prepare speech parameters
                AndroidJavaObject params = new AndroidJavaObject("android.os.Bundle");
                params.Call("putFloat", "volume", volume);
                
                // Start speaking
                int result = textToSpeech.Call<int>("speak", text, 0, params, "jarvis_speech");
                
                if (result != 0) // TextToSpeech.SUCCESS = 0
                {
                    Debug.LogError($"[VoiceOutputManager] TTS speak failed with result: {result}");
                    OnSpeechError?.Invoke("Failed to start speech synthesis");
                    isSpeaking = false;
                    yield break;
                }

                // Wait for speech to complete
                while (textToSpeech.Call<bool>("isSpeaking"))
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceOutputManager] Error during speech: {e.Message}");
                OnSpeechError?.Invoke($"Speech error: {e.Message}");
            }
#else
            // Editor simulation
            Debug.Log($"[VoiceOutputManager] [SIMULATED SPEECH]: {text}");
            float simulatedDuration = Mathf.Max(2f, text.Length * 0.05f); // Rough estimation
            yield return new WaitForSeconds(simulatedDuration);
#endif

            isSpeaking = false;
            OnSpeechFinished?.Invoke();
            Debug.Log("[VoiceOutputManager] Speech completed");
        }

        public void StopSpeaking()
        {
            if (!isInitialized || !isSpeaking) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                textToSpeech?.Call<int>("stop");
                Debug.Log("[VoiceOutputManager] Speech stopped");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceOutputManager] Error stopping speech: {e.Message}");
            }
#endif

            // Clear queue and reset state
            speechQueue.Clear();
            isSpeaking = false;
            isProcessingQueue = false;
            OnSpeechFinished?.Invoke();
        }

        public void SetSpeechRate(float rate)
        {
            speechRate = Mathf.Clamp(rate, 0.1f, 3.0f);
            
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                textToSpeech?.Call<int>("setSpeechRate", speechRate);
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceOutputManager] Error setting speech rate: {e.Message}");
            }
#endif
        }

        public void SetPitch(float newPitch)
        {
            pitch = Mathf.Clamp(newPitch, 0.1f, 2.0f);
            
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                textToSpeech?.Call<int>("setPitch", pitch);
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceOutputManager] Error setting pitch: {e.Message}");
            }
#endif
        }

        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
        }

        // Called from Android native code
        public void OnTTSInitialized(int status)
        {
            if (status == 0) // TextToSpeech.SUCCESS
            {
                Debug.Log("[VoiceOutputManager] TTS initialization successful");
                ConfigureTTSSettings();
            }
            else
            {
                Debug.LogError($"[VoiceOutputManager] TTS initialization failed with status: {status}");
                OnSpeechError?.Invoke("TTS initialization failed");
            }
        }

        // Called from Android native code
        public void OnTTSError(string error)
        {
            Debug.LogError($"[VoiceOutputManager] TTS error: {error}");
            OnSpeechError?.Invoke(error);
            isSpeaking = false;
        }

        public bool HasQueuedSpeech()
        {
            return speechQueue.Count > 0 || isSpeaking;
        }

        public void ClearSpeechQueue()
        {
            speechQueue.Clear();
        }

        private void OnDestroy()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                textToSpeech?.Call("shutdown");
                Debug.Log("[VoiceOutputManager] TTS shutdown complete");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceOutputManager] Error shutting down TTS: {e.Message}");
            }
#endif
        }

        // Public properties for debugging
        public int QueuedSpeechCount => speechQueue.Count;
        public float CurrentSpeechRate => speechRate;
        public float CurrentPitch => pitch;
        public float CurrentVolume => volume;
    }
}