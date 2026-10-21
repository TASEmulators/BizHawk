using System.Runtime.CompilerServices;

using BizHawk.Common.NumberExtensions;

using TimestampAbs = long;
using TimestampRel = long;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Holds the counter and logic for the host framerate display.
	/// The caller is responsible for providing a monotonic clock, and actually displaying the estimate.
	/// </summary>
	public struct HostFramerateHistory
	{
		private const double ZERO_FPS = 0.0;

		/// <summary>how many ticks of the monotonic clock correspond to 1 second of wall time</summary>
		private readonly TimestampRel _countsPerSecond;

		/// <summary>number of frames rendered since the estimate was last revised</summary>
		private uint _framesSinceRevision;

		/// <summary>the minimum time (in ticks of the monotonic clock) that must pass before revising the estimate</summary>
		private readonly TimestampRel _revisionsMinDelay;

		/// <summary>the point in time (according to the monotonic clock) when the estimate was last revised</summary>
		private TimestampAbs _waitingUntil;

		/// <value>latest estimate (to nearest whole fps)</value>
		public readonly int ApproxFramerate
			=> Framerate.RoundToInt();

		/// <value>latest estimate (with fractional part)</value>
		public double Framerate { get; private set; } = ZERO_FPS;

		/// <param name="countsPerSecond">how many ticks of the monotonic clock correspond to 1 second of wall time</param>
		/// <param name="initTimestamp">from the monotonic clock</param>
		public HostFramerateHistory(TimestampRel countsPerSecond, TimestampAbs initTimestamp)
		{
			const long UPDATES_PER_SECOND = 4;
			_countsPerSecond = countsPerSecond;
			_revisionsMinDelay = _countsPerSecond / UPDATES_PER_SECOND;
			Reinit(initTimestamp);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IncrementCounter()
			=> _framesSinceRevision++;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IncrementCounterBy(uint numFrames)
			=> _framesSinceRevision += numFrames;

		/// <param name="currentTimestamp">from the monotonic clock</param>
		/// <param name="fpsString">w/ <c> fps</c> suffix</param>
		/// <returns><see langword="true"/> iff the estimate was revised (which only happens after <see cref="_revisionsMinDelay"/> ticks have passed)</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool MaybeRevise(TimestampAbs currentTimestamp, out string fpsString)
		{
			if (currentTimestamp < _waitingUntil)
			{
				fpsString = null;
				return false;
			}
			Revise(currentTimestamp);
			fpsString = $"{ApproxFramerate} fps";
			return true;
		}

		/// <param name="initTimestamp">from the monotonic clock</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reinit(TimestampAbs initTimestamp)
		{
			Framerate = ZERO_FPS;
			_framesSinceRevision = 0U;
			_waitingUntil = initTimestamp + _revisionsMinDelay;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void Revise(TimestampAbs currentTimestamp)
		{
			const double SMOOTHING_FACTOR = 8.0;
			TimestampRel elapsedTicks = currentTimestamp - _waitingUntil + _revisionsMinDelay;
			Framerate = Framerate is ZERO_FPS
				? (_framesSinceRevision - 1U) * _countsPerSecond / (double) elapsedTicks
				: (_framesSinceRevision * SMOOTHING_FACTOR + Framerate) / (elapsedTicks * SMOOTHING_FACTOR / _countsPerSecond + 1.0);
			_framesSinceRevision = 0U;
			_waitingUntil = currentTimestamp + _revisionsMinDelay;
		}
	}
}
