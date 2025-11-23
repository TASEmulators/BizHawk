# BizHawkRafaelia - macOS Installation Guide

**Author**: Rafael Melo Reis  
**Last Updated**: 2025-11-23  
**Version**: 1.0  

---

## Table of Contents

1. [Important Notice](#important-notice)
2. [System Requirements](#system-requirements)
3. [Installation Options](#installation-options)
4. [Workarounds and Alternatives](#workarounds-and-alternatives)
5. [Legacy Version Installation](#legacy-version-installation)
6. [Troubleshooting](#troubleshooting)

---

## Important Notice

⚠️ **BizHawkRafaelia 2.x+ does not have native macOS support.**

This is due to fundamental platform limitations:
- **OpenGL Deprecation**: macOS deprecated OpenGL in favor of Metal
- **Graphics Compatibility**: Required graphics libraries don't work on macOS
- **Apple Silicon**: ARM64 architecture requires extensive porting work
- **Maintenance Burden**: Limited macOS development resources

### Current Status

| Platform | Support Status | Notes |
|----------|---------------|-------|
| macOS (Intel) - Modern BizHawk 2.x+ | ❌ Not Available | Native libraries incompatible |
| macOS (Intel) - Legacy BizHawk 1.x | ⚠️ Community Port | Via @Sappharad's fork |
| macOS (Apple Silicon) | ❌ Not Available | Requires Rosetta 2 for legacy |

---

## System Requirements

### For Legacy BizHawk 1.x (Intel Mac)

- **macOS Version**: 10.13 High Sierra or later
- **Architecture**: Intel x86_64
- **RAM**: 4 GB minimum, 8 GB recommended
- **Storage**: 500 MB free space
- **Dependencies**: Mono Framework, OpenGL 3.3+

### For Apple Silicon Macs

- **macOS Version**: 11 Big Sur or later
- **Architecture**: Apple Silicon (M1/M2/M3)
- **Rosetta 2**: Required
- **RAM**: 8 GB minimum
- **Storage**: 500 MB free space
- **Compatibility**: Legacy 1.x only, via Rosetta 2

---

## Installation Options

### Option 1: Virtual Machine (Recommended)

Run Windows or Linux in a VM for full BizHawkRafaelia 2.x support.

#### Using UTM (ARM-optimized)

**Download**: https://mac.getutm.app/

```bash
# 1. Install UTM
brew install --cask utm

# 2. Download Windows 11 ARM ISO
# https://www.microsoft.com/software-download/windowsinsiderpreviewARM64

# 3. Create VM in UTM:
#    - Architecture: ARM64
#    - RAM: 8 GB
#    - Disk: 64 GB
#    - Install Windows 11 ARM

# 4. Install BizHawkRafaelia in Windows
# Follow INSTALLATION_WINDOWS.md
```

**Performance**:
- Native ARM64 performance via virtualization
- Hardware acceleration support
- Full feature parity with Windows

#### Using Parallels Desktop

**Download**: https://www.parallels.com/

```bash
# 1. Install Parallels (requires license)
# Download from website

# 2. Create Windows 11 VM
# Parallels has automatic setup

# 3. Install BizHawkRafaelia
# Follow INSTALLATION_WINDOWS.md
```

**Notes**:
- Best performance (commercial solution)
- Supports DirectX and OpenGL
- Requires paid license ($99/year)

#### Using VMware Fusion

**Download**: https://www.vmware.com/products/fusion.html

```bash
# 1. Install VMware Fusion
# Free for personal use as of 2024

# 2. Create Windows or Linux VM
# Follow wizard

# 3. Install BizHawkRafaelia
# Follow respective installation guide
```

#### Using VirtualBox

**Download**: https://www.virtualbox.org/

```bash
# 1. Install VirtualBox
brew install --cask virtualbox

# 2. Install VirtualBox Extension Pack (for USB support)
# Download from VirtualBox website

# 3. Create VM (Ubuntu recommended)
# Allocate 8 GB RAM, 50 GB disk

# 4. Install Linux
# Ubuntu 22.04 LTS recommended

# 5. Install BizHawkRafaelia on Linux
# Follow INSTALLATION_LINUX.md
```

**Notes**:
- Free and open source
- Lower performance than Parallels/UTM
- 3D acceleration limited

### Option 2: Dual Boot Linux

Install Linux alongside macOS for native performance.

#### Using rEFInd Boot Manager

```bash
# 1. Disable SIP (System Integrity Protection)
# Reboot to Recovery Mode (Cmd+R)
# Open Terminal: csrutil disable

# 2. Install rEFInd
# https://www.rodsbooks.com/refind/

# 3. Create Linux partition
# Use Disk Utility to shrink macOS partition
# Leave unformatted space for Linux

# 4. Install Linux
# Boot from USB installer
# Install to free space

# 5. Re-enable SIP (optional, after setup)
```

**Recommended Linux Distributions**:
- Ubuntu 22.04 LTS (easiest)
- Fedora 39+ (modern)
- Pop!_OS 22.04 (gaming-focused)

**Performance**:
- Native hardware performance
- No VM overhead
- Full Linux feature set

### Option 3: Legacy BizHawk 1.x (Intel Macs)

Use community-ported BizHawk 1.x for Intel Macs.

#### Installation Steps

```bash
# 1. Install Mono Framework
brew install mono

# 2. Install dependencies
brew install openal-soft sdl2

# 3. Download legacy BizHawk for macOS
# Visit: https://tasvideos.org/Forum/Topics/12659
# Download latest macOS build from @Sappharad

# 4. Extract archive
unzip BizHawk-1.x-mac.zip -d ~/Applications/BizHawk

# 5. Make executable
chmod +x ~/Applications/BizHawk/EmuHawk.app/Contents/MacOS/mono

# 6. Run
open ~/Applications/BizHawk/EmuHawk.app
```

**Important Limitations**:
- ⚠️ **Outdated**: BizHawk 1.x is from 2015-2017
- ⚠️ **No Updates**: No bug fixes or new features
- ⚠️ **Limited Cores**: Many newer cores unavailable
- ⚠️ **Stability Issues**: Known bugs won't be fixed
- ⚠️ **No Rafaelia**: BizHawkRafaelia features unavailable

**What Works**:
- ✅ NES, SNES, Genesis, GB/GBC/GBA
- ✅ Basic TASing features
- ✅ Save states and rewind
- ✅ Lua scripting (basic)

**What Doesn't Work**:
- ❌ Modern cores (melonDS, Encore, etc.)
- ❌ Recent emulation accuracy improvements
- ❌ Latest UI features
- ❌ Performance optimizations
- ❌ BizHawkRafaelia enhancements

### Option 4: Cloud Gaming / Remote Desktop

Access BizHawk on a Windows/Linux system remotely.

#### Shadow PC

**Website**: https://shadow.tech/

- Cloud gaming PC
- Full Windows environment
- Install BizHawk normally
- $30-40/month

#### Parsec

**Website**: https://parsec.app/

- Remote desktop for gaming
- Host on your own Windows/Linux PC
- Stream to Mac
- Free for personal use

#### Chrome Remote Desktop

**Website**: https://remotedesktop.google.com/

- Free remote desktop
- Access your Windows/Linux PC from Mac
- Higher latency than Parsec
- Good for non-real-time use

---

## Workarounds and Alternatives

### Alternative Emulators for macOS

If you need native macOS emulation:

| System | Native macOS Emulator | TAS Support |
|--------|----------------------|-------------|
| NES | FCEUX, Mesen | ✅ Yes |
| SNES | Snes9x, bsnes-plus | ✅ Yes |
| Genesis | Kega Fusion | ⚠️ Limited |
| N64 | Mupen64Plus | ⚠️ Limited |
| GBA | mGBA | ✅ Yes |
| GB/GBC | gambatte-speedrun, SameBoy | ✅ Yes |
| DS | DeSmuME | ⚠️ Limited |

**Note**: Most lack BizHawk's comprehensive TASing features.

### Using Wine/CrossOver

⚠️ **Not Recommended**: BizHawk is .NET/Mono-based and does not work reliably under Wine.

```bash
# This typically does NOT work
# Wine is for native Windows applications
# BizHawk uses .NET which Wine handles poorly

# If you insist on trying:
brew install --cask wine-stable
wine ~/Downloads/EmuHawk.exe
# Expect crashes and compatibility issues
```

### Using Docker

⚠️ **Limited Success**: GUI applications in Docker on macOS have issues.

```bash
# Experimental - not officially supported
docker pull ubuntu:22.04
docker run -it --rm \
  -e DISPLAY=host.docker.internal:0 \
  -v ~/ROMs:/roms \
  ubuntu:22.04 bash

# Inside container, install BizHawk
# Requires X11 server on macOS (XQuartz)
```

---

## Legacy Version Installation

### For Intel Macs (x86_64)

#### Step 1: Install Prerequisites

```bash
# Install Homebrew (if not already installed)
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Install Mono
brew install mono

# Install dependencies
brew install openal-soft sdl2
```

#### Step 2: Download Legacy Build

Visit the TASVideos forum thread:
https://tasvideos.org/Forum/Topics/12659

Download the latest available macOS build (usually page 2-3 of thread).

#### Step 3: Extract and Install

```bash
# Extract download
cd ~/Downloads
unzip BizHawk-*-mac.zip

# Move to Applications
mv BizHawk ~/Applications/

# Make executable
chmod +x ~/Applications/BizHawk/*.sh
```

#### Step 4: First Run

```bash
# Launch from Terminal
cd ~/Applications/BizHawk
./EmuHawk.sh

# Or double-click EmuHawk.app if available
```

#### Step 5: Configure Paths

```bash
# Create config directories
mkdir -p ~/.config/BizHawk/Firmware
mkdir -p ~/.config/BizHawk/SaveRAM

# Copy firmware files
cp /path/to/firmware/* ~/.config/BizHawk/Firmware/
```

### For Apple Silicon Macs (M1/M2/M3)

#### Step 1: Install Rosetta 2

```bash
# Rosetta 2 translates x86_64 to ARM64
softwareupdate --install-rosetta --agree-to-license
```

#### Step 2: Install Mono (x86_64)

```bash
# Install Homebrew x86_64 version
arch -x86_64 /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Add to PATH (if not done automatically)
echo 'eval "$(/usr/local/bin/brew shellenv)"' >> ~/.zprofile

# Install Mono (x86_64)
arch -x86_64 brew install mono
```

#### Step 3: Download and Install

Same as Intel Mac steps 2-5, but run with Rosetta:

```bash
# Run with Rosetta 2
arch -x86_64 ./EmuHawk.sh
```

**Performance Note**: Running x86_64 code on Apple Silicon via Rosetta incurs ~20-30% performance penalty.

---

## Troubleshooting

### Issue 1: "EmuHawk.app is damaged"

**Cause**: Gatekeeper blocking unsigned application

**Solution**:
```bash
# Remove quarantine attribute
xattr -dr com.apple.quarantine ~/Applications/BizHawk/EmuHawk.app

# Or allow in System Preferences:
# System Preferences → Security & Privacy → General
# Click "Open Anyway"
```

### Issue 2: Mono Not Found

**Cause**: Mono not installed or not in PATH

**Solution**:
```bash
# Check Mono installation
which mono

# If not found, reinstall
brew reinstall mono

# Verify installation
mono --version
```

### Issue 3: Graphics/OpenGL Errors

**Cause**: OpenGL version incompatibility

**Solution**:
- Update macOS to latest version
- Update GPU drivers (automatic on macOS)
- Try software rendering mode (if available)

**Note**: macOS OpenGL is capped at 4.1 and deprecated.

### Issue 4: Audio Issues

**Cause**: CoreAudio/OpenAL configuration

**Solution**:
```bash
# Reinstall OpenAL
brew reinstall openal-soft

# Check audio output
# System Preferences → Sound → Output
# Ensure correct device selected
```

### Issue 5: Crashes on Launch

**Cause**: Incompatible .NET libraries

**Solution**:
- Verify Mono version (6.0+)
- Clear Mono cache: `rm -rf ~/.cache/mono`
- Try older BizHawk version

### Issue 6: Poor Performance

**Cause**: Software rendering or Rosetta 2 overhead

**Solution**:
- Use Intel Mac for better compatibility
- Consider dual-boot Linux option
- Use VM with hardware acceleration
- Disable rewind and visual filters

### Issue 7: Controller Not Detected

**Cause**: macOS controller support limitations

**Solution**:
```bash
# USB controllers should work automatically
# For Bluetooth:
# System Preferences → Bluetooth → Connect

# For Xbox/PlayStation controllers:
# May need third-party driver:
brew install --cask 360controller
```

### Issue 8: File Permissions

**Cause**: macOS sandboxing

**Solution**:
```bash
# Grant full disk access (if needed)
# System Preferences → Security & Privacy → Privacy
# Full Disk Access → Add EmuHawk

# Or run from Terminal (has your permissions)
cd ~/Applications/BizHawk
./EmuHawk.sh
```

---

## Future macOS Support

### Why Native Support is Difficult

1. **OpenGL Deprecation**: Apple deprecated OpenGL in macOS 10.14
2. **Metal API**: Would require complete graphics rewrite
3. **Sandboxing**: Mac App Store requirements restrict emulator functionality
4. **Code Signing**: Notarization requirements for modern macOS
5. **Development Resources**: Limited macOS expertise in team

### Potential Path Forward

For native macOS support to return:
- ✅ Port graphics backend to Metal
- ✅ Rewrite audio backend for CoreAudio
- ✅ Handle Apple Silicon natively
- ✅ Implement notarization and signing
- ✅ Test on multiple macOS versions
- ✅ Maintain ongoing compatibility

**Status**: ❌ Not currently planned due to resource constraints

### Community Involvement

If you're interested in porting BizHawk to macOS:
1. Visit the BizHawk GitHub: https://github.com/TASEmulators/BizHawk
2. Join the Discord: https://discord.gg/GySG2b6
3. Discuss in #bizhawk channel
4. See [contributing.md](contributing.md) for guidelines

The team welcomes contributions but cannot provide extensive macOS development support.

---

## Recommended Solution Summary

| Your Situation | Best Option | Effort | Performance |
|----------------|-------------|--------|-------------|
| Want full BizHawk 2.x | **Parallels VM** (paid) or **UTM** (free) | Medium | Good |
| Intel Mac, casual use | **Legacy BizHawk 1.x** | Low | Fair |
| Apple Silicon Mac | **UTM VM with Windows 11 ARM** | Medium | Good |
| Serious TASing | **Dual-boot Linux** | High | Excellent |
| Already have Windows PC | **Parsec/Remote Desktop** | Low | Variable |
| Budget-conscious | **VirtualBox + Ubuntu** | Medium | Fair |

**Top Recommendation**: **UTM with Windows 11 ARM** (for Apple Silicon) or **Parallels Desktop** (for best performance)

---

## Additional Resources

- **TASVideos Forum**: https://tasvideos.org/Forum/Topics/12659 (macOS builds)
- **Upstream BizHawk**: https://github.com/TASEmulators/BizHawk
- **BizHawkRafaelia**: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia
- **Discord**: TASVideos server, #bizhawk channel

---

## Frequently Asked Questions

**Q: Will BizHawk ever work natively on macOS again?**  
A: Not in the near future. The required Metal port is substantial work with limited developer resources.

**Q: Can I run Windows BizHawk with Wine?**  
A: Not recommended. .NET/Mono applications have poor Wine compatibility.

**Q: Is the legacy 1.x version good enough?**  
A: For basic TASing of older systems, yes. For modern cores and features, no.

**Q: What about Hackintosh?**  
A: If running on PC hardware, just use native Windows/Linux instead.

**Q: Will my save states work if I switch platforms?**  
A: Generally yes, but verify on a per-core basis. Always keep backups.

**Q: Can I contribute to macOS porting?**  
A: Yes! Contact the team on Discord or GitHub. Metal expertise especially welcome.

---

**Last Updated**: 2025-11-23  
**Maintainer**: Rafael Melo Reis  
**License**: MIT

For questions: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/issues
