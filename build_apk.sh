#!/bin/bash

# XREAL Air 2 Jarvis AR Assistant - APK Build Script
# This script builds the Android APK using Unity command line

set -e  # Exit on any error

# Configuration
UNITY_PATH="/Applications/Unity/Hub/Editor/2021.3.33f1/Unity.app/Contents/MacOS/Unity"  # Mac path
# UNITY_PATH="C:/Program Files/Unity/Hub/Editor/2021.3.33f1/Editor/Unity.exe"  # Windows path
# UNITY_PATH="/opt/unity/Editor/Unity"  # Linux path

PROJECT_PATH="$(pwd)"
BUILD_PATH="$PROJECT_PATH/Builds"
LOG_FILE="$BUILD_PATH/build.log"
APK_NAME="JarvisAR_$(date +%Y%m%d_%H%M%S).apk"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== XREAL Air 2 Jarvis AR Assistant - Build Script ===${NC}"
echo -e "${BLUE}Project Path: $PROJECT_PATH${NC}"
echo -e "${BLUE}Build Path: $BUILD_PATH${NC}"
echo -e "${BLUE}APK Name: $APK_NAME${NC}"
echo ""

# Check if Unity exists
if [ ! -f "$UNITY_PATH" ]; then
    echo -e "${RED}Error: Unity not found at $UNITY_PATH${NC}"
    echo -e "${YELLOW}Please update UNITY_PATH in this script to point to your Unity installation${NC}"
    exit 1
fi

# Create build directory
mkdir -p "$BUILD_PATH"

echo -e "${YELLOW}Step 1: Validating project structure...${NC}"

# Check for required files
REQUIRED_FILES=(
    "Assets/Scripts/Core/JarvisManager.cs"
    "Assets/Scripts/Voice/VoiceInputManager.cs"
    "Assets/Scripts/AI/ChatGPTManager.cs"
    "Assets/StreamingAssets/config.json"
    "Plugins/Android/AndroidManifest.xml"
)

for file in "${REQUIRED_FILES[@]}"; do
    if [ ! -f "$file" ]; then
        echo -e "${RED}Error: Required file not found: $file${NC}"
        exit 1
    fi
done

echo -e "${GREEN}✓ Project structure validated${NC}"

echo -e "${YELLOW}Step 2: Checking API keys configuration...${NC}"

# Check if config.json has placeholder values
if grep -q "YOUR_.*_API_KEY_HERE" "Assets/StreamingAssets/config.json"; then
    echo -e "${YELLOW}Warning: Found placeholder API keys in config.json${NC}"
    echo -e "${YELLOW}Please update with your actual API keys before building for production${NC}"
fi

echo -e "${GREEN}✓ Configuration checked${NC}"

echo -e "${YELLOW}Step 3: Building Android APK...${NC}"

# Unity command line build
"$UNITY_PATH" \
    -batchmode \
    -quit \
    -projectPath "$PROJECT_PATH" \
    -buildTarget Android \
    -executeMethod XREALJarvis.Editor.BuildScript.BuildAndroid \
    -logFile "$LOG_FILE" \
    -nographics

BUILD_EXIT_CODE=$?

echo ""
echo -e "${BLUE}=== Build Results ===${NC}"

if [ $BUILD_EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✓ Build completed successfully!${NC}"
    
    # Find the generated APK
    GENERATED_APK=$(find "$BUILD_PATH" -name "*.apk" -type f -printf '%T@ %p\n' | sort -n | tail -1 | cut -d' ' -f2-)
    
    if [ -f "$GENERATED_APK" ]; then
        APK_SIZE=$(du -h "$GENERATED_APK" | cut -f1)
        echo -e "${GREEN}APK Location: $GENERATED_APK${NC}"
        echo -e "${GREEN}APK Size: $APK_SIZE${NC}"
        
        # Show installation instructions
        echo ""
        echo -e "${BLUE}=== Installation Instructions ===${NC}"
        echo -e "${YELLOW}1. Enable Developer Options on your Android device${NC}"
        echo -e "${YELLOW}2. Enable USB Debugging${NC}"
        echo -e "${YELLOW}3. Connect your device via USB${NC}"
        echo -e "${YELLOW}4. Install the APK:${NC}"
        echo -e "   ${GREEN}adb install -r \"$GENERATED_APK\"${NC}"
        echo ""
        echo -e "${YELLOW}5. For XREAL Air 2:${NC}"
        echo -e "   ${GREEN}• Install XREAL Nebula app on your device${NC}"
        echo -e "   ${GREEN}• Connect XREAL Air 2 glasses${NC}"
        echo -e "   ${GREEN}• Launch Jarvis AR through Nebula${NC}"
        
    else
        echo -e "${YELLOW}Warning: APK file not found in build directory${NC}"
    fi
    
else
    echo -e "${RED}✗ Build failed with exit code: $BUILD_EXIT_CODE${NC}"
    echo -e "${YELLOW}Check the build log for details: $LOG_FILE${NC}"
    
    if [ -f "$LOG_FILE" ]; then
        echo ""
        echo -e "${BLUE}=== Last 20 lines of build log ===${NC}"
        tail -20 "$LOG_FILE"
    fi
    
    exit $BUILD_EXIT_CODE
fi

echo ""
echo -e "${BLUE}=== Build Log Location ===${NC}"
echo -e "${GREEN}Full build log: $LOG_FILE${NC}"

# Optional: Open build folder
if command -v open >/dev/null 2>&1; then
    # macOS
    echo -e "${YELLOW}Opening build folder...${NC}"
    open "$BUILD_PATH"
elif command -v xdg-open >/dev/null 2>&1; then
    # Linux
    echo -e "${YELLOW}Opening build folder...${NC}"
    xdg-open "$BUILD_PATH"
elif command -v explorer >/dev/null 2>&1; then
    # Windows (if running in Git Bash or similar)
    echo -e "${YELLOW}Opening build folder...${NC}"
    explorer "$BUILD_PATH"
fi

echo -e "${GREEN}Build script completed!${NC}"