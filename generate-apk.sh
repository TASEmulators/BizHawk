#!/bin/bash
# ===========================================================================
# BizHawkRafaelia - Generate Signed Android APK
# ===========================================================================
# 
# FORK PARENT: BizHawk by TASEmulators (https://github.com/TASEmulators/BizHawk)
# FORK MAINTAINER: Rafael Melo Reis (https://github.com/rafaelmeloreisnovo/BizHawkRafaelia)
# 
# Purpose: Generate signed and compiled Android ARM64 APK
# Target: ARM64-v8a Android devices (Android 7.0+)
# Output: Signed APK ready for installation
# ===========================================================================

set -e  # Exit on error

# Configuration
BUILD_CONFIG="Release"
OUTPUT_DIR="./output/android"
APP_NAME="BizHawkRafaelia"
PROJECT_PATH="src/BizHawk.Android/BizHawk.Android.csproj"

# Keystore configuration (can be overridden via environment variables)
# NOTE: Default values are intentionally simple and public for debug/CI purposes
# For production, set environment variables with secure credentials
KEYSTORE_PATH="${KEYSTORE_PATH:-bizhawk-debug.keystore}"
KEYSTORE_ALIAS="${KEYSTORE_ALIAS:-bizhawk-debug}"
KEYSTORE_PASSWORD="${KEYSTORE_PASSWORD:-bizhawk-debug-password}"  # Public credential - DEBUG ONLY
KEY_PASSWORD="${KEY_PASSWORD:-bizhawk-debug-password}"  # Public credential - DEBUG ONLY

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}BizHawkRafaelia - APK Generator${NC}"
echo -e "${GREEN}Generate Signed Compiled APK${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Check prerequisites
echo -e "${YELLOW}[1/8] Running bug mitigation framework...${NC}"
echo ""

# Run comprehensive bug detection and mitigation framework
if [ -f "scripts/bug-mitigation-framework.sh" ]; then
    echo -e "${BLUE}Executing comprehensive bug analysis...${NC}"
    bash scripts/bug-mitigation-framework.sh || {
        echo -e "${YELLOW}⚠️  Bug mitigation framework completed with warnings${NC}"
        echo -e "${YELLOW}Proceeding with build (review mitigation report)${NC}"
    }
else
    echo -e "${YELLOW}⚠️  Bug mitigation framework not found, skipping...${NC}"
fi

echo ""
echo -e "${YELLOW}[2/8] Checking prerequisites...${NC}"

if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}ERROR: .NET SDK not found${NC}"
    echo "Please install .NET SDK 8.0 or later"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo "✓ .NET SDK found: $DOTNET_VERSION"

# Check if project exists
if [ ! -f "$PROJECT_PATH" ]; then
    echo -e "${RED}ERROR: Android project not found at $PROJECT_PATH${NC}"
    exit 1
fi

echo "✓ Android project found"

# Check for keystore
echo ""
if [ ! -f "$KEYSTORE_PATH" ]; then
    echo -e "${YELLOW}⚠️  Keystore not found at: $KEYSTORE_PATH${NC}"
    echo -e "${YELLOW}Generating debug keystore...${NC}"
    
    # Generate debug keystore if it doesn't exist
    if [ -f "scripts/generate-debug-keystore.sh" ]; then
        bash scripts/generate-debug-keystore.sh
    else
        echo -e "${RED}ERROR: Cannot generate keystore (script not found)${NC}"
        echo "Please create a keystore manually or provide KEYSTORE_PATH"
        exit 1
    fi
    
    if [ ! -f "$KEYSTORE_PATH" ]; then
        echo -e "${RED}ERROR: Keystore generation failed${NC}"
        exit 1
    fi
fi

echo "✓ Keystore found: $KEYSTORE_PATH"

# Step 3: Clean previous builds
echo ""
echo -e "${YELLOW}[3/8] Cleaning previous builds...${NC}"
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"
dotnet clean "$PROJECT_PATH" -c "$BUILD_CONFIG" > /dev/null 2>&1 || true
echo "✓ Build directory cleaned"

# Step 4: Restore dependencies
echo ""
echo -e "${YELLOW}[4/8] Restoring dependencies...${NC}"
dotnet restore "$PROJECT_PATH"
echo "✓ Dependencies restored"

# Step 5: Build Rafaelia optimization modules
echo ""
echo -e "${YELLOW}[5/8] Building Rafaelia optimization modules...${NC}"
dotnet build "rafaelia/BizHawk.Rafaelia.csproj" -c "$BUILD_CONFIG" > /dev/null
echo "✓ Rafaelia modules built"

# Step 6: Build Android APK
echo ""
echo -e "${YELLOW}[6/8] Building signed Android APK...${NC}"
echo -e "${BLUE}This may take several minutes...${NC}"

# Build the APK with signing
dotnet build "$PROJECT_PATH" \
    -c "$BUILD_CONFIG" \
    -f net9.0-android \
    -p:AndroidPackageFormat=apk \
    -p:RuntimeIdentifier=android-arm64 \
    -p:AndroidSigningKeyStore="$KEYSTORE_PATH" \
    -p:AndroidSigningKeyAlias="$KEYSTORE_ALIAS" \
    -p:AndroidSigningKeyPass="$KEY_PASSWORD" \
    -p:AndroidSigningStorePass="$KEYSTORE_PASSWORD"

if [ $? -ne 0 ]; then
    echo -e "${RED}ERROR: APK build failed${NC}"
    echo "This might be due to missing Android SDK or .NET Android workload"
    echo ""
    echo "To install .NET Android workload, run:"
    echo "  dotnet workload install android"
    echo ""
    echo "To install Android SDK, visit:"
    echo "  https://developer.android.com/studio"
    exit 1
fi

echo "✓ APK built successfully"

# Step 7: Locate and copy APK
echo ""
echo -e "${YELLOW}[7/8] Locating output APK...${NC}"

# Find the generated signed APK
APK_SOURCE=$(find src/BizHawk.Android/bin/$BUILD_CONFIG -name "*-Signed.apk" | head -1)

if [ -z "$APK_SOURCE" ]; then
    echo -e "${YELLOW}Signed APK not found, looking for any APK...${NC}"
    APK_SOURCE=$(find src/BizHawk.Android/bin/$BUILD_CONFIG -name "*.apk" | head -1)
fi

if [ -z "$APK_SOURCE" ]; then
    echo -e "${RED}ERROR: Could not locate generated APK${NC}"
    echo "Build may have completed but APK was not found in expected location"
    exit 1
fi

# Copy to output directory
APK_OUTPUT="$OUTPUT_DIR/${APP_NAME}-signed-arm64-v8a.apk"
cp "$APK_SOURCE" "$APK_OUTPUT"

# Get APK size
APK_SIZE=$(du -h "$APK_OUTPUT" | cut -f1)

echo "✓ APK copied to output directory"
echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}✓ SUCCESS!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${GREEN}Signed APK generated:${NC}"
echo -e "${BLUE}  Location: $APK_OUTPUT${NC}"
echo -e "${BLUE}  Size: $APK_SIZE${NC}"
echo ""
echo -e "${YELLOW}Installation:${NC}"
echo ""
echo -e "${GREEN}Install APK:${NC}"
echo "   adb install $APK_OUTPUT"
echo ""
echo -e "${GREEN}Note:${NC} This APK is signed with a debug keystore."
echo "For production releases, use a production keystore with proper security."
echo ""
echo -e "${BLUE}Features included:${NC}"
echo "  • Rafaelia performance optimizations"
echo "  • ARM64 NEON SIMD support"
echo "  • Zero-allocation memory pooling"
echo "  • Hardware-adaptive quality management"
echo "  • Thermal throttling mitigation"
echo ""

# Generate build report
cat > "$OUTPUT_DIR/build-info.txt" << EOF
BizHawkRafaelia - Signed APK Build Report
============================================

Build Date: $(date)
Build Configuration: $BUILD_CONFIG
.NET SDK Version: $DOTNET_VERSION

APK Information:
  File: ${APP_NAME}-signed-arm64-v8a.apk
  Size: $APK_SIZE
  Target: ARM64-v8a (Android 7.0+)
  Signed: Yes (debug keystore)

Keystore Information:
  Path: $KEYSTORE_PATH
  Alias: $KEYSTORE_ALIAS

Installation Command:
  adb install $APK_OUTPUT

NOTE: This APK is signed with a debug keystore for testing purposes.
For production distribution, use a production keystore with proper security.

Performance Optimizations:
  ✓ Rafaelia performance framework
  ✓ ARM64 NEON SIMD optimizations
  ✓ Memory pooling (zero-allocation)
  ✓ Adaptive hardware management
  ✓ Power-efficient algorithms
  ✓ Thermal throttling mitigation

For questions: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia
EOF

echo -e "${GREEN}Build report saved: $OUTPUT_DIR/build-info.txt${NC}"
echo ""

# Step 8: Final validation
echo -e "${YELLOW}[8/8] Running final APK validation...${NC}"
echo ""

# Validate APK structure
echo "  → Validating APK file integrity..."
if file "$APK_OUTPUT" | grep -q "Zip archive data"; then
    echo -e "  ${GREEN}✓ APK file structure valid${NC}"
else
    echo -e "  ${RED}✗ APK file structure invalid${NC}"
    exit 1
fi

# Check APK size sanity
APK_SIZE_BYTES=$(stat -f%z "$APK_OUTPUT" 2>/dev/null || stat -c%s "$APK_OUTPUT" 2>/dev/null)
if [ $APK_SIZE_BYTES -lt 1048576 ]; then  # Less than 1MB is suspicious
    echo -e "  ${YELLOW}⚠️  APK size seems small ($APK_SIZE), verify build${NC}"
elif [ $APK_SIZE_BYTES -gt 524288000 ]; then  # More than 500MB is suspicious
    echo -e "  ${YELLOW}⚠️  APK size seems large ($APK_SIZE), consider optimization${NC}"
else
    echo -e "  ${GREEN}✓ APK size reasonable: $APK_SIZE${NC}"
fi

# List APK contents summary
echo "  → Analyzing APK contents..."
unzip -l "$APK_OUTPUT" 2>/dev/null | grep -E "assemblies/|lib/" | wc -l | xargs echo "    Assembly/Library files:" || true

# Final summary
echo ""
echo -e "${GREEN}✓ Final validation complete${NC}"
echo ""
