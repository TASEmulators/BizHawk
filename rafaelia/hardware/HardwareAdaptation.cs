/*
 * ===========================================================================
 * BizHawkRafaelia - Hardware Adaptation Module
 * ===========================================================================
 * 
 * FORK PARENT: BizHawk by TASEmulators (https://github.com/TASEmulators/BizHawk)
 * FORK MAINTAINER: Rafael Melo Reis (https://github.com/rafaelmeloreisnovo/BizHawkRafaelia)
 * 
 * Module: Hardware Adaptation
 * Purpose: Dynamic quality adjustment from minimum to optimal hardware
 * Target: Support 2GB RAM devices to 32GB+ workstations
 * ===========================================================================
 */

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BizHawk.Rafaelia.Hardware
{
    /// <summary>
    /// Hardware capability detection and classification.
    /// Detects CPU, RAM, GPU capabilities and classifies system as minimum/good/excellent.
    /// </summary>
    public sealed class HardwareDetector
    {
        private static HardwareDetector? _instance;
        private static readonly object _lock = new object();

        private readonly HardwareProfile _profile;

        private HardwareDetector()
        {
            _profile = DetectHardware();
        }

        /// <summary>
        /// Gets singleton instance of hardware detector.
        /// Thread-safe lazy initialization.
        /// </summary>
        public static HardwareDetector Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new HardwareDetector();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets detected hardware profile.
        /// </summary>
        public HardwareProfile Profile => _profile;

        /// <summary>
        /// Detects hardware capabilities and returns profile.
        /// </summary>
        private HardwareProfile DetectHardware()
        {
            var profile = new HardwareProfile
            {
                ProcessorCount = Environment.ProcessorCount,
                Is64BitProcess = Environment.Is64BitProcess,
                IsArm64 = RuntimeInformation.ProcessArchitecture == Architecture.Arm64,
                OperatingSystem = GetOperatingSystem()
            };

            // Detect available memory (in GB)
            // Note: GC.GetGCMemoryInfo() provides more accurate total memory on modern .NET
            try
            {
                var gcMemInfo = GC.GetGCMemoryInfo();
                profile.TotalMemoryGB = gcMemInfo.TotalAvailableMemoryBytes / (1024.0 * 1024.0 * 1024.0);
            }
            catch
            {
                // Fallback: estimate from GC heap limit
                profile.TotalMemoryGB = 4.0; // Conservative estimate
            }

            // Classify hardware tier
            profile.Tier = ClassifyHardwareTier(profile);

            return profile;
        }

        /// <summary>
        /// Classifies hardware into performance tiers.
        /// </summary>
        private HardwareTier ClassifyHardwareTier(HardwareProfile profile)
        {
            // Minimum: 2GB RAM, 1-2 cores
            if (profile.TotalMemoryGB < 3.0 || profile.ProcessorCount <= 2)
                return HardwareTier.Minimum;

            // Good: 4-8GB RAM, 4+ cores
            if (profile.TotalMemoryGB < 12.0 || profile.ProcessorCount <= 4)
                return HardwareTier.Good;

            // Excellent: 12GB+ RAM, 6+ cores
            return HardwareTier.Excellent;
        }

        /// <summary>
        /// Detects operating system.
        /// </summary>
        private string GetOperatingSystem()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Windows";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "Linux";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "macOS";
            return "Unknown";
        }
    }

    /// <summary>
    /// Hardware profile information.
    /// </summary>
    public sealed class HardwareProfile
    {
        public int ProcessorCount { get; set; }
        public double TotalMemoryGB { get; set; }
        public bool Is64BitProcess { get; set; }
        public bool IsArm64 { get; set; }
        public string OperatingSystem { get; set; } = string.Empty;
        public HardwareTier Tier { get; set; }

        /// <summary>
        /// Gets recommended cache size in MB based on available memory.
        /// </summary>
        public int RecommendedCacheSizeMB
        {
            get
            {
                return Tier switch
                {
                    HardwareTier.Minimum => 64,    // 64MB cache for minimum hardware
                    HardwareTier.Good => 256,       // 256MB cache for good hardware
                    HardwareTier.Excellent => 1024, // 1GB cache for excellent hardware
                    _ => 128
                };
            }
        }

        /// <summary>
        /// Gets recommended worker thread count.
        /// </summary>
        public int RecommendedWorkerThreads
        {
            get
            {
                return Tier switch
                {
                    HardwareTier.Minimum => Math.Max(1, ProcessorCount / 2),
                    HardwareTier.Good => Math.Max(2, (ProcessorCount * 3) / 4),
                    HardwareTier.Excellent => ProcessorCount,
                    _ => 2
                };
            }
        }

        /// <summary>
        /// Checks if SIMD optimizations should be enabled.
        /// </summary>
        public bool EnableSimdOptimizations => Is64BitProcess && ProcessorCount >= 2;

        /// <summary>
        /// Checks if parallel processing should be enabled.
        /// </summary>
        public bool EnableParallelProcessing => ProcessorCount >= 4;
    }

    /// <summary>
    /// Hardware performance tier classification.
    /// </summary>
    public enum HardwareTier
    {
        /// <summary>
        /// Minimum hardware: 2GB RAM, 1-2 cores. Reduced quality, essential features only.
        /// </summary>
        Minimum,

        /// <summary>
        /// Good hardware: 4-8GB RAM, 4 cores. Standard quality, most features enabled.
        /// </summary>
        Good,

        /// <summary>
        /// Excellent hardware: 12GB+ RAM, 6+ cores. Maximum quality, all features enabled.
        /// </summary>
        Excellent
    }

    /// <summary>
    /// Adaptive quality settings that adjust based on hardware.
    /// Ensures smooth performance on all devices from minimum to excellent.
    /// </summary>
    public sealed class AdaptiveQualityManager
    {
        private readonly HardwareProfile _hardware;

        public AdaptiveQualityManager()
        {
            _hardware = HardwareDetector.Instance.Profile;
        }

        /// <summary>
        /// Gets recommended framebuffer scale factor (1.0 = native resolution).
        /// Lower values reduce memory and GPU load on weak hardware.
        /// </summary>
        public float FramebufferScale
        {
            get
            {
                return _hardware.Tier switch
                {
                    HardwareTier.Minimum => 0.75f,   // 75% resolution
                    HardwareTier.Good => 1.0f,        // Native resolution
                    HardwareTier.Excellent => 1.0f,   // Native resolution
                    _ => 1.0f
                };
            }
        }

        /// <summary>
        /// Gets whether to enable advanced rendering effects.
        /// </summary>
        public bool EnableAdvancedEffects => _hardware.Tier >= HardwareTier.Good;

        /// <summary>
        /// Gets whether to enable aggressive caching.
        /// </summary>
        public bool EnableAggressiveCaching => _hardware.Tier >= HardwareTier.Good;

        /// <summary>
        /// Gets whether to enable texture compression to save memory.
        /// </summary>
        public bool EnableTextureCompression => _hardware.Tier == HardwareTier.Minimum;

        /// <summary>
        /// Gets maximum number of cached frames.
        /// Higher values improve rewind performance but use more memory.
        /// </summary>
        public int MaxCachedFrames
        {
            get
            {
                return _hardware.Tier switch
                {
                    HardwareTier.Minimum => 300,      // ~5 seconds at 60fps
                    HardwareTier.Good => 1800,         // ~30 seconds
                    HardwareTier.Excellent => 7200,    // ~2 minutes
                    _ => 600
                };
            }
        }

        /// <summary>
        /// Gets whether to enable audio resampling quality.
        /// High-quality resampling uses more CPU.
        /// </summary>
        public AudioQuality AudioQuality
        {
            get
            {
                return _hardware.Tier switch
                {
                    HardwareTier.Minimum => AudioQuality.Fast,
                    HardwareTier.Good => AudioQuality.Good,
                    HardwareTier.Excellent => AudioQuality.Best,
                    _ => AudioQuality.Good
                };
            }
        }

        /// <summary>
        /// Prints hardware detection results for diagnostics.
        /// </summary>
        public string GetDiagnostics()
        {
            return $@"Hardware Profile:
  Tier: {_hardware.Tier}
  CPU Cores: {_hardware.ProcessorCount}
  Total Memory: {_hardware.TotalMemoryGB:F1} GB
  Architecture: {(_hardware.IsArm64 ? "ARM64" : "x64")}
  OS: {_hardware.OperatingSystem}
  64-bit Process: {_hardware.Is64BitProcess}

Adaptive Settings:
  Framebuffer Scale: {FramebufferScale:F2}x
  Advanced Effects: {EnableAdvancedEffects}
  Aggressive Caching: {EnableAggressiveCaching}
  Max Cached Frames: {MaxCachedFrames}
  Audio Quality: {AudioQuality}
  SIMD Enabled: {_hardware.EnableSimdOptimizations}
  Parallel Processing: {_hardware.EnableParallelProcessing}
  Recommended Cache: {_hardware.RecommendedCacheSizeMB} MB
  Worker Threads: {_hardware.RecommendedWorkerThreads}";
        }
    }

    /// <summary>
    /// Audio quality levels.
    /// </summary>
    public enum AudioQuality
    {
        Fast,   // Fastest, lower quality (linear interpolation)
        Good,   // Balanced quality/performance
        Best    // Best quality (sinc interpolation)
    }
}
