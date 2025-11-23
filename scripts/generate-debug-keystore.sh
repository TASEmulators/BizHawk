#!/bin/bash
# ===========================================================================
# BizHawkRafaelia - Generate Debug Keystore for APK Signing
# ===========================================================================
# 
# FORK PARENT: BizHawk by TASEmulators (https://github.com/TASEmulators/BizHawk)
# FORK MAINTAINER: Rafael Melo Reis (https://github.com/rafaelmeloreisnovo/BizHawkRafaelia)
# 
# Purpose: Generate a debug keystore for signing Android APK
# Usage: ./scripts/generate-debug-keystore.sh
# Output: Creates bizhawk-debug.keystore in current directory
# ===========================================================================

set -e

# Configuration
# NOTE: These credentials are INTENTIONALLY simple and public for debug/CI purposes
# For production, generate a separate keystore with strong, private passwords
KEYSTORE_NAME="bizhawk-debug.keystore"
KEYSTORE_ALIAS="bizhawk-debug"
KEYSTORE_PASSWORD="bizhawk-debug-password"  # Public credential - DEBUG ONLY
VALIDITY_DAYS=10000

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}BizHawkRafaelia - Debug Keystore Generator${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Check if keytool is available
if ! command -v keytool &> /dev/null; then
    echo -e "${RED}ERROR: keytool not found${NC}"
    echo "keytool is part of the Java Development Kit (JDK)"
    echo "Please install JDK 17 or later"
    exit 1
fi

echo -e "${BLUE}Generating debug keystore...${NC}"
echo ""

# Generate keystore
keytool -genkey -v \
    -keystore "$KEYSTORE_NAME" \
    -alias "$KEYSTORE_ALIAS" \
    -keyalg RSA \
    -keysize 2048 \
    -validity $VALIDITY_DAYS \
    -storepass "$KEYSTORE_PASSWORD" \
    -keypass "$KEYSTORE_PASSWORD" \
    -dname "CN=BizHawkRafaelia Debug, OU=Development, O=BizHawkRafaelia, L=Development, ST=Development, C=BR"

if [ $? -ne 0 ]; then
    echo -e "${RED}ERROR: Failed to generate keystore${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}✓ Debug keystore generated successfully!${NC}"
echo ""
echo -e "${YELLOW}Keystore Information:${NC}"
echo "  File: $KEYSTORE_NAME"
echo "  Alias: $KEYSTORE_ALIAS"
echo "  Password: $KEYSTORE_PASSWORD"
echo "  Validity: $VALIDITY_DAYS days"
echo ""
echo -e "${YELLOW}For GitHub Actions:${NC}"
echo "1. Convert keystore to base64:"
echo "   base64 -i $KEYSTORE_NAME | tr -d '\\n' > keystore.base64"
echo ""
echo "2. Add to GitHub Secrets:"
echo "   - ANDROID_KEYSTORE_BASE64: (content of keystore.base64)"
echo "   - ANDROID_KEYSTORE_PASSWORD: $KEYSTORE_PASSWORD"
echo "   - ANDROID_KEY_ALIAS: $KEYSTORE_ALIAS"
echo "   - ANDROID_KEY_PASSWORD: $KEYSTORE_PASSWORD"
echo ""
echo -e "${RED}⚠️  WARNING: This is a DEBUG keystore for development and CI only!${NC}"
echo -e "${RED}Do NOT use this keystore for production releases!${NC}"
echo ""
echo -e "${BLUE}For production releases:${NC}"
echo "1. Generate a production keystore with a strong password"
echo "2. Store it securely and NEVER commit it to the repository"
echo "3. Use it only for official releases"
echo ""
