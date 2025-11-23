// ===========================================================================
// BizHawkRafaelia - Memory Leak Detection & Mitigation Module
// ===========================================================================
// 
// FORK PARENT: BizHawk by TASEmulators (https://github.com/TASEmulators/BizHawk)
// FORK MAINTAINER: Rafael Melo Reis (https://github.com/rafaelmeloreisnovo/BizHawkRafaelia)
// 
// Purpose: Detect and mitigate memory leaks in real-time
// Implements: ZIPRAF_OMEGA memory management compliance
// ===========================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BizHawk.Rafaelia.Core
{
	/// <summary>
	/// Real-time memory leak detection and mitigation system
	/// </summary>
	public sealed class MemoryLeakDetector : IDisposable
	{
		private readonly ConcurrentDictionary<string, AllocationTracker> _allocations = new();
		private readonly Timer _monitoringTimer;
		private readonly object _lock = new();
		private long _totalAllocations = 0;
		private long _totalDeallocations = 0;
		private long _suspectedLeaks = 0;
		private bool _disposed = false;

		// Configuration thresholds
		private const long LEAK_THRESHOLD_BYTES = 10 * 1024 * 1024; // 10MB
		private const int MONITORING_INTERVAL_MS = 5000; // 5 seconds
		private const int MAX_ALLOCATION_AGE_MS = 60000; // 60 seconds

		/// <summary>
		/// Initializes memory leak detector with automatic monitoring
		/// </summary>
		public MemoryLeakDetector()
		{
			_monitoringTimer = new Timer(MonitorMemoryUsage, null, 
				MONITORING_INTERVAL_MS, MONITORING_INTERVAL_MS);
		}

		/// <summary>
		/// Tracks memory allocation
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void TrackAllocation(string context, long size, string callerFile = "", 
			int callerLine = 0)
		{
			Interlocked.Increment(ref _totalAllocations);

			var key = $"{context}@{callerFile}:{callerLine}";
			_allocations.AddOrUpdate(key,
				_ => new AllocationTracker
				{
					Context = context,
					Size = size,
					Timestamp = DateTime.UtcNow,
					Count = 1,
					CallerFile = callerFile,
					CallerLine = callerLine
				},
				(_, tracker) =>
				{
					tracker.Count++;
					tracker.Size += size;
					tracker.Timestamp = DateTime.UtcNow;
					return tracker;
				});
		}

		/// <summary>
		/// Tracks memory deallocation
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void TrackDeallocation(string context, string callerFile = "", 
			int callerLine = 0)
		{
			Interlocked.Increment(ref _totalDeallocations);

			var key = $"{context}@{callerFile}:{callerLine}";
			if (_allocations.TryGetValue(key, out var tracker))
			{
				tracker.Count--;
				if (tracker.Count <= 0)
				{
					_allocations.TryRemove(key, out _);
				}
			}
		}

		/// <summary>
		/// Monitors memory usage and detects leaks
		/// </summary>
		private void MonitorMemoryUsage(object? state)
		{
			if (_disposed) return;

			lock (_lock)
			{
				var now = DateTime.UtcNow;
				var process = Process.GetCurrentProcess();
				var workingSet = process.WorkingSet64;
				var privateMemory = process.PrivateMemorySize64;

				// Check for stale allocations (potential leaks)
				foreach (var kvp in _allocations)
				{
					var tracker = kvp.Value;
					var age = (now - tracker.Timestamp).TotalMilliseconds;

					// If allocation is old and large, it might be a leak
					if (age > MAX_ALLOCATION_AGE_MS && tracker.Size > LEAK_THRESHOLD_BYTES)
					{
						Interlocked.Increment(ref _suspectedLeaks);
						
						#if DEBUG
						Debug.WriteLine($"[MEMORY LEAK] Suspected leak detected:");
						Debug.WriteLine($"  Context: {tracker.Context}");
						Debug.WriteLine($"  Size: {tracker.Size / (1024.0 * 1024.0):F2} MB");
						Debug.WriteLine($"  Age: {age / 1000.0:F1} seconds");
						Debug.WriteLine($"  Location: {tracker.CallerFile}:{tracker.CallerLine}");
						#endif

						// Apply mitigation
						MitigateMemoryLeak(kvp.Key, tracker);
					}
				}

				// Log current state
				#if DEBUG
				Debug.WriteLine($"[MEMORY MONITOR] Working Set: {workingSet / (1024.0 * 1024.0):F2} MB, " +
					$"Private: {privateMemory / (1024.0 * 1024.0):F2} MB, " +
					$"Tracked: {_allocations.Count} allocations");
				#endif
			}
		}

		/// <summary>
		/// Attempts to mitigate detected memory leak
		/// </summary>
		private void MitigateMemoryLeak(string key, AllocationTracker tracker)
		{
			// Strategy 1: Force garbage collection
			if (tracker.Size > LEAK_THRESHOLD_BYTES * 2)
			{
				GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
				GC.WaitForPendingFinalizers();
				
				#if DEBUG
				Debug.WriteLine($"[MITIGATION] Forced GC collection for {tracker.Context}");
				#endif
			}

			// Strategy 2: Remove stale tracking entry
			_allocations.TryRemove(key, out _);
		}

		/// <summary>
		/// Gets current memory statistics
		/// </summary>
		public MemoryStatistics GetStatistics()
		{
			var process = Process.GetCurrentProcess();
			
			return new MemoryStatistics
			{
				WorkingSetBytes = process.WorkingSet64,
				PrivateMemoryBytes = process.PrivateMemorySize64,
				TotalAllocations = _totalAllocations,
				TotalDeallocations = _totalDeallocations,
				ActiveTrackers = _allocations.Count,
				SuspectedLeaks = _suspectedLeaks,
				TrackedAllocations = new List<AllocationTracker>(_allocations.Values)
			};
		}

		/// <summary>
		/// Generates memory leak report
		/// </summary>
		public string GenerateLeakReport()
		{
			var stats = GetStatistics();
			var report = new System.Text.StringBuilder();

			report.AppendLine("═══════════════════════════════════════════════════════════");
			report.AppendLine("Memory Leak Detection Report");
			report.AppendLine("═══════════════════════════════════════════════════════════");
			report.AppendLine();
			report.AppendLine($"Working Set: {stats.WorkingSetBytes / (1024.0 * 1024.0):F2} MB");
			report.AppendLine($"Private Memory: {stats.PrivateMemoryBytes / (1024.0 * 1024.0):F2} MB");
			report.AppendLine($"Total Allocations: {stats.TotalAllocations}");
			report.AppendLine($"Total Deallocations: {stats.TotalDeallocations}");
			report.AppendLine($"Active Trackers: {stats.ActiveTrackers}");
			report.AppendLine($"Suspected Leaks: {stats.SuspectedLeaks}");
			report.AppendLine();

			if (stats.TrackedAllocations.Count > 0)
			{
				report.AppendLine("Top Memory Consumers:");
				report.AppendLine("───────────────────────────────────────────────────────────");
				
				var topConsumers = stats.TrackedAllocations
					.OrderByDescending(t => t.Size)
					.Take(10);

				foreach (var tracker in topConsumers)
				{
					var age = (DateTime.UtcNow - tracker.Timestamp).TotalSeconds;
					report.AppendLine($"  {tracker.Context}");
					report.AppendLine($"    Size: {tracker.Size / (1024.0 * 1024.0):F2} MB");
					report.AppendLine($"    Count: {tracker.Count}");
					report.AppendLine($"    Age: {age:F1} seconds");
					report.AppendLine($"    Location: {tracker.CallerFile}:{tracker.CallerLine}");
					report.AppendLine();
				}
			}

			report.AppendLine("═══════════════════════════════════════════════════════════");

			return report.ToString();
		}

		/// <summary>
		/// Clears all tracking data
		/// </summary>
		public void ClearTracking()
		{
			_allocations.Clear();
			_totalAllocations = 0;
			_totalDeallocations = 0;
			_suspectedLeaks = 0;
		}

		/// <summary>
		/// Disposes resources
		/// </summary>
		public void Dispose()
		{
			if (_disposed) return;

			_disposed = true;
			_monitoringTimer?.Dispose();
			_allocations.Clear();

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Allocation tracker structure
		/// </summary>
		public class AllocationTracker
		{
			public string Context { get; set; } = string.Empty;
			public long Size { get; set; }
			public DateTime Timestamp { get; set; }
			public int Count { get; set; }
			public string CallerFile { get; set; } = string.Empty;
			public int CallerLine { get; set; }
		}

		/// <summary>
		/// Memory statistics structure
		/// </summary>
		public struct MemoryStatistics
		{
			public long WorkingSetBytes { get; set; }
			public long PrivateMemoryBytes { get; set; }
			public long TotalAllocations { get; set; }
			public long TotalDeallocations { get; set; }
			public int ActiveTrackers { get; set; }
			public long SuspectedLeaks { get; set; }
			public List<AllocationTracker> TrackedAllocations { get; set; }
		}
	}

	/// <summary>
	/// Global memory leak detector instance
	/// </summary>
	public static class GlobalMemoryMonitor
	{
		private static readonly Lazy<MemoryLeakDetector> _instance = 
			new(() => new MemoryLeakDetector());

		public static MemoryLeakDetector Instance => _instance.Value;

		/// <summary>
		/// Tracks allocation with automatic caller information
		/// </summary>
		public static void TrackAllocation(string context, long size,
			[CallerFilePath] string callerFile = "",
			[CallerLineNumber] int callerLine = 0)
		{
			Instance.TrackAllocation(context, size, callerFile, callerLine);
		}

		/// <summary>
		/// Tracks deallocation with automatic caller information
		/// </summary>
		public static void TrackDeallocation(string context,
			[CallerFilePath] string callerFile = "",
			[CallerLineNumber] int callerLine = 0)
		{
			Instance.TrackDeallocation(context, callerFile, callerLine);
		}
	}
}
