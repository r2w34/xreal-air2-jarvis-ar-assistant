@echo off
REM XREAL Air 2 Jarvis AR Assistant - APK Build Script for Windows
REM This script builds the Android APK using Unity command line

setlocal enabledelayedexpansion

REM Configuration
set UNITY_PATH=C:\Program Files\Unity\Hub\Editor\2021.3.33f1\Editor\Unity.exe
set PROJECT_PATH=%cd%
set BUILD_PATH=%PROJECT_PATH%\Builds
set LOG_FILE=%BUILD_PATH%\build.log

REM Generate timestamp for APK name
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "YY=%dt:~2,2%" & set "YYYY=%dt:~0,4%" & set "MM=%dt:~4,2%" & set "DD=%dt:~6,2%"
set "HH=%dt:~8,2%" & set "Min=%dt:~10,2%" & set "Sec=%dt:~12,2%"
set APK_NAME=JarvisAR_%YYYY%%MM%%DD%_%HH%%Min%%Sec%.apk

echo === XREAL Air 2 Jarvis AR Assistant - Build Script ===
echo Project Path: %PROJECT_PATH%
echo Build Path: %BUILD_PATH%
echo APK Name: %APK_NAME%
echo.

REM Check if Unity exists
if not exist "%UNITY_PATH%" (
    echo Error: Unity not found at %UNITY_PATH%
    echo Please update UNITY_PATH in this script to point to your Unity installation
    pause
    exit /b 1
)

REM Create build directory
if not exist "%BUILD_PATH%" mkdir "%BUILD_PATH%"

echo Step 1: Validating project structure...

REM Check for required files
set REQUIRED_FILES=Assets\Scripts\Core\JarvisManager.cs Assets\Scripts\Voice\VoiceInputManager.cs Assets\Scripts\AI\ChatGPTManager.cs Assets\StreamingAssets\config.json Plugins\Android\AndroidManifest.xml

for %%f in (%REQUIRED_FILES%) do (
    if not exist "%%f" (
        echo Error: Required file not found: %%f
        pause
        exit /b 1
    )
)

echo ✓ Project structure validated

echo Step 2: Checking API keys configuration...

REM Check if config.json has placeholder values
findstr /C:"YOUR_" "Assets\StreamingAssets\config.json" >nul
if !errorlevel! equ 0 (
    echo Warning: Found placeholder API keys in config.json
    echo Please update with your actual API keys before building for production
)

echo ✓ Configuration checked

echo Step 3: Building Android APK...

REM Unity command line build
"%UNITY_PATH%" -batchmode -quit -projectPath "%PROJECT_PATH%" -buildTarget Android -executeMethod XREALJarvis.Editor.BuildScript.BuildAndroid -logFile "%LOG_FILE%" -nographics

set BUILD_EXIT_CODE=%errorlevel%

echo.
echo === Build Results ===

if %BUILD_EXIT_CODE% equ 0 (
    echo ✓ Build completed successfully!
    
    REM Find the generated APK (get the newest .apk file)
    for /f "delims=" %%i in ('dir /b /o-d "%BUILD_PATH%\*.apk" 2^>nul') do (
        set GENERATED_APK=%BUILD_PATH%\%%i
        goto :found_apk
    )
    
    :found_apk
    if exist "!GENERATED_APK!" (
        for %%A in ("!GENERATED_APK!") do set APK_SIZE=%%~zA
        set /a APK_SIZE_MB=!APK_SIZE!/1024/1024
        echo APK Location: !GENERATED_APK!
        echo APK Size: !APK_SIZE_MB! MB
        
        REM Show installation instructions
        echo.
        echo === Installation Instructions ===
        echo 1. Enable Developer Options on your Android device
        echo 2. Enable USB Debugging
        echo 3. Connect your device via USB
        echo 4. Install the APK:
        echo    adb install -r "!GENERATED_APK!"
        echo.
        echo 5. For XREAL Air 2:
        echo    • Install XREAL Nebula app on your device
        echo    • Connect XREAL Air 2 glasses
        echo    • Launch Jarvis AR through Nebula
        
    ) else (
        echo Warning: APK file not found in build directory
    )
    
) else (
    echo ✗ Build failed with exit code: %BUILD_EXIT_CODE%
    echo Check the build log for details: %LOG_FILE%
    
    if exist "%LOG_FILE%" (
        echo.
        echo === Last 20 lines of build log ===
        powershell "Get-Content '%LOG_FILE%' | Select-Object -Last 20"
    )
    
    pause
    exit /b %BUILD_EXIT_CODE%
)

echo.
echo === Build Log Location ===
echo Full build log: %LOG_FILE%

REM Open build folder
if exist "%BUILD_PATH%" (
    echo Opening build folder...
    explorer "%BUILD_PATH%"
)

echo Build script completed!
pause