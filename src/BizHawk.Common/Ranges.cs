using System.Runtime.CompilerServices;

namespace BizHawk.Common
{
	/// <summary>
	/// represents a closed interval (a.k.a. range) of <typeparamref name="T"/>
	/// <br/>(class invariant: <see cref="INumericClosedIntervalContra{T}.Start"/> ≤ <see cref="INumericClosedIntervalContra{T}.EndInclusive"/>)
	/// </summary>
	public interface INumericClosedInterval<T> : INumericClosedIntervalContra<T>,
		IEquatable<INumericClosedIntervalContra<T>>
		where T : IComparable<T>
	{
		/// <summary>
		/// leaves <paramref name="value"/> unchanged if it's contained in the interval,
		/// or else sets it to whichever bound is closest
		/// </summary>
		void Clamp(ref T value);

		/// <returns>
		/// <see langword="true"/> iff <paramref name="value"/> is contained in this interval
		/// (<paramref name="value"/> is considered to be in the interval if it's exactly equal to either bound)
		/// </returns>
		bool Contains(in T value);

		/// <returns>
		/// <see langword="true"/> iff <paramref name="value"/> is strictly contained in this interval
		/// (<paramref name="value"/> is considered to be OUTSIDE the interval if it's exactly equal to either bound)
		/// </returns>
		bool StrictlyContains(in T value);
	}

	/// <summary>see <see cref="INumericClosedInterval{T}"/>; this is a type parameter variance hack</summary>
	public interface INumericClosedIntervalContra<out T>
	{
		T Start { get; }

		T EndInclusive { get; }
	}

	/// <summary>
	/// represents a closed range (a.k.a. interval) of <see cref="int"><c>s32</c>s</see>
	/// <br/>(class invariant: <see cref="NumericClosedRangeBase{T}.Start"/> ≤ <see cref="NumericClosedRangeBase{T}.EndInclusive"/>)
	/// </summary>
#pragma warning disable MA0077 // already implements `IEquatable<TSelf>`
	public sealed class Int32ClosedRange : NumericClosedRangeBase<int>
#pragma warning restore MA0077
	{
		private string? _ser = null;

		/// <inheritdoc cref="NumericClosedRangeBase{T}(T,T)"/>
		public Int32ClosedRange(int start, int endInclusive)
			: base(start, endInclusive) {}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Contains(in int value)
			=> Start <= value && value <= EndInclusive;

		/// <remarks>beware integer overflow when this range spans every value</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint Count()
			=> (uint) ((long) EndInclusive - Start) + 1U;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Int32ClosedRange other)
			=> Start == other.Start && EndInclusive == other.EndInclusive;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool StrictlyContains(in int value)
			=> Start < value && value < EndInclusive;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
			=> _ser ??= $"{Start}..={EndInclusive}";
	}

	/// <summary>
	/// represents a closed range (a.k.a. interval) of <see cref="long"><c>s64</c>s</see>
	/// <br/>(class invariant: <see cref="NumericClosedRangeBase{T}.Start"/> ≤ <see cref="NumericClosedRangeBase{T}.EndInclusive"/>)
	/// </summary>
#pragma warning disable MA0077 // already implements `IEquatable<TSelf>`
	public sealed class Int64ClosedRange : NumericClosedRangeBase<long>
#pragma warning restore MA0077
	{
		private const ulong MIN_LONG_NEGATION_AS_ULONG = 9223372036854775808UL;

		private ulong? _count = null;

		private string? _ser = null;

		/// <inheritdoc cref="NumericClosedRangeBase{T}(T,T)"/>
		public Int64ClosedRange(long start, long endInclusive)
			: base(start, endInclusive) {}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Contains(in long value)
			=> Start <= value && value <= EndInclusive;

		/// <inheritdoc cref="Int32ClosedRange.Count"/>
		public ulong Count()
			=> _count ??= (Contains(0L)
				? (Start is long.MinValue ? MIN_LONG_NEGATION_AS_ULONG : (ulong) -Start) + (ulong) EndInclusive
				: (ulong) (EndInclusive - Start)
			) + 1UL;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Int64ClosedRange other)
			=> Start == other.Start && EndInclusive == other.EndInclusive;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool StrictlyContains(in long value)
			=> Start < value && value < EndInclusive;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
			=> _ser ??= $"{Start}L..={EndInclusive}L";
	}

	/// <seealso cref="Int32ClosedRange"/>
	/// <seealso cref="Int64ClosedRange"/>
	public abstract class NumericClosedRangeBase<T> : INumericClosedInterval<T>
		where T : IComparable<T>
	{
		public T Start { get; }

		public T EndInclusive { get; }

		/// <exception cref="ArgumentOutOfRangeException"><paramref name="endInclusive"/> &lt; <paramref name="start"/></exception>
		protected NumericClosedRangeBase(T start, T endInclusive)
		{
			if (endInclusive.CompareTo(start) < 0) throw new ArgumentOutOfRangeException(paramName: nameof(endInclusive), actualValue: endInclusive, message: "range end < start");
			Start = start;
			EndInclusive = endInclusive;
		}

		public virtual void Clamp(ref T value)
		{
			if (Start.CompareTo(value) > 0) value = Start;
			else if (EndInclusive.CompareTo(value) < 0) value = EndInclusive;
		}

		public virtual bool Contains(in T value)
			=> Start.CompareTo(value) <= 0 && EndInclusive.CompareTo(value) >= 0;

		public bool Equals(INumericClosedIntervalContra<T> other)
			=> Start.CompareTo(other.Start) is 0 && EndInclusive.CompareTo(other.EndInclusive) is 0;

		public override bool Equals(object? other)
			=> other is INumericClosedIntervalContra<T> interval && Equals(interval);

		public override int GetHashCode()
			=> HashCode.Combine(Start, EndInclusive);

		public virtual bool StrictlyContains(in T value)
			=> Start.CompareTo(value) < 0 && EndInclusive.CompareTo(value) > 0;

		public abstract override string ToString();
	}

	public static class RangeExtensions
	{
		private static ArithmeticException ExclusiveRangeMinValExc
			=> new("exclusive range end is min value of integral type");

		/// <returns>
		/// <paramref name="value"/> unchanged if it's contained in <paramref name="range"/>,
		/// or else whichever bound of <paramref name="range"/> is closest to <paramref name="value"/>
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Clamp<T>(this INumericClosedInterval<T> range, T value)
			where T : IComparable<T>
		{
			range.Clamp(ref value);
			return value;
		}

		/// <returns>
		/// <paramref name="value"/> unchanged if it's contained in <paramref name="range"/>,
		/// or else whichever bound of <paramref name="range"/> is closest to <paramref name="value"/>
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T ConstrainWithin<T>(this T value, INumericClosedInterval<T> range)
			where T : IComparable<T>
		{
			range.Clamp(ref value);
			return value;
		}

		/// <inheritdoc cref="Int32ClosedRange(int,int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Int32ClosedRange RangeTo(this int start, int endInclusive)
			=> new(start: start, endInclusive: endInclusive);

		/// <inheritdoc cref="Int64ClosedRange(long,long)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Int64ClosedRange RangeTo(this long start, long endInclusive)
			=> new(start: start, endInclusive: endInclusive);

		/// <exception cref="ArgumentOutOfRangeException"><paramref name="endExclusive"/> ≤ <paramref name="start"/> (empty ranges where <paramref name="start"/> = <paramref name="endExclusive"/> are not permitted)</exception>
		/// <exception cref="ArithmeticException"><paramref name="endExclusive"/> is min value of integral type (therefore <paramref name="endExclusive"/> ≤ <paramref name="start"/>)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Int32ClosedRange RangeToExclusive(this int start, int endExclusive)
			=> endExclusive is int.MinValue
				? throw ExclusiveRangeMinValExc
				: new(start: start, endInclusive: endExclusive - 1);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Int64ClosedRange RangeToExclusive(this long start, long endExclusive)
			=> endExclusive is long.MinValue
				? throw ExclusiveRangeMinValExc
				: new(start: start, endInclusive: endExclusive - 1L);

		/// <returns>
		/// <see langword="true"/> iff <paramref name="value"/> is strictly contained in <paramref name="range"/>
		/// (<paramref name="value"/> is considered to be OUTSIDE the range if it's exactly equal to either bound)
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool StrictlyBoundedBy<T>(this T value, INumericClosedInterval<T> range)
			where T : IComparable<T>
			=> range.StrictlyContains(value);
	}
}
