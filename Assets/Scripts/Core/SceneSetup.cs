using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NRKernal;

namespace XREALJarvis.Core
{
    /// <summary>
    /// Handles initial scene setup and AR environment configuration
    /// Sets up NRSDK components and initializes the AR scene
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private float initializationDelay = 1f;
        [SerializeField] private bool enablePlaneDetection = false;
        [SerializeField] private bool enableImageTracking = false;

        [Header("AR Camera Setup")]
        [SerializeField] private GameObject nrCameraPrefab;
        [SerializeField] private Transform cameraParent;
        [SerializeField] private Vector3 cameraOffset = Vector3.zero;

        [Header("UI Canvas Setup")]
        [SerializeField] private Canvas worldSpaceCanvas;
        [SerializeField] private float canvasDistance = 2f;
        [SerializeField] private Vector2 canvasSize = new Vector2(1920, 1080);

        [Header("Lighting")]
        [SerializeField] private Light mainLight;
        [SerializeField] private bool adjustLightingForAR = true;
        [SerializeField] private Color arAmbientColor = new Color(0.4f, 0.4f, 0.4f, 1f);

        // Components
        private Camera arCamera;
        private NRHMDPoseTracker poseTracker;
        private NRInput nrInput;

        // Events
        public System.Action OnSceneReady;
        public System.Action<string> OnSetupError;

        private void Start()
        {
            if (autoInitialize)
            {
                StartCoroutine(InitializeScene());
            }
        }

        public void InitializeScene()
        {
            StartCoroutine(InitializeSceneCoroutine());
        }

        private IEnumerator InitializeSceneCoroutine()
        {
            Debug.Log("[SceneSetup] Starting AR scene initialization...");

            // Wait for initial delay
            yield return new WaitForSeconds(initializationDelay);

            // Initialize NRSDK
            yield return StartCoroutine(InitializeNRSDK());

            // Setup AR camera
            yield return StartCoroutine(SetupARCamera());

            // Setup UI canvas
            SetupWorldSpaceCanvas();

            // Configure lighting
            ConfigureLighting();

            // Setup input handling
            SetupInputHandling();

            // Final validation
            if (ValidateSetup())
            {
                Debug.Log("[SceneSetup] AR scene initialized successfully!");
                OnSceneReady?.Invoke();
            }
            else
            {
                string error = "Scene setup validation failed";
                Debug.LogError($"[SceneSetup] {error}");
                OnSetupError?.Invoke(error);
            }
        }

        private IEnumerator InitializeNRSDK()
        {
            try
            {
                // Check if NRSDK is available
                if (!NRDevice.Subsystem.IsAvailable())
                {
                    Debug.LogWarning("[SceneSetup] NRSDK not available, running in simulation mode");
                    yield break;
                }

                // Wait for NRSDK initialization
                yield return new WaitUntil(() => NRSessionManager.Instance != null);
                yield return new WaitUntil(() => NRSessionManager.Instance.NRSessionBehaviour != null);

                // Configure session
                var sessionConfig = NRSessionManager.Instance.NRSessionBehaviour.SessionConfig;
                if (sessionConfig != null)
                {
                    sessionConfig.PlaneFindingMode = enablePlaneDetection ? 
                        TrackablePlaneFindingMode.HORIZONTAL : TrackablePlaneFindingMode.DISABLE;
                    
                    sessionConfig.ImageTrackingMode = enableImageTracking ? 
                        TrackableImageFindingMode.ENABLE : TrackableImageFindingMode.DISABLE;
                }

                Debug.Log("[SceneSetup] NRSDK initialized successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SceneSetup] NRSDK initialization failed: {e.Message}");
                OnSetupError?.Invoke($"NRSDK initialization failed: {e.Message}");
            }
        }

        private IEnumerator SetupARCamera()
        {
            try
            {
                // Find or create AR camera
                if (NRSessionManager.Instance?.NRHMDPoseTracker != null)
                {
                    poseTracker = NRSessionManager.Instance.NRHMDPoseTracker;
                    arCamera = poseTracker.centerCamera;
                    
                    if (arCamera != null)
                    {
                        Debug.Log("[SceneSetup] Using NRSDK AR camera");
                    }
                }

                // Fallback to main camera if NRSDK camera not available
                if (arCamera == null)
                {
                    arCamera = Camera.main;
                    if (arCamera == null)
                    {
                        arCamera = FindObjectOfType<Camera>();
                    }
                    
                    if (arCamera != null)
                    {
                        Debug.Log("[SceneSetup] Using fallback camera for AR");
                        ConfigureFallbackCamera();
                    }
                }

                if (arCamera == null)
                {
                    throw new System.Exception("No suitable camera found for AR");
                }

                // Configure camera settings
                ConfigureARCamera();

                yield return null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SceneSetup] AR camera setup failed: {e.Message}");
                OnSetupError?.Invoke($"AR camera setup failed: {e.Message}");
            }
        }

        private void ConfigureARCamera()
        {
            if (arCamera == null) return;

            // Set camera properties for AR
            arCamera.clearFlags = CameraClearFlags.SolidColor;
            arCamera.backgroundColor = Color.black;
            arCamera.nearClipPlane = 0.1f;
            arCamera.farClipPlane = 1000f;

            // Add audio listener if not present
            if (arCamera.GetComponent<AudioListener>() == null)
            {
                arCamera.gameObject.AddComponent<AudioListener>();
            }

            // Position camera if parent is specified
            if (cameraParent != null)
            {
                arCamera.transform.SetParent(cameraParent);
                arCamera.transform.localPosition = cameraOffset;
                arCamera.transform.localRotation = Quaternion.identity;
            }

            Debug.Log("[SceneSetup] AR camera configured");
        }

        private void ConfigureFallbackCamera()
        {
            if (arCamera == null) return;

            // Configure camera for non-NRSDK mode (testing/simulation)
            arCamera.fieldOfView = 60f;
            arCamera.transform.position = new Vector3(0, 1.6f, 0); // Average eye height
            arCamera.transform.rotation = Quaternion.identity;

            // Add simple mouse look for testing
            var mouseLook = arCamera.gameObject.GetComponent<SimpleCameraController>();
            if (mouseLook == null)
            {
                mouseLook = arCamera.gameObject.AddComponent<SimpleCameraController>();
            }
        }

        private void SetupWorldSpaceCanvas()
        {
            if (worldSpaceCanvas == null)
            {
                // Create world space canvas
                GameObject canvasObj = new GameObject("WorldSpaceCanvas");
                worldSpaceCanvas = canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            // Configure canvas for AR
            worldSpaceCanvas.renderMode = RenderMode.WorldSpace;
            worldSpaceCanvas.worldCamera = arCamera;

            // Position canvas in front of camera
            if (arCamera != null)
            {
                Vector3 canvasPosition = arCamera.transform.position + 
                                       arCamera.transform.forward * canvasDistance;
                worldSpaceCanvas.transform.position = canvasPosition;
                worldSpaceCanvas.transform.LookAt(arCamera.transform);
                worldSpaceCanvas.transform.Rotate(0, 180, 0); // Face the camera
            }

            // Set canvas size
            RectTransform canvasRect = worldSpaceCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = canvasSize;

            // Scale for comfortable viewing
            float scale = canvasDistance * 0.001f; // Adjust scale based on distance
            worldSpaceCanvas.transform.localScale = Vector3.one * scale;

            Debug.Log("[SceneSetup] World space canvas configured");
        }

        private void ConfigureLighting()
        {
            if (!adjustLightingForAR) return;

            // Set ambient lighting for AR
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = arAmbientColor;

            // Configure main light
            if (mainLight == null)
            {
                mainLight = FindObjectOfType<Light>();
            }

            if (mainLight != null)
            {
                mainLight.type = LightType.Directional;
                mainLight.intensity = 1.0f;
                mainLight.shadows = LightShadows.Soft;
                mainLight.color = Color.white;
                
                // Position light to simulate natural lighting
                mainLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            Debug.Log("[SceneSetup] Lighting configured for AR");
        }

        private void SetupInputHandling()
        {
            try
            {
                // Initialize NR Input if available
                if (NRInput.GetAvailableControllersCount() > 0)
                {
                    NRInput.SetInputSource(InputSourceEnum.Controller);
                    Debug.Log("[SceneSetup] NR Input initialized with controller");
                }
                else
                {
                    // Fallback to other input methods
                    Debug.Log("[SceneSetup] No NR controllers found, using fallback input");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SceneSetup] Input setup warning: {e.Message}");
            }
        }

        private bool ValidateSetup()
        {
            bool isValid = true;
            List<string> issues = new List<string>();

            // Check camera
            if (arCamera == null)
            {
                issues.Add("AR Camera not found");
                isValid = false;
            }

            // Check canvas
            if (worldSpaceCanvas == null)
            {
                issues.Add("World Space Canvas not configured");
                isValid = false;
            }

            // Check NRSDK (warning only)
            if (NRSessionManager.Instance == null)
            {
                Debug.LogWarning("[SceneSetup] NRSDK not available - running in simulation mode");
            }

            if (!isValid)
            {
                Debug.LogError($"[SceneSetup] Validation failed: {string.Join(", ", issues)}");
            }

            return isValid;
        }

        public Camera GetARCamera()
        {
            return arCamera;
        }

        public Canvas GetWorldSpaceCanvas()
        {
            return worldSpaceCanvas;
        }

        public bool IsNRSDKAvailable()
        {
            return NRDevice.Subsystem.IsAvailable();
        }

        // Simple camera controller for testing without NRSDK
        private class SimpleCameraController : MonoBehaviour
        {
            public float mouseSensitivity = 2f;
            public float moveSpeed = 5f;

            private float rotationX = 0f;
            private float rotationY = 0f;

            private void Update()
            {
                // Mouse look
                if (Input.GetMouseButton(1)) // Right mouse button
                {
                    rotationX += Input.GetAxis("Mouse X") * mouseSensitivity;
                    rotationY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                    rotationY = Mathf.Clamp(rotationY, -90f, 90f);

                    transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);
                }

                // Movement
                Vector3 movement = Vector3.zero;
                if (Input.GetKey(KeyCode.W)) movement += transform.forward;
                if (Input.GetKey(KeyCode.S)) movement -= transform.forward;
                if (Input.GetKey(KeyCode.A)) movement -= transform.right;
                if (Input.GetKey(KeyCode.D)) movement += transform.right;
                if (Input.GetKey(KeyCode.Q)) movement -= transform.up;
                if (Input.GetKey(KeyCode.E)) movement += transform.up;

                transform.position += movement * moveSpeed * Time.deltaTime;
            }
        }

        private void OnDestroy()
        {
            // Cleanup if needed
        }

        // Debug methods
        [ContextMenu("Reinitialize Scene")]
        private void DebugReinitialize()
        {
            if (Application.isPlaying)
            {
                InitializeScene();
            }
        }

        [ContextMenu("Validate Setup")]
        private void DebugValidate()
        {
            ValidateSetup();
        }
    }
}