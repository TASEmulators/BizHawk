namespace BizHawk.Common
{
	/// <summary>represents a closed range of <typeparamref name="T"/> (class invariant: <see cref="Start"/> ≤ <see cref="EndInclusive"/>)</summary>
#pragma warning disable CA1715 // breaks IInterface convention
	public interface Range<out T> where T : unmanaged, IComparable<T>
#pragma warning restore CA1715
	{
		T Start { get; }

		T EndInclusive { get; }
	}

	internal class MutableRange<T> : Range<T> where T : unmanaged, IComparable<T>
	{
		private (T Start, T EndInclusive) r;

		public T Start
		{
			get => r.Start;
		}

		public T EndInclusive
		{
			get => r.EndInclusive;
		}

		/// <exception cref="ArgumentException"><typeparamref name="T"/> is <see langword="float"/>/<see langword="double"/> and either bound is <see cref="float.NaN"/></exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="endInclusive"/> &lt; <paramref name="start"/></exception>
		internal MutableRange(T start, T endInclusive)
		{
			if (endInclusive.CompareTo(start) < 0) throw new ArgumentOutOfRangeException(nameof(endInclusive), endInclusive, "range end < start");
			if (start is float fs)
			{
				if (float.IsNaN(fs)) throw new ArgumentException("range start is NaN", nameof(start));
				if (endInclusive is float fe && float.IsNaN(fe)) throw new ArgumentException("range end is NaN", nameof(endInclusive));
			}
			else if (start is double ds)
			{
				if (double.IsNaN(ds)) throw new ArgumentException("range start is NaN", nameof(start));
				if (endInclusive is double de && double.IsNaN(de)) throw new ArgumentException("range end is NaN", nameof(endInclusive));
			}
			r = (start, endInclusive);
		}
	}

	/// <summary>contains most of the logic for ranges</summary>
	/// <remarks>
	/// non-generic overloads are used where the method requires an increment or decrement
	/// </remarks>
	public static class RangeExtensions
	{
		private const ulong MIN_LONG_NEGATION_AS_ULONG = 9223372036854775808UL;

		private static ArithmeticException ExclusiveRangeMinValExc
			=> new("exclusive range end is min value of integral type");

		/// <returns><paramref name="value"/> if it's contained in <paramref name="range"/>, or else whichever bound of <paramref name="range"/> is closest to <paramref name="value"/></returns>
		public static T ConstrainWithin<T>(this T value, Range<T> range) where T : unmanaged, IComparable<T> => value.CompareTo(range.Start) < 0
			? range.Start
			: range.EndInclusive.CompareTo(value) < 0
				? range.EndInclusive
				: value;

		/// <returns>true iff <paramref name="value"/> is contained in <paramref name="range"/> (<paramref name="value"/> is considered to be in the range if it's exactly equal to either bound)</returns>
		public static bool Contains<T>(this Range<T> range, T value) where T : unmanaged, IComparable<T> => !(value.CompareTo(range.Start) < 0 || range.EndInclusive.CompareTo(value) < 0);

		/// <remarks>beware integer overflow when <paramref name="range"/> contains every value</remarks>
		public static uint Count(this Range<int> range) => (uint) ((long) range.EndInclusive - range.Start) + 1U;

		/// <inheritdoc cref="Count(Range{int})"/>
		public static ulong Count(this Range<long> range) => (range.Contains(0L)
			? (range.Start == long.MinValue ? MIN_LONG_NEGATION_AS_ULONG : (ulong) -range.Start) + (ulong) range.EndInclusive
			: (ulong) (range.EndInclusive - range.Start)
		) + 1UL;

		/// <inheritdoc cref="MutableRange{T}(T,T)"/>
		private static MutableRange<T> MutableRangeTo<T>(this T start, T endInclusive) where T : unmanaged, IComparable<T> => new MutableRange<T>(start, endInclusive);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		private static MutableRange<int> MutableRangeToExclusive(this int start, int endExclusive) => endExclusive == int.MinValue
			? throw ExclusiveRangeMinValExc
			: new MutableRange<int>(start, endExclusive - 1);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		private static MutableRange<long> MutableRangeToExclusive(this long start, long endExclusive) => endExclusive == long.MinValue
			? throw ExclusiveRangeMinValExc
			: new MutableRange<long>(start, endExclusive - 1L);

		/// <inheritdoc cref="MutableRange{T}(T,T)"/>
		public static Range<T> RangeTo<T>(this T start, T endInclusive) where T : unmanaged, IComparable<T> => start.MutableRangeTo(endInclusive);

		/// <exception cref="ArgumentOutOfRangeException"><paramref name="endExclusive"/> ≤ <paramref name="start"/> (empty ranges where <paramref name="start"/> = <paramref name="endExclusive"/> are not permitted)</exception>
		/// <exception cref="ArithmeticException"><paramref name="endExclusive"/> is min value of integral type (therefore <paramref name="endExclusive"/> ≤ <paramref name="start"/>)</exception>
		public static Range<int> RangeToExclusive(this int start, int endExclusive) => MutableRangeToExclusive(start, endExclusive);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static Range<long> RangeToExclusive(this long start, long endExclusive) => MutableRangeToExclusive(start, endExclusive);

		/// <returns>true iff <paramref name="value"/> is strictly contained in <paramref name="range"/> (<paramref name="value"/> is considered to be OUTSIDE the range if it's exactly equal to either bound)</returns>
		public static bool StrictlyBoundedBy<T>(this T value, Range<T> range) where T : unmanaged, IComparable<T> => range.Start.CompareTo(value) < 0 && value.CompareTo(range.EndInclusive) < 0;
	}
}
