using BizHawk.Common;

namespace BizHawk.Tests.Common;

[TestClass]
public class IntervalTests
{
	private Int32ClosedRange S32
		=> (-3).RangeTo(4);

	private Int64ClosedRange S64
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
		Assert.IsFalse(S32.Contains(-4), $"({S32}).{nameof(Int32ClosedRange.Contains)}(-4)");
		Assert.IsTrue(S32.Contains(-3), $"({S32}).{nameof(Int32ClosedRange.Contains)}(-3)");
		Assert.IsTrue(S32.Contains(-2), $"({S32}).{nameof(Int32ClosedRange.Contains)}(-2)");
		Assert.IsTrue(S32.Contains(3), $"({S32}).{nameof(Int32ClosedRange.Contains)}(3)");
		Assert.IsTrue(S32.Contains(4), $"({S32}).{nameof(Int32ClosedRange.Contains)}(4)");
		Assert.IsFalse(S32.Contains(5), $"({S32}).{nameof(Int32ClosedRange.Contains)}(5)");

		var i = -4;
		Assert.IsFalse(S32.Contains(in i), $"({S32}).{nameof(Int32ClosedRange.Contains)}(-4)");
		i = 4;
		Assert.IsTrue(S32.Contains(in i), $"({S32}).{nameof(Int32ClosedRange.Contains)}(4)");

		Assert.IsFalse(S64.Contains(-4L), $"({S64}).{nameof(Int64ClosedRange.Contains)}(-4L)");
		Assert.IsTrue(S64.Contains(-3L), $"({S64}).{nameof(Int64ClosedRange.Contains)}(-3L)");
		Assert.IsTrue(S64.Contains(-2L), $"({S64}).{nameof(Int64ClosedRange.Contains)}(-2L)");
		Assert.IsTrue(S64.Contains(3L), $"({S64}).{nameof(Int64ClosedRange.Contains)}(3L)");
		Assert.IsTrue(S64.Contains(4L), $"({S64}).{nameof(Int64ClosedRange.Contains)}(4L)");
		Assert.IsFalse(S64.Contains(5L), $"({S64}).{nameof(Int64ClosedRange.Contains)}(5L)");

		var l = -4L;
		Assert.IsFalse(S64.Contains(in l), $"({S64}).{nameof(Int64ClosedRange.Contains)}(-4L)");
		l = 4L;
		Assert.IsTrue(S64.Contains(in l), $"({S64}).{nameof(Int64ClosedRange.Contains)}(4L)");
	}

	[TestMethod]
	public void TestCreationAroundZero()
	{
		for (int start = -3, endInclusive = -1; start <= 1;)
		{
			var interval = start.RangeTo(endInclusive);
			Assert.AreEqual(start, interval.Start, $"start of {interval} survives round trip");
			Assert.AreEqual(endInclusive, interval.EndInclusive, $"end of {interval} survives round trip");
			Assert.AreEqual(3U, interval.Count(), $"{interval} has 3 elems");
			start++;
			endInclusive++;
		}
		for (long start = -3L, endInclusive = -1L; start <= 1L;)
		{
			var interval = start.RangeTo(endInclusive);
			Assert.AreEqual(start, interval.Start, $"start of {interval} survives round trip");
			Assert.AreEqual(endInclusive, interval.EndInclusive, $"end of {interval} survives round trip");
			Assert.AreEqual(3UL, interval.Count(), $"{interval} has 3 elems");
			start++;
			endInclusive++;
		}
	}

	[TestMethod]
	public void TestCreationFailsIfEmpty()
	{
		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = 1.RangeTo(-2), $"s32, {nameof(RangeExtensions.RangeTo)}");
		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = 1.RangeToExclusive(-1), $"s32, {nameof(RangeExtensions.RangeToExclusive)}");
		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new Int32ClosedRange(start: 1, endInclusive: -2), "s32, ctor");

		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = 1L.RangeTo(-2L), $"s64, {nameof(RangeExtensions.RangeTo)}");
		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = 1L.RangeToExclusive(-1L), $"s64, {nameof(RangeExtensions.RangeToExclusive)}");
		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new Int64ClosedRange(start: 1L, endInclusive: -2L), "s64, ctor");
	}

	[TestMethod]
	public void TestCreationFullRange()
	{
		/*scope*/{
			var interval = int.MinValue.RangeTo(int.MaxValue);
			Assert.IsTrue(interval.Contains(0), $"every s32 {nameof(Int32ClosedRange.Contains)} 0");
			Assert.AreEqual(0U, interval.Count(), $"every s32 {nameof(Int32ClosedRange.Count)} overflows");
		}
		/*scope*/{
			var interval = long.MinValue.RangeTo(long.MaxValue);
			Assert.IsTrue(interval.Contains(0L), $"every s64 {nameof(Int64ClosedRange.Contains)} 0");
			Assert.AreEqual(0UL, interval.Count(), $"every s64 {nameof(Int64ClosedRange.Count)} overflows");
		}
	}

	[TestMethod]
	public void TestCreationNormal()
	{
		/*scope*/{
			var interval = new Int32ClosedRange(start: -3, endInclusive: 4);
			Assert.AreEqual(interval, (-3).RangeTo(4), $"s32, {nameof(RangeExtensions.RangeTo)}");
			Assert.AreEqual(interval, (-3).RangeToExclusive(5), $"s32, {nameof(RangeExtensions.RangeToExclusive)}");
		}
		/*scope*/{
			var interval = new Int64ClosedRange(start: -3L, endInclusive: 4L);
			Assert.AreEqual(interval, (-3L).RangeTo(4L), $"s64, {nameof(RangeExtensions.RangeTo)}");
			Assert.AreEqual(interval, (-3L).RangeToExclusive(5L), $"s64, {nameof(RangeExtensions.RangeToExclusive)}");
		}
	}

	[TestMethod]
	public void TestCreationSingleElem()
	{
		_ = Assert.ThrowsExactly<ArithmeticException>(() => _ = int.MinValue.RangeToExclusive(int.MinValue), $"s32 {nameof(int.MinValue)}.{nameof(RangeExtensions.RangeToExclusive)}({nameof(int.MinValue)})");
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
			interval = new Int32ClosedRange(start: start, endInclusive: start);
			Assert.AreEqual(1U, interval.Count(), $"ctor, {interval}");
		}
		_ = Assert.ThrowsExactly<ArithmeticException>(() => _ = long.MinValue.RangeToExclusive(long.MinValue), $"s64 {nameof(long.MinValue)}.{nameof(RangeExtensions.RangeToExclusive)}({nameof(long.MinValue)})");
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
			interval = new Int64ClosedRange(start: start, endInclusive: start);
			Assert.AreEqual(1UL, interval.Count(), $"ctor, {interval}");
		}
	}
}
