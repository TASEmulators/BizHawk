using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;

namespace BizHawk.Tests.Common.MultiPredicateSort
{
	[TestClass]
	public class MultiPredicateSortTests
	{
		private static readonly (int X, string Y)[] Unsorted = { (1, "b"), (2, "a"), (2, "b"), (1, "a") };

		private static readonly (int X, string Y)[] SortedByXThenYDesc = { (1, "b"), (1, "a"), (2, "b"), (2, "a") };

		private static readonly (int X, string Y)[] SortedByYDescThenXDesc = { (2, "b"), (1, "b"), (2, "a"), (1, "a") };

		[TestMethod]
		public void SanityCheck()
		{
			CollectionAssert.AreEqual(
				SortedByYDescThenXDesc,
				Unsorted.OrderByDescending(static t => t.Y).ThenByDescending(static t => t.X).ToArray(),
				".OrderByDesc(Y).ThenByDesc(X)");
#pragma warning disable MA0030
			CollectionAssert.AreEqual(
				SortedByYDescThenXDesc,
				Unsorted.OrderByDescending(static t => t.X).OrderByDescending(static t => t.Y).ToArray(),
				".OrderByDesc(X).OrderByDesc(Y)");
#pragma warning restore MA0030
		}

		[TestMethod]
		public void TestRigidSort()
		{
			var sorts = new RigidMultiPredicateSort<(int X, string Y)>(new Dictionary<string, Func<(int X, string Y), IComparable>>
			{
				["by_x"] = t => t.X,
				["by_y"] = t => t.Y,
			});
			CollectionAssert.AreEqual(
				SortedByXThenYDesc,
				sorts.AppliedTo(
					Unsorted,
					"by_x",
					new Dictionary<string, bool> { ["by_x"] = false, ["by_y"] = true }),
				"by X asc., then by Y desc.");
			CollectionAssert.AreEqual(
				SortedByYDescThenXDesc,
				sorts.AppliedTo(
					Unsorted,
					"by_y",
					new Dictionary<string, bool> { ["by_x"] = true, ["by_y"] = true }),
				"by Y desc., then by X desc.");
		}
	}
}
