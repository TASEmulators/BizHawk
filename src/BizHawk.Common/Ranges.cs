using System.Runtime.CompilerServices;

namespace BizHawk.Common
{
	/// <summary>
	/// represents a closed interval (a.k.a. range) of <see cref="int"><c>s32</c>s</see>
	/// (class invariant: <see cref="Start"/> ≤ <see cref="EndInclusive"/>)
	/// </summary>
	public sealed class Int32Interval
	{
		public readonly int Start;

		public readonly int EndInclusive;

		/// <exception cref="ArgumentOutOfRangeException"><paramref name="endInclusive"/> &lt; <paramref name="start"/></exception>
		internal Int32Interval(int start, int endInclusive)
		{
			if (endInclusive < start) throw new ArgumentOutOfRangeException(paramName: nameof(endInclusive), actualValue: endInclusive, message: "interval end < start");
			Start = start;
			EndInclusive = endInclusive;
		}

		/// <returns>true iff <paramref name="value"/> is contained in this interval (<paramref name="value"/> is considered to be in the interval if it's exactly equal to either bound)</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(int value)
			=> Start <= value && value <= EndInclusive;

		/// <remarks>beware integer overflow when this interval spans every value</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint Count()
			=> (uint) ((long) EndInclusive - Start) + 1U;
	}

	/// <summary>
	/// represents a closed interval (a.k.a. range) of <see cref="long"><c>s64</c>s</see>
	/// (class invariant: <see cref="Start"/> ≤ <see cref="EndInclusive"/>)
	/// </summary>
	public sealed class Int64Interval
	{
		private const ulong MIN_LONG_NEGATION_AS_ULONG = 9223372036854775808UL;

		public readonly long Start;

		public readonly long EndInclusive;

		/// <inheritdoc cref="Int32Interval(int,int)"/>
		internal Int64Interval(long start, long endInclusive)
		{
			if (endInclusive < start) throw new ArgumentOutOfRangeException(paramName: nameof(endInclusive), actualValue: endInclusive, message: "interval end < start");
			Start = start;
			EndInclusive = endInclusive;
		}

		/// <inheritdoc cref="Int32Interval.Contains"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(long value)
			=> Start <= value && value <= EndInclusive;

		/// <inheritdoc cref="Int32Interval.Count"/>
		public ulong Count()
			=> (Contains(0L)
				? (Start == long.MinValue ? MIN_LONG_NEGATION_AS_ULONG : (ulong) -Start) + (ulong) EndInclusive
				: (ulong) (EndInclusive - Start)
			) + 1UL;
	}

	public static class RangeExtensions
	{
		private static ArithmeticException ExclusiveRangeMinValExc
			=> new("exclusive range end is min value of integral type");

		/// <returns><paramref name="value"/> if it's contained in <paramref name="range"/>, or else whichever bound of <paramref name="range"/> is closest to <paramref name="value"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ConstrainWithin(this int value, Int32Interval range)
			=> value < range.Start
				? range.Start
				: range.EndInclusive < value ? range.EndInclusive : value;

		/// <inheritdoc cref="Int32Interval(int,int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Int32Interval RangeTo(this int start, int endInclusive)
			=> new(start: start, endInclusive: endInclusive);

		/// <inheritdoc cref="Int64Interval(long,long)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Int64Interval RangeTo(this long start, long endInclusive)
			=> new(start: start, endInclusive: endInclusive);

		/// <exception cref="ArgumentOutOfRangeException"><paramref name="endExclusive"/> ≤ <paramref name="start"/> (empty ranges where <paramref name="start"/> = <paramref name="endExclusive"/> are not permitted)</exception>
		/// <exception cref="ArithmeticException"><paramref name="endExclusive"/> is min value of integral type (therefore <paramref name="endExclusive"/> ≤ <paramref name="start"/>)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Int32Interval RangeToExclusive(this int start, int endExclusive)
			=> endExclusive == int.MinValue
				? throw ExclusiveRangeMinValExc
				: new(start: start, endInclusive: endExclusive - 1);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Int64Interval RangeToExclusive(this long start, long endExclusive)
			=> endExclusive == long.MinValue
				? throw ExclusiveRangeMinValExc
				: new(start: start, endInclusive: endExclusive - 1L);

		/// <returns>true iff <paramref name="value"/> is strictly contained in <paramref name="range"/> (<paramref name="value"/> is considered to be OUTSIDE the range if it's exactly equal to either bound)</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool StrictlyBoundedBy(this int value, Int32Interval range)
			=> range.Start < value && value < range.EndInclusive;
	}
}
