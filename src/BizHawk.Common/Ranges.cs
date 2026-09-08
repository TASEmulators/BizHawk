using System.Runtime.CompilerServices;

namespace BizHawk.Common
{
	/// <summary>
	/// represents a half-open interval (a.k.a. range) of <typeparamref name="T"/>
	/// <br/>(class invariant: <see cref="INumericHalfOpenIntervalContra{T}.Start"/> &lt; <see cref="INumericHalfOpenIntervalContra{T}.EndExclusive"/>)
	/// </summary>
	public interface INumericHalfOpenInterval<T> : INumericHalfOpenIntervalContra<T>,
		IEquatable<INumericHalfOpenIntervalContra<T>>
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
	}

	/// <summary>see <see cref="INumericHalfOpenInterval{T}"/>; this is a type parameter variance hack</summary>
	public interface INumericHalfOpenIntervalContra<out T>
	{
		T Start { get; }

		T EndExclusive { get; }
	}

	/// <summary>
	/// represents a half-open range (a.k.a. interval) of <see cref="int"><c>s32</c>s</see>
	/// <br/>(class invariant: <see cref="NumericHalfOpenRangeBase{T}.Start"/> &lt; <see cref="NumericHalfOpenRangeBase{T}.EndExclusive"/>)
	/// </summary>
#pragma warning disable MA0077 // already implements `IEquatable<TSelf>`
	public sealed class Int32HalfOpenRange : NumericHalfOpenRangeBase<int>
#pragma warning restore MA0077
	{
		private string? _ser = null;

		public override int EndInclusive
			=> EndExclusive - 1;

		/// <inheritdoc cref="NumericHalfOpenRangeBase{T}(T,T)"/>
		public Int32HalfOpenRange(int start, int endExclusive)
			: base(start, endExclusive) {}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Contains(in int value)
			=> Start <= value && value < EndExclusive;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(int value)
			=> Start <= value && value < EndExclusive;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint Count()
			=> (uint) ((long) EndExclusive - Start);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Int32HalfOpenRange other)
			=> Start == other.Start && EndExclusive == other.EndExclusive;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
			=> _ser ??= $"{Start}..<{EndExclusive}";
	}

	/// <summary>
	/// represents a half-open range (a.k.a. interval) of <see cref="long"><c>s64</c>s</see>
	/// <br/>(class invariant: <see cref="NumericHalfOpenRangeBase{T}.Start"/> &lt; <see cref="NumericHalfOpenRangeBase{T}.EndExclusive"/>)
	/// </summary>
#pragma warning disable MA0077 // already implements `IEquatable<TSelf>`
	public sealed class Int64HalfOpenRange : NumericHalfOpenRangeBase<long>
#pragma warning restore MA0077
	{
		private const ulong MIN_LONG_NEGATION_AS_ULONG = 9223372036854775808UL;

		private ulong? _count = null;

		private string? _ser = null;

		public override long EndInclusive
			=> EndExclusive - 1L;

		/// <inheritdoc cref="NumericHalfOpenRangeBase{T}(T,T)"/>
		public Int64HalfOpenRange(long start, long endExclusive)
			: base(start, endExclusive) {}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Contains(in long value)
			=> Start <= value && value < EndExclusive;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(long value)
			=> Start <= value && value < EndExclusive;

		public ulong Count()
			=> _count ??= (Contains(0L)
				? (Start is long.MinValue ? MIN_LONG_NEGATION_AS_ULONG : (ulong) -Start) + (ulong) EndExclusive
				: (ulong) (EndExclusive - Start));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Int64HalfOpenRange other)
			=> Start == other.Start && EndExclusive == other.EndExclusive;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
			=> _ser ??= $"{Start}L..<{EndExclusive}L";
	}

	/// <seealso cref="Int32HalfOpenRange"/>
	/// <seealso cref="Int64HalfOpenRange"/>
	public abstract class NumericHalfOpenRangeBase<T> : INumericHalfOpenInterval<T>
		where T : IComparable<T>
	{
		public T Start { get; }

		public T EndExclusive { get; }

		public abstract T EndInclusive { get; }

		/// <exception cref="ArgumentOutOfRangeException"><paramref name="endExclusive"/> ≤ <paramref name="start"/> (empty ranges where <paramref name="start"/> = <paramref name="endExclusive"/> are not permitted)</exception>
		protected NumericHalfOpenRangeBase(T start, T endExclusive)
		{
			if (endExclusive.CompareTo(start) <= 0) throw new ArgumentOutOfRangeException(paramName: nameof(endExclusive), actualValue: endExclusive, message: "range end <= start");
			Start = start;
			EndExclusive = endExclusive;
		}

		public virtual void Clamp(ref T value)
		{
			if (Start.CompareTo(value) > 0) value = Start;
			else if (EndExclusive.CompareTo(value) <= 0) value = EndInclusive;
		}

		public virtual bool Contains(in T value)
			=> Start.CompareTo(value) <= 0 && EndExclusive.CompareTo(value) > 0;

		public bool Equals(INumericHalfOpenIntervalContra<T> other)
			=> Start.CompareTo(other.Start) is 0 && EndExclusive.CompareTo(other.EndExclusive) is 0;

		public override bool Equals(object? other)
			=> other is INumericHalfOpenIntervalContra<T> interval && Equals(interval);

		public override int GetHashCode()
			=> HashCode.Combine(Start, EndExclusive);

		public abstract override string ToString();
	}

	public static class RangeExtensions
	{
		private const string ERR_MSG_EXCL_END_MIN_VAL = "exclusive range end is min. value of integral type";

		private static OverflowException InclusiveRangeGTMaxValExc
			=> new("inclusive range end is max. value of integral type");

		/// <returns>
		/// <paramref name="value"/> unchanged if it's contained in <paramref name="range"/>,
		/// or else whichever bound of <paramref name="range"/> is closest to <paramref name="value"/>
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Clamp<T>(this INumericHalfOpenInterval<T> range, T value)
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
		public static T ConstrainWithin<T>(this T value, INumericHalfOpenInterval<T> range)
			where T : IComparable<T>
		{
			range.Clamp(ref value);
			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Deconstruct<T>(this INumericHalfOpenInterval<T> range, out T start, out T endExclusive)
			where T : IComparable<T>
		{
			start = range.Start;
			endExclusive = range.EndExclusive;
		}

		/// <exception cref="ArgumentOutOfRangeException"><paramref name="endInclusive"/> &lt; <paramref name="start"/> (empty ranges are not permitted)</exception>
		/// <exception cref="OverflowException"><paramref name="endInclusive"/> is <see cref="int.MaxValue"/> (it's incremented to be stored as an exclusive bound)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Int32HalfOpenRange RangeTo(this int start, int endInclusive)
			=> endInclusive is int.MaxValue
				? throw InclusiveRangeGTMaxValExc
				: new(start: start, endExclusive: unchecked(endInclusive + 1));

		/// <exception cref="ArgumentOutOfRangeException"><paramref name="endInclusive"/> &lt; <paramref name="start"/> (empty ranges are not permitted)</exception>
		/// <exception cref="OverflowException"><paramref name="endInclusive"/> is <see cref="long.MaxValue"/> (it's incremented to be stored as an exclusive bound)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Int64HalfOpenRange RangeTo(this long start, long endInclusive)
			=> endInclusive is long.MaxValue
				? throw InclusiveRangeGTMaxValExc
				: new(start: start, endExclusive: unchecked(endInclusive + 1L));

		/// <inheritdoc cref="Int32HalfOpenRange(int,int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Int32HalfOpenRange RangeToExclusive(this int start, int endExclusive)
			=> endExclusive is int.MinValue
				? throw new ArgumentOutOfRangeException(paramName: nameof(endExclusive), endExclusive, message: ERR_MSG_EXCL_END_MIN_VAL)
				: new(start: start, endExclusive: endExclusive);

		/// <inheritdoc cref="Int64HalfOpenRange(long,long)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Int64HalfOpenRange RangeToExclusive(this long start, long endExclusive)
			=> endExclusive is long.MinValue
				? throw new ArgumentOutOfRangeException(paramName: nameof(endExclusive), endExclusive, message: ERR_MSG_EXCL_END_MIN_VAL)
				: new(start: start, endExclusive: endExclusive);
	}
}
