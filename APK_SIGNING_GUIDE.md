# BizHawkRafaelia - APK Signing Guide

**Author**: Rafael Melo Reis (rafaelmeloreisnovo)  
**Project**: BizHawkRafaelia - Fork of BizHawk with Rafaelia performance optimizations

---

## Overview

This guide explains how APK signing works in BizHawkRafaelia and how to configure it for different scenarios (development, CI/CD, and production).

## Why Sign APKs?

Android requires all APKs to be digitally signed before they can be installed on a device. Signing:

✅ **Verifies the app's identity** - Ensures the APK comes from you  
✅ **Prevents tampering** - Detects if the APK has been modified  
✅ **Enables updates** - Only APKs signed with the same key can update each other  
✅ **Required for distribution** - Google Play and other stores require signed APKs  

---

## Signing Types

### 1. Debug Signing (Development & CI)

**Use for**: Local development, automated CI builds, internal testing

**Characteristics**:
- Uses a debug keystore with known credentials
- Quick to set up
- Not suitable for production releases
- Can be shared among team members for testing

**Security**: ⚠️ Low - credentials are public

### 2. Release Signing (Production)

**Use for**: Official releases, Google Play distribution, public downloads

**Characteristics**:
- Uses a release keystore with strong, private credentials
- Must be kept secure and confidential
- Used for all production releases
- Lost keystore = cannot update app!

**Security**: ✅ High - credentials must be kept secret

---

## Current Implementation

BizHawkRafaelia is currently configured to use **debug signing** for:
- Automated builds in GitHub Actions
- Local development builds
- Testing and CI/CD

This allows anyone to build and test the APK without needing production credentials.

---

## Quick Start

### Local Build (Debug Signed)

```bash
# Clone the repository
git clone https://github.com/rafaelmeloreisnovo/BizHawkRafaelia.git
cd BizHawkRafaelia

# Run the APK generation script
./generate-apk.sh
```

The script will:
1. Check for an existing debug keystore
2. Generate one if it doesn't exist
3. Build and sign the APK automatically

**Output**: `./output/android/BizHawkRafaelia-signed-arm64-v8a.apk`

### GitHub Actions (Automatic)

APKs are automatically built and signed on every commit:

1. Go to [GitHub Actions](../../actions/workflows/build-and-upload-apk.yml)
2. Find the latest successful workflow run (✅ green checkmark)
3. Download the artifact: `BizHawkRafaelia-Signed-APK-*`
4. Extract and install the APK

---

## Configuration

### Debug Keystore Details

The default debug keystore has these settings:

| Property | Value |
|----------|-------|
| **File** | `bizhawk-debug.keystore` |
| **Alias** | `bizhawk-debug` |
| **Store Password** | `bizhawk-debug-password` |
| **Key Password** | `bizhawk-debug-password` |
| **Validity** | 10,000 days (~27 years) |
| **Algorithm** | RSA 2048-bit |

⚠️ **Security Note**: These credentials are public and should only be used for development/testing!

### Environment Variables

You can override the default keystore settings using environment variables:

```bash
export KEYSTORE_PATH="/path/to/your/keystore.jks"
export KEYSTORE_ALIAS="your-key-alias"
export KEYSTORE_PASSWORD="your-store-password"
export KEY_PASSWORD="your-key-password"

./generate-apk.sh
```

---

## GitHub Actions Setup

The APK signing in GitHub Actions works as follows:

### 1. Keystore Storage

The debug keystore is stored as a GitHub Secret in base64 format:

1. **Generate keystore** (if not already generated):
   ```bash
   ./scripts/generate-debug-keystore.sh
   ```

2. **Convert to base64**:
   ```bash
   base64 -w 0 bizhawk-debug.keystore > keystore.base64
   ```

3. **Add to GitHub Secrets**:
   - Go to repository Settings → Secrets and variables → Actions
   - Add secret: `ANDROID_KEYSTORE_BASE64` (content of `keystore.base64`)

### 2. Workflow Configuration

The workflow automatically:
1. Decodes the keystore from the secret
2. Configures signing parameters
3. Builds and signs the APK
4. Uploads the signed APK as an artifact

**File**: `.github/workflows/build-and-upload-apk.yml`

---

## Production Release Signing

For official production releases, follow these steps:

### 1. Generate Production Keystore

**⚠️ CRITICAL**: Keep this keystore and its passwords SECURE! If you lose it, you cannot update your app!

```bash
keytool -genkey -v \
  -keystore bizhawk-release.keystore \
  -alias bizhawk-release \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000 \
  -storepass "YOUR_STRONG_PASSWORD" \
  -keypass "YOUR_STRONG_PASSWORD" \
  -dname "CN=Your Name, OU=Your Organization, O=Your Company, L=Your City, ST=Your State, C=Your Country"
```

### 2. Secure Storage

**DO**:
- ✅ Store in a secure password manager
- ✅ Keep encrypted backups in multiple locations
- ✅ Limit access to authorized personnel only
- ✅ Use strong, unique passwords
- ✅ Document the keystore location securely

**DON'T**:
- ❌ Commit to version control (even private repos!)
- ❌ Share via email or chat
- ❌ Store in cloud without encryption
- ❌ Use weak passwords
- ❌ Share credentials with unauthorized people

### 3. Build with Production Keystore

```bash
export KEYSTORE_PATH="/secure/path/bizhawk-release.keystore"
export KEYSTORE_ALIAS="bizhawk-release"
export KEYSTORE_PASSWORD="YOUR_STRONG_PASSWORD"
export KEY_PASSWORD="YOUR_STRONG_PASSWORD"

./generate-apk.sh
```

### 4. Verify Signature

```bash
# Verify the APK is signed correctly
apksigner verify --verbose output/android/BizHawkRafaelia-signed-arm64-v8a.apk

# Check certificate details
keytool -printcert -jarfile output/android/BizHawkRafaelia-signed-arm64-v8a.apk
```

---

## Manual Signing

If you prefer to sign manually or need to re-sign an existing APK:

### Using apksigner (Recommended)

```bash
# Sign the APK
apksigner sign \
  --ks /path/to/keystore.jks \
  --ks-key-alias your-alias \
  --ks-pass pass:your-store-password \
  --key-pass pass:your-key-password \
  --out BizHawkRafaelia-signed.apk \
  BizHawkRafaelia-unsigned.apk

# Verify
apksigner verify BizHawkRafaelia-signed.apk
```

### Using jarsigner (Legacy)

```bash
# Sign the APK
jarsigner -verbose \
  -sigalg SHA256withRSA \
  -digestalg SHA-256 \
  -keystore /path/to/keystore.jks \
  BizHawkRafaelia-unsigned.apk \
  your-alias

# Verify
jarsigner -verify -verbose -certs BizHawkRafaelia-unsigned.apk
```

---

## Project File Configuration

The Android project is configured to accept signing parameters:

**File**: `src/BizHawk.Android/BizHawk.Android.csproj`

```xml
<PropertyGroup>
  <AndroidSigningKeyStore>$(AndroidSigningKeyStore)</AndroidSigningKeyStore>
  <AndroidSigningKeyAlias>$(AndroidSigningKeyAlias)</AndroidSigningKeyAlias>
  <AndroidSigningKeyPass>$(AndroidSigningKeyPass)</AndroidSigningKeyPass>
  <AndroidSigningStorePass>$(AndroidSigningStorePass)</AndroidSigningStorePass>
</PropertyGroup>
```

These properties can be set via:
- Command line: `-p:AndroidSigningKeyStore=/path/to/keystore`
- Environment variables: Automatically mapped by MSBuild
- Project file: Hardcoded (not recommended for security)

---

## Troubleshooting

### "Keystore not found"

**Solution**: The script will automatically generate a debug keystore. If this fails:
```bash
./scripts/generate-debug-keystore.sh
```

### "Invalid keystore password"

**Solution**: Ensure environment variables are set correctly:
```bash
export KEYSTORE_PASSWORD="correct-password"
export KEY_PASSWORD="correct-password"
```

### "APK signature verification failed"

**Solution**: Check that the keystore passwords are correct and the keystore file is not corrupted:
```bash
keytool -list -v -keystore /path/to/keystore.jks
```

### "Cannot update app - signature mismatch"

**Cause**: Trying to update an app that was signed with a different keystore.

**Solution**: 
1. Uninstall the old app first
2. Install the new APK
3. For future updates, always use the same keystore

### "jarsigner/apksigner not found"

**Solution**: Install Java Development Kit (JDK):
```bash
# Ubuntu/Debian
sudo apt install openjdk-17-jdk

# macOS
brew install openjdk@17

# Windows
# Download from https://adoptium.net/
```

---

## Best Practices

### For Development

1. ✅ Use the debug keystore for all non-production builds
2. ✅ Share the debug keystore among team members
3. ✅ Regenerate debug keystore periodically
4. ✅ Never use debug keystore for production

### For Production

1. ✅ Generate a unique production keystore
2. ✅ Use strong, random passwords (20+ characters)
3. ✅ Store keystore and passwords in a secure password manager
4. ✅ Keep encrypted backups in multiple secure locations
5. ✅ Limit access to authorized personnel only
6. ✅ Never commit keystore to version control
7. ✅ Document the keystore location (securely)
8. ✅ Test the backup keystore periodically

### For CI/CD

1. ✅ Use GitHub Secrets for keystore storage
2. ✅ Use debug keystore for automated builds
3. ✅ Only use production keystore for official releases
4. ✅ Restrict secret access to necessary workflows only
5. ✅ Rotate credentials periodically

---

## Security Checklist

Before releasing to production, verify:

- [ ] Production keystore generated with strong password
- [ ] Keystore and passwords stored securely (password manager)
- [ ] Encrypted backups created in multiple locations
- [ ] Keystore is NOT committed to version control
- [ ] Access restricted to authorized personnel only
- [ ] APK signature verified with `apksigner verify`
- [ ] Certificate details confirmed with `keytool -printcert`
- [ ] Backup keystore tested and working
- [ ] Documentation updated with keystore location (secure doc)

---

## References

- **Android Developer Guide**: [Sign your app](https://developer.android.com/studio/publish/app-signing)
- **APK Signature Scheme v2**: [Technical details](https://source.android.com/docs/security/features/apksigning/v2)
- **.NET MAUI Signing**: [Configure Android signing](https://learn.microsoft.com/en-us/dotnet/maui/android/deployment/overview)

---

## Support

For questions or issues:

- **GitHub Issues**: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/issues
- **Documentation**: See APK_GENERATION_README.md
- **Parent Project**: https://github.com/TASEmulators/BizHawk

---

**Amor, Luz e Coerência**  
💚 BizHawkRafaelia - Secure and High-Performance Emulation for ARM64 Mobile Devices
