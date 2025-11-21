/*
 * ===========================================================================
 * BizHawkRafaelia - Interoperability Module
 * ===========================================================================
 * 
 * FORK PARENT: BizHawk by TASEmulators (https://github.com/TASEmulators/BizHawk)
 * FORK MAINTAINER: Rafael Melo Reis (https://github.com/rafaelmeloreisnovo/BizHawkRafaelia)
 * 
 * Module: Interoperability
 * Purpose: Cross-platform and cross-version compatibility layer
 * Target: Support .NET Framework 4.8, .NET 6, .NET 8, Mono
 * ===========================================================================
 */

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BizHawk.Rafaelia.Interop
{
    /// <summary>
    /// Runtime detection and feature availability.
    /// Detects which .NET runtime is active and available features.
    /// </summary>
    public static class RuntimeDetector
    {
        /// <summary>
        /// Checks if running on .NET Framework (Windows only).
        /// </summary>
        public static bool IsNetFramework => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");

        /// <summary>
        /// Checks if running on .NET Core or .NET 5+.
        /// </summary>
        public static bool IsNetCore => RuntimeInformation.FrameworkDescription.StartsWith(".NET Core") ||
                                         RuntimeInformation.FrameworkDescription.StartsWith(".NET ");

        /// <summary>
        /// Checks if running on Mono runtime.
        /// </summary>
        public static bool IsMono => Type.GetType("Mono.Runtime") != null;

        /// <summary>
        /// Gets runtime description string.
        /// </summary>
        public static string RuntimeDescription => RuntimeInformation.FrameworkDescription;

        /// <summary>
        /// Gets runtime version.
        /// </summary>
        public static Version RuntimeVersion => Environment.Version;

        /// <summary>
        /// Checks if Span<T> is fully supported with hardware optimization.
        /// </summary>
        public static bool IsSpanSupported => !IsNetFramework;

        /// <summary>
        /// Checks if Vector<T> SIMD is supported.
        /// </summary>
        public static bool IsVectorSupported => System.Numerics.Vector.IsHardwareAccelerated;

        /// <summary>
        /// Checks if advanced intrinsics (AVX2, NEON) are available.
        /// Only available on .NET Core 3.0+.
        /// </summary>
        public static bool IsAdvancedIntrinsicsSupported => !IsNetFramework;
    }

    /// <summary>
    /// Platform-specific feature detection.
    /// </summary>
    public static class PlatformFeatures
    {
        /// <summary>
        /// Checks if running on Windows.
        /// </summary>
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Checks if running on Linux.
        /// </summary>
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>
        /// Checks if running on macOS.
        /// </summary>
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// Checks if running on Android.
        /// </summary>
        public static bool IsAndroid => RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"));

        /// <summary>
        /// Gets OS architecture (x64, ARM64, etc).
        /// </summary>
        public static Architecture OSArchitecture => RuntimeInformation.OSArchitecture;

        /// <summary>
        /// Gets process architecture.
        /// </summary>
        public static Architecture ProcessArchitecture => RuntimeInformation.ProcessArchitecture;

        /// <summary>
        /// Checks if running in 64-bit process.
        /// </summary>
        public static bool Is64Bit => Environment.Is64BitProcess;

        /// <summary>
        /// Checks if running on ARM64 architecture.
        /// </summary>
        public static bool IsArm64 => RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
    }

    /// <summary>
    /// Compatibility shims for features not available on all runtimes.
    /// Provides fallback implementations for older runtimes.
    /// </summary>
    public static class CompatibilityShims
    {
        /// <summary>
        /// Gets array as Span&lt;T&gt; if supported, otherwise returns wrapper.
        /// Zero-allocation on .NET Core, minimal overhead on .NET Framework.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(T[] array)
        {
#if NETCOREAPP || NET5_0_OR_GREATER
            return array.AsSpan();
#else
            return new Span<T>(array);
#endif
        }

        /// <summary>
        /// Gets array segment as Span&lt;T&gt;.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(T[] array, int start, int length)
        {
#if NETCOREAPP || NET5_0_OR_GREATER
            return array.AsSpan(start, length);
#else
            return new Span<T>(array, start, length);
#endif
        }

        /// <summary>
        /// High-resolution timestamp for performance measurements.
        /// Uses QueryPerformanceCounter on Windows, clock_gettime on Unix.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetTimestamp()
        {
            return System.Diagnostics.Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// Converts timestamp to milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TimestampToMilliseconds(long timestamp)
        {
            return (timestamp * 1000.0) / System.Diagnostics.Stopwatch.Frequency;
        }
    }

    /// <summary>
    /// Memory alignment utilities compatible across all runtimes.
    /// </summary>
    public static class AlignmentHelper
    {
        /// <summary>
        /// Checks if address is aligned to specified boundary.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsAligned(void* ptr, int alignment)
        {
            if (ptr == null)
                throw new ArgumentNullException(nameof(ptr));
            
            return ((long)ptr & (alignment - 1)) == 0;
        }

        /// <summary>
        /// Aligns size up to next boundary.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AlignUp(int size, int alignment)
        {
            return (size + alignment - 1) & ~(alignment - 1);
        }

        /// <summary>
        /// Aligns size down to boundary.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AlignDown(int size, int alignment)
        {
            return size & ~(alignment - 1);
        }
    }

    /// <summary>
    /// Diagnostics and profiling utilities.
    /// </summary>
    public sealed class PerformanceDiagnostics
    {
        /// <summary>
        /// Gets current GC memory info.
        /// </summary>
        public static string GetMemoryInfo()
        {
            var gcInfo = GC.GetGCMemoryInfo();
            return $@"Memory Info:
  Heap Size: {GC.GetTotalMemory(false) / (1024 * 1024):F2} MB
  Total Available: {gcInfo.TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024):F2} GB
  Gen 0 Collections: {GC.CollectionCount(0)}
  Gen 1 Collections: {GC.CollectionCount(1)}
  Gen 2 Collections: {GC.CollectionCount(2)}";
        }

        /// <summary>
        /// Gets runtime environment info.
        /// </summary>
        public static string GetRuntimeInfo()
        {
            return $@"Runtime Info:
  Framework: {RuntimeDetector.RuntimeDescription}
  Version: {RuntimeDetector.RuntimeVersion}
  Is .NET Framework: {RuntimeDetector.IsNetFramework}
  Is .NET Core: {RuntimeDetector.IsNetCore}
  Is Mono: {RuntimeDetector.IsMono}
  
Platform Info:
  OS: {RuntimeInformation.OSDescription}
  OS Arch: {PlatformFeatures.OSArchitecture}
  Process Arch: {PlatformFeatures.ProcessArchitecture}
  64-bit Process: {PlatformFeatures.Is64Bit}
  Is ARM64: {PlatformFeatures.IsArm64}
  
Feature Support:
  Span<T>: {RuntimeDetector.IsSpanSupported}
  SIMD: {RuntimeDetector.IsVectorSupported}
  Advanced Intrinsics: {RuntimeDetector.IsAdvancedIntrinsicsSupported}";
        }

        /// <summary>
        /// Gets full diagnostics report.
        /// </summary>
        public static string GetFullDiagnostics()
        {
            return $@"=== BizHawkRafaelia Performance Diagnostics ===

{GetRuntimeInfo()}

{GetMemoryInfo()}

CPU Info:
  Processor Count: {Environment.ProcessorCount}
  Is Server GC: {GCSettings.IsServerGC}
  GC Mode: {(GCSettings.IsServerGC ? "Server" : "Workstation")}
  GC LOH Compaction: {GCSettings.LargeObjectHeapCompactionMode}

============================================";
        }
    }
}
