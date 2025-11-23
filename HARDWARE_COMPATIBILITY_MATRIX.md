# BizHawkRafaelia - Hardware Compatibility Matrix

**Author**: Rafael Melo Reis  
**Last Updated**: 2025-11-23  
**Version**: 1.0  

## Overview

This document provides a comprehensive hardware compatibility matrix for BizHawkRafaelia, detailing support across various hardware configurations, operating systems, and architectures.

---

## Supported Hardware Architectures

### CPU Architectures

| Architecture | Support Level | Notes |
|--------------|---------------|-------|
| x86_64 (AMD64) | ✅ Full Support | Primary development platform |
| ARM64 (AArch64) | ✅ Full Support | Mobile and embedded systems |
| x86 (32-bit) | ⚠️ Limited | Legacy support, not recommended |
| ARM (32-bit) | ❌ Not Supported | Use ARM64 instead |
| RISC-V | 🔧 Experimental | Community contributions welcome |

### GPU Support

| GPU Type | Windows | Linux | macOS | Android |
|----------|---------|-------|-------|---------|
| NVIDIA (OpenGL 3.3+) | ✅ | ✅ | ✅ | N/A |
| AMD (OpenGL 3.3+) | ✅ | ✅ | ✅ | N/A |
| Intel (OpenGL 3.3+) | ✅ | ✅ | ✅ | N/A |
| ARM Mali | N/A | ⚠️ | N/A | ✅ |
| Qualcomm Adreno | N/A | N/A | N/A | ✅ |
| Apple Silicon GPU | N/A | N/A | ⚠️ | N/A |

---

## Operating System Compatibility

### Desktop Operating Systems

#### Windows

| Version | Architecture | Support Level | Minimum RAM | Notes |
|---------|-------------|---------------|-------------|-------|
| Windows 11 (22H2+) | x64 | ✅ Full | 4 GB | Recommended |
| Windows 10 (22H2) | x64 | ✅ Full | 4 GB | Supported |
| Windows 10 (older) | x64 | ⚠️ Limited | 4 GB | Update recommended |
| Windows 8.1 | x64 | ⚠️ Legacy | 4 GB | EOL, security risk |
| Windows 7 | x64 | ❌ Unsupported | N/A | EOL |

**Prerequisites**:
- .NET 8 Runtime (or .NET Desktop Runtime)
- Visual C++ Redistributable 2015-2022
- OpenGL 3.3+ compatible GPU driver

#### Linux

| Distribution | Versions | Architecture | Support Level | Package Manager |
|--------------|----------|-------------|---------------|-----------------|
| Ubuntu | 22.04, 24.04 | x64, ARM64 | ✅ Full | apt |
| Debian | 11, 12 | x64, ARM64 | ✅ Full | apt |
| Fedora | 38, 39, 40 | x64, ARM64 | ✅ Full | dnf |
| Arch Linux | Rolling | x64, ARM64 | ✅ Full | pacman (AUR) |
| openSUSE | Leap 15.5, Tumbleweed | x64, ARM64 | ✅ Full | zypper |
| Linux Mint | 21, 22 | x64 | ✅ Full | apt |
| Manjaro | Rolling | x64, ARM64 | ✅ Full | pacman |
| Pop!_OS | 22.04 | x64 | ✅ Full | apt |
| NixOS | 23.11, Unstable | x64, ARM64 | ✅ Full | nix |
| Alpine Linux | 3.18+ | x64, ARM64 | ⚠️ Limited | apk |

**Prerequisites**:
- Mono Complete (6.12+) or .NET Runtime 8.0+
- OpenAL
- Lua 5.4
- lsb_release
- OpenGL 3.3+ compatible GPU driver

**Verified Hardware**:
- Intel/AMD processors (2010+)
- Raspberry Pi 4 (4GB+ RAM)
- ARM-based SBCs with 2GB+ RAM

#### macOS

| Version | Architecture | Support Level | Notes |
|---------|-------------|---------------|-------|
| macOS 15 (Sequoia) | Apple Silicon | ⚠️ Experimental | Via Rosetta 2 |
| macOS 14 (Sonoma) | Apple Silicon | ⚠️ Experimental | Via Rosetta 2 |
| macOS 13 (Ventura) | Apple Silicon | ⚠️ Experimental | Via Rosetta 2 |
| macOS 12 (Monterey) | Intel x64 | ⚠️ Limited | Legacy BizHawk 1.x |
| macOS 11 (Big Sur) | Intel x64 | ⚠️ Limited | Legacy BizHawk 1.x |

**Notes**:
- Native Apple Silicon support is not available
- OpenGL deprecation affects graphics performance
- Consider using x86_64 VM or dual-boot Linux
- Legacy 1.x releases available via community ports

### Mobile Operating Systems

#### Android

| Version | API Level | Architecture | Support Level | Notes |
|---------|-----------|-------------|---------------|-------|
| Android 13+ | 33+ | ARM64 | ✅ Full | Recommended |
| Android 12 | 31-32 | ARM64 | ✅ Full | Supported |
| Android 11 | 30 | ARM64 | ✅ Full | Supported |
| Android 10 | 29 | ARM64 | ⚠️ Limited | Some features unavailable |
| Android 9 | 28 | ARM64 | ⚠️ Limited | Performance reduced |
| Android 7-8 | 24-27 | ARM64 | ⚠️ Minimal | Legacy support |

**Minimum Device Requirements**:
- 3 GB RAM (4 GB recommended)
- Snapdragon 665+ or equivalent
- OpenGL ES 3.2+ or Vulkan 1.1+
- 500 MB free storage

**Verified Devices**:
- Google Pixel 5+
- Samsung Galaxy S10+
- OnePlus 7+
- Xiaomi devices with 4GB+ RAM

#### iOS

| Version | Support Level | Notes |
|---------|---------------|-------|
| iOS 17 | ❌ Not Available | Apple restrictions |
| iOS 16 | ❌ Not Available | Apple restrictions |
| iPadOS | ❌ Not Available | Apple restrictions |

**Notes**:
- iOS development requires Apple Developer Program ($99/year)
- App Store submission restrictions for emulators
- Sideloading requires jailbreak (not recommended)
- Consider Android alternative

---

## Hardware Tier Requirements

### Minimum Configuration (Budget/Legacy)

**Desktop/Laptop**:
- **CPU**: Intel Core 2 Duo / AMD Athlon 64 X2 (2008+)
- **RAM**: 2 GB (4 GB strongly recommended)
- **GPU**: Integrated graphics with OpenGL 3.3 support
- **Storage**: 500 MB free space
- **OS**: Windows 10, Ubuntu 20.04, or equivalent

**Performance**:
- NES, GB, SMS, GG: Full speed
- SNES, Genesis, GBA: 90-100% speed
- N64, PSX, DS: 30-60% speed (not recommended)

**Automatic Adaptations**:
- Reduced rendering resolution
- Disabled post-processing effects
- Lower audio quality (22 kHz)
- Frame skipping enabled
- Memory pool size: 32 MB

### Recommended Configuration

**Desktop/Laptop**:
- **CPU**: Intel Core i3 (8th gen) / AMD Ryzen 3 (2018+)
- **RAM**: 4 GB
- **GPU**: Intel UHD 630 / Radeon RX 550 or better
- **Storage**: 1 GB free space
- **OS**: Windows 11, Ubuntu 22.04, or equivalent

**Mobile**:
- **SoC**: Snapdragon 665 / MediaTek Helio G80
- **RAM**: 4 GB
- **Storage**: 500 MB free space
- **OS**: Android 11+

**Performance**:
- All 8/16-bit systems: Full speed
- N64, PSX, DS: 100% speed
- Saturn, 3DS: 60-90% speed

**Automatic Adaptations**:
- Standard rendering resolution
- Basic post-processing
- Standard audio (44.1 kHz)
- Memory pool size: 128 MB

### Optimal Configuration

**Desktop/Laptop**:
- **CPU**: Intel Core i5 (10th gen+) / AMD Ryzen 5 (3000+)
- **RAM**: 8 GB
- **GPU**: NVIDIA GTX 1050 / AMD RX 570 or better
- **Storage**: 2 GB free space (SSD recommended)
- **OS**: Windows 11, Ubuntu 24.04, or equivalent

**Mobile**:
- **SoC**: Snapdragon 865+ / MediaTek Dimensity 1100+
- **RAM**: 6-8 GB
- **Storage**: 1 GB free space
- **OS**: Android 12+

**Performance**:
- All systems: Full speed with enhancements
- HD texture packs supported
- Multiple rewind states
- Real-time save states

**Automatic Adaptations**:
- High rendering resolution (2x-4x)
- Advanced post-processing (shaders)
- High-quality audio (48 kHz)
- Memory pool size: 256 MB

### Excellent Configuration (High-End)

**Desktop/Laptop**:
- **CPU**: Intel Core i7 (12th gen+) / AMD Ryzen 7 (5000+)
- **RAM**: 16-32 GB
- **GPU**: NVIDIA RTX 3060 / AMD RX 6700 XT or better
- **Storage**: 5 GB free space (NVMe SSD)
- **OS**: Windows 11, Ubuntu 24.04, or equivalent

**Mobile**:
- **SoC**: Snapdragon 8 Gen 2+ / MediaTek Dimensity 9200+
- **RAM**: 12-16 GB
- **Storage**: 2 GB free space
- **OS**: Android 13+

**Performance**:
- All systems: Full speed with all features
- 8K rendering support
- Extensive rewind buffer (minutes)
- Real-time texture processing
- Multiple concurrent instances

**Automatic Adaptations**:
- Maximum rendering resolution (8x-16x)
- All post-processing effects
- Studio-quality audio (96 kHz)
- Memory pool size: 512 MB - 1 GB
- Aggressive parallel processing

---

## Peripheral Support

### Input Devices

| Device Type | Windows | Linux | macOS | Android | Notes |
|-------------|---------|-------|-------|---------|-------|
| Keyboard | ✅ | ✅ | ✅ | ✅ | Full hotkey support |
| Mouse | ✅ | ✅ | ✅ | ✅ | For light guns, etc. |
| Xbox Controllers | ✅ | ✅ | ✅ | ✅ | XInput/Native |
| PlayStation Controllers | ✅ | ✅ | ✅ | ✅ | Via USB/Bluetooth |
| Nintendo Controllers | ✅ | ✅ | ✅ | ✅ | Switch Pro, Joy-Cons |
| Generic USB Gamepads | ✅ | ✅ | ✅ | ✅ | DirectInput/evdev |
| Touch Screen | N/A | N/A | N/A | ✅ | Virtual gamepad overlay |
| MIDI Controllers | ⚠️ | ⚠️ | ⚠️ | ❌ | Experimental |

### Storage Devices

| Type | Windows | Linux | macOS | Android | Notes |
|------|---------|-------|-------|---------|-------|
| Internal HDD | ✅ | ✅ | ✅ | ✅ | Basic support |
| Internal SSD | ✅ | ✅ | ✅ | ✅ | Recommended |
| External USB | ✅ | ✅ | ✅ | ✅ | Via OTG on Android |
| Network Share | ✅ | ✅ | ✅ | ⚠️ | SMB/NFS |
| Cloud Storage | ⚠️ | ⚠️ | ⚠️ | ⚠️ | Via sync clients |

---

## Network Requirements

### Online Features

| Feature | Bandwidth | Latency | Notes |
|---------|-----------|---------|-------|
| ROM Verification | 1 Kbps | Any | Hash database lookup |
| Firmware Download | 100+ Kbps | Any | One-time download |
| Netplay (planned) | 1+ Mbps | <50ms | Future feature |
| Cloud Saves (planned) | 10+ Kbps | Any | Future feature |

---

## Compatibility Testing Matrix

### Test Environments

We continuously test on the following configurations:

#### Windows
- ✅ Windows 11 Pro (x64) - Intel i7-12700K, 32GB RAM, RTX 3080
- ✅ Windows 11 Home (x64) - Intel i5-10400, 16GB RAM, GTX 1660
- ✅ Windows 10 Pro (x64) - Intel i3-8100, 8GB RAM, UHD 630

#### Linux
- ✅ Ubuntu 24.04 LTS (x64) - AMD Ryzen 7 5800X, 32GB RAM, RX 6800
- ✅ Ubuntu 22.04 LTS (x64) - Intel i5-9400F, 16GB RAM, GTX 1650
- ✅ Arch Linux (x64) - AMD Ryzen 5 3600, 16GB RAM, RX 580
- ✅ Raspberry Pi OS (ARM64) - Raspberry Pi 4B, 8GB RAM

#### Android
- ✅ Google Pixel 7 Pro (Android 14) - Tensor G2, 12GB RAM
- ✅ Samsung Galaxy S21 (Android 13) - Snapdragon 888, 8GB RAM
- ✅ OnePlus 9 (Android 13) - Snapdragon 888, 8GB RAM

---

## Known Limitations

### By Platform

**Windows**:
- Requires Visual C++ Runtime
- May trigger antivirus false positives
- Windows Defender may slow first launch

**Linux**:
- Mono required for Mono builds
- Some cores may have reduced performance
- Audio latency varies by distribution

**macOS**:
- Limited to legacy 1.x versions
- No native Apple Silicon support
- OpenGL deprecated, Metal unsupported

**Android**:
- Large APK size (200+ MB)
- Battery drain on intensive cores
- Thermal throttling on extended play
- Limited storage on budget devices

### By Hardware

**Low-End Systems**:
- PSX, N64, Saturn cores may struggle
- Reduced rewind buffer
- Save state creation slower
- Audio may crackle under load

**ARM Systems**:
- Some x86-specific optimizations unavailable
- JIT compilation limited in some cores
- NEON vs SSE differences

---

## Troubleshooting by Hardware

### Intel Graphics

**Issue**: Poor performance, visual artifacts  
**Solution**:
- Update Intel GPU driver
- Disable anti-aliasing
- Reduce internal resolution
- Try OpenGL renderer

### AMD Graphics

**Issue**: Shader compilation stuttering  
**Solution**:
- Update AMD GPU driver
- Enable shader cache
- Use Vulkan if available
- Warm up shaders on first run

### NVIDIA Graphics

**Issue**: Input lag, screen tearing  
**Solution**:
- Update NVIDIA GPU driver
- Enable V-Sync or G-Sync
- Adjust pre-rendered frames (1-2)
- Use exclusive fullscreen

### Low RAM Systems

**Issue**: Crashes, slowdowns, frequent GC  
**Solution**:
- Close background applications
- Disable rewind feature
- Reduce memory pool size
- Enable manual GC mode
- Use lighter cores (e.g., quickerNES)

### Mobile Devices

**Issue**: Overheating, battery drain  
**Solution**:
- Enable power saving mode
- Reduce screen brightness
- Lower framerate cap (30 FPS)
- Take breaks every 30 minutes
- Remove case for better cooling

---

## Installation Guides by Platform

See dedicated installation documentation:
- [INSTALLATION_WINDOWS.md](INSTALLATION_WINDOWS.md)
- [INSTALLATION_LINUX.md](INSTALLATION_LINUX.md)
- [INSTALLATION_MACOS.md](INSTALLATION_MACOS.md)
- [INSTALLATION_ANDROID.md](INSTALLATION_ANDROID.md)

---

## Hardware-Specific Optimizations

BizHawkRafaelia automatically detects hardware capabilities and adjusts settings. This includes:

### CPU Optimizations
- SIMD instruction detection (SSE2, AVX2, AVX-512, NEON)
- Thread count optimization (based on core count)
- Cache-aware buffer sizing
- Hyperthreading detection and optimization

### GPU Optimizations
- Vendor-specific shader paths (NVIDIA, AMD, Intel)
- API selection (OpenGL, Vulkan, Metal)
- Memory transfer optimization
- Texture compression selection

### Memory Optimizations
- Adaptive pool sizing
- GC tuning based on available RAM
- Memory-mapped file usage on low-RAM systems
- Compression for save states on limited storage

---

## Reporting Hardware Compatibility Issues

If you encounter issues on your hardware:

1. **Check this matrix** for known limitations
2. **Update all drivers** (GPU, chipset, etc.)
3. **Try adaptive settings** via hardware detection
4. **Report issue** with full hardware specs:
   - CPU model and speed
   - RAM amount and speed
   - GPU model and driver version
   - OS and version
   - BizHawk version

---

## Future Platform Support

Under consideration:
- 🔮 iOS/iPadOS (pending Apple policy changes)
- 🔮 FreeBSD
- 🔮 Haiku OS
- 🔮 Chrome OS (Linux container)
- 🔮 Steam Deck optimization
- 🔮 Raspberry Pi 5 optimization

---

**Last Updated**: 2025-11-23  
**Maintainer**: Rafael Melo Reis  
**Contributors**: BizHawk Team, Community

For questions or hardware-specific issues, please open an issue on GitHub.
