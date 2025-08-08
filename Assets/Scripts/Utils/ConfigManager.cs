using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace XREALJarvis.Utils
{
    /// <summary>
    /// Manages application configuration and API keys
    /// Handles loading from files, PlayerPrefs, and runtime configuration
    /// </summary>
    public class ConfigManager : MonoBehaviour
    {
        private static ConfigManager instance;
        public static ConfigManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("ConfigManager");
                    instance = go.AddComponent<ConfigManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("Configuration Files")]
        [SerializeField] private string configFileName = "config.json";
        [SerializeField] private bool loadFromStreamingAssets = true;
        [SerializeField] private bool loadFromPersistentData = true;
        [SerializeField] private bool usePlayerPrefs = true;

        // Configuration data
        private JarvisConfig config;
        private bool isLoaded = false;

        // Events
        public Action OnConfigLoaded;
        public Action<string> OnConfigError;

        [System.Serializable]
        public class JarvisConfig
        {
            [Header("API Keys")]
            public string openai_api_key = "";
            public string google_maps_api_key = "";
            public string mapbox_api_key = "";

            [Header("AI Settings")]
            public string ai_model = "gpt-4o-mini";
            public int max_tokens = 500;
            public float temperature = 0.7f;
            public string system_prompt = "You are Jarvis, a helpful AI assistant for AR glasses.";

            [Header("Voice Settings")]
            public string speech_language = "en-US";
            public float speech_rate = 1.0f;
            public float speech_pitch = 1.0f;
            public float speech_volume = 0.8f;
            public string[] wake_words = { "hey jarvis", "jarvis" };

            [Header("UI Settings")]
            public float panel_distance = 1.5f;
            public bool use_body_anchor = true;
            public float fade_duration = 0.3f;
            public int max_chat_messages = 50;

            [Header("Map Settings")]
            public bool use_google_maps = true;
            public int map_width = 512;
            public int map_height = 512;
            public int default_zoom = 15;
            public string map_type = "roadmap";

            [Header("Location Settings")]
            public float location_update_interval = 5f;
            public float location_accuracy = 10f;
            public bool enable_location_services = true;

            [Header("Performance Settings")]
            public int target_frame_rate = 60;
            public bool enable_debug_logs = true;
            public float request_timeout = 30f;
            public float min_request_interval = 1f;
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                LoadConfiguration();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void LoadConfiguration()
        {
            StartCoroutine(LoadConfigurationCoroutine());
        }

        private IEnumerator LoadConfigurationCoroutine()
        {
            config = new JarvisConfig();
            bool configLoaded = false;

            // Try loading from StreamingAssets first
            if (loadFromStreamingAssets)
            {
                string streamingPath = Path.Combine(Application.streamingAssetsPath, configFileName);
                configLoaded = yield return StartCoroutine(LoadFromPath(streamingPath));
            }

            // Try loading from PersistentDataPath if not found
            if (!configLoaded && loadFromPersistentData)
            {
                string persistentPath = Path.Combine(Application.persistentDataPath, configFileName);
                configLoaded = yield return StartCoroutine(LoadFromPath(persistentPath));
            }

            // Load from PlayerPrefs as fallback
            if (!configLoaded && usePlayerPrefs)
            {
                LoadFromPlayerPrefs();
                configLoaded = true;
            }

            // Apply default values if nothing was loaded
            if (!configLoaded)
            {
                Debug.LogWarning("[ConfigManager] No configuration found, using defaults");
                ApplyDefaultConfiguration();
            }

            // Validate configuration
            ValidateConfiguration();

            isLoaded = true;
            OnConfigLoaded?.Invoke();
            Debug.Log("[ConfigManager] Configuration loaded successfully");
        }

        private IEnumerator LoadFromPath(string path)
        {
            try
            {
                string configJson = "";

                if (path.Contains("://") || path.Contains(":///"))
                {
                    // StreamingAssets path - use UnityWebRequest
                    using (UnityEngine.Networking.UnityWebRequest request = 
                           UnityEngine.Networking.UnityWebRequest.Get(path))
                    {
                        yield return request.SendWebRequest();

                        if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                        {
                            configJson = request.downloadHandler.text;
                        }
                        else
                        {
                            Debug.LogWarning($"[ConfigManager] Could not load config from {path}: {request.error}");
                            yield return false;
                        }
                    }
                }
                else
                {
                    // Regular file path
                    if (File.Exists(path))
                    {
                        configJson = File.ReadAllText(path);
                    }
                    else
                    {
                        Debug.LogWarning($"[ConfigManager] Config file not found at {path}");
                        yield return false;
                    }
                }

                if (!string.IsNullOrEmpty(configJson))
                {
                    config = JsonUtility.FromJson<JarvisConfig>(configJson);
                    Debug.Log($"[ConfigManager] Configuration loaded from {path}");
                    yield return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigManager] Error loading config from {path}: {e.Message}");
                OnConfigError?.Invoke($"Failed to load configuration: {e.Message}");
            }

            yield return false;
        }

        private void LoadFromPlayerPrefs()
        {
            try
            {
                // Load API keys
                config.openai_api_key = PlayerPrefs.GetString("OPENAI_API_KEY", config.openai_api_key);
                config.google_maps_api_key = PlayerPrefs.GetString("GOOGLE_MAPS_API_KEY", config.google_maps_api_key);
                config.mapbox_api_key = PlayerPrefs.GetString("MAPBOX_API_KEY", config.mapbox_api_key);

                // Load other settings
                config.ai_model = PlayerPrefs.GetString("AI_MODEL", config.ai_model);
                config.speech_language = PlayerPrefs.GetString("SPEECH_LANGUAGE", config.speech_language);
                config.speech_rate = PlayerPrefs.GetFloat("SPEECH_RATE", config.speech_rate);
                config.panel_distance = PlayerPrefs.GetFloat("PANEL_DISTANCE", config.panel_distance);
                config.use_body_anchor = PlayerPrefs.GetInt("USE_BODY_ANCHOR", config.use_body_anchor ? 1 : 0) == 1;

                Debug.Log("[ConfigManager] Configuration loaded from PlayerPrefs");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigManager] Error loading from PlayerPrefs: {e.Message}");
                ApplyDefaultConfiguration();
            }
        }

        private void ApplyDefaultConfiguration()
        {
            config = new JarvisConfig();
            Debug.Log("[ConfigManager] Applied default configuration");
        }

        private void ValidateConfiguration()
        {
            // Validate and clamp values
            config.temperature = Mathf.Clamp(config.temperature, 0f, 2f);
            config.speech_rate = Mathf.Clamp(config.speech_rate, 0.1f, 3f);
            config.speech_pitch = Mathf.Clamp(config.speech_pitch, 0.1f, 2f);
            config.speech_volume = Mathf.Clamp01(config.speech_volume);
            config.panel_distance = Mathf.Clamp(config.panel_distance, 0.5f, 5f);
            config.max_tokens = Mathf.Clamp(config.max_tokens, 50, 2000);
            config.max_chat_messages = Mathf.Clamp(config.max_chat_messages, 10, 200);

            // Validate API keys
            if (string.IsNullOrEmpty(config.openai_api_key))
            {
                Debug.LogWarning("[ConfigManager] OpenAI API key is missing!");
            }

            if (string.IsNullOrEmpty(config.google_maps_api_key) && string.IsNullOrEmpty(config.mapbox_api_key))
            {
                Debug.LogWarning("[ConfigManager] No map API keys found!");
            }

            // Validate wake words
            if (config.wake_words == null || config.wake_words.Length == 0)
            {
                config.wake_words = new string[] { "hey jarvis", "jarvis" };
            }
        }

        public void SaveConfiguration()
        {
            if (!isLoaded) return;

            try
            {
                string configJson = JsonUtility.ToJson(config, true);
                string savePath = Path.Combine(Application.persistentDataPath, configFileName);
                
                File.WriteAllText(savePath, configJson);
                Debug.Log($"[ConfigManager] Configuration saved to {savePath}");

                // Also save to PlayerPrefs as backup
                SaveToPlayerPrefs();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigManager] Error saving configuration: {e.Message}");
                OnConfigError?.Invoke($"Failed to save configuration: {e.Message}");
            }
        }

        private void SaveToPlayerPrefs()
        {
            try
            {
                PlayerPrefs.SetString("OPENAI_API_KEY", config.openai_api_key);
                PlayerPrefs.SetString("GOOGLE_MAPS_API_KEY", config.google_maps_api_key);
                PlayerPrefs.SetString("MAPBOX_API_KEY", config.mapbox_api_key);
                PlayerPrefs.SetString("AI_MODEL", config.ai_model);
                PlayerPrefs.SetString("SPEECH_LANGUAGE", config.speech_language);
                PlayerPrefs.SetFloat("SPEECH_RATE", config.speech_rate);
                PlayerPrefs.SetFloat("PANEL_DISTANCE", config.panel_distance);
                PlayerPrefs.SetInt("USE_BODY_ANCHOR", config.use_body_anchor ? 1 : 0);
                
                PlayerPrefs.Save();
                Debug.Log("[ConfigManager] Configuration saved to PlayerPrefs");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigManager] Error saving to PlayerPrefs: {e.Message}");
            }
        }

        // Public getters for configuration values
        public string OpenAIAPIKey => config?.openai_api_key ?? "";
        public string GoogleMapsAPIKey => config?.google_maps_api_key ?? "";
        public string MapboxAPIKey => config?.mapbox_api_key ?? "";
        public string AIModel => config?.ai_model ?? "gpt-4o-mini";
        public int MaxTokens => config?.max_tokens ?? 500;
        public float Temperature => config?.temperature ?? 0.7f;
        public string SystemPrompt => config?.system_prompt ?? "";
        public string SpeechLanguage => config?.speech_language ?? "en-US";
        public float SpeechRate => config?.speech_rate ?? 1.0f;
        public float SpeechPitch => config?.speech_pitch ?? 1.0f;
        public float SpeechVolume => config?.speech_volume ?? 0.8f;
        public string[] WakeWords => config?.wake_words ?? new string[] { "hey jarvis" };
        public float PanelDistance => config?.panel_distance ?? 1.5f;
        public bool UseBodyAnchor => config?.use_body_anchor ?? true;
        public bool EnableDebugLogs => config?.enable_debug_logs ?? true;
        public int MaxChatMessages => config?.max_chat_messages ?? 50;
        public bool UseGoogleMaps => config?.use_google_maps ?? true;
        public int MapWidth => config?.map_width ?? 512;
        public int MapHeight => config?.map_height ?? 512;

        // Setters for runtime configuration changes
        public void SetAPIKey(string service, string key)
        {
            if (!isLoaded) return;

            switch (service.ToLower())
            {
                case "openai":
                    config.openai_api_key = key;
                    break;
                case "googlemaps":
                    config.google_maps_api_key = key;
                    break;
                case "mapbox":
                    config.mapbox_api_key = key;
                    break;
            }
        }

        public void SetSpeechSettings(float rate, float pitch, float volume)
        {
            if (!isLoaded) return;

            config.speech_rate = Mathf.Clamp(rate, 0.1f, 3f);
            config.speech_pitch = Mathf.Clamp(pitch, 0.1f, 2f);
            config.speech_volume = Mathf.Clamp01(volume);
        }

        public void SetUISettings(float distance, bool bodyAnchor)
        {
            if (!isLoaded) return;

            config.panel_distance = Mathf.Clamp(distance, 0.5f, 5f);
            config.use_body_anchor = bodyAnchor;
        }

        public JarvisConfig GetConfig()
        {
            return config;
        }

        public bool IsLoaded => isLoaded;

        private void OnDestroy()
        {
            if (instance == this)
            {
                SaveConfiguration();
                instance = null;
            }
        }

        // Debug methods
        [ContextMenu("Save Configuration")]
        private void DebugSaveConfig()
        {
            SaveConfiguration();
        }

        [ContextMenu("Reload Configuration")]
        private void DebugReloadConfig()
        {
            LoadConfiguration();
        }

        [ContextMenu("Reset to Defaults")]
        private void DebugResetConfig()
        {
            ApplyDefaultConfiguration();
            SaveConfiguration();
        }
    }
}