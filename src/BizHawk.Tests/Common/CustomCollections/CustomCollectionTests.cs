using System.Linq;

using BizHawk.Common;

namespace BizHawk.Tests.Common.CustomCollections
{
	[TestClass]
	public class CustomCollectionTests
	{
		[TestMethod]
		public void TestSortedListAddRemove()
		{
			var list = new SortedList<int>(new[] { 1, 3, 4, 7, 8, 9, 11 }); // this causes one sort, collection initializer syntax wouldn't
			list.Add(5); // `Insert` when `BinarySearch` returns negative
			list.Add(8); // `Insert` when `BinarySearch` returns non-negative
			list.Remove(3); // `Remove` when `BinarySearch` returns non-negative
			Assert.IsTrue(list.ToArray().SequenceEqual(new[] { 1, 4, 5, 7, 8, 8, 9, 11 }));
			Assert.IsFalse(list.Remove(10)); // `Remove` when `BinarySearch` returns negative
		}

		[TestMethod]
		public void TestSortedListContains()
		{
			var list = new SortedList<int>(new[] { 1, 3, 4, 7, 8, 9, 11 });
			Assert.IsFalse(list.Contains(6)); // `Contains` when `BinarySearch` returns negative
			Assert.IsTrue(list.Contains(11)); // `Contains` when `BinarySearch` returns non-negative
		}


		[TestMethod]
		[DataRow(new[] {1, 5, 9, 10, 11, 12}, new[] {1, 5, 9}, 9)]
		[DataRow(new[] { 2, 3 }, new[] { 2, 3 }, 5)]
		[DataRow(new[] { 4, 7 }, new int[] { }, 0)]
		public void TestSortedListRemoveAfter(int[] before, int[] after, int removeItem)
		{
			var sortlist = new SortedList<int>(before);
			sortlist.RemoveAfter(removeItem);
			Assert.IsTrue(sortlist.ToArray().SequenceEqual(after));
		}
	}
}
