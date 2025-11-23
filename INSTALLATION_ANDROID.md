# BizHawkRafaelia - Android Installation Guide

**Author**: Rafael Melo Reis  
**Last Updated**: 2025-11-23  
**Version**: 1.0  

---

## Table of Contents

1. [System Requirements](#system-requirements)
2. [Prerequisites](#prerequisites)
3. [Installation Methods](#installation-methods)
4. [Post-Installation Setup](#post-installation-setup)
5. [Controller Configuration](#controller-configuration)
6. [Performance Optimization](#performance-optimization)
7. [Troubleshooting](#troubleshooting)
8. [Building from Source](#building-from-source)

---

## System Requirements

### Minimum Requirements

- **Android Version**: 7.0 Nougat (API 24) or later
- **Architecture**: ARM64-v8a (64-bit ARM)
- **RAM**: 3 GB (4 GB recommended)
- **Storage**: 500 MB free space (1 GB recommended)
- **GPU**: OpenGL ES 3.0+ or Vulkan 1.0+
- **CPU**: Snapdragon 665 / MediaTek Helio G80 or equivalent

### Recommended Requirements

- **Android Version**: 11+ (API 30+)
- **Architecture**: ARM64-v8a
- **RAM**: 6-8 GB
- **Storage**: 2 GB free space
- **GPU**: Adreno 618+ / Mali-G72+
- **CPU**: Snapdragon 865+ / MediaTek Dimensity 1100+

### Verified Compatible Devices

| Device | Android | RAM | Performance | Notes |
|--------|---------|-----|-------------|-------|
| Google Pixel 7 Pro | 13+ | 12 GB | Excellent | Best tested device |
| Samsung Galaxy S21 | 13+ | 8 GB | Excellent | Exynos may vary |
| OnePlus 9 | 13 | 8 GB | Excellent | Snapdragon 888 |
| Xiaomi Mi 11 | 12+ | 8 GB | Very Good | MIUI optimizations |
| Samsung Galaxy S10 | 12+ | 8 GB | Good | Older but capable |
| Google Pixel 5 | 13+ | 8 GB | Good | Good efficiency |
| Poco F3 | 12+ | 6 GB | Good | Budget option |
| Poco X3 Pro | 11+ | 6 GB | Fair | Entry-level gaming |

### Incompatible Devices

- **32-bit ARM devices** (ARMv7): Not supported
- **x86/x86_64 Android devices**: Not currently supported
- **Devices with < 2 GB RAM**: Will crash
- **Android < 7.0**: Incompatible APIs

---

## Prerequisites

### Enable Developer Options

```
Settings → About Phone → Tap "Build Number" 7 times
```

### Enable USB Debugging (for ADB installation)

```
Settings → Developer Options → USB Debugging: ON
```

### Allow Unknown Sources (for APK installation)

```
Settings → Security → Unknown Sources: ON

Or (Android 8+):
Settings → Apps → Special Access → Install Unknown Apps
→ Select your file manager → Allow from this source
```

### Storage Permissions

BizHawk requires storage access for:
- ROM files
- Save states
- Firmware files
- Screenshots

Grant permissions when prompted on first launch.

---

## Installation Methods

### Method 1: Pre-built APK (Recommended)

#### Step 1: Download APK

**Official Release**:
```
Visit: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/releases/latest
Download: BizHawkRafaelia-android-arm64.apk
```

**Development Build**:
```
Visit: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/actions
Download latest artifact: BizHawk-dev-android.apk
```

#### Step 2: Transfer to Device

**Method A: Direct Download**
- Use browser on Android device
- Download APK directly
- Open from Downloads folder

**Method B: USB Transfer**
```bash
# From computer
adb push BizHawkRafaelia-android-arm64.apk /sdcard/Download/
```

**Method C: Cloud Storage**
- Upload to Google Drive / Dropbox / etc.
- Download on Android device

#### Step 3: Install APK

1. Open file manager on Android device
2. Navigate to Download folder
3. Tap on APK file
4. Tap "Install"
5. Wait for installation to complete
6. Tap "Open" or find app in launcher

### Method 2: Via ADB

Install directly from computer via USB:

```bash
# 1. Enable USB Debugging on device
# 2. Connect device via USB
# 3. Authorize computer on device

# 4. Verify connection
adb devices
# Should show your device

# 5. Install APK
adb install BizHawkRafaelia-android-arm64.apk

# 6. Launch app (optional)
adb shell am start -n com.bizhawk.rafaelia/.MainActivity
```

### Method 3: Via F-Droid / Custom Repository (Future)

**Status**: Not yet available

When available:
```
1. Add BizHawkRafaelia repository to F-Droid
2. Install via F-Droid app
3. Automatic updates
```

---

## Post-Installation Setup

### First Launch

1. **Launch App**: Tap BizHawk Rafaelia icon
2. **Grant Permissions**: Allow storage access when prompted
3. **Welcome Screen**: Read initial instructions
4. **Directory Setup**: Choose/create folders for ROMs, saves, etc.

### Directory Structure

BizHawkRafaelia uses the following directory structure:

```
/sdcard/
├── BizHawk/
│   ├── ROMs/              # Place your ROM files here
│   ├── Firmware/          # BIOS and firmware files
│   ├── SaveStates/        # Save states
│   ├── SaveRAM/           # In-game saves
│   ├── Screenshots/       # Screenshots
│   ├── Movies/            # TAS recordings
│   └── Config/            # Configuration files
```

### Setting Up ROM Directory

```bash
# Via ADB
adb push /path/to/roms /sdcard/BizHawk/ROMs/

# Or manually:
# 1. Connect device to computer via USB
# 2. Open file manager on computer
# 3. Navigate to Internal Storage/BizHawk/ROMs/
# 4. Copy ROM files
```

### Setting Up Firmware

**Required Firmware** (by system):
- PlayStation: SCPH1001.BIN, SCPH5500.BIN, SCPH5502.BIN
- Saturn: sega_101.bin, mpr-17933.bin
- Nintendo DS: bios7.bin, bios9.bin, firmware.bin
- Atari 7800: 7800 BIOS (U).rom

```bash
# Copy firmware files
adb push /path/to/firmware /sdcard/BizHawk/Firmware/

# Or use file manager
```

### Initial Configuration

#### Display Settings

```
Settings → Display:
- Screen Orientation: Landscape
- Aspect Ratio: Original or Stretch
- Filtering: Bilinear (recommended)
- Frame Skip: Auto
```

#### Audio Settings

```
Settings → Audio:
- Enable Audio: ON
- Sample Rate: 44100 Hz
- Buffer Size: 2048 samples (adjust for latency)
```

#### Performance Settings

```
Settings → Performance:
- Hardware Acceleration: ON
- Frame Limit: 60 FPS
- Fast Forward Speed: 2x-4x
- Rewind: ON (uses ~100 MB RAM)
```

---

## Controller Configuration

### Touchscreen Controls

#### Virtual Gamepad

Default layout:
```
┌──────────────────────────────────────────┐
│                                          │
│                              (A) (B)     │
│   D-Pad                                  │
│    ▲                                     │
│  ◄ ■ ►                                   │
│    ▼                                     │
│                                          │
│  [SELECT] [START]                        │
└──────────────────────────────────────────┘
```

**Customize Virtual Controls**:
```
Settings → Input → Virtual Gamepad:
- Size: 50-150% (adjust to preference)
- Opacity: 50-100%
- Position: Drag buttons on screen
- Haptic Feedback: ON/OFF
```

### Bluetooth Controllers

#### Pairing Instructions

**Xbox Controller**:
1. Hold Xbox + Pair buttons
2. Android Settings → Bluetooth → Pair new device
3. Select "Xbox Wireless Controller"
4. BizHawk should auto-detect

**PlayStation Controller**:
1. Hold PS + Share buttons
2. Android Settings → Bluetooth → Pair new device
3. Select "Wireless Controller"
4. BizHawk should auto-detect

**Nintendo Switch Pro Controller**:
1. Hold sync button (top edge)
2. Android Settings → Bluetooth → Pair new device
3. Select "Pro Controller"
4. May need manual mapping in BizHawk

**Generic Controllers**:
1. Put controller in pairing mode
2. Android Settings → Bluetooth → Pair
3. Manual button mapping in BizHawk

#### Controller Mapping

```
Settings → Input → Controller Mapping:
- Auto-detect: Try this first
- Manual: Map each button individually
- Profiles: Save for different controllers
- Test: Test inputs in test screen
```

### USB OTG Controllers

1. **Connect Controller**: Via USB OTG adapter
2. **Auto-detect**: Should work immediately
3. **Power**: Some controllers need external power

**Compatible USB Controllers**:
- Xbox 360/One wired controllers
- PlayStation 3/4 wired controllers
- Generic USB HID gamepads
- Arcade sticks (most models)

---

## Performance Optimization

### For Budget Devices (3-4 GB RAM)

```
Settings:
- Frame Skip: Auto
- Audio Buffer: 4096 samples
- Rewind: OFF
- Fast Forward: 2x maximum
- Video Filter: None
- Hardware Acceleration: ON

Recommended Cores:
- NES: NesHawk (fastest)
- SNES: Snes9x (faster)
- Genesis: Genplus-gx
- GB/GBC: Gambatte
```

### For Mid-Range Devices (4-6 GB RAM)

```
Settings:
- Frame Skip: Off
- Audio Buffer: 2048 samples
- Rewind: 30 seconds
- Fast Forward: 4x
- Video Filter: Bilinear
- Hardware Acceleration: ON
```

### For High-End Devices (8+ GB RAM)

```
Settings:
- Frame Skip: Off
- Audio Buffer: 1024 samples
- Rewind: 2 minutes
- Fast Forward: 8x+
- Video Filter: xBRZ or shaders
- Hardware Acceleration: ON
```

### Battery Optimization

```
Settings → Power:
- Battery Saver Mode: Auto (enables at < 20%)
- Screen Dimming: Auto after 30s inactivity
- Vibration: Minimal or OFF
- Background Audio: OFF
```

**Tips for Longer Battery Life**:
- Reduce screen brightness
- Use wired headphones (not Bluetooth)
- Close background apps
- Enable battery saver at 50%
- Take breaks every 30-60 minutes
- Use power bank for extended sessions

### Thermal Management

**Prevent Overheating**:
- Remove phone case during play
- Avoid direct sunlight
- Use in cool environment
- Enable auto frame skip if thermal throttling detected
- Take 5-10 minute breaks every hour
- Reduce screen brightness

**Auto Thermal Throttling**:
BizHawk automatically reduces performance when device temperature exceeds safe thresholds.

---

## Troubleshooting

### Issue 1: "App Not Installed"

**Causes**:
- 32-bit device (ARM32)
- Android version too old
- Insufficient storage

**Solutions**:
```
1. Verify device is 64-bit:
   Settings → About Phone → CPU Architecture
   Must show "arm64-v8a" or "ARM64"

2. Check Android version:
   Settings → About Phone → Android Version
   Must be 7.0+

3. Free up storage:
   Delete unused apps, clear cache
   Need at least 500 MB free
```

### Issue 2: Crashes on Launch

**Cause**: Insufficient RAM or incompatible device

**Solutions**:
```
1. Close all background apps
2. Restart device
3. Clear app cache:
   Settings → Apps → BizHawk → Storage → Clear Cache
4. Reinstall app
5. Check device compatibility list above
```

### Issue 3: Poor Performance / Lag

**Solutions**:
```
1. Close background apps
2. Enable battery saver mode
3. Reduce settings:
   - Disable rewind
   - Increase audio buffer
   - Enable frame skip
   - Use faster cores

4. Check thermal throttling:
   - If device hot, let cool down
   - Remove case
   - Use in cooler environment
```

### Issue 4: Audio Crackling

**Solutions**:
```
1. Increase audio buffer size:
   Settings → Audio → Buffer: 4096

2. Check CPU usage:
   If high, reduce quality settings

3. Disable background apps

4. Try wired headphones instead of Bluetooth
```

### Issue 5: Touch Controls Not Responsive

**Solutions**:
```
1. Recalibrate touch controls:
   Settings → Input → Calibrate

2. Increase button size:
   Settings → Input → Virtual Gamepad → Size: 150%

3. Check screen protector:
   Some protectors reduce touch sensitivity

4. Clean screen
```

### Issue 6: Controller Not Detected

**Solutions**:
```
Bluetooth:
1. Unpair and re-pair controller
2. Restart Bluetooth
3. Restart device
4. Try manual mapping in BizHawk

USB OTG:
1. Check OTG adapter compatibility
2. Try different USB cable
3. Check device supports OTG:
   Download "USB OTG Checker" from Play Store
```

### Issue 7: ROMs Not Loading

**Solutions**:
```
1. Verify ROM format is correct
2. Check ROM isn't corrupted
3. Ensure ROM is in correct directory:
   /sdcard/BizHawk/ROMs/

4. Check required firmware present:
   /sdcard/BizHawk/Firmware/

5. Try different ROM dump
```

### Issue 8: Save States Fail

**Solutions**:
```
1. Check storage permissions granted
2. Free up storage space
3. Check save state directory writable:
   /sdcard/BizHawk/SaveStates/

4. Try in-game saves instead (SaveRAM)
```

### Issue 9: Black Screen

**Solutions**:
```
1. Check GPU compatibility:
   Device must support OpenGL ES 3.0+

2. Update Android system

3. Try software renderer:
   Settings → Display → Renderer: Software

4. Restart app
```

### Issue 10: App Drains Battery Fast

**Expected**: Emulation is CPU-intensive

**Mitigation**:
```
1. Enable battery saver:
   Settings → Power → Battery Saver: ON

2. Reduce settings (see Battery Optimization)

3. Use power bank

4. Take regular breaks

5. Consider native games for mobile play
```

---

## Building from Source

### Prerequisites

```bash
# On Linux/macOS:
sudo apt install -y \
    android-sdk \
    android-ndk \
    openjdk-17-jdk \
    dotnet-sdk-8.0

# Or download Android Studio:
# https://developer.android.com/studio

# Set environment variables
export ANDROID_SDK_ROOT=/path/to/android-sdk
export ANDROID_NDK_ROOT=/path/to/android-ndk
```

### Build Steps

```bash
# 1. Clone repository
git clone https://github.com/rafaelmeloreisnovo/BizHawkRafaelia.git
cd BizHawkRafaelia

# 2. Build Android APK
./build-android-arm64.sh

# Or manually:
dotnet publish -c Release \
    -r android-arm64 \
    -p:PublishTrimmed=true \
    -p:AndroidPackageFormat=apk \
    -o output/android/

# 3. Output APK location
ls output/android/*.apk

# 4. Install on device
adb install output/android/BizHawkRafaelia-unsigned.apk
```

### Signing APK (Optional)

For release builds:

```bash
# 1. Generate keystore (first time only)
keytool -genkey -v \
    -keystore bizhawk-release.keystore \
    -alias bizhawk \
    -keyalg RSA -keysize 2048 -validity 10000

# 2. Sign APK
jarsigner -verbose -sigalg SHA256withRSA -digestalg SHA-256 \
    -keystore bizhawk-release.keystore \
    output/android/BizHawkRafaelia-unsigned.apk \
    bizhawk

# 3. Optimize APK
zipalign -v 4 \
    output/android/BizHawkRafaelia-unsigned.apk \
    output/android/BizHawkRafaelia-signed.apk
```

---

## Advanced Features

### TAS Recording on Android

```
1. Load ROM
2. Menu → Movie → Record
3. Choose input source (touch/controller)
4. Play normally, inputs are recorded
5. Menu → Movie → Stop Recording
6. Movie saved to /sdcard/BizHawk/Movies/
```

### Netplay (Planned)

**Status**: Not yet implemented for Android

**Planned Features**:
- LAN multiplayer
- Internet play via relay server
- Low-latency rollback netcode

### Cloud Saves (Planned)

**Status**: Not yet implemented

**Planned Features**:
- Google Drive integration
- Automatic save backup
- Cross-device save sync

---

## Known Limitations

### Android-Specific

- Some cores unavailable (Mupen64Plus, melonDS)
- Performance lower than desktop
- Battery drain on intensive games
- Thermal throttling on extended play
- Limited rewind buffer (RAM constraints)

### Feature Parity

| Feature | Desktop | Android | Notes |
|---------|---------|---------|-------|
| Basic Emulation | ✅ | ✅ | |
| Save States | ✅ | ✅ | |
| Rewind | ✅ | ⚠️ | Limited buffer |
| Fast Forward | ✅ | ✅ | |
| Cheats | ✅ | ✅ | |
| Lua Scripting | ✅ | ❌ | Not yet ported |
| External Tools | ✅ | ❌ | Desktop only |
| TAStudio | ✅ | ❌ | Desktop only |
| Hex Editor | ✅ | ⚠️ | Limited UI |

---

## Additional Resources

- [Hardware Compatibility Matrix](HARDWARE_COMPATIBILITY_MATRIX.md)
- [Android APK Generation README](APK_GENERATION_README.md)
- [ARM64 Mobile Support](ARM64_MOBILE_SUPPORT.md)
- [Main README](README.md)

---

## Frequently Asked Questions

**Q: Can I use this for speedrunning?**  
A: Yes, for TAS speedrunning. Not recommended for real-time speedruns due to platform differences.

**Q: Does it work on Chromebooks?**  
A: If Chromebook supports Android apps and meets requirements, yes.

**Q: Can I use my existing BizHawk save states?**  
A: Usually yes, copy .State files to /sdcard/BizHawk/SaveStates/

**Q: Will this damage my phone?**  
A: No, but extended play generates heat. Take breaks.

**Q: Is this on Play Store?**  
A: Not yet. Emulators face restrictions on Play Store.

**Q: Can I use with a tablet?**  
A: Yes! Larger screen is better for gaming.

**Q: Does it support pen input (stylus)?**  
A: Yes, for DS games and games that support touch input.

---

**Last Updated**: 2025-11-23  
**Maintainer**: Rafael Melo Reis  
**License**: MIT

For issues: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/issues
