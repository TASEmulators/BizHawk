using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common.NumberExtensions;

namespace BizHawk.Common
{
	public struct RangeStruct<T> where T : unmanaged, IComparable<T>
	{
		public T Start;

		public T EndInclusive;
	}

	/// <summary>represents a closed, inclusive range of <typeparamref name="T"/> (<see cref="Start"/> &le; <see cref="EndInclusive"/>)</summary>
	public interface Range<out T> where T : unmanaged, IComparable<T>
	{
		T Start { get; }

		T EndInclusive { get; }
	}

	public class RangeImpl<T> : Range<T> where T : unmanaged, IComparable<T>
	{
		protected RangeStruct<T> r;

		/// <exception cref="ArgumentException"><paramref name="range"/>.<see cref="RangeStruct{T}.EndInclusive"/> &lt; <paramref name="range"/>.<see cref="RangeStruct{T}.Start"/>, or <typeparamref name="T"/> is <see cref="float">float</see>/<see cref="double">double</see> and either bound is <c>NaN</c></exception>
		internal RangeImpl(RangeStruct<T> range)
		{
			r = ValidatedOrThrow(range);
		}

		/// <exception cref="ArgumentException"><paramref name="endInclusive"/> &lt; <paramref name="start"/>, or <typeparamref name="T"/> is <see langword="float"/>/<see langword="double"/> and either bound is <see cref="float.NaN"/></exception>
		public RangeImpl(T start, T endInclusive) : this(new RangeStruct<T> { Start = start, EndInclusive = endInclusive }) {}

		public T Start => r.Start;

		public T EndInclusive => r.EndInclusive;

		internal RangeStruct<T> CopyStruct() => r;

		protected static RangeStruct<T1> ValidatedOrThrow<T1>(RangeStruct<T1> range) where T1 : unmanaged, IComparable<T1>
		{
			if (range is RangeStruct<float> fr && (float.IsNaN(fr.Start) || float.IsNaN(fr.EndInclusive))
				|| range is RangeStruct<double> dr && (double.IsNaN(dr.Start) || double.IsNaN(dr.EndInclusive)))
			{
				throw new ArgumentException("range bound is NaN", nameof(range));
			}
			if (range.EndInclusive.CompareTo(range.Start) < 0) throw new ArgumentException("range end < start", nameof(range));
			return range;
		}
	}

	/// <summary>represents a closed, inclusive range of <typeparamref name="T"/> (<see cref="Start"/> &le; <see cref="EndInclusive"/>) which can be grown or shrunk</summary>
	/// <remarks>inheriting <see cref="RangeImpl{T}"/> reduces code duplication in <see cref="RangeExtensions"/></remarks>
	public class MutableRange<T> : RangeImpl<T> where T : unmanaged, IComparable<T>
	{
		/// <inheritdoc cref="RangeImpl{T}(BizHawk.Common.RangeStruct{T})"/>
		internal MutableRange(RangeStruct<T> range) : base(range) {}

		/// <inheritdoc cref="RangeImpl{T}(T,T)"/>
		public MutableRange(T start, T endInclusive) : base(new RangeStruct<T> { Start = start, EndInclusive = endInclusive }) {}

		/// <exception cref="ArgumentException">(from setter) <paramref name="value"/> > <see cref="EndInclusive"/></exception>
		public new T Start
		{
			get => r.Start;
			set => r.Start = r.EndInclusive.CompareTo(value) < 0
				? throw new ArgumentException("attempted to set start > end", nameof(value))
				: value;
		}

		/// <exception cref="ArgumentException">(from setter) <paramref name="value"/> &lt; <see cref="Start"/></exception>
		public new T EndInclusive
		{
			get => r.EndInclusive;
			set => r.EndInclusive = value.CompareTo(r.Start) < 0
				? throw new ArgumentException("attempted to set end < start", nameof(value))
				: value;
		}

		public void Overwrite(T start, T endInclusive) => r = ValidatedOrThrow(new RangeStruct<T> { Start = start, EndInclusive = endInclusive });
	}

	/// <summary>contains most of the logic for ranges</summary>
	/// <remarks>non-generic overloads are used where the method requires an increment or decrement</remarks>
	public static class RangeExtensions
	{
		private const string EXCL_RANGE_ARITH_EXC_TEXT = "exclusive range end is min value of integral type";

		/// <returns><paramref name="value"/> if it's contained in <paramref name="range"/>, or else whichever bound of <paramref name="range"/> is closest to <paramref name="value"/></returns>
		public static T ConstrainWithin<T>(this T value, Range<T> range) where T : unmanaged, IComparable<T> => value.CompareTo(range.Start) < 0
			? range.Start
			: range.EndInclusive.CompareTo(value) < 0
				? range.EndInclusive
				: value;

		/// <returns>true iff <paramref name="value"/> is contained in <paramref name="range"/> (<paramref name="value"/> is considered to be in the range if it's exactly equal to either bound)</returns>
		/// <seealso cref="StrictlyInBounds"/>
		public static bool Contains<T>(this Range<T> range, T value) where T : unmanaged, IComparable<T> => !(value.CompareTo(range.Start) < 0 || range.EndInclusive.CompareTo(value) < 0);

		public static byte Count(this Range<byte> range) => (byte) (range.EndInclusive - range.Start + (byte) 1U);

		public static uint Count(this Range<int> range) => (uint) (1L + range.EndInclusive - range.Start);

		public static ulong Count(this Range<long> range) => throw new NotImplementedException("TODO fancy math");

		public static byte Count(this Range<sbyte> range) => (byte) (1 + range.EndInclusive - range.Start);

		public static ushort Count(this Range<short> range) => (ushort) (1 + range.EndInclusive - range.Start);

		public static uint Count(this Range<uint> range) => range.EndInclusive - range.Start + 1U;

		public static ulong Count(this Range<ulong> range) => range.EndInclusive - range.Start + 1U;

		public static ushort Count(this Range<ushort> range) => (ushort) (range.EndInclusive - range.Start + (ushort) 1U);

		/// <remarks>TODO is this faster or slower than the <c>yield return</c> algorithm used in <see cref="Enumerate(Range{int})"/>?</remarks>
		public static IEnumerable<byte> Enumerate(this Range<byte> range) => Enumerable.Range(range.Start, range.Count()).Select(i => (byte) i);

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

		public static IEnumerable<sbyte> Enumerate(this Range<sbyte> range) => Enumerable.Range(range.Start, range.Count()).Select(i => (sbyte) i);

		public static IEnumerable<short> Enumerate(this Range<short> range) => Enumerable.Range(range.Start, range.Count()).Select(i => (short) i);

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

		public static IEnumerable<ushort> Enumerate(this Range<ushort> range) => Enumerable.Range(range.Start, range.Count()).Select(i => (ushort) i);

		public static Range<T> GetImmutableCopy<T>(this MutableRange<T> range) where T : unmanaged, IComparable<T> => new MutableRange<T>(range.CopyStruct());

		public static MutableRange<T> GetMutableCopy<T>(this Range<T> range) where T : unmanaged, IComparable<T> => range is RangeImpl<T> impl
			? new MutableRange<T>(impl.CopyStruct()) // copied by value when using the implementations in this file
			: new MutableRange<T>(range.Start, range.EndInclusive);

		/// <inheritdoc cref="MutableRange{T}(T,T)"/>
		public static MutableRange<T> MutableRangeTo<T>(this T start, T endInclusive) where T : unmanaged, IComparable<T> => new MutableRange<T>(start, endInclusive);

		/// <inheritdoc cref="RangeToExcluding(int,int)"/>
		public static MutableRange<byte> MutableRangeToExcluding(this byte start, byte endExclusive) => endExclusive == byte.MinValue
			? throw new ArgumentException(EXCL_RANGE_ARITH_EXC_TEXT, nameof(endExclusive))
			: new MutableRange<byte>(start, (byte) (endExclusive - 1U));

		/// <inheritdoc cref="RangeToExcluding(int,int)"/>
		public static MutableRange<int> MutableRangeToExcluding(this int start, int endExclusive) => endExclusive == int.MinValue
			? throw new ArgumentException(EXCL_RANGE_ARITH_EXC_TEXT, nameof(endExclusive))
			: new MutableRange<int>(start, endExclusive - 1);

		/// <inheritdoc cref="RangeToExcluding(int,int)"/>
		public static MutableRange<long> MutableRangeToExcluding(this long start, long endExclusive) => endExclusive == long.MinValue
			? throw new ArgumentException(EXCL_RANGE_ARITH_EXC_TEXT, nameof(endExclusive))
			: new MutableRange<long>(start, endExclusive - 1L);

		/// <inheritdoc cref="RangeToExcluding(int,int)"/>
		public static MutableRange<sbyte> MutableRangeToExcluding(this sbyte start, sbyte endExclusive) => endExclusive == sbyte.MinValue
			? throw new ArgumentException(EXCL_RANGE_ARITH_EXC_TEXT, nameof(endExclusive))
			: new MutableRange<sbyte>(start, (sbyte) (endExclusive - 1));

		/// <inheritdoc cref="RangeToExcluding(int,int)"/>
		public static MutableRange<short> MutableRangeToExcluding(this short start, short endExclusive) => endExclusive == short.MinValue
			? throw new ArgumentException(EXCL_RANGE_ARITH_EXC_TEXT, nameof(endExclusive))
			: new MutableRange<short>(start, (short) (endExclusive - 1));

		/// <inheritdoc cref="RangeToExcluding(int,int)"/>
		public static MutableRange<uint> MutableRangeToExcluding(this uint start, uint endExclusive) => endExclusive == uint.MinValue
			? throw new ArgumentException(EXCL_RANGE_ARITH_EXC_TEXT, nameof(endExclusive))
			: new MutableRange<uint>(start, endExclusive - 1U);

		/// <inheritdoc cref="RangeToExcluding(int,int)"/>
		public static MutableRange<ulong> MutableRangeToExcluding(this ulong start, ulong endExclusive) => endExclusive == ulong.MinValue
			? throw new ArgumentException(EXCL_RANGE_ARITH_EXC_TEXT, nameof(endExclusive))
			: new MutableRange<ulong>(start, endExclusive - 1UL);

		/// <inheritdoc cref="RangeToExcluding(int,int)"/>
		public static MutableRange<ushort> MutableRangeToExcluding(this ushort start, ushort endExclusive) => endExclusive == ushort.MinValue
			? throw new ArgumentException(EXCL_RANGE_ARITH_EXC_TEXT, nameof(endExclusive))
			: new MutableRange<ushort>(start, (ushort) (endExclusive - 1U));

		/// <inheritdoc cref="RangeImpl{T}(T,T)"/>
		public static Range<T> RangeTo<T>(this T start, T endInclusive) where T : unmanaged, IComparable<T> => new RangeImpl<T>(start, endInclusive);

		/// <inheritdoc cref="RangeImpl{T}(T,T)"/>
		public static Range<byte> RangeToExcluding(this byte start, byte endExclusive) => MutableRangeToExcluding(start, endExclusive);

		/// <exception cref="ArgumentException"><paramref name="endExclusive"/> &le; <paramref name="start"/> (empty ranges where <paramref name="start"/> = <paramref name="endExclusive"/> are not permitted)</exception>
		/// <exception cref="ArithmeticException"><paramref name="endExclusive"/> is min value of integral type (therefore <paramref name="endExclusive"/> &le; <paramref name="start"/>)</exception>
		public static Range<int> RangeToExcluding(this int start, int endExclusive) => MutableRangeToExcluding(start, endExclusive);

		/// <inheritdoc cref="RangeImpl{T}(T,T)"/>
		public static Range<long> RangeToExcluding(this long start, long endExclusive) => MutableRangeToExcluding(start, endExclusive);

		/// <inheritdoc cref="RangeImpl{T}(T,T)"/>
		public static Range<sbyte> RangeToExcluding(this sbyte start, sbyte endExclusive) => MutableRangeToExcluding(start, endExclusive);

		/// <inheritdoc cref="RangeImpl{T}(T,T)"/>
		public static Range<short> RangeToExcluding(this short start, short endExclusive) => MutableRangeToExcluding(start, endExclusive);

		/// <inheritdoc cref="RangeImpl{T}(T,T)"/>
		public static Range<uint> RangeToExcluding(this uint start, uint endExclusive) => MutableRangeToExcluding(start, endExclusive);

		/// <inheritdoc cref="RangeImpl{T}(T,T)"/>
		public static Range<ulong> RangeToExcluding(this ulong start, ulong endExclusive) => MutableRangeToExcluding(start, endExclusive);

		/// <inheritdoc cref="RangeImpl{T}(T,T)"/>
		public static Range<ushort> RangeToExcluding(this ushort start, ushort endExclusive) => MutableRangeToExcluding(start, endExclusive);

		/// <returns>true iff <paramref name="value"/> is strictly contained in <paramref name="range"/> (<paramref name="value"/> is considered to be OUTSIDE the range if it's exactly equal to either bound)</returns>
		/// <seealso cref="Contains"/>
		public static bool StrictlyBoundedBy<T>(this T value, Range<T> range) where T : unmanaged, IComparable<T> => range.Start.CompareTo(value) < 0 && value.CompareTo(range.EndInclusive) < 0;
	}
}