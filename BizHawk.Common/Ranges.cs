namespace BizHawk.Common
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using BizHawk.Common.NumberExtensions;

	/// <summary>semantically similar to <see cref="Range{T}"/>, but obviously does no checks at runtime</summary>
	public struct RangeStruct<T> where T : unmanaged, IComparable<T>
	{
		public T Start;

		public T EndInclusive;
	}

	/// <summary>represents a closed range of <typeparamref name="T"/> (class invariant: <see cref="Start"/> &le; <see cref="EndInclusive"/>)</summary>
	public interface Range<out T> where T : unmanaged, IComparable<T>
	{
		T Start { get; }

		T EndInclusive { get; }
	}

	/// <summary>represents a closed range of <typeparamref name="T"/> which can be grown or shrunk (class invariant: <see cref="Start"/> &le; <see cref="EndInclusive"/>)</summary>
	public class MutableRange<T> : Range<T> where T : unmanaged, IComparable<T>
	{
		private RangeStruct<T> r;

		/// <inheritdoc cref="Overwrite"/>
		public MutableRange(T start, T endInclusive) => Overwrite(start, endInclusive);

		/// <exception cref="ArgumentOutOfRangeException">(from setter) <paramref name="value"/> > <see cref="EndInclusive"/></exception>
		public T Start
		{
			get => r.Start;
			set => r.Start = r.EndInclusive.CompareTo(value) < 0
				? throw new ArgumentOutOfRangeException(nameof(value), value, "attempted to set start > end")
				: value;
		}

		/// <exception cref="ArgumentOutOfRangeException">(from setter) <paramref name="value"/> &lt; <see cref="Start"/></exception>
		public T EndInclusive
		{
			get => r.EndInclusive;
			set => r.EndInclusive = value.CompareTo(r.Start) < 0
				? throw new ArgumentOutOfRangeException(nameof(value), value, "attempted to set end < start")
				: value;
		}

		/// <exception cref="ArgumentException"><typeparamref name="T"/> is <see langword="float"/>/<see langword="double"/> and either bound is <see cref="float.NaN"/></exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="endInclusive"/> &lt; <paramref name="start"/></exception>
		public void Overwrite(T start, T endInclusive)
		{
			var range = new RangeStruct<T> { Start = start, EndInclusive = endInclusive };
			if (range.EndInclusive.CompareTo(range.Start) < 0) throw new ArgumentOutOfRangeException(nameof(range), range, "range end < start");
			if (range is RangeStruct<float> fr && (float.IsNaN(fr.Start) || float.IsNaN(fr.EndInclusive))
				|| range is RangeStruct<double> dr && (double.IsNaN(dr.Start) || double.IsNaN(dr.EndInclusive)))
			{
				throw new ArgumentException("range bound is NaN", nameof(range));
			}
			r = range;
		}
	}

	/// <summary>contains most of the logic for ranges</summary>
	/// <remarks>
	/// non-generic overloads are used where the method requires an increment or decrement<br/>
	/// TODO which Enumerate algorithm is faster - <c>yield return</c> in loop or <see cref="Enumerable.Range"/>?
	/// </remarks>
	public static class RangeExtensions
	{
		private const ulong MIN_LONG_NEGATION_AS_ULONG = 9223372036854775808UL;

		private static readonly ArithmeticException ExclusiveRangeMinValExc = new ArithmeticException("exclusive range end is min value of integral type");

		/// <returns><paramref name="value"/> if it's contained in <paramref name="range"/>, or else whichever bound of <paramref name="range"/> is closest to <paramref name="value"/></returns>
		public static T ConstrainWithin<T>(this T value, Range<T> range) where T : unmanaged, IComparable<T> => value.CompareTo(range.Start) < 0
			? range.Start
			: range.EndInclusive.CompareTo(value) < 0
				? range.EndInclusive
				: value;

		/// <returns>true iff <paramref name="value"/> is contained in <paramref name="range"/> (<paramref name="value"/> is considered to be in the range if it's exactly equal to either bound)</returns>
		/// <seealso cref="StrictlyBoundedBy"/>
		public static bool Contains<T>(this Range<T> range, T value) where T : unmanaged, IComparable<T> => !(value.CompareTo(range.Start) < 0 || range.EndInclusive.CompareTo(value) < 0);

		public static uint Count(this Range<byte> range) => (uint) (range.EndInclusive - range.Start + 1);

		/// <remarks>beware integer overflow when <paramref name="range"/> contains every value</remarks>
		public static uint Count(this Range<int> range) => (uint) ((long) range.EndInclusive - range.Start) + 1U;

		/// <inheritdoc cref="Count(Range{int})"/>
		public static ulong Count(this Range<long> range) => (range.Contains(0L)
			? (range.Start == long.MinValue ? MIN_LONG_NEGATION_AS_ULONG : (ulong) -range.Start) + (ulong) range.EndInclusive
			: (ulong) (range.EndInclusive - range.Start)
		) + 1UL;

		public static uint Count(this Range<sbyte> range) => (uint) (range.EndInclusive - range.Start + 1);

		public static uint Count(this Range<short> range) => (uint) (range.EndInclusive - range.Start + 1);

		/// <inheritdoc cref="Count(Range{int})"/>
		public static uint Count(this Range<uint> range) => range.EndInclusive - range.Start + 1U;

		/// <inheritdoc cref="Count(Range{int})"/>
		public static ulong Count(this Range<ulong> range) => range.EndInclusive - range.Start + 1UL;

		public static uint Count(this Range<ushort> range) => (uint) (range.EndInclusive - range.Start + 1);

		public static IEnumerable<byte> Enumerate(this Range<byte> range) => Enumerable.Range(range.Start, (int) range.Count()).Select(i => (byte) i);

		/// <inheritdoc cref="Enumerate(Range{float},float)"/>
		public static IEnumerable<double> Enumerate(this Range<double> range, double step)
		{
			var d = range.Start;
			while (d < range.EndInclusive)
			{
				yield return d;
				d += step;
			}
			if (d.HawkFloatEquality(range.EndInclusive)) yield return d;
		}

		/// <remarks>beware precision errors</remarks>
		public static IEnumerable<float> Enumerate(this Range<float> range, float step)
		{
			var f = range.Start;
			while (f < range.EndInclusive)
			{
				yield return f;
				f += step;
			}
			if (f.HawkFloatEquality(range.EndInclusive)) yield return f;
		}

		public static IEnumerable<int> Enumerate(this Range<int> range)
		{
			var i = range.Start;
			while (i < range.EndInclusive) yield return i++;
			yield return i;
		}

		public static IEnumerable<long> Enumerate(this Range<long> range)
		{
			var l = range.Start;
			while (l < range.EndInclusive) yield return l++;
			yield return l;
		}

		public static IEnumerable<sbyte> Enumerate(this Range<sbyte> range) => Enumerable.Range(range.Start, (int) range.Count()).Select(i => (sbyte) i);

		public static IEnumerable<short> Enumerate(this Range<short> range) => Enumerable.Range(range.Start, (int) range.Count()).Select(i => (short) i);

		public static IEnumerable<uint> Enumerate(this Range<uint> range)
		{
			var i = range.Start;
			while (i < range.EndInclusive) yield return i++;
			yield return i;
		}

		public static IEnumerable<ulong> Enumerate(this Range<ulong> range)
		{
			var l = range.Start;
			while (l < range.EndInclusive) yield return l++;
			yield return l;
		}

		public static IEnumerable<ushort> Enumerate(this Range<ushort> range) => Enumerable.Range(range.Start, (int) range.Count()).Select(i => (ushort) i);

		public static Range<T> GetImmutableCopy<T>(this Range<T> range) where T : unmanaged, IComparable<T> => GetMutableCopy(range);

		public static MutableRange<T> GetMutableCopy<T>(this Range<T> range) where T : unmanaged, IComparable<T> => new MutableRange<T>(range.Start, range.EndInclusive);

		/// <inheritdoc cref="MutableRange{T}(T,T)"/>
		public static MutableRange<T> MutableRangeTo<T>(this T start, T endInclusive) where T : unmanaged, IComparable<T> => new MutableRange<T>(start, endInclusive);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static MutableRange<byte> MutableRangeToExclusive(this byte start, byte endExclusive) => endExclusive == byte.MinValue
			? throw ExclusiveRangeMinValExc
			: new MutableRange<byte>(start, (byte) (endExclusive - 1U));

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static MutableRange<int> MutableRangeToExclusive(this int start, int endExclusive) => endExclusive == int.MinValue
			? throw ExclusiveRangeMinValExc
			: new MutableRange<int>(start, endExclusive - 1);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static MutableRange<long> MutableRangeToExclusive(this long start, long endExclusive) => endExclusive == long.MinValue
			? throw ExclusiveRangeMinValExc
			: new MutableRange<long>(start, endExclusive - 1L);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static MutableRange<sbyte> MutableRangeToExclusive(this sbyte start, sbyte endExclusive) => endExclusive == sbyte.MinValue
			? throw ExclusiveRangeMinValExc
			: new MutableRange<sbyte>(start, (sbyte) (endExclusive - 1));

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static MutableRange<short> MutableRangeToExclusive(this short start, short endExclusive) => endExclusive == short.MinValue
			? throw ExclusiveRangeMinValExc
			: new MutableRange<short>(start, (short) (endExclusive - 1));

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static MutableRange<uint> MutableRangeToExclusive(this uint start, uint endExclusive) => endExclusive == uint.MinValue
			? throw ExclusiveRangeMinValExc
			: new MutableRange<uint>(start, endExclusive - 1U);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static MutableRange<ulong> MutableRangeToExclusive(this ulong start, ulong endExclusive) => endExclusive == ulong.MinValue
			? throw ExclusiveRangeMinValExc
			: new MutableRange<ulong>(start, endExclusive - 1UL);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static MutableRange<ushort> MutableRangeToExclusive(this ushort start, ushort endExclusive) => endExclusive == ushort.MinValue
			? throw ExclusiveRangeMinValExc
			: new MutableRange<ushort>(start, (ushort) (endExclusive - 1U));

		/// <inheritdoc cref="MutableRange{T}(T,T)"/>
		public static Range<T> RangeTo<T>(this T start, T endInclusive) where T : unmanaged, IComparable<T> => start.MutableRangeTo(endInclusive);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static Range<byte> RangeToExclusive(this byte start, byte endExclusive) => MutableRangeToExclusive(start, endExclusive);

		/// <exception cref="ArgumentOutOfRangeException"><paramref name="endExclusive"/> &le; <paramref name="start"/> (empty ranges where <paramref name="start"/> = <paramref name="endExclusive"/> are not permitted)</exception>
		/// <exception cref="ArithmeticException"><paramref name="endExclusive"/> is min value of integral type (therefore <paramref name="endExclusive"/> &le; <paramref name="start"/>)</exception>
		public static Range<int> RangeToExclusive(this int start, int endExclusive) => MutableRangeToExclusive(start, endExclusive);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static Range<long> RangeToExclusive(this long start, long endExclusive) => MutableRangeToExclusive(start, endExclusive);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static Range<sbyte> RangeToExclusive(this sbyte start, sbyte endExclusive) => MutableRangeToExclusive(start, endExclusive);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static Range<short> RangeToExclusive(this short start, short endExclusive) => MutableRangeToExclusive(start, endExclusive);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static Range<uint> RangeToExclusive(this uint start, uint endExclusive) => MutableRangeToExclusive(start, endExclusive);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static Range<ulong> RangeToExclusive(this ulong start, ulong endExclusive) => MutableRangeToExclusive(start, endExclusive);

		/// <inheritdoc cref="RangeToExclusive(int,int)"/>
		public static Range<ushort> RangeToExclusive(this ushort start, ushort endExclusive) => MutableRangeToExclusive(start, endExclusive);

		/// <returns>true iff <paramref name="value"/> is strictly contained in <paramref name="range"/> (<paramref name="value"/> is considered to be OUTSIDE the range if it's exactly equal to either bound)</returns>
		/// <seealso cref="Contains"/>
		public static bool StrictlyBoundedBy<T>(this T value, Range<T> range) where T : unmanaged, IComparable<T> => range.Start.CompareTo(value) < 0 && value.CompareTo(range.EndInclusive) < 0;
	}
}
