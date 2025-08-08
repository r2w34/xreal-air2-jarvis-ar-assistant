using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace XREALJarvis.Utils
{
    /// <summary>
    /// Bridge for Android-specific functionality
    /// Handles permissions, native calls, and Android system integration
    /// </summary>
    public class AndroidBridge : MonoBehaviour
    {
        private static AndroidBridge instance;
        public static AndroidBridge Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("AndroidBridge");
                    instance = go.AddComponent<AndroidBridge>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        // Android objects
        private AndroidJavaObject currentActivity;
        private AndroidJavaClass unityPlayer;
        private AndroidJavaObject context;

        // Events
        public Action<bool> OnPermissionResult;
        public Action<string> OnAndroidError;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAndroidBridge();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAndroidBridge()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
                
                Debug.Log("[AndroidBridge] Initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AndroidBridge] Initialization failed: {e.Message}");
                OnAndroidError?.Invoke($"Android bridge initialization failed: {e.Message}");
            }
#endif
        }

        public bool HasPermission(string permission)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaClass contextCompat = new AndroidJavaClass("androidx.core.content.ContextCompat");
                int result = contextCompat.CallStatic<int>("checkSelfPermission", currentActivity, permission);
                return result == 0; // PackageManager.PERMISSION_GRANTED
            }
            catch (Exception e)
            {
                Debug.LogError($"[AndroidBridge] Error checking permission {permission}: {e.Message}");
                return false;
            }
#else
            return true; // Assume granted in editor
#endif
        }

        public void RequestPermission(string permission)
        {
            RequestPermissions(new string[] { permission });
        }

        public void RequestPermissions(string[] permissions)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaClass activityCompat = new AndroidJavaClass("androidx.core.app.ActivityCompat");
                activityCompat.CallStatic("requestPermissions", currentActivity, permissions, 1001);
                Debug.Log($"[AndroidBridge] Requested permissions: {string.Join(", ", permissions)}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AndroidBridge] Error requesting permissions: {e.Message}");
                OnAndroidError?.Invoke($"Permission request failed: {e.Message}");
            }
#else
            // Simulate permission granted in editor
            OnPermissionResult?.Invoke(true);
#endif
        }

        public bool CheckAndRequestMicrophonePermission()
        {
            const string micPermission = "android.permission.RECORD_AUDIO";
            
            if (HasPermission(micPermission))
            {
                return true;
            }
            else
            {
                RequestPermission(micPermission);
                return false;
            }
        }

        public bool CheckAndRequestLocationPermissions()
        {
            string[] locationPermissions = {
                "android.permission.ACCESS_FINE_LOCATION",
                "android.permission.ACCESS_COARSE_LOCATION"
            };

            bool hasAllPermissions = true;
            foreach (string permission in locationPermissions)
            {
                if (!HasPermission(permission))
                {
                    hasAllPermissions = false;
                    break;
                }
            }

            if (!hasAllPermissions)
            {
                RequestPermissions(locationPermissions);
                return false;
            }

            return true;
        }

        public void ShowToast(string message, bool longDuration = false)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                int duration = longDuration ? 1 : 0; // Toast.LENGTH_LONG : Toast.LENGTH_SHORT
                
                AndroidJavaObject toast = toastClass.CallStatic<AndroidJavaObject>("makeText", 
                    context, message, duration);
                toast.Call("show");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AndroidBridge] Error showing toast: {e.Message}");
            }
#else
            Debug.Log($"[AndroidBridge] Toast: {message}");
#endif
        }

        public void VibrateDevice(long milliseconds = 100)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaClass vibrator = new AndroidJavaClass("android.os.Vibrator");
                AndroidJavaObject vibratorService = context.Call<AndroidJavaObject>("getSystemService", "vibrator");
                
                if (vibratorService != null)
                {
                    vibratorService.Call("vibrate", milliseconds);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AndroidBridge] Error vibrating device: {e.Message}");
            }
#endif
        }

        public float GetBatteryLevel()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaClass intentFilter = new AndroidJavaClass("android.content.IntentFilter");
                AndroidJavaObject filter = new AndroidJavaObject("android.content.IntentFilter", 
                    "android.intent.action.BATTERY_CHANGED");
                
                AndroidJavaObject batteryIntent = context.Call<AndroidJavaObject>("registerReceiver", 
                    null, filter);
                
                if (batteryIntent != null)
                {
                    int level = batteryIntent.Call<int>("getIntExtra", "level", -1);
                    int scale = batteryIntent.Call<int>("getIntExtra", "scale", -1);
                    
                    if (level >= 0 && scale > 0)
                    {
                        return (float)level / scale;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AndroidBridge] Error getting battery level: {e.Message}");
            }
#endif
            return SystemInfo.batteryLevel; // Fallback to Unity's method
        }

        public bool IsNetworkAvailable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaObject connectivityManager = context.Call<AndroidJavaObject>("getSystemService", 
                    "connectivity");
                
                if (connectivityManager != null)
                {
                    AndroidJavaObject activeNetwork = connectivityManager.Call<AndroidJavaObject>("getActiveNetworkInfo");
                    return activeNetwork != null && activeNetwork.Call<bool>("isConnected");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AndroidBridge] Error checking network: {e.Message}");
            }
#endif
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        public void OpenAppSettings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
                AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", 
                    "android.settings.APPLICATION_DETAILS_SETTINGS");
                
                AndroidJavaClass uri = new AndroidJavaClass("android.net.Uri");
                AndroidJavaObject uriObject = uri.CallStatic<AndroidJavaObject>("parse", 
                    "package:" + Application.identifier);
                
                intent.Call<AndroidJavaObject>("setData", uriObject);
                currentActivity.Call("startActivity", intent);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AndroidBridge] Error opening app settings: {e.Message}");
            }
#endif
        }

        public void KeepScreenOn(bool keepOn)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaObject window = currentActivity.Call<AndroidJavaObject>("getWindow");
                
                if (keepOn)
                {
                    window.Call("addFlags", 128); // WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON
                }
                else
                {
                    window.Call("clearFlags", 128);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AndroidBridge] Error setting screen on: {e.Message}");
            }
#endif
        }

        public void SetScreenBrightness(float brightness)
        {
            brightness = Mathf.Clamp01(brightness);
            
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaObject window = currentActivity.Call<AndroidJavaObject>("getWindow");
                AndroidJavaObject attributes = window.Call<AndroidJavaObject>("getAttributes");
                
                attributes.Set<float>("screenBrightness", brightness);
                window.Call("setAttributes", attributes);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AndroidBridge] Error setting brightness: {e.Message}");
            }
#endif
        }

        public string GetDeviceInfo()
        {
            string deviceInfo = $"Device: {SystemInfo.deviceModel}\n";
            deviceInfo += $"OS: {SystemInfo.operatingSystem}\n";
            deviceInfo += $"Memory: {SystemInfo.systemMemorySize}MB\n";
            deviceInfo += $"Graphics: {SystemInfo.graphicsDeviceName}\n";
            
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaClass buildClass = new AndroidJavaClass("android.os.Build");
                string manufacturer = buildClass.GetStatic<string>("MANUFACTURER");
                string model = buildClass.GetStatic<string>("MODEL");
                string version = buildClass.GetStatic<string>("RELEASE");
                
                deviceInfo += $"Manufacturer: {manufacturer}\n";
                deviceInfo += $"Model: {model}\n";
                deviceInfo += $"Android Version: {version}\n";
            }
            catch (Exception e)
            {
                Debug.LogError($"[AndroidBridge] Error getting device info: {e.Message}");
            }
#endif
            
            return deviceInfo;
        }

        // Called from Android native code for permission results
        public void OnRequestPermissionsResult(string result)
        {
            bool granted = result.Equals("granted", StringComparison.OrdinalIgnoreCase);
            OnPermissionResult?.Invoke(granted);
            Debug.Log($"[AndroidBridge] Permission result: {result}");
        }

        // Utility methods for common Android operations
        public static class Permissions
        {
            public const string RECORD_AUDIO = "android.permission.RECORD_AUDIO";
            public const string ACCESS_FINE_LOCATION = "android.permission.ACCESS_FINE_LOCATION";
            public const string ACCESS_COARSE_LOCATION = "android.permission.ACCESS_COARSE_LOCATION";
            public const string CAMERA = "android.permission.CAMERA";
            public const string WRITE_EXTERNAL_STORAGE = "android.permission.WRITE_EXTERNAL_STORAGE";
            public const string READ_EXTERNAL_STORAGE = "android.permission.READ_EXTERNAL_STORAGE";
            public const string INTERNET = "android.permission.INTERNET";
            public const string ACCESS_NETWORK_STATE = "android.permission.ACCESS_NETWORK_STATE";
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}