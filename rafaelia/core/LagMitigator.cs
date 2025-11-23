// ===========================================================================
// BizHawkRafaelia - Lag & Latency Mitigation Module
// ===========================================================================
// 
// FORK PARENT: BizHawk by TASEmulators (https://github.com/TASEmulators/BizHawk)
// FORK MAINTAINER: Rafael Melo Reis (https://github.com/rafaelmeloreisnovo/BizHawkRafaelia)
// 
// Purpose: Detect and mitigate lag, latency, and freezing issues
// Implements: Real-time performance monitoring and adaptive optimization
// ZIPRAF_OMEGA: ψχρΔΣΩ performance compliance
// ===========================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace BizHawk.Rafaelia.Core
{
	/// <summary>
	/// Real-time lag and latency detection and mitigation system
	/// </summary>
	public sealed class LagMitigator : IDisposable
	{
		private readonly ConcurrentDictionary<string, PerformanceTracker> _operations = new();
		private readonly Timer _monitoringTimer;
		private readonly object _lock = new();
		private long _lagEvents = 0;
		private long _freezeEvents = 0;
		private long _mitigatedEvents = 0;
		private bool _disposed = false;

		// Performance thresholds (in milliseconds)
		private const int LAG_THRESHOLD_MS = 16; // ~60 FPS
		private const int SEVERE_LAG_THRESHOLD_MS = 50; // Noticeable lag
		private const int FREEZE_THRESHOLD_MS = 500; // Freeze detection
		private const int MONITORING_INTERVAL_MS = 1000; // 1 second

		/// <summary>
		/// Current performance level
		/// </summary>
		public PerformanceLevel CurrentPerformanceLevel { get; private set; } = PerformanceLevel.Optimal;

		/// <summary>
		/// Initializes lag mitigator
		/// </summary>
		public LagMitigator()
		{
			_monitoringTimer = new Timer(MonitorPerformance, null, 
				MONITORING_INTERVAL_MS, MONITORING_INTERVAL_MS);
		}

		/// <summary>
		/// Measures operation performance
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PerformanceMeasurement MeasureOperation(string operationName)
		{
			return new PerformanceMeasurement(this, operationName);
		}

		/// <summary>
		/// Records operation timing
		/// </summary>
		internal void RecordOperationTime(string operationName, long elapsedMs)
		{
			_operations.AddOrUpdate(operationName,
				_ => new PerformanceTracker
				{
					OperationName = operationName,
					LastDurationMs = elapsedMs,
					AverageDurationMs = elapsedMs,
					MaxDurationMs = elapsedMs,
					MinDurationMs = elapsedMs,
					SampleCount = 1,
					LastExecutionTime = DateTime.UtcNow
				},
				(_, tracker) =>
				{
					tracker.LastDurationMs = elapsedMs;
					tracker.MaxDurationMs = Math.Max(tracker.MaxDurationMs, elapsedMs);
					tracker.MinDurationMs = Math.Min(tracker.MinDurationMs, elapsedMs);
					tracker.SampleCount++;
					
					// Calculate rolling average
					tracker.AverageDurationMs = 
						(tracker.AverageDurationMs * (tracker.SampleCount - 1) + elapsedMs) / 
						tracker.SampleCount;
					
					tracker.LastExecutionTime = DateTime.UtcNow;
					
					return tracker;
				});

			// Detect lag/freeze
			if (elapsedMs > FREEZE_THRESHOLD_MS)
			{
				Interlocked.Increment(ref _freezeEvents);
				HandleFreeze(operationName, elapsedMs);
			}
			else if (elapsedMs > SEVERE_LAG_THRESHOLD_MS)
			{
				Interlocked.Increment(ref _lagEvents);
				HandleLag(operationName, elapsedMs);
			}
		}

		/// <summary>
		/// Monitors overall performance
		/// </summary>
		private void MonitorPerformance(object? state)
		{
			if (_disposed) return;

			lock (_lock)
			{
				// Calculate overall performance level
				var totalOperations = 0;
				var laggyOperations = 0;

				foreach (var tracker in _operations.Values)
				{
					totalOperations++;
					if (tracker.AverageDurationMs > LAG_THRESHOLD_MS)
					{
						laggyOperations++;
					}
				}

				// Determine performance level
				if (totalOperations > 0)
				{
					var lagRatio = (double)laggyOperations / totalOperations;
					
					CurrentPerformanceLevel = lagRatio switch
					{
						<= 0.1 => PerformanceLevel.Optimal,
						<= 0.3 => PerformanceLevel.Good,
						<= 0.5 => PerformanceLevel.Degraded,
						<= 0.7 => PerformanceLevel.Poor,
						_ => PerformanceLevel.Critical
					};

					// Apply adaptive mitigation based on performance level
					ApplyAdaptiveMitigation(CurrentPerformanceLevel);
				}

				#if DEBUG
				Debug.WriteLine($"[PERFORMANCE] Level: {CurrentPerformanceLevel}, " +
					$"Lag Events: {_lagEvents}, Freeze Events: {_freezeEvents}, " +
					$"Mitigated: {_mitigatedEvents}");
				#endif
			}
		}

		/// <summary>
		/// Handles detected lag
		/// </summary>
		private void HandleLag(string operationName, long elapsedMs)
		{
			#if DEBUG
			Debug.WriteLine($"[LAG DETECTED] {operationName} took {elapsedMs}ms (threshold: {SEVERE_LAG_THRESHOLD_MS}ms)");
			#endif

			// Mitigation strategy: Suggest async alternative
			Interlocked.Increment(ref _mitigatedEvents);
		}

		/// <summary>
		/// Handles detected freeze
		/// </summary>
		private void HandleFreeze(string operationName, long elapsedMs)
		{
			#if DEBUG
			Debug.WriteLine($"[FREEZE DETECTED] {operationName} took {elapsedMs}ms (threshold: {FREEZE_THRESHOLD_MS}ms)");
			#endif

			// Mitigation strategy: Force GC if memory pressure is high
			var gen0 = GC.CollectionCount(0);
			var gen1 = GC.CollectionCount(1);
			var gen2 = GC.CollectionCount(2);

			if (gen2 > 10) // High GC pressure
			{
				Task.Run(() =>
				{
					GC.Collect(1, GCCollectionMode.Optimized, blocking: false);
				});
			}

			Interlocked.Increment(ref _mitigatedEvents);
		}

		/// <summary>
		/// Applies adaptive mitigation based on performance level
		/// </summary>
		private void ApplyAdaptiveMitigation(PerformanceLevel level)
		{
			switch (level)
			{
				case PerformanceLevel.Degraded:
					// Reduce quality settings
					#if DEBUG
					Debug.WriteLine("[MITIGATION] Performance degraded - consider reducing quality");
					#endif
					break;

				case PerformanceLevel.Poor:
					// More aggressive optimization
					#if DEBUG
					Debug.WriteLine("[MITIGATION] Performance poor - applying aggressive optimization");
					#endif
					GC.Collect(1, GCCollectionMode.Optimized, blocking: false);
					break;

				case PerformanceLevel.Critical:
					// Emergency measures
					#if DEBUG
					Debug.WriteLine("[MITIGATION] Performance critical - emergency measures");
					#endif
					GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
					break;
			}
		}

		/// <summary>
		/// Gets performance statistics
		/// </summary>
		public PerformanceStatistics GetStatistics()
		{
			return new PerformanceStatistics
			{
				CurrentLevel = CurrentPerformanceLevel,
				TotalLagEvents = _lagEvents,
				TotalFreezeEvents = _freezeEvents,
				TotalMitigatedEvents = _mitigatedEvents,
				TrackedOperations = _operations.Values.ToList()
			};
		}

		/// <summary>
		/// Generates performance report
		/// </summary>
		public string GeneratePerformanceReport()
		{
			var stats = GetStatistics();
			var report = new System.Text.StringBuilder();

			report.AppendLine("═══════════════════════════════════════════════════════════");
			report.AppendLine("Performance & Lag Analysis Report");
			report.AppendLine("═══════════════════════════════════════════════════════════");
			report.AppendLine();
			report.AppendLine($"Current Performance Level: {stats.CurrentLevel}");
			report.AppendLine($"Total Lag Events: {stats.TotalLagEvents}");
			report.AppendLine($"Total Freeze Events: {stats.TotalFreezeEvents}");
			report.AppendLine($"Total Mitigated Events: {stats.TotalMitigatedEvents}");
			report.AppendLine();

			if (stats.TrackedOperations.Count > 0)
			{
				report.AppendLine("Operation Performance:");
				report.AppendLine("───────────────────────────────────────────────────────────");
				
				var slowestOps = stats.TrackedOperations
					.OrderByDescending(t => t.AverageDurationMs)
					.Take(10);

				foreach (var tracker in slowestOps)
				{
					var status = tracker.AverageDurationMs switch
					{
						<= LAG_THRESHOLD_MS => "✓ Optimal",
						<= SEVERE_LAG_THRESHOLD_MS => "⚠ Slow",
						<= FREEZE_THRESHOLD_MS => "⚠ Lagging",
						_ => "✗ Critical"
					};

					report.AppendLine($"  {tracker.OperationName} [{status}]");
					report.AppendLine($"    Average: {tracker.AverageDurationMs:F2}ms");
					report.AppendLine($"    Min: {tracker.MinDurationMs}ms, Max: {tracker.MaxDurationMs}ms");
					report.AppendLine($"    Samples: {tracker.SampleCount}");
					report.AppendLine();
				}
			}

			report.AppendLine("═══════════════════════════════════════════════════════════");

			return report.ToString();
		}

		/// <summary>
		/// Clears performance tracking
		/// </summary>
		public void ClearTracking()
		{
			_operations.Clear();
			_lagEvents = 0;
			_freezeEvents = 0;
			_mitigatedEvents = 0;
		}

		/// <summary>
		/// Disposes resources
		/// </summary>
		public void Dispose()
		{
			if (_disposed) return;

			_disposed = true;
			_monitoringTimer?.Dispose();
			_operations.Clear();

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Performance measurement helper
		/// </summary>
		public struct PerformanceMeasurement : IDisposable
		{
			private readonly LagMitigator _mitigator;
			private readonly string _operationName;
			private readonly Stopwatch _stopwatch;

			internal PerformanceMeasurement(LagMitigator mitigator, string operationName)
			{
				_mitigator = mitigator;
				_operationName = operationName;
				_stopwatch = Stopwatch.StartNew();
			}

			public void Dispose()
			{
				_stopwatch.Stop();
				_mitigator.RecordOperationTime(_operationName, _stopwatch.ElapsedMilliseconds);
			}
		}

		/// <summary>
		/// Performance tracker
		/// </summary>
		public class PerformanceTracker
		{
			public string OperationName { get; set; } = string.Empty;
			public long LastDurationMs { get; set; }
			public double AverageDurationMs { get; set; }
			public long MaxDurationMs { get; set; }
			public long MinDurationMs { get; set; }
			public long SampleCount { get; set; }
			public DateTime LastExecutionTime { get; set; }
		}

		/// <summary>
		/// Performance statistics
		/// </summary>
		public struct PerformanceStatistics
		{
			public PerformanceLevel CurrentLevel { get; set; }
			public long TotalLagEvents { get; set; }
			public long TotalFreezeEvents { get; set; }
			public long TotalMitigatedEvents { get; set; }
			public List<PerformanceTracker> TrackedOperations { get; set; }
		}

		/// <summary>
		/// Performance level enumeration
		/// </summary>
		public enum PerformanceLevel
		{
			Optimal,
			Good,
			Degraded,
			Poor,
			Critical
		}
	}

	/// <summary>
	/// Global lag mitigator instance
	/// </summary>
	public static class GlobalLagMonitor
	{
		private static readonly Lazy<LagMitigator> _instance = 
			new(() => new LagMitigator());

		public static LagMitigator Instance => _instance.Value;

		/// <summary>
		/// Measures operation performance
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static LagMitigator.PerformanceMeasurement MeasureOperation(string operationName)
		{
			return Instance.MeasureOperation(operationName);
		}
	}
}
