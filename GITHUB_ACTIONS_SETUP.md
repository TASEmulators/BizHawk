# GitHub Actions Setup for APK Signing

This document provides instructions for the repository maintainer to set up APK signing in GitHub Actions.

## Required GitHub Secrets

To enable automatic APK signing in GitHub Actions, you need to add the following secret:

### 1. ANDROID_KEYSTORE_BASE64

This secret contains the debug keystore encoded in base64 format.

#### Steps to Create and Add:

1. **Generate the debug keystore** (if not already done):
   ```bash
   cd /path/to/BizHawkRafaelia
   ./scripts/generate-debug-keystore.sh
   ```

2. **Convert keystore to base64**:
   ```bash
   base64 -w 0 bizhawk-debug.keystore > keystore.base64
   ```
   
   Or on macOS:
   ```bash
   base64 -i bizhawk-debug.keystore | tr -d '\n' > keystore.base64
   ```

3. **Copy the base64 content**:
   ```bash
   cat keystore.base64
   # Copy the entire output
   ```

4. **Add to GitHub Secrets**:
   - Go to: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/settings/secrets/actions
   - Click "New repository secret"
   - Name: `ANDROID_KEYSTORE_BASE64`
   - Value: Paste the entire base64 content
   - Click "Add secret"

5. **Clean up** (important for security):
   ```bash
   rm keystore.base64
   # Keep bizhawk-debug.keystore in a secure location if you need it for local builds
   ```

## Verification

After adding the secret:

1. Push a commit to the `master` branch or any branch that triggers the workflow
2. Go to Actions tab: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/actions
3. Find the "Build and Upload APK" workflow run
4. Check that the "Setup Android Keystore for Signing" step succeeds
5. Verify the build completes and creates a signed APK artifact

## Keystore Details

The debug keystore has these credentials (public, for development only):

- **Alias**: `bizhawk-debug`
- **Store Password**: `bizhawk-debug-password`
- **Key Password**: `bizhawk-debug-password`

These are hardcoded in the workflow file and are intentionally public for development purposes.

## Security Notes

⚠️ **Important**: This is a DEBUG keystore intended for:
- Automated CI/CD builds
- Development testing
- Internal distribution

For production releases:
1. Generate a separate production keystore with strong passwords
2. Store it securely (NOT in GitHub)
3. Sign production releases manually or via a secure signing service
4. See [APK_SIGNING_GUIDE.md](APK_SIGNING_GUIDE.md) for production signing instructions

## Troubleshooting

### Secret not working

If the workflow fails at the "Setup Android Keystore for Signing" step:

1. **Check secret name**: Must be exactly `ANDROID_KEYSTORE_BASE64`
2. **Check base64 encoding**: Ensure no newlines in the base64 string
3. **Regenerate secret**: Delete and recreate the secret with fresh base64 encoding

### Cannot decode base64

Error: `base64: invalid input`

**Solution**: Make sure you used `base64 -w 0` (Linux) or `base64 -i ... | tr -d '\n'` (macOS) to create a single-line base64 string.

### Keystore not found after decode

Error: `Keystore file not found at $HOME/bizhawk-debug.keystore`

**Solution**: Check that the base64 decoding step in the workflow succeeded. The file should be created in `$HOME/bizhawk-debug.keystore`.

## Alternative: Manual Setup

If you prefer not to use the automatic keystore decoding, you can:

1. Upload the keystore file to a secure artifact storage
2. Download it during the workflow
3. Use it for signing

However, the base64 secret approach is recommended as it's more secure and simpler.

---

**Last Updated**: 2025-11-23  
**Maintainer**: Rafael Melo Reis (rafaelmeloreisnovo)
