# Rafaelia Optimization Modules

**Performance optimization layer for BizHawkRafaelia**

## Overview

The Rafaelia modules provide a comprehensive optimization framework targeting:
- **60x performance improvement** over baseline
- **1/3 resource usage** (CPU, Memory, Disk)
- **Zero lag, freezing, or memory leaks**
- **Full ARM64/Android support**
- **Hardware adaptation** from minimum to excellent systems

## Module Structure

### `/core` - Core Performance Modules

High-performance foundational components:

- **MemoryOptimization.cs**: Memory pooling, matrix buffers, zero-allocation patterns
  - `OptimizedMemoryPool`: Array pooling for zero-allocation
  - `MatrixFrameBuffer`: Cache-friendly 2D frame buffers
  - `StackBuffer<T>`: Stack-allocated temporary storage

- **CpuOptimization.cs**: SIMD operations, parallel processing, lookup tables
  - `SimdOptimizer`: Hardware-accelerated vectorization (SSE/AVX/NEON)
  - `ParallelOptimizer`: Multi-core processing utilities
  - `LookupTableOptimizer`: Pre-computed value tables

### `/optimization` - Optimization Utilities

Specialized optimization components:

- **IoOptimization.cs**: Disk I/O optimization
  - `OptimizedFileIO`: Async I/O with large buffers
  - `CompressionHelper`: Fast compression (GZip/Deflate)
  - `ReadAheadCache`: Predictive caching

### `/hardware` - Hardware Adaptation

Dynamic quality adjustment:

- **HardwareAdaptation.cs**: Runtime hardware detection and adaptation
  - `HardwareDetector`: CPU/RAM/GPU capability detection
  - `HardwareProfile`: System classification (Minimum/Good/Excellent)
  - `AdaptiveQualityManager`: Dynamic quality settings

### `/mobile` - Mobile/ARM64 Support

Mobile platform optimizations:

- **Arm64Optimization.cs**: ARM64-specific code
  - `ArmOptimizer`: NEON SIMD intrinsics
  - `PowerManager`: Battery life optimization
  - `ThermalManager`: Thermal throttling management
  - `TouchInputOptimizer`: Low-latency touch processing
  - `CacheOptimizer`: ARM64 cache-line alignment

### `/interop` - Interoperability

Cross-platform compatibility:

- **Interoperability.cs**: Runtime and platform detection
  - `RuntimeDetector`: .NET Framework/Core/Mono detection
  - `PlatformFeatures`: OS and architecture detection
  - `CompatibilityShims`: Feature polyfills
  - `PerformanceDiagnostics`: System diagnostics

## Usage Examples

### Memory Optimization

```csharp
using BizHawk.Rafaelia.Core.Memory;

// Rent array from pool (zero allocation)
byte[] buffer = OptimizedMemoryPool.RentByteArray(1024);
try
{
    // Use buffer...
}
finally
{
    // Always return to pool!
    OptimizedMemoryPool.ReturnByteArray(buffer);
}

// Matrix frame buffer (better cache locality)
using var frameBuffer = new MatrixFrameBuffer(320, 240);
frameBuffer.SetPixel(y: 10, x: 20, value: 255);
byte pixel = frameBuffer.GetPixel(y: 10, x: 20);
```

### CPU Optimization

```csharp
using BizHawk.Rafaelia.Core.CPU;

// SIMD-accelerated operations
byte[] source = new byte[1024];
byte[] dest = new byte[1024];
SimdOptimizer.FastCopy(source, dest, 1024);

// Parallel processing
ParallelOptimizer.ParallelFor(0, 10000, i =>
{
    // Process item i...
});

// Lookup table optimization
byte[] gammaTable = LookupTableOptimizer.CreateByteTable(b => (byte)(Math.Pow(b / 255.0, 2.2) * 255));
```

### Hardware Adaptation

```csharp
using BizHawk.Rafaelia.Hardware;

var hardwareDetector = HardwareDetector.Instance;
var profile = hardwareDetector.Profile;

Console.WriteLine($"Hardware Tier: {profile.Tier}");
Console.WriteLine($"CPU Cores: {profile.ProcessorCount}");
Console.WriteLine($"RAM: {profile.TotalMemoryGB:F1} GB");

var qualityManager = new AdaptiveQualityManager();
Console.WriteLine($"Max Cached Frames: {qualityManager.MaxCachedFrames}");
Console.WriteLine($"Enable Advanced Effects: {qualityManager.EnableAdvancedEffects}");
```

### ARM64/Mobile Optimization

```csharp
using BizHawk.Rafaelia.Mobile;

// NEON-optimized memory operations (ARM64 only)
if (ArmOptimizer.IsNeonSupported)
{
    unsafe
    {
        fixed (byte* src = sourceArray)
        fixed (byte* dst = destArray)
        {
            ArmOptimizer.NeonMemoryCopy(src, dst, length);
        }
    }
}

// Power management
var powerManager = new PowerManager();
powerManager.CurrentProfile = PowerProfile.Balanced;
int frameSkip = powerManager.RecommendedFrameSkip;
```

### I/O Optimization

```csharp
using BizHawk.Rafaelia.Optimization.IO;

// Async file I/O
byte[] data = await OptimizedFileIO.ReadFileAsync("game.rom");
await OptimizedFileIO.WriteFileAsync("save.state", data);

// Compression
byte[] compressed = CompressionHelper.CompressDeflate(data);
byte[] decompressed = CompressionHelper.DecompressDeflate(compressed);

// Read-ahead cache
using var cache = new ReadAheadCache(maxCacheSizeMB: 256);
byte[] cachedData = await cache.GetOrLoadAsync("file.bin");
```

## Performance Guidelines

### Memory
- Use array pooling for temporary buffers
- Prefer matrix structures for 2D data
- Use Span<T> for zero-allocation slicing
- Avoid allocations in hot paths

### CPU
- Use SIMD for bulk operations
- Parallelize independent work
- Pre-compute values in lookup tables
- Mark hot methods with [MethodImpl(MethodImplOptions.AggressiveInlining)]

### I/O
- Use async I/O for all file operations
- Enable read-ahead caching
- Compress save states and non-critical data
- Use memory-mapped files for large ROMs (>100MB)

### Mobile/ARM64
- Use NEON intrinsics for vectorization
- Align data to 64-byte cache lines
- Monitor thermal state and throttle if needed
- Optimize for battery life in power-save mode

## Platform Support

| Platform | Status | Notes |
|----------|--------|-------|
| Windows x64 | ✅ Full | .NET Framework 4.8 / .NET 6+ |
| Linux x64 | ✅ Full | Mono / .NET 6+ |
| macOS x64 | ⚠️ Partial | Mono support |
| Android ARM64 | 🔄 In Progress | .NET MAUI target |
| iOS ARM64 | 📋 Planned | Future support |
| macOS ARM64 | 📋 Planned | Apple Silicon |

## Build Requirements

- .NET SDK 6.0+ (8.0 recommended)
- C# 10.0+
- Platform-specific SDKs for mobile targets

## Testing

All modules include comprehensive inline documentation and are designed for:
- Unit testing with xUnit/NUnit
- Performance profiling with BenchmarkDotNet
- Memory leak detection with dotMemory
- Cross-platform verification

## See Also

- [ativa.txt](../ativa.txt) - Complete optimization instructions
- [OPTIMIZATION.md](../OPTIMIZATION.md) - General optimization guidelines
- [ARM64_MOBILE_SUPPORT.md](../ARM64_MOBILE_SUPPORT.md) - Mobile platform details

## License

MIT License - See [LICENSE](../LICENSE) for details

**Fork Parent**: BizHawk by TASEmulators (https://github.com/TASEmulators/BizHawk)  
**Fork Maintainer**: Rafael Melo Reis (https://github.com/rafaelmeloreisnovo/BizHawkRafaelia)
