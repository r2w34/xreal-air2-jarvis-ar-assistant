using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System;

namespace XREALJarvis.Editor
{
    /// <summary>
    /// Automated build script for XREAL Air 2 Jarvis AR Assistant
    /// Handles Android APK building with proper configuration
    /// </summary>
    public class BuildScript
    {
        private const string BUILD_PATH = "Builds";
        private const string APK_NAME = "JarvisAR";
        private const string COMPANY_NAME = "XREAL Jarvis";
        private const string PRODUCT_NAME = "Jarvis AR Assistant";
        private const string BUNDLE_IDENTIFIER = "com.xreal.jarvis.ar";

        [MenuItem("Build/Build Android APK")]
        public static void BuildAndroidAPK()
        {
            BuildAndroid(false);
        }

        [MenuItem("Build/Build Android APK (Development)")]
        public static void BuildAndroidAPKDevelopment()
        {
            BuildAndroid(true);
        }

        [MenuItem("Build/Build and Run Android")]
        public static void BuildAndRunAndroid()
        {
            BuildAndroid(true, true);
        }

        public static void BuildAndroid(bool developmentBuild = false, bool autoRun = false)
        {
            Debug.Log("[BuildScript] Starting Android build process...");

            try
            {
                // Configure build settings
                ConfigureBuildSettings(developmentBuild);

                // Prepare build options
                BuildPlayerOptions buildOptions = PrepareBuildOptions(developmentBuild, autoRun);

                // Execute build
                BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
                
                // Handle build result
                HandleBuildResult(report, buildOptions.locationPathName);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuildScript] Build failed with exception: {e.Message}");
                EditorUtility.DisplayDialog("Build Failed", $"Build failed with error:\n{e.Message}", "OK");
            }
        }

        private static void ConfigureBuildSettings(bool developmentBuild)
        {
            Debug.Log("[BuildScript] Configuring build settings...");

            // Switch to Android platform
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // Configure Android settings
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

            // Player settings
            PlayerSettings.companyName = COMPANY_NAME;
            PlayerSettings.productName = PRODUCT_NAME;
            PlayerSettings.applicationIdentifier = BUNDLE_IDENTIFIER;
            
            // Android specific settings
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            
            // Scripting settings
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Standard_2_0);

            // Graphics settings
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new UnityEngine.Rendering.GraphicsDeviceType[] 
            { 
                UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3,
                UnityEngine.Rendering.GraphicsDeviceType.Vulkan 
            });

            // Optimization settings
            PlayerSettings.stripEngineCode = !developmentBuild;
            PlayerSettings.Android.useCustomKeystore = false;

            // XR settings for NRSDK
            PlayerSettings.virtualRealitySupported = false; // NRSDK handles this

            // Development build settings
            EditorUserBuildSettings.development = developmentBuild;
            EditorUserBuildSettings.allowDebugging = developmentBuild;
            EditorUserBuildSettings.connectProfiler = developmentBuild;

            Debug.Log($"[BuildScript] Build settings configured (Development: {developmentBuild})");
        }

        private static BuildPlayerOptions PrepareBuildOptions(bool developmentBuild, bool autoRun)
        {
            // Ensure build directory exists
            if (!Directory.Exists(BUILD_PATH))
            {
                Directory.CreateDirectory(BUILD_PATH);
            }

            // Generate APK filename with timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string buildType = developmentBuild ? "_dev" : "_release";
            string apkFileName = $"{APK_NAME}{buildType}_{timestamp}.apk";
            string fullPath = Path.Combine(BUILD_PATH, apkFileName);

            // Get scenes to build
            string[] scenes = GetScenesToBuild();

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = fullPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            // Add development build options
            if (developmentBuild)
            {
                buildOptions.options |= BuildOptions.Development;
                buildOptions.options |= BuildOptions.AllowDebugging;
                buildOptions.options |= BuildOptions.ConnectWithProfiler;
            }

            // Add auto-run option
            if (autoRun)
            {
                buildOptions.options |= BuildOptions.AutoRunPlayer;
            }

            Debug.Log($"[BuildScript] Build target: {fullPath}");
            Debug.Log($"[BuildScript] Scenes to build: {string.Join(", ", scenes)}");

            return buildOptions;
        }

        private static string[] GetScenesToBuild()
        {
            // Get enabled scenes from build settings
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            System.Collections.Generic.List<string> scenePaths = new System.Collections.Generic.List<string>();

            foreach (EditorBuildSettingsScene scene in buildScenes)
            {
                if (scene.enabled)
                {
                    scenePaths.Add(scene.path);
                }
            }

            // If no scenes in build settings, add current scene
            if (scenePaths.Count == 0)
            {
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
                if (!string.IsNullOrEmpty(currentScene))
                {
                    scenePaths.Add(currentScene);
                }
                else
                {
                    Debug.LogWarning("[BuildScript] No scenes found to build!");
                }
            }

            return scenePaths.ToArray();
        }

        private static void HandleBuildResult(BuildReport report, string buildPath)
        {
            BuildSummary summary = report.summary;

            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"[BuildScript] Build succeeded!");
                    Debug.Log($"[BuildScript] Build size: {FormatBytes(summary.totalSize)}");
                    Debug.Log($"[BuildScript] Build time: {summary.totalTime}");
                    Debug.Log($"[BuildScript] Output: {buildPath}");

                    // Show success dialog
                    bool openFolder = EditorUtility.DisplayDialog(
                        "Build Successful", 
                        $"APK built successfully!\n\nSize: {FormatBytes(summary.totalSize)}\nTime: {summary.totalTime}\n\nLocation: {buildPath}", 
                        "Open Folder", 
                        "OK");

                    if (openFolder)
                    {
                        EditorUtility.RevealInFinder(buildPath);
                    }

                    // Log build info
                    LogBuildInfo(report);
                    break;

                case BuildResult.Failed:
                    Debug.LogError($"[BuildScript] Build failed!");
                    LogBuildErrors(report);
                    
                    EditorUtility.DisplayDialog(
                        "Build Failed", 
                        "Build failed! Check the console for details.", 
                        "OK");
                    break;

                case BuildResult.Cancelled:
                    Debug.LogWarning("[BuildScript] Build was cancelled.");
                    break;

                case BuildResult.Unknown:
                    Debug.LogWarning("[BuildScript] Build result unknown.");
                    break;
            }
        }

        private static void LogBuildInfo(BuildReport report)
        {
            Debug.Log("[BuildScript] === BUILD REPORT ===");
            Debug.Log($"Platform: {report.summary.platform}");
            Debug.Log($"Build GUID: {report.summary.guid}");
            Debug.Log($"Total Warnings: {report.summary.totalWarnings}");
            Debug.Log($"Total Errors: {report.summary.totalErrors}");

            // Log file sizes
            foreach (BuildFile file in report.files)
            {
                Debug.Log($"File: {file.path} ({FormatBytes(file.size)})");
            }
        }

        private static void LogBuildErrors(BuildReport report)
        {
            foreach (BuildStep step in report.steps)
            {
                foreach (BuildStepMessage message in step.messages)
                {
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                    {
                        Debug.LogError($"[BuildScript] {message.content}");
                    }
                    else if (message.type == LogType.Warning)
                    {
                        Debug.LogWarning($"[BuildScript] {message.content}");
                    }
                }
            }
        }

        private static string FormatBytes(ulong bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }

        [MenuItem("Build/Configure for XREAL Air 2")]
        public static void ConfigureForXREAL()
        {
            Debug.Log("[BuildScript] Configuring project for XREAL Air 2...");

            // Set scripting define symbols
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            if (!defines.Contains("XREAL_AR"))
            {
                defines += ";XREAL_AR";
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, defines);
            }

            // Configure XR settings
            PlayerSettings.virtualRealitySupported = false; // NRSDK handles XR

            // Set orientation
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            // Configure rendering
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.use32BitDisplayBuffer = true;

            Debug.Log("[BuildScript] XREAL Air 2 configuration complete!");
        }

        [MenuItem("Build/Validate Build Requirements")]
        public static void ValidateBuildRequirements()
        {
            Debug.Log("[BuildScript] Validating build requirements...");

            bool isValid = true;
            System.Text.StringBuilder issues = new System.Text.StringBuilder();

            // Check Android SDK
            if (string.IsNullOrEmpty(EditorPrefs.GetString("AndroidSdkRoot")))
            {
                issues.AppendLine("- Android SDK path not set");
                isValid = false;
            }

            // Check NDK
            if (string.IsNullOrEmpty(EditorPrefs.GetString("AndroidNdkRoot")))
            {
                issues.AppendLine("- Android NDK path not set");
                isValid = false;
            }

            // Check NRSDK
            if (!Directory.Exists("Assets/NRSDK"))
            {
                issues.AppendLine("- NRSDK not found in Assets folder");
                isValid = false;
            }

            // Check API keys
            string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");
            if (!File.Exists(configPath))
            {
                issues.AppendLine("- config.json not found in StreamingAssets");
                isValid = false;
            }

            // Check scenes
            if (EditorBuildSettings.scenes.Length == 0)
            {
                issues.AppendLine("- No scenes added to build settings");
                isValid = false;
            }

            // Display results
            if (isValid)
            {
                EditorUtility.DisplayDialog("Validation Passed", "All build requirements are met!", "OK");
                Debug.Log("[BuildScript] Build validation passed!");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation Failed", $"Build requirements not met:\n\n{issues.ToString()}", "OK");
                Debug.LogError($"[BuildScript] Build validation failed:\n{issues.ToString()}");
            }
        }

        [MenuItem("Build/Clean Build Cache")]
        public static void CleanBuildCache()
        {
            Debug.Log("[BuildScript] Cleaning build cache...");

            try
            {
                // Clear Library cache
                if (Directory.Exists("Library"))
                {
                    Directory.Delete("Library", true);
                }

                // Clear Temp folder
                if (Directory.Exists("Temp"))
                {
                    Directory.Delete("Temp", true);
                }

                // Clear Builds folder
                if (Directory.Exists(BUILD_PATH))
                {
                    Directory.Delete(BUILD_PATH, true);
                }

                AssetDatabase.Refresh();
                Debug.Log("[BuildScript] Build cache cleaned successfully!");
                
                EditorUtility.DisplayDialog("Cache Cleaned", "Build cache has been cleaned. Unity will reimport assets on next build.", "OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuildScript] Failed to clean cache: {e.Message}");
                EditorUtility.DisplayDialog("Clean Failed", $"Failed to clean cache:\n{e.Message}", "OK");
            }
        }
    }
}