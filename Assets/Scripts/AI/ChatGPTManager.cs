using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;

namespace XREALJarvis.AI
{
    /// <summary>
    /// Manages ChatGPT API integration for the Jarvis AI assistant
    /// Handles conversation context, API calls, and response processing
    /// </summary>
    public class ChatGPTManager : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string apiKey = ""; // Set in inspector or load from config
        [SerializeField] private string model = "gpt-4o-mini";
        [SerializeField] private string apiUrl = "https://api.openai.com/v1/chat/completions";
        [SerializeField] private int maxTokens = 500;
        [SerializeField] private float temperature = 0.7f;

        [Header("Conversation Settings")]
        [SerializeField] private int maxConversationHistory = 20;
        [SerializeField] private bool maintainContext = true;
        [SerializeField] private string systemPrompt = "You are Jarvis, a helpful AI assistant for AR glasses. You provide concise, helpful responses and can assist with navigation, information lookup, and general questions. Keep responses brief but informative, suitable for voice interaction.";

        [Header("Response Processing")]
        [SerializeField] private bool enableStreamingResponse = false;
        [SerializeField] private float requestTimeout = 30f;

        // Events
        public Action<string> OnResponseReceived;
        public Action<string> OnPartialResponse; // For streaming
        public Action<string> OnError;
        public Action OnRequestStarted;
        public Action OnRequestCompleted;

        // Conversation state
        private List<ChatMessage> conversationHistory = new List<ChatMessage>();
        private bool isProcessingRequest = false;
        private Coroutine currentRequestCoroutine;

        // API rate limiting
        private float lastRequestTime = 0f;
        private const float MIN_REQUEST_INTERVAL = 1f;

        [System.Serializable]
        public class ChatMessage
        {
            public string role;
            public string content;
            public long timestamp;

            public ChatMessage(string role, string content)
            {
                this.role = role;
                this.content = content;
                this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        [System.Serializable]
        public class ChatGPTRequest
        {
            public string model;
            public ChatMessage[] messages;
            public int max_tokens;
            public float temperature;
            public bool stream;
        }

        [System.Serializable]
        public class ChatGPTResponse
        {
            public string id;
            public string @object;
            public long created;
            public string model;
            public Choice[] choices;
            public Usage usage;
        }

        [System.Serializable]
        public class Choice
        {
            public int index;
            public ChatMessage message;
            public string finish_reason;
        }

        [System.Serializable]
        public class Usage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }

        public void Initialize()
        {
            // Load API key from config if not set in inspector
            if (string.IsNullOrEmpty(apiKey))
            {
                LoadAPIKey();
            }

            // Initialize conversation with system prompt
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                conversationHistory.Add(new ChatMessage("system", systemPrompt));
            }

            Debug.Log("[ChatGPTManager] Initialized successfully");
        }

        private void LoadAPIKey()
        {
            // Try to load from StreamingAssets or PlayerPrefs
            string configPath = System.IO.Path.Combine(Application.streamingAssetsPath, "config.json");
            
            if (System.IO.File.Exists(configPath))
            {
                try
                {
                    string configJson = System.IO.File.ReadAllText(configPath);
                    var config = JsonUtility.FromJson<APIConfig>(configJson);
                    apiKey = config.openai_api_key;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ChatGPTManager] Failed to load API key from config: {e.Message}");
                }
            }
            else
            {
                // Fallback to PlayerPrefs
                apiKey = PlayerPrefs.GetString("OPENAI_API_KEY", "");
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("[ChatGPTManager] No API key found! Please set it in the inspector or config file.");
            }
        }

        public void SendMessage(string userMessage)
        {
            if (string.IsNullOrEmpty(userMessage) || isProcessingRequest)
            {
                return;
            }

            // Rate limiting check
            if (Time.time - lastRequestTime < MIN_REQUEST_INTERVAL)
            {
                Debug.LogWarning("[ChatGPTManager] Request rate limited");
                return;
            }

            // Add user message to conversation history
            if (maintainContext)
            {
                conversationHistory.Add(new ChatMessage("user", userMessage));
                TrimConversationHistory();
            }

            // Start API request
            if (currentRequestCoroutine != null)
            {
                StopCoroutine(currentRequestCoroutine);
            }

            currentRequestCoroutine = StartCoroutine(SendChatGPTRequest(userMessage));
        }

        private IEnumerator SendChatGPTRequest(string userMessage)
        {
            isProcessingRequest = true;
            lastRequestTime = Time.time;
            OnRequestStarted?.Invoke();

            // Prepare request data
            ChatGPTRequest request = new ChatGPTRequest
            {
                model = model,
                messages = maintainContext ? conversationHistory.ToArray() : 
                          new ChatMessage[] { 
                              new ChatMessage("system", systemPrompt),
                              new ChatMessage("user", userMessage) 
                          },
                max_tokens = maxTokens,
                temperature = temperature,
                stream = enableStreamingResponse
            };

            string jsonRequest = JsonUtility.ToJson(request);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);

            // Create web request
            using (UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                webRequest.timeout = (int)requestTimeout;

                Debug.Log($"[ChatGPTManager] Sending request: {userMessage}");

                // Send request
                yield return webRequest.SendWebRequest();

                // Handle response
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    HandleSuccessResponse(webRequest.downloadHandler.text);
                }
                else
                {
                    HandleErrorResponse(webRequest.error, webRequest.responseCode, webRequest.downloadHandler.text);
                }
            }

            isProcessingRequest = false;
            OnRequestCompleted?.Invoke();
        }

        private void HandleSuccessResponse(string responseText)
        {
            try
            {
                ChatGPTResponse response = JsonUtility.FromJson<ChatGPTResponse>(responseText);
                
                if (response.choices != null && response.choices.Length > 0)
                {
                    string aiResponse = response.choices[0].message.content;
                    
                    // Add AI response to conversation history
                    if (maintainContext)
                    {
                        conversationHistory.Add(new ChatMessage("assistant", aiResponse));
                    }

                    // Process response for AR-specific enhancements
                    string processedResponse = ProcessResponseForAR(aiResponse);

                    Debug.Log($"[ChatGPTManager] Received response: {processedResponse}");
                    OnResponseReceived?.Invoke(processedResponse);

                    // Log token usage
                    if (response.usage != null)
                    {
                        Debug.Log($"[ChatGPTManager] Token usage - Prompt: {response.usage.prompt_tokens}, " +
                                $"Completion: {response.usage.completion_tokens}, Total: {response.usage.total_tokens}");
                    }
                }
                else
                {
                    OnError?.Invoke("No response choices received from API");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChatGPTManager] Error parsing response: {e.Message}");
                OnError?.Invoke($"Failed to parse AI response: {e.Message}");
            }
        }

        private void HandleErrorResponse(string error, long responseCode, string responseText)
        {
            string errorMessage = $"API request failed: {error} (Code: {responseCode})";
            
            if (!string.IsNullOrEmpty(responseText))
            {
                try
                {
                    // Try to parse error details from response
                    var errorResponse = JsonUtility.FromJson<APIErrorResponse>(responseText);
                    if (errorResponse?.error != null)
                    {
                        errorMessage = $"API Error: {errorResponse.error.message}";
                    }
                }
                catch
                {
                    // Use raw response if parsing fails
                    errorMessage += $" - {responseText}";
                }
            }

            Debug.LogError($"[ChatGPTManager] {errorMessage}");
            OnError?.Invoke(errorMessage);
        }

        private string ProcessResponseForAR(string response)
        {
            // Process response for better AR experience
            // Remove excessive formatting, ensure appropriate length, etc.
            
            // Remove markdown formatting that doesn't work well in AR
            response = response.Replace("**", "").Replace("*", "");
            
            // Ensure response isn't too long for comfortable AR reading
            if (response.Length > 300)
            {
                // Try to find a natural break point
                int lastSentence = response.LastIndexOf('.', 300);
                if (lastSentence > 200)
                {
                    response = response.Substring(0, lastSentence + 1);
                }
                else
                {
                    response = response.Substring(0, 300) + "...";
                }
            }

            return response;
        }

        private void TrimConversationHistory()
        {
            // Keep conversation history within limits
            while (conversationHistory.Count > maxConversationHistory)
            {
                // Remove oldest messages but keep system prompt
                for (int i = 1; i < conversationHistory.Count; i++)
                {
                    if (conversationHistory[i].role != "system")
                    {
                        conversationHistory.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public void ClearConversationHistory()
        {
            conversationHistory.Clear();
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                conversationHistory.Add(new ChatMessage("system", systemPrompt));
            }
            Debug.Log("[ChatGPTManager] Conversation history cleared");
        }

        public void SetSystemPrompt(string newSystemPrompt)
        {
            systemPrompt = newSystemPrompt;
            
            // Update or add system message
            if (conversationHistory.Count > 0 && conversationHistory[0].role == "system")
            {
                conversationHistory[0].content = systemPrompt;
            }
            else
            {
                conversationHistory.Insert(0, new ChatMessage("system", systemPrompt));
            }
        }

        public void CancelCurrentRequest()
        {
            if (currentRequestCoroutine != null)
            {
                StopCoroutine(currentRequestCoroutine);
                currentRequestCoroutine = null;
            }
            isProcessingRequest = false;
        }

        // Public properties
        public bool IsProcessingRequest => isProcessingRequest;
        public int ConversationLength => conversationHistory.Count;
        public bool HasAPIKey => !string.IsNullOrEmpty(apiKey);

        [System.Serializable]
        private class APIConfig
        {
            public string openai_api_key;
            public string google_maps_api_key;
        }

        [System.Serializable]
        private class APIErrorResponse
        {
            public APIError error;
        }

        [System.Serializable]
        private class APIError
        {
            public string message;
            public string type;
            public string param;
            public string code;
        }

        private void OnDestroy()
        {
            CancelCurrentRequest();
        }

        // Debug methods
        [ContextMenu("Test API Connection")]
        private void TestAPIConnection()
        {
            if (Application.isPlaying)
            {
                SendMessage("Hello, can you hear me?");
            }
        }

        [ContextMenu("Clear History")]
        private void DebugClearHistory()
        {
            ClearConversationHistory();
        }
    }
}