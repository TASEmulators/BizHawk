# BizHawkRafaelia - Windows Installation Guide

**Author**: Rafael Melo Reis  
**Last Updated**: 2025-11-23  
**Version**: 1.0  

---

## Table of Contents

1. [System Requirements](#system-requirements)
2. [Prerequisites](#prerequisites)
3. [Installation Methods](#installation-methods)
4. [Post-Installation Setup](#post-installation-setup)
5. [Troubleshooting](#troubleshooting)
6. [Uninstallation](#uninstallation)

---

## System Requirements

### Minimum
- **OS**: Windows 10 (64-bit), version 1909 or later
- **CPU**: Intel Core 2 Duo / AMD Athlon 64 X2 (2008+)
- **RAM**: 4 GB (2 GB absolute minimum, not recommended)
- **GPU**: OpenGL 3.3 compatible (integrated graphics OK)
- **Storage**: 500 MB free space
- **Display**: 1024x768 resolution

### Recommended
- **OS**: Windows 11 (64-bit), version 22H2 or later
- **CPU**: Intel Core i5 (8th gen+) / AMD Ryzen 5 (2000+)
- **RAM**: 8 GB
- **GPU**: NVIDIA GTX 1050 / AMD RX 570 or better
- **Storage**: 2 GB free space (SSD recommended)
- **Display**: 1920x1080 or higher

---

## Prerequisites

### Required Components

BizHawkRafaelia requires the following system components:

#### 1. .NET Runtime 8.0 (or .NET Desktop Runtime 8.0)

**Download**: https://dotnet.microsoft.com/download/dotnet/8.0

**Installation**:
```powershell
# Option 1: Download and run installer from Microsoft
# Option 2: Install via winget
winget install Microsoft.DotNet.Runtime.8

# Verify installation
dotnet --version
```

#### 2. Visual C++ Redistributable 2015-2022

**Download**: https://aka.ms/vs/17/release/vc_redist.x64.exe

**Installation**:
```powershell
# Option 1: Download and run installer
# Option 2: Install via chocolatey
choco install vcredist140

# Option 3: Use all-in-one installer
# Download from: https://github.com/TASEmulators/BizHawk-Prereqs/releases/latest
```

### Optional Components

#### DirectX End-User Runtime (for older systems)
- **Download**: https://www.microsoft.com/download/details.aspx?id=35
- **Note**: Usually not needed on Windows 10/11

#### OpenAL (for enhanced audio)
- **Download**: https://openal.org/downloads/
- **Note**: Optional, improves audio quality

---

## Installation Methods

### Method 1: Binary Release (Recommended)

**Step 1**: Download the latest release

Visit the [Releases page](https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/releases/latest) and download:
```
BizHawkRafaelia-<version>-win-x64.zip
```

**Step 2**: Extract the archive

```powershell
# Extract to a folder (replace <version> with actual version)
# Example: C:\BizHawkRafaelia\
Expand-Archive -Path BizHawkRafaelia-<version>-win-x64.zip -DestinationPath C:\BizHawkRafaelia\
```

**Important Notes**:
- ✅ DO extract to a folder with write permissions (e.g., C:\BizHawkRafaelia\)
- ❌ DO NOT extract to Program Files (permission issues)
- ❌ DO NOT extract to a OneDrive/Dropbox sync folder (corruption risk)
- ❌ DO NOT mix different versions in the same folder

**Step 3**: Run EmuHawk

```powershell
cd C:\BizHawkRafaelia\
.\EmuHawk.exe
```

### Method 2: Development Build

For latest features and fixes:

**Step 1**: Download from GitHub Actions

1. Visit: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/actions
2. Click on the latest successful workflow run
3. Download `BizHawk-dev-windows.zip` from artifacts

**Step 2**: Extract and run (same as Method 1)

**Warning**: Development builds may have bugs and are not recommended for critical TASing work.

### Method 3: Build from Source

For developers and contributors:

**Prerequisites**:
- Visual Studio 2022 (Community or higher)
- .NET 8 SDK
- Git

**Steps**:

```powershell
# 1. Clone repository
git clone https://github.com/rafaelmeloreisnovo/BizHawkRafaelia.git
cd BizHawkRafaelia

# 2. Open in Visual Studio
start BizHawk.sln

# Or build from command line:
dotnet build BizHawk.sln -c Release

# 3. Run
cd output\
.\EmuHawk.exe
```

---

## Post-Installation Setup

### Initial Launch

**First Run**:

1. **Windows SmartScreen**: If blocked, click "More info" → "Run anyway"
2. **Antivirus**: If blocked, add exception (see Troubleshooting)
3. **Welcome Screen**: Configure initial settings

### Essential Configuration

#### 1. Set Up Firmware Directory

Go to `Config` → `Paths...` → `Global` tab:

```
Firmware: C:\BizHawkRafaelia\Firmware
```

Place your legally dumped firmware files in this folder.

#### 2. Configure Controllers

Go to `Config` → `Controllers...`:
- Bind keyboard/gamepad buttons to virtual gamepad
- Test input in the test panel
- Save configuration

#### 3. Set Up Hotkeys

Go to `Config` → `Hotkeys...`:
- Configure essential hotkeys:
  - Save State: Shift+F1-F10
  - Load State: F1-F10
  - Fast Forward: Tab
  - Frame Advance: \ (backslash)
  - Toggle Pause: Pause
  - Screenshot: F12

#### 4. Configure Display

Go to `Config` → `Display...`:
- **Vsync**: On (reduces tearing)
- **Final Filter**: None or xBRZ (based on preference)
- **Window Size**: 2x or 3x

#### 5. Set Performance Options

Go to `Config` → `Customize...`:

**General Tab**:
- Rewind: Enable (uses ~200MB RAM per minute)
- Auto-Minimize on Pause: Enable (optional)

**Advanced Tab**:
- AutoSaveRAM: Every 5 minutes (or disable for manual)
- Skip Lag Frame Recording: Enable for better performance

---

## Advanced Configuration

### Portable Installation

To make installation portable (USB drive, etc.):

1. Create `portable.txt` in BizHawk folder:
```powershell
cd C:\BizHawkRafaelia\
New-Item -ItemType File -Name portable.txt
```

2. All config files will now save in BizHawk folder instead of AppData

### Multi-Instance Setup

To run multiple instances simultaneously:

```powershell
# Create separate folders for each instance
mkdir C:\BizHawkRafaelia-Instance1
mkdir C:\BizHawkRafaelia-Instance2

# Copy files to each folder
# Run from different folders
```

### Custom Paths

Edit `config.ini` (in BizHawk folder or %AppData%\BizHawk):

```ini
[Paths]
ROMs=D:\Games\ROMs
Firmware=D:\Emulation\Firmware
SaveRAM=D:\Saves\BizHawk
Screenshots=D:\Screenshots
```

---

## Troubleshooting

### Issue 1: "EmuHawk needs X in order to run"

**Cause**: Missing prerequisite

**Solution**:
1. Read error message carefully
2. Install missing component (usually VC++ Redist or .NET Runtime)
3. Restart computer
4. Try again

### Issue 2: Windows SmartScreen Blocks Launch

**Cause**: Unsigned executable

**Solution**:
1. Click "More info"
2. Click "Run anyway"
3. Optionally: Right-click EmuHawk.exe → Properties → Check "Unblock" → Apply

### Issue 3: Antivirus False Positive

**Cause**: Emulator behavior triggers heuristics

**Solution**:

**Windows Defender**:
```powershell
# Add folder exclusion (run as Administrator)
Add-MpPreference -ExclusionPath "C:\BizHawkRafaelia\"
```

**Third-Party Antivirus**:
- Add folder to exclusions/whitelist
- Consult antivirus documentation

### Issue 4: Black Screen on Launch

**Cause**: GPU driver or OpenGL issue

**Solution**:
1. Update GPU drivers:
   - **NVIDIA**: https://www.nvidia.com/Download/index.aspx
   - **AMD**: https://www.amd.com/support
   - **Intel**: https://www.intel.com/content/www/us/en/download-center/home.html

2. Try different renderer:
   - Config → Display → Renderer: GDI+ or OpenGL

3. Verify OpenGL support:
```powershell
# Download and run OpenGL Extensions Viewer
# https://www.realtech-vr.com/home/glview
```

### Issue 5: Crashes on Loading ROM

**Cause**: Incompatible firmware or corrupt ROM

**Solution**:
1. Verify ROM integrity (green checkmark in status bar)
2. Check firmware (Config → Firmwares...)
3. Update to latest BizHawk version
4. Try different core (Config → Cores)

### Issue 6: Poor Performance / Lag

**Cause**: Hardware limitations or suboptimal settings

**Solution**:

**For Low-End Systems**:
1. Disable rewind (Config → Customize → Rewind: Off)
2. Reduce display filter (Config → Display → Final Filter: None)
3. Lower window size (View → Window Size → 1x)
4. Use faster cores:
   - NES: quickerNES
   - SNES: Snes9x
   - GB: Gambatte

**For High-End Systems with lag**:
1. Disable V-Sync (Config → Display → V-Sync: Off)
2. Check background applications (Task Manager)
3. Disable Windows Game Bar (Windows Settings → Gaming)
4. Set power plan to High Performance

### Issue 7: Audio Crackling/Stuttering

**Cause**: Audio buffer settings or driver issues

**Solution**:
1. Increase audio buffer: Config → Sound → Buffer: 100-150ms
2. Update audio drivers
3. Try different audio device (Config → Sound)
4. Disable audio enhancements (Windows Sound settings)

### Issue 8: Save States Fail to Load

**Cause**: Version mismatch or corrupted state

**Solution**:
1. Verify using same BizHawk version
2. Check save state file exists and isn't 0 bytes
3. Try different save slot
4. Last resort: Use save RAM instead (Ctrl+S)

### Issue 9: Cannot Write to Folder

**Cause**: Permission issues

**Solution**:
1. Move BizHawk out of Program Files
2. Run as Administrator (not recommended long-term)
3. Check folder permissions:
   - Right-click folder → Properties → Security
   - Ensure your user has Full Control

### Issue 10: High Memory Usage

**Cause**: Rewind feature or memory leak

**Solution**:
1. Reduce rewind buffer (Config → Customize → Rewind)
2. Restart BizHawk periodically
3. Update to latest version
4. Report bug if persistent

---

## Performance Optimization

### For Low-End Systems (2-4 GB RAM)

```
Config → Customize:
- Rewind: Off
- AutoSaveRAM: Every 15 minutes
- Skip Lag Frame Recording: On

Config → Display:
- V-Sync: Off
- Final Filter: None
- Window Size: 1x

Config → Sound:
- Output Method: DirectSound
- Buffer: 150ms
```

### For Mid-Range Systems (4-8 GB RAM)

```
Config → Customize:
- Rewind: 30 seconds buffer
- AutoSaveRAM: Every 5 minutes

Config → Display:
- V-Sync: On
- Final Filter: Bilinear or xBRZ
- Window Size: 2x-3x

Config → Sound:
- Output Method: WASAPI
- Buffer: 100ms
```

### For High-End Systems (16+ GB RAM)

```
Config → Customize:
- Rewind: Full (120+ seconds)
- AutoSaveRAM: Every 1 minute

Config → Display:
- V-Sync: G-Sync/FreeSync if available
- Final Filter: xBRZ5x or custom shaders
- Window Size: Max or fullscreen

Config → Sound:
- Output Method: WASAPI Exclusive
- Buffer: 50ms
```

---

## Uninstallation

### Standard Uninstallation

1. **Delete BizHawk folder**:
```powershell
Remove-Item -Path C:\BizHawkRafaelia -Recurse -Force
```

2. **Remove configuration** (if not portable):
```powershell
Remove-Item -Path "$env:APPDATA\BizHawk" -Recurse -Force
```

3. **Remove registry entries** (rare, usually none):
```powershell
# BizHawk typically doesn't use registry
# Check: HKEY_CURRENT_USER\Software\BizHawk (usually empty)
```

### Clean Uninstallation (Remove All Traces)

```powershell
# 1. Close all BizHawk instances
Stop-Process -Name EmuHawk -ErrorAction SilentlyContinue

# 2. Remove main folder
Remove-Item -Path C:\BizHawkRafaelia -Recurse -Force

# 3. Remove config
Remove-Item -Path "$env:APPDATA\BizHawk" -Recurse -Force

# 4. Remove local data
Remove-Item -Path "$env:LOCALAPPDATA\BizHawk" -Recurse -Force -ErrorAction SilentlyContinue

# 5. Remove temp files
Remove-Item -Path "$env:TEMP\BizHawk*" -Recurse -Force -ErrorAction SilentlyContinue
```

---

## Additional Resources

### Documentation
- [Hardware Compatibility Matrix](HARDWARE_COMPATIBILITY_MATRIX.md)
- [Bug Mitigation Guide](BUG_MITIGATION_GUIDE.md)
- [Performance Optimization Guide](PERFORMANCE_OPTIMIZATION_GUIDE.md)
- [Main README](README.md)

### Community Support
- **Discord**: TASVideos Server - #bizhawk channel
- **Forum**: https://tasvideos.org/Forum/Subforum/64
- **IRC**: #bizhawk on Libera Chat
- **Reddit**: /r/BizHawk, /r/TAS

### Official Links
- **GitHub**: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia
- **Upstream**: https://github.com/TASEmulators/BizHawk
- **TASVideos**: https://tasvideos.org/Bizhawk

---

## Frequently Asked Questions

**Q: Can I install multiple versions?**  
A: Yes! Keep each version in a separate folder.

**Q: Do I need to install .NET SDK or just Runtime?**  
A: Just Runtime for running. SDK only needed for development.

**Q: Can I run on 32-bit Windows?**  
A: No. BizHawk 2.x+ requires 64-bit Windows.

**Q: Is it safe? My antivirus flags it.**  
A: Yes, it's safe. False positives are common with emulators.

**Q: Can I use this for speedrunning?**  
A: Yes! BizHawk is approved for TAS speedrunning on TASVideos.

**Q: What's the difference from upstream BizHawk?**  
A: BizHawkRafaelia adds optimizations, enhanced documentation, and additional platform support while maintaining compatibility.

**Q: Can I sync my config across computers?**  
A: Yes, copy the config folder or use portable mode with cloud storage.

---

**Last Updated**: 2025-11-23  
**Maintainer**: Rafael Melo Reis  
**License**: MIT

For issues or questions, please visit: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/issues
