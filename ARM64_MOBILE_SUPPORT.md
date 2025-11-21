# ARM64 and Mobile Platform Support

## Overview

This document outlines the path to ARM64 support and mobile platform (Android APK) generation for BizHawkRafaelia.

## Current Status

### Platform Support

**Currently Supported**:
- ✅ Windows x64 (.NET Framework / .NET Core)
- ✅ Linux x64 (Mono / .NET Core)
- ⚠️ macOS x64 (partial, via Mono)

**In Progress**:
- 🔄 ARM64 (Linux/Android)
- 🔄 Android APK generation

**Future Targets**:
- 📋 iOS ARM64
- 📋 macOS ARM64 (Apple Silicon)

### .NET Runtime Support

BizHawkRafaelia currently targets **.NET Standard 2.0** for maximum compatibility.

**Runtime Options**:
1. **.NET Framework 4.8**: Windows only, x64
2. **Mono**: Cross-platform, includes ARM64 support
3. **.NET 6+ / .NET 8**: Modern cross-platform, excellent ARM64 support

## ARM64 Architecture Considerations

### Memory and Performance

ARM64 devices typically have:
- **Less RAM**: Mobile devices have 2-8GB vs desktop 16-32GB
- **Different cache architecture**: Requires different optimization strategies
- **Power constraints**: Battery life considerations
- **Thermal throttling**: Performance degrades under sustained load

### Optimization Priorities for ARM64

1. **Memory footprint**: Critical on mobile devices
2. **Battery efficiency**: Prefer algorithms with fewer operations
3. **NEON SIMD**: ARM's SIMD instruction set (like x86 SSE/AVX)
4. **Cache-friendly code**: L1/L2 cache is smaller on ARM

### Code Changes for ARM64

Most C# code is platform-agnostic, but some areas need attention:

#### P/Invoke and Native Libraries

**Problem**: Native libraries must be compiled for ARM64.

**Current Native Dependencies**:
- libquicknes (NES core)
- libmupen64plus (N64 core)
- libbizlynx (Lynx core)
- Various audio/video libraries

**Solution**:
```bash
# Cross-compile native libraries for ARM64
# Example for Linux ARM64:
cmake -DCMAKE_SYSTEM_NAME=Linux \
      -DCMAKE_SYSTEM_PROCESSOR=aarch64 \
      -DCMAKE_C_COMPILER=aarch64-linux-gnu-gcc \
      -DCMAKE_CXX_COMPILER=aarch64-linux-gnu-g++ \
      .
make
```

#### SIMD Optimization

**x86/x64 Code**:
```csharp
#if X86 || X64
using System.Runtime.Intrinsics.X86;

if (Sse2.IsSupported)
{
    // SSE2 optimized code
}
#endif
```

**ARM64 Equivalent**:
```csharp
#if ARM64
using System.Runtime.Intrinsics.Arm;

if (AdvSimd.IsSupported)
{
    // NEON optimized code
}
#endif
```

**Portable Alternative**:
```csharp
using System.Runtime.Intrinsics;

// Use Vector<T> for platform-agnostic SIMD
var vec1 = new Vector<float>(data1);
var vec2 = new Vector<float>(data2);
var result = vec1 + vec2;
```

#### Pointer Size and Endianness

ARM64 is 64-bit and little-endian (same as x64), so most code Just Works™.

**Watch Out For**:
```csharp
// Avoid assuming pointer size
// BAD:
int ptr = (int)somePointer; // Breaks on 64-bit!

// GOOD:
IntPtr ptr = somePointer;
long ptrValue = ptr.ToInt64();
```

## Android APK Generation

### Prerequisites

1. **.NET 6+ with Android workload**:
```bash
dotnet workload install android
```

2. **Android SDK and NDK**:
```bash
# Install via Android Studio or command line
sdkmanager "platforms;android-33" "ndk;25.2.9519653"
```

3. **Java Development Kit (JDK) 11+**

### Project Structure for Android

Create an Android project that references emulation cores:

```xml
<!-- BizHawk.Client.Android/BizHawk.Client.Android.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-android</TargetFramework>
    <OutputType>Exe</OutputType>
    <SupportedOSPlatformVersion>21</SupportedOSPlatformVersion>
    <RuntimeIdentifiers>android-arm64;android-x64</RuntimeIdentifiers>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference emulation cores -->
    <ProjectReference Include="..\BizHawk.Emulation.Cores\BizHawk.Emulation.Cores.csproj" />
    <ProjectReference Include="..\BizHawk.Common\BizHawk.Common.csproj" />
  </ItemGroup>

  <!-- Native libraries for Android -->
  <ItemGroup>
    <AndroidNativeLibrary Include="native\arm64-v8a\libquicknes.so" />
    <AndroidNativeLibrary Include="native\arm64-v8a\libbizlynx.so" />
  </ItemGroup>
</Project>
```

### Building Android APK

```bash
# Debug build
dotnet build -c Debug -f net8.0-android

# Release build (optimized, signed)
dotnet publish -c Release -f net8.0-android \
  -p:AndroidSigningKeyStore=myapp.keystore \
  -p:AndroidSigningKeyAlias=myalias \
  -p:AndroidSigningKeyPass=mypassword \
  -p:AndroidSigningStorePass=mystorepassword

# Output: bin/Release/net8.0-android/publish/com.bizhawkrafaelia.apk
```

### Android-Specific Optimizations

#### 1. Reduce APK Size

```xml
<PropertyGroup>
  <!-- Enable code trimming -->
  <PublishTrimmed>true</PublishTrimmed>
  
  <!-- Use R8 for additional optimization -->
  <AndroidEnableProguard>true</AndroidEnableProguard>
  
  <!-- AOT compilation for faster startup -->
  <RunAOTCompilation>true</RunAOTCompilation>
  
  <!-- Compress native libraries -->
  <AndroidCreatePackagePerAbi>true</AndroidCreatePackagePerAbi>
</PropertyGroup>
```

**Expected APK Sizes**:
- Without optimization: 50-100 MB
- With trimming: 30-60 MB
- With compression: 20-40 MB
- Per-ABI split: 15-30 MB each

#### 2. Memory Management for Android

```csharp
// Check available memory
var activityManager = (ActivityManager)GetSystemService(ActivityService);
var memoryInfo = new ActivityManager.MemoryInfo();
activityManager.GetMemoryInfo(memoryInfo);

if (memoryInfo.LowMemory)
{
    // Reduce cache sizes
    OptimizedLRUCache.ReduceCapacity();
    
    // Trigger GC if necessary
    GC.Collect(2, GCCollectionMode.Forced);
}
```

#### 3. Battery Optimization

```csharp
// Detect battery state
var batteryManager = (BatteryManager)GetSystemService(BatteryService);
var batteryLevel = batteryManager.GetIntProperty(BatteryProperty.Capacity);
var isCharging = batteryManager.IsCharging;

if (!isCharging && batteryLevel < 20)
{
    // Reduce emulation accuracy for better battery life
    EmulationSettings.FrameSkip = 1;
    EmulationSettings.AudioQuality = AudioQuality.Low;
}
```

#### 4. Touch Controls

Emulators need touch controls on mobile:

```csharp
// Virtual gamepad overlay
public class VirtualGamepad : View
{
    private readonly Dictionary<int, GamepadButton> _touchButtons = new();

    public override bool OnTouchEvent(MotionEvent e)
    {
        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
            case MotionEventActions.PointerDown:
                HandleButtonPress(e);
                break;
            
            case MotionEventActions.Up:
            case MotionEventActions.PointerUp:
                HandleButtonRelease(e);
                break;
        }
        return true;
    }

    private void HandleButtonPress(MotionEvent e)
    {
        var button = GetButtonAtPosition(e.GetX(), e.GetY());
        if (button != GamepadButton.None)
        {
            _controller.SetButton(button, true);
        }
    }
}
```

## Native Library Cross-Compilation

### Building for ARM64 Linux

#### Prerequisites

```bash
# Install cross-compilation tools
sudo apt-get install gcc-aarch64-linux-gnu g++-aarch64-linux-gnu
```

#### CMake Configuration

```cmake
# ARM64 toolchain file (arm64-toolchain.cmake)
set(CMAKE_SYSTEM_NAME Linux)
set(CMAKE_SYSTEM_PROCESSOR aarch64)

set(CMAKE_C_COMPILER aarch64-linux-gnu-gcc)
set(CMAKE_CXX_COMPILER aarch64-linux-gnu-g++)

set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)
```

#### Build Script

```bash
#!/bin/bash
# build-arm64.sh

# Clean previous builds
rm -rf build-arm64
mkdir build-arm64
cd build-arm64

# Configure for ARM64
cmake -DCMAKE_TOOLCHAIN_FILE=../arm64-toolchain.cmake \
      -DCMAKE_BUILD_TYPE=Release \
      ..

# Build
make -j$(nproc)

# Output in build-arm64/lib/
```

### Building for Android

#### Android NDK Configuration

```cmake
# Android toolchain is provided by NDK
set(CMAKE_TOOLCHAIN_FILE ${ANDROID_NDK}/build/cmake/android.toolchain.cmake)
set(ANDROID_ABI arm64-v8a)
set(ANDROID_PLATFORM android-21)
```

#### Build Script

```bash
#!/bin/bash
# build-android.sh

export ANDROID_NDK=/path/to/ndk
export ANDROID_ABI=arm64-v8a

rm -rf build-android
mkdir build-android
cd build-android

cmake -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK/build/cmake/android.toolchain.cmake \
      -DANDROID_ABI=$ANDROID_ABI \
      -DANDROID_PLATFORM=android-21 \
      -DCMAKE_BUILD_TYPE=Release \
      ..

make -j$(nproc)

# Output in build-android/lib/
```

## Testing on ARM64

### QEMU Emulation (for development)

```bash
# Install QEMU user mode emulation
sudo apt-get install qemu-user-static

# Register ARM64 binaries
sudo update-binfmts --enable qemu-aarch64

# Run ARM64 binary on x64 host
./BizHawk.Client.EmuHawk-arm64
```

**Note**: QEMU is slow. Use real hardware for performance testing.

### Real Hardware Testing

**Raspberry Pi 4/5** (ARM64 Linux):
```bash
# Install .NET runtime
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0

# Run BizHawk
./BizHawk.Client.EmuHawk
```

**Android Device**:
```bash
# Install APK
adb install -r BizHawk.Client.Android.apk

# View logs
adb logcat | grep BizHawk
```

## Performance Expectations

### ARM64 vs x64 Performance

Based on typical emulator performance:

| Platform | CPU | Expected FPS (SNES) | Notes |
|----------|-----|---------------------|-------|
| x64 Desktop | i5-8600K | 200-300+ | Reference |
| ARM64 Server | Ampere Altra | 150-250 | Server-class ARM |
| Raspberry Pi 5 | Cortex-A76 | 60-120 | Single-board computer |
| Android Flagship | Snapdragon 8 Gen 2 | 120-200 | High-end phone |
| Android Mid-range | Snapdragon 778G | 60-90 | Mid-range phone |

**Note**: Actual performance varies by:
- Emulation core complexity
- Optimization level
- Thermal conditions
- Background apps

### Memory Requirements

| Platform | RAM Available | Recommended | Minimum |
|----------|---------------|-------------|---------|
| x64 Desktop | 16-32GB | 4GB | 2GB |
| ARM64 Linux | 4-16GB | 2GB | 1GB |
| Android Flagship | 8-16GB | 1.5GB | 1GB |
| Android Mid-range | 4-8GB | 1GB | 512MB |

## Roadmap

### Phase 1: ARM64 Linux Support (Current)

- [x] Audit code for platform-specific assumptions
- [x] Implement memory optimization (ArrayPool, caching)
- [ ] Cross-compile native libraries for ARM64
- [ ] Test on Raspberry Pi 4/5
- [ ] Optimize performance for ARM NEON
- [ ] Document build process

### Phase 2: Android APK Generation (Next)

- [ ] Create Android project structure
- [ ] Implement touch controls UI
- [ ] Port windowing system to Android
- [ ] Handle Android lifecycle (pause/resume)
- [ ] Optimize for mobile battery life
- [ ] Build and test APK
- [ ] Publish to testing track

### Phase 3: Optimization and Polish

- [ ] Profile on real ARM64 devices
- [ ] Implement ARM-specific optimizations
- [ ] Reduce APK size < 30MB
- [ ] Add cloud save support
- [ ] Implement achievement system
- [ ] Multi-language support

### Phase 4: iOS Support (Future)

- [ ] Port to .NET MAUI or Xamarin.iOS
- [ ] Implement iOS-specific UI
- [ ] Handle App Store requirements
- [ ] JIT limitations workarounds
- [ ] TestFlight beta testing
- [ ] App Store submission

## Known Issues and Limitations

### Current Limitations

1. **JIT Compilation**: iOS doesn't allow JIT, requiring AOT compilation
2. **Native Libraries**: Must be compiled for each target architecture
3. **OpenGL/Vulkan**: Android may have limited graphics API support
4. **Input Lag**: Touch controls have inherent latency vs physical buttons

### Compatibility Concerns

| Feature | Windows x64 | Linux x64 | ARM64 Linux | Android | iOS |
|---------|-------------|-----------|-------------|---------|-----|
| Basic Emulation | ✅ | ✅ | ⚠️ | 🔄 | 📋 |
| Native Cores | ✅ | ✅ | ⚠️ | ⚠️ | ⚠️ |
| OpenGL Rendering | ✅ | ✅ | ✅ | ⚠️ | ⚠️ |
| Audio Output | ✅ | ✅ | ✅ | ⚠️ | ⚠️ |
| Savestates | ✅ | ✅ | ✅ | ✅ | 📋 |
| TAS Recording | ✅ | ✅ | ✅ | ⚠️ | ⚠️ |

Legend: ✅ Full support, ⚠️ Partial/needs work, 🔄 In progress, 📋 Planned

## Resources

### Documentation

- [.NET Android Documentation](https://docs.microsoft.com/en-us/dotnet/maui/android/)
- [Android NDK Documentation](https://developer.android.com/ndk/guides)
- [ARM NEON Intrinsics](https://developer.arm.com/architectures/instruction-sets/intrinsics/)

### Tools

- **Android Studio**: IDE with emulator
- **Visual Studio**: .NET Android development
- **adb**: Android Debug Bridge
- **CMake**: Cross-platform build system
- **QEMU**: ARM emulation for testing

### Community

- [TASVideos Forums](https://tasvideos.org/forum)
- [Emulation Development Discord](https://discord.gg/emulation)
- BizHawk GitHub Issues

## Contributing

To contribute to ARM64/Android support:

1. Test on real ARM64 hardware
2. Report platform-specific issues
3. Submit cross-compilation fixes
4. Help with Android UI development
5. Write platform-specific optimizations

## Conclusion

ARM64 and Android support is a significant undertaking, but with careful planning and incremental progress, BizHawkRafaelia can become a premier multi-platform emulator.

The optimizations already implemented (ArrayPool, caching, profiling) lay the groundwork for efficient mobile emulation. The next steps are building and testing on real ARM64 hardware.

---

**Last Updated**: 2025-11-21  
**Maintained By**: Rafael Melo Reis  
**Project**: BizHawkRafaelia
