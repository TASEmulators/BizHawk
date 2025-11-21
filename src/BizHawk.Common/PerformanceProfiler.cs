using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BizHawk.Common
{
	/// <summary>
	/// Lightweight performance profiler for monitoring execution time
	/// and identifying bottlenecks in emulation code.
	/// </summary>
	public sealed class PerformanceProfiler
	{
		private sealed class ProfileEntry
		{
			public long TotalTicks;
			public long CallCount;
			public long MinTicks = long.MaxValue;
			public long MaxTicks;

			public double AverageMilliseconds => 
				CallCount > 0 ? (TotalTicks / (double)CallCount) * 1000.0 / Stopwatch.Frequency : 0;

			public double TotalMilliseconds => 
				TotalTicks * 1000.0 / Stopwatch.Frequency;

			public double MinMilliseconds => 
				MinTicks != long.MaxValue ? MinTicks * 1000.0 / Stopwatch.Frequency : 0;

			public double MaxMilliseconds => 
				MaxTicks * 1000.0 / Stopwatch.Frequency;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void AddSample(long ticks)
			{
				TotalTicks += ticks;
				CallCount++;
				if (ticks < MinTicks) MinTicks = ticks;
				if (ticks > MaxTicks) MaxTicks = ticks;
			}

			public void Reset()
			{
				TotalTicks = 0;
				CallCount = 0;
				MinTicks = long.MaxValue;
				MaxTicks = 0;
			}
		}

		private static readonly PerformanceProfiler _instance = new PerformanceProfiler();
		private readonly Dictionary<string, ProfileEntry> _profiles = new Dictionary<string, ProfileEntry>();
		private readonly object _lock = new object();
		private bool _enabled;

		/// <summary>
		/// Gets the singleton instance of the performance profiler.
		/// </summary>
		public static PerformanceProfiler Instance => _instance;

		/// <summary>
		/// Gets or sets whether profiling is enabled.
		/// Disable in production for zero overhead.
		/// </summary>
		public bool Enabled
		{
			get => _enabled;
			set => _enabled = value;
		}

		private PerformanceProfiler()
		{
			_enabled = false; // Disabled by default for performance
		}

		/// <summary>
		/// Measures the execution time of an action.
		/// Returns immediately if profiling is disabled.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Measure(string name, Action action)
		{
			if (!_enabled)
			{
				action();
				return;
			}

			var sw = Stopwatch.StartNew();
			try
			{
				action();
			}
			finally
			{
				sw.Stop();
				RecordSample(name, sw.ElapsedTicks);
			}
		}

		/// <summary>
		/// Measures the execution time of a function.
		/// Returns immediately if profiling is disabled.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Measure<T>(string name, Func<T> func)
		{
			if (!_enabled)
			{
				return func();
			}

			var sw = Stopwatch.StartNew();
			try
			{
				return func();
			}
			finally
			{
				sw.Stop();
				RecordSample(name, sw.ElapsedTicks);
			}
		}

		/// <summary>
		/// Creates a profiling scope that automatically measures time when disposed.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ProfileScope BeginScope(string name)
		{
			return new ProfileScope(this, name, _enabled);
		}

		/// <summary>
		/// Records a timing sample for a named operation.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RecordSample(string name, long ticks)
		{
			if (!_enabled) return;

			lock (_lock)
			{
				if (!_profiles.TryGetValue(name, out var entry))
				{
					entry = new ProfileEntry();
					_profiles[name] = entry;
				}
				entry.AddSample(ticks);
			}
		}

		/// <summary>
		/// Gets profiling statistics for all measured operations.
		/// </summary>
		public Dictionary<string, ProfileStats> GetStatistics()
		{
			lock (_lock)
			{
				var stats = new Dictionary<string, ProfileStats>();
				foreach (var kvp in _profiles)
				{
					stats[kvp.Key] = new ProfileStats
					{
						Name = kvp.Key,
						CallCount = kvp.Value.CallCount,
						TotalMilliseconds = kvp.Value.TotalMilliseconds,
						AverageMilliseconds = kvp.Value.AverageMilliseconds,
						MinMilliseconds = kvp.Value.MinMilliseconds,
						MaxMilliseconds = kvp.Value.MaxMilliseconds
					};
				}
				return stats;
			}
		}

		/// <summary>
		/// Resets all profiling data.
		/// </summary>
		public void Reset()
		{
			lock (_lock)
			{
				foreach (var entry in _profiles.Values)
				{
					entry.Reset();
				}
			}
		}

		/// <summary>
		/// Clears all profiling data.
		/// </summary>
		public void Clear()
		{
			lock (_lock)
			{
				_profiles.Clear();
			}
		}

		/// <summary>
		/// Generates a formatted report of profiling statistics.
		/// </summary>
		public string GenerateReport(bool sortByTotal = true)
		{
			var stats = GetStatistics();
			if (stats.Count == 0)
			{
				return "No profiling data available. Enable profiling with PerformanceProfiler.Instance.Enabled = true";
			}

			var sortedStats = new List<ProfileStats>(stats.Values);
			if (sortByTotal)
			{
				sortedStats.Sort((a, b) => b.TotalMilliseconds.CompareTo(a.TotalMilliseconds));
			}
			else
			{
				sortedStats.Sort((a, b) => b.AverageMilliseconds.CompareTo(a.AverageMilliseconds));
			}

			var report = new System.Text.StringBuilder();
			report.AppendLine("=== Performance Profile Report ===");
			report.AppendLine();
			report.AppendFormat("{0,-40} {1,10} {2,12} {3,12} {4,12} {5,12}\n",
				"Operation", "Calls", "Total (ms)", "Avg (ms)", "Min (ms)", "Max (ms)");
			report.AppendLine(new string('-', 110));

			foreach (var stat in sortedStats)
			{
				report.AppendFormat("{0,-40} {1,10} {2,12:F3} {3,12:F3} {4,12:F3} {5,12:F3}\n",
					stat.Name,
					stat.CallCount,
					stat.TotalMilliseconds,
					stat.AverageMilliseconds,
					stat.MinMilliseconds,
					stat.MaxMilliseconds);
			}

			return report.ToString();
		}

		/// <summary>
		/// Disposable scope for automatic timing measurement.
		/// </summary>
		public readonly struct ProfileScope : IDisposable
		{
			private readonly PerformanceProfiler _profiler;
			private readonly string _name;
			private readonly Stopwatch? _stopwatch;
			private readonly bool _enabled;

			internal ProfileScope(PerformanceProfiler profiler, string name, bool enabled)
			{
				_profiler = profiler;
				_name = name;
				_enabled = enabled;
				_stopwatch = enabled ? Stopwatch.StartNew() : null;
			}

			public void Dispose()
			{
				if (_enabled)
				{
					_stopwatch!.Stop();
					_profiler.RecordSample(_name, _stopwatch.ElapsedTicks);
				}
			}
		}
	}

	/// <summary>
	/// Statistics for a profiled operation.
	/// </summary>
	public sealed class ProfileStats
	{
		public string Name { get; set; }
		public long CallCount { get; set; }
		public double TotalMilliseconds { get; set; }
		public double AverageMilliseconds { get; set; }
		public double MinMilliseconds { get; set; }
		public double MaxMilliseconds { get; set; }
	}

	/// <summary>
	/// Memory usage tracker for monitoring GC pressure and allocations.
	/// </summary>
	public static class MemoryMonitor
	{
		private static long _lastGen0Count;
		private static long _lastGen1Count;
		private static long _lastGen2Count;

		/// <summary>
		/// Captures current memory statistics.
		/// </summary>
		public static MemoryStats GetMemoryStats()
		{
			var gen0 = GC.CollectionCount(0);
			var gen1 = GC.CollectionCount(1);
			var gen2 = GC.CollectionCount(2);

			var stats = new MemoryStats
			{
				TotalMemoryBytes = GC.GetTotalMemory(false),
				Gen0Collections = gen0,
				Gen1Collections = gen1,
				Gen2Collections = gen2,
				Gen0Delta = (int)(gen0 - _lastGen0Count),
				Gen1Delta = (int)(gen1 - _lastGen1Count),
				Gen2Delta = (int)(gen2 - _lastGen2Count)
			};

			_lastGen0Count = gen0;
			_lastGen1Count = gen1;
			_lastGen2Count = gen2;

			return stats;
		}

		/// <summary>
		/// Resets the collection count deltas.
		/// </summary>
		public static void ResetDeltas()
		{
			_lastGen0Count = GC.CollectionCount(0);
			_lastGen1Count = GC.CollectionCount(1);
			_lastGen2Count = GC.CollectionCount(2);
		}

		/// <summary>
		/// Formats memory size in human-readable format.
		/// </summary>
		public static string FormatBytes(long bytes)
		{
			const long KB = 1024;
			const long MB = KB * 1024;
			const long GB = MB * 1024;

			if (bytes >= GB)
				return $"{bytes / (double)GB:F2} GB";
			if (bytes >= MB)
				return $"{bytes / (double)MB:F2} MB";
			if (bytes >= KB)
				return $"{bytes / (double)KB:F2} KB";
			return $"{bytes} bytes";
		}
	}

	/// <summary>
	/// Memory usage statistics.
	/// </summary>
	public sealed class MemoryStats
	{
		public long TotalMemoryBytes { get; set; }
		public int Gen0Collections { get; set; }
		public int Gen1Collections { get; set; }
		public int Gen2Collections { get; set; }
		public int Gen0Delta { get; set; }
		public int Gen1Delta { get; set; }
		public int Gen2Delta { get; set; }

		public override string ToString()
		{
			return $"Memory: {MemoryMonitor.FormatBytes(TotalMemoryBytes)}, " +
			       $"GC: Gen0={Gen0Collections}(+{Gen0Delta}), " +
			       $"Gen1={Gen1Collections}(+{Gen1Delta}), " +
			       $"Gen2={Gen2Collections}(+{Gen2Delta})";
		}
	}
}
