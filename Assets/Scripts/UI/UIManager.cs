using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NRKernal;

namespace XREALJarvis.UI
{
    /// <summary>
    /// Manages all AR UI elements for the Jarvis assistant
    /// Handles chat display, status indicators, and AR-optimized layouts
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Main UI Panels")]
        [SerializeField] private GameObject chatPanel;
        [SerializeField] private GameObject mapPanel;
        [SerializeField] private GameObject statusPanel;
        [SerializeField] private GameObject infoPanel;

        [Header("Chat UI")]
        [SerializeField] private ScrollRect chatScrollRect;
        [SerializeField] private Transform chatContent;
        [SerializeField] private GameObject userMessagePrefab;
        [SerializeField] private GameObject aiMessagePrefab;
        [SerializeField] private int maxChatMessages = 50;

        [Header("Status Indicators")]
        [SerializeField] private GameObject listeningIndicator;
        [SerializeField] private GameObject thinkingIndicator;
        [SerializeField] private GameObject wakeWordIndicator;
        [SerializeField] private GameObject voiceCaptureIndicator;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Map UI")]
        [SerializeField] private RawImage mapImage;
        [SerializeField] private TextMeshProUGUI navigationText;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private Button stopNavigationButton;

        [Header("Info Display")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI locationText;
        [SerializeField] private TextMeshProUGUI batteryText;

        [Header("AR Settings")]
        [SerializeField] private float panelDistance = 1.5f;
        [SerializeField] private float panelHeight = 0f;
        [SerializeField] private bool useBodyAnchor = true;
        [SerializeField] private float followSmoothness = 5f;

        [Header("Visual Settings")]
        [SerializeField] private Color userMessageColor = Color.white;
        [SerializeField] private Color aiMessageColor = Color.cyan;
        [SerializeField] private float messageSpacing = 10f;
        [SerializeField] private float fadeInDuration = 0.3f;

        // UI State
        private List<GameObject> chatMessages = new List<GameObject>();
        private bool isInitialized = false;
        private Camera arCamera;
        private Transform cameraTransform;

        // Animation
        private Coroutine statusAnimationCoroutine;
        private Coroutine panelAnimationCoroutine;

        public void Initialize()
        {
            StartCoroutine(InitializeUI());
        }

        private IEnumerator InitializeUI()
        {
            // Wait for NRSDK camera
            yield return new WaitUntil(() => NRSessionManager.Instance.NRHMDPoseTracker != null);
            
            arCamera = NRSessionManager.Instance.NRHMDPoseTracker.centerCamera;
            if (arCamera == null)
                arCamera = Camera.main;
            
            cameraTransform = arCamera.transform;

            // Setup UI panels
            SetupARPanels();
            
            // Initialize UI components
            InitializeChatUI();
            InitializeStatusUI();
            InitializeMapUI();
            InitializeInfoUI();

            // Show welcome message
            ShowWelcomeMessage();

            isInitialized = true;
            Debug.Log("[UIManager] UI initialized successfully");
        }

        private void SetupARPanels()
        {
            // Position panels in AR space
            if (useBodyAnchor)
            {
                SetupBodyAnchoredPanels();
            }
            else
            {
                SetupHeadLockedPanels();
            }
        }

        private void SetupBodyAnchoredPanels()
        {
            // Position panels relative to initial camera position
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            Vector3 basePosition = cameraTransform.position + forward * panelDistance + Vector3.up * panelHeight;

            // Chat panel - center
            if (chatPanel != null)
            {
                chatPanel.transform.position = basePosition;
                chatPanel.transform.LookAt(cameraTransform.position);
                chatPanel.transform.Rotate(0, 180, 0);
            }

            // Map panel - right side
            if (mapPanel != null)
            {
                mapPanel.transform.position = basePosition + right * 0.8f;
                mapPanel.transform.LookAt(cameraTransform.position);
                mapPanel.transform.Rotate(0, 180, 0);
                mapPanel.SetActive(false);
            }

            // Status panel - top
            if (statusPanel != null)
            {
                statusPanel.transform.position = basePosition + Vector3.up * 0.3f;
                statusPanel.transform.LookAt(cameraTransform.position);
                statusPanel.transform.Rotate(0, 180, 0);
            }

            // Info panel - left side
            if (infoPanel != null)
            {
                infoPanel.transform.position = basePosition - right * 0.8f;
                infoPanel.transform.LookAt(cameraTransform.position);
                infoPanel.transform.Rotate(0, 180, 0);
            }
        }

        private void SetupHeadLockedPanels()
        {
            // Attach panels to camera for head-locked behavior
            if (chatPanel != null)
            {
                chatPanel.transform.SetParent(cameraTransform);
                chatPanel.transform.localPosition = new Vector3(0, 0, panelDistance);
                chatPanel.transform.localRotation = Quaternion.identity;
            }

            if (mapPanel != null)
            {
                mapPanel.transform.SetParent(cameraTransform);
                mapPanel.transform.localPosition = new Vector3(0.5f, 0, panelDistance);
                mapPanel.transform.localRotation = Quaternion.identity;
                mapPanel.SetActive(false);
            }

            if (statusPanel != null)
            {
                statusPanel.transform.SetParent(cameraTransform);
                statusPanel.transform.localPosition = new Vector3(0, 0.3f, panelDistance);
                statusPanel.transform.localRotation = Quaternion.identity;
            }

            if (infoPanel != null)
            {
                infoPanel.transform.SetParent(cameraTransform);
                infoPanel.transform.localPosition = new Vector3(-0.5f, 0, panelDistance);
                infoPanel.transform.localRotation = Quaternion.identity;
            }
        }

        private void InitializeChatUI()
        {
            if (chatScrollRect != null)
            {
                // Configure scroll rect for AR
                chatScrollRect.vertical = true;
                chatScrollRect.horizontal = false;
                chatScrollRect.movementType = ScrollRect.MovementType.Clamped;
            }

            // Setup navigation button
            if (stopNavigationButton != null)
            {
                stopNavigationButton.onClick.AddListener(OnStopNavigationClicked);
                stopNavigationButton.gameObject.SetActive(false);
            }
        }

        private void InitializeStatusUI()
        {
            // Hide all indicators initially
            ShowListeningIndicator(false);
            ShowThinkingIndicator(false);
            ShowWakeWordDetected(false);
            ShowVoiceCapture(false);

            if (statusText != null)
            {
                statusText.text = "Jarvis Ready";
            }
        }

        private void InitializeMapUI()
        {
            if (mapPanel != null)
            {
                mapPanel.SetActive(false);
            }
        }

        private void InitializeInfoUI()
        {
            StartCoroutine(UpdateInfoDisplay());
        }

        public void ShowWelcomeMessage()
        {
            string welcomeMessage = "Hello! I'm Jarvis, your AR assistant. Say 'Hey Jarvis' to get started.";
            AddChatMessage(welcomeMessage, false);
        }

        public void AddChatMessage(string message, bool isUser)
        {
            if (chatContent == null) return;

            GameObject messagePrefab = isUser ? userMessagePrefab : aiMessagePrefab;
            if (messagePrefab == null) return;

            // Create message object
            GameObject messageObj = Instantiate(messagePrefab, chatContent);
            
            // Set message text
            TextMeshProUGUI messageText = messageObj.GetComponentInChildren<TextMeshProUGUI>();
            if (messageText != null)
            {
                messageText.text = message;
                messageText.color = isUser ? userMessageColor : aiMessageColor;
            }

            // Add to message list
            chatMessages.Add(messageObj);

            // Limit message count
            while (chatMessages.Count > maxChatMessages)
            {
                GameObject oldMessage = chatMessages[0];
                chatMessages.RemoveAt(0);
                DestroyImmediate(oldMessage);
            }

            // Animate message appearance
            StartCoroutine(AnimateMessageAppearance(messageObj));

            // Scroll to bottom
            StartCoroutine(ScrollToBottom());

            Debug.Log($"[UIManager] Added {(isUser ? "user" : "AI")} message: {message}");
        }

        private IEnumerator AnimateMessageAppearance(GameObject messageObj)
        {
            CanvasGroup canvasGroup = messageObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = messageObj.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            if (chatScrollRect != null)
            {
                chatScrollRect.normalizedPosition = new Vector2(0, 0);
            }
        }

        public void ShowListeningIndicator(bool show)
        {
            if (listeningIndicator != null)
            {
                listeningIndicator.SetActive(show);
                if (show)
                {
                    StartStatusAnimation(listeningIndicator, "Listening...");
                }
            }
        }

        public void ShowThinkingIndicator(bool show)
        {
            if (thinkingIndicator != null)
            {
                thinkingIndicator.SetActive(show);
                if (show)
                {
                    StartStatusAnimation(thinkingIndicator, "Thinking...");
                }
            }
        }

        public void ShowWakeWordDetected()
        {
            if (wakeWordIndicator != null)
            {
                StartCoroutine(FlashIndicator(wakeWordIndicator, 1f));
            }
            ShowMessage("Wake word detected!", 2f);
        }

        public void ShowVoiceCapture(bool show)
        {
            if (voiceCaptureIndicator != null)
            {
                voiceCaptureIndicator.SetActive(show);
                if (show)
                {
                    StartStatusAnimation(voiceCaptureIndicator, "Listening for command...");
                }
            }
        }

        private void StartStatusAnimation(GameObject indicator, string message)
        {
            if (statusAnimationCoroutine != null)
            {
                StopCoroutine(statusAnimationCoroutine);
            }
            statusAnimationCoroutine = StartCoroutine(AnimateStatusIndicator(indicator, message));
        }

        private IEnumerator AnimateStatusIndicator(GameObject indicator, string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            // Pulse animation
            Transform indicatorTransform = indicator.transform;
            Vector3 originalScale = indicatorTransform.localScale;
            
            while (indicator.activeInHierarchy)
            {
                // Scale up
                float elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.deltaTime;
                    float scale = Mathf.Lerp(1f, 1.2f, elapsed / 0.5f);
                    indicatorTransform.localScale = originalScale * scale;
                    yield return null;
                }

                // Scale down
                elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.deltaTime;
                    float scale = Mathf.Lerp(1.2f, 1f, elapsed / 0.5f);
                    indicatorTransform.localScale = originalScale * scale;
                    yield return null;
                }
            }

            indicatorTransform.localScale = originalScale;
        }

        private IEnumerator FlashIndicator(GameObject indicator, float duration)
        {
            indicator.SetActive(true);
            yield return new WaitForSeconds(duration);
            indicator.SetActive(false);
        }

        public void ShowMessage(string message, float duration = 3f)
        {
            StartCoroutine(ShowTemporaryMessage(message, duration));
        }

        private IEnumerator ShowTemporaryMessage(string message, float duration)
        {
            if (statusText != null)
            {
                string originalText = statusText.text;
                statusText.text = message;
                yield return new WaitForSeconds(duration);
                statusText.text = originalText;
            }
        }

        public void ShowMapPanel(bool show)
        {
            if (mapPanel != null)
            {
                mapPanel.SetActive(show);
                if (show)
                {
                    StartCoroutine(AnimatePanelAppearance(mapPanel));
                }
            }
        }

        public void UpdateMapImage(Texture2D mapTexture)
        {
            if (mapImage != null && mapTexture != null)
            {
                mapImage.texture = mapTexture;
                ShowMapPanel(true);
            }
        }

        public void UpdateNavigationInfo(string destination, float distance)
        {
            if (navigationText != null)
            {
                navigationText.text = $"To: {destination}";
            }

            if (distanceText != null)
            {
                if (distance < 1000)
                {
                    distanceText.text = $"{distance:F0}m";
                }
                else
                {
                    distanceText.text = $"{distance / 1000:F1}km";
                }
            }

            if (stopNavigationButton != null)
            {
                stopNavigationButton.gameObject.SetActive(true);
            }
        }

        private IEnumerator AnimatePanelAppearance(GameObject panel)
        {
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panel.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }

        private IEnumerator UpdateInfoDisplay()
        {
            while (true)
            {
                // Update time
                if (timeText != null)
                {
                    timeText.text = System.DateTime.Now.ToString("HH:mm");
                }

                // Update battery (Android specific)
                if (batteryText != null)
                {
                    float batteryLevel = SystemInfo.batteryLevel;
                    if (batteryLevel >= 0)
                    {
                        batteryText.text = $"Battery: {batteryLevel * 100:F0}%";
                    }
                }

                yield return new WaitForSeconds(30f); // Update every 30 seconds
            }
        }

        public void UpdateLocationDisplay(string location)
        {
            if (locationText != null)
            {
                locationText.text = location;
            }
        }

        private void OnStopNavigationClicked()
        {
            // Find MapManager and stop navigation
            var mapManager = FindObjectOfType<Maps.MapManager>();
            if (mapManager != null)
            {
                mapManager.StopNavigation();
            }

            // Hide navigation UI
            if (stopNavigationButton != null)
            {
                stopNavigationButton.gameObject.SetActive(false);
            }

            ShowMapPanel(false);
        }

        public void ClearChat()
        {
            foreach (GameObject message in chatMessages)
            {
                if (message != null)
                    DestroyImmediate(message);
            }
            chatMessages.Clear();
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Update panel positions for smooth follow mode
            if (!useBodyAnchor)
            {
                UpdateSmoothFollowPanels();
            }
        }

        private void UpdateSmoothFollowPanels()
        {
            // Implement smooth follow behavior for head-locked panels
            // This creates a more comfortable AR experience
            
            if (chatPanel != null && chatPanel.transform.parent == cameraTransform)
            {
                Vector3 targetPosition = new Vector3(0, 0, panelDistance);
                chatPanel.transform.localPosition = Vector3.Lerp(
                    chatPanel.transform.localPosition, 
                    targetPosition, 
                    Time.deltaTime * followSmoothness);
            }
        }

        // Public properties
        public bool IsInitialized => isInitialized;
        public int ChatMessageCount => chatMessages.Count;

        private void OnDestroy()
        {
            if (stopNavigationButton != null)
            {
                stopNavigationButton.onClick.RemoveAllListeners();
            }
        }
    }
}