using BizHawk.Common;

namespace BizHawk.Tests.Common;

[TestClass]
public class IntervalTests
{
	private Int32HalfOpenRange S32
		=> (-3).RangeTo(4);

	private Int64HalfOpenRange S64
		=> (-3L).RangeTo(4L);

	[TestMethod]
	public void TestConstrainWithin()
	{
		Assert.AreEqual(-3, (-4).ConstrainWithin(S32), $"-4.{nameof(RangeExtensions.ConstrainWithin)}({S32})");
		Assert.AreEqual(-3, (-3).ConstrainWithin(S32), $"-3.{nameof(RangeExtensions.ConstrainWithin)}({S32})");
		Assert.AreEqual(-2, (-2).ConstrainWithin(S32), $"-2.{nameof(RangeExtensions.ConstrainWithin)}({S32})");
		Assert.AreEqual(3, 3.ConstrainWithin(S32), $"3.{nameof(RangeExtensions.ConstrainWithin)}({S32})");
		Assert.AreEqual(4, 4.ConstrainWithin(S32), $"4.{nameof(RangeExtensions.ConstrainWithin)}({S32})");
		Assert.AreEqual(4, 5.ConstrainWithin(S32), $"5.{nameof(RangeExtensions.ConstrainWithin)}({S32})");

		Assert.AreEqual(-3L, (-4L).ConstrainWithin(S64), $"-4.{nameof(RangeExtensions.ConstrainWithin)}({S64})");
		Assert.AreEqual(-3L, (-3L).ConstrainWithin(S64), $"-3.{nameof(RangeExtensions.ConstrainWithin)}({S64})");
		Assert.AreEqual(-2L, (-2L).ConstrainWithin(S64), $"-2.{nameof(RangeExtensions.ConstrainWithin)}({S64})");
		Assert.AreEqual(3L, 3L.ConstrainWithin(S64), $"3.{nameof(RangeExtensions.ConstrainWithin)}({S64})");
		Assert.AreEqual(4L, 4L.ConstrainWithin(S64), $"4.{nameof(RangeExtensions.ConstrainWithin)}({S64})");
		Assert.AreEqual(4L, 5L.ConstrainWithin(S64), $"5.{nameof(RangeExtensions.ConstrainWithin)}({S64})");
	}

	[TestMethod]
	public void TestContains()
	{
		Assert.IsFalse(S32.Contains(-4), $"({S32}).{nameof(Int32HalfOpenRange.Contains)}(-4)");
		Assert.IsTrue(S32.Contains(-3), $"({S32}).{nameof(Int32HalfOpenRange.Contains)}(-3)");
		Assert.IsTrue(S32.Contains(-2), $"({S32}).{nameof(Int32HalfOpenRange.Contains)}(-2)");
		Assert.IsTrue(S32.Contains(3), $"({S32}).{nameof(Int32HalfOpenRange.Contains)}(3)");
		Assert.IsTrue(S32.Contains(4), $"({S32}).{nameof(Int32HalfOpenRange.Contains)}(4)");
		Assert.IsFalse(S32.Contains(5), $"({S32}).{nameof(Int32HalfOpenRange.Contains)}(5)");

		var i = -4;
		Assert.IsFalse(S32.Contains(in i), $"({S32}).{nameof(Int32HalfOpenRange.Contains)}(-4)");
		i = 4;
		Assert.IsTrue(S32.Contains(in i), $"({S32}).{nameof(Int32HalfOpenRange.Contains)}(4)");

		Assert.IsFalse(S64.Contains(-4L), $"({S64}).{nameof(Int64HalfOpenRange.Contains)}(-4L)");
		Assert.IsTrue(S64.Contains(-3L), $"({S64}).{nameof(Int64HalfOpenRange.Contains)}(-3L)");
		Assert.IsTrue(S64.Contains(-2L), $"({S64}).{nameof(Int64HalfOpenRange.Contains)}(-2L)");
		Assert.IsTrue(S64.Contains(3L), $"({S64}).{nameof(Int64HalfOpenRange.Contains)}(3L)");
		Assert.IsTrue(S64.Contains(4L), $"({S64}).{nameof(Int64HalfOpenRange.Contains)}(4L)");
		Assert.IsFalse(S64.Contains(5L), $"({S64}).{nameof(Int64HalfOpenRange.Contains)}(5L)");

		var l = -4L;
		Assert.IsFalse(S64.Contains(in l), $"({S64}).{nameof(Int32HalfOpenRange.Contains)}(-4L)");
		l = 4L;
		Assert.IsTrue(S64.Contains(in l), $"({S64}).{nameof(Int32HalfOpenRange.Contains)}(4L)");
	}

	[TestMethod]
	public void TestCreationAroundZero()
	{
		for (int start = -3, endExclusive = 0; start <= 1;)
		{
			var interval = start.RangeToExclusive(endExclusive);
			Assert.AreEqual(start, interval.Start, $"start of {interval} survives round trip");
			Assert.AreEqual(endExclusive, interval.EndExclusive, $"end of {interval} survives round trip");
			Assert.AreEqual(3U, interval.Count(), $"{interval} has 3 elems");
			start++;
			endExclusive++;
		}
		for (long start = -3L, endExclusive = 0L; start <= 1L;)
		{
			var interval = start.RangeToExclusive(endExclusive);
			Assert.AreEqual(start, interval.Start, $"start of {interval} survives round trip");
			Assert.AreEqual(endExclusive, interval.EndExclusive, $"end of {interval} survives round trip");
			Assert.AreEqual(3UL, interval.Count(), $"{interval} has 3 elems");
			start++;
			endExclusive++;
		}
	}

	[TestMethod]
	public void TestCreationFailsIfEmpty()
	{
		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = 1.RangeTo(-2), $"s32, {nameof(RangeExtensions.RangeTo)}");
		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = 1.RangeToExclusive(-1), $"s32, {nameof(RangeExtensions.RangeToExclusive)}");
		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new Int32HalfOpenRange(start: 1, endExclusive: -1), "s32, ctor");

		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = 1L.RangeTo(-2L), $"s64, {nameof(RangeExtensions.RangeTo)}");
		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = 1L.RangeToExclusive(-1L), $"s64, {nameof(RangeExtensions.RangeToExclusive)}");
		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new Int64HalfOpenRange(start: 1L, endExclusive: -1L), "s64, ctor");
	}

	[TestMethod]
	public void TestCreationFullRange()
	{
		_ = Assert.ThrowsExactly<OverflowException>(() => _ = int.MinValue.RangeTo(int.MaxValue), "every s32");
		/*scope*/{
			var interval = int.MinValue.RangeTo(int.MaxValue - 1);
			Assert.IsTrue(interval.Contains(0), $"almost every s32 {nameof(Int32HalfOpenRange.Contains)} 0");
			Assert.AreEqual(uint.MaxValue, interval.Count(), $"almost every s32 {nameof(Int32HalfOpenRange.Count)} has {nameof(uint.MaxValue)} elems");
		}
		_ = Assert.ThrowsExactly<OverflowException>(() => _ = long.MinValue.RangeTo(long.MaxValue), "every s64");
		/*scope*/{
			var interval = long.MinValue.RangeTo(long.MaxValue - 1L);
			Assert.IsTrue(interval.Contains(0L), $"almost every s64 {nameof(Int64HalfOpenRange.Contains)} 0");
			Assert.AreEqual(ulong.MaxValue, interval.Count(), $"almost every s64 {nameof(Int64HalfOpenRange.Count)} has {nameof(ulong.MaxValue)} elems");
		}
	}

	[TestMethod]
	public void TestCreationNormal()
	{
		/*scope*/{
			var interval = new Int32HalfOpenRange(start: -3, endExclusive: 5);
			Assert.AreEqual(interval, (-3).RangeTo(4), $"s32, {nameof(RangeExtensions.RangeTo)}");
			Assert.AreEqual(interval, (-3).RangeToExclusive(5), $"s32, {nameof(RangeExtensions.RangeToExclusive)}");
		}
		/*scope*/{
			var interval = new Int64HalfOpenRange(start: -3L, endExclusive: 5L);
			Assert.AreEqual(interval, (-3L).RangeTo(4L), $"s64, {nameof(RangeExtensions.RangeTo)}");
			Assert.AreEqual(interval, (-3L).RangeToExclusive(5L), $"s64, {nameof(RangeExtensions.RangeToExclusive)}");
		}
	}

	[TestMethod]
	public void TestCreationSingleElem()
	{
		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = int.MinValue.RangeToExclusive(int.MinValue), $"s32 {nameof(int.MinValue)}.{nameof(RangeExtensions.RangeToExclusive)}({nameof(int.MinValue)})");
		foreach (var (start, endExclusive) in new[]
		{
			(int.MinValue, int.MinValue + 1),
			(int.MinValue + 1, int.MinValue + 2),
			(-1, 0),
			(0, 1),
			(1, 2),
			(int.MaxValue - 2, int.MaxValue - 1),
			(int.MaxValue - 1, int.MaxValue),
		})
		{
			var interval = start.RangeTo(start);
			Assert.AreEqual(1U, interval.Count(), $"{nameof(RangeExtensions.RangeTo)}, {interval}");
			interval = start.RangeToExclusive(endExclusive);
			Assert.AreEqual(1U, interval.Count(), $"{nameof(RangeExtensions.RangeToExclusive)}, {interval}");
			interval = new Int32HalfOpenRange(start: start, endExclusive: endExclusive);
			Assert.AreEqual(1U, interval.Count(), $"ctor, {interval}");
		}
		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = long.MinValue.RangeToExclusive(long.MinValue), $"s64 {nameof(long.MinValue)}.{nameof(RangeExtensions.RangeToExclusive)}({nameof(long.MinValue)})");
		foreach (var (start, endExclusive) in new[]
		{
			(long.MinValue, long.MinValue + 1L),
			(long.MinValue + 1L, long.MinValue + 2L),
			(-1L, 0L),
			(0L, 1L),
			(1L, 2L),
			(long.MaxValue - 2L, long.MaxValue - 1L),
			(long.MaxValue - 1L, long.MaxValue),
		})
		{
			var interval = start.RangeTo(start);
			Assert.AreEqual(1UL, interval.Count(), $"{nameof(RangeExtensions.RangeTo)}, {interval}");
			interval = start.RangeToExclusive(endExclusive);
			Assert.AreEqual(1UL, interval.Count(), $"{nameof(RangeExtensions.RangeToExclusive)}, {interval}");
			interval = new Int64HalfOpenRange(start: start, endExclusive: endExclusive);
			Assert.AreEqual(1UL, interval.Count(), $"ctor, {interval}");
		}
	}
}
