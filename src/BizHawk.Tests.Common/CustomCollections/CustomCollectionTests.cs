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
			CollectionAssert.AreEqual(new[] { 1, 4, 5, 7, 8, 8, 9, 11 }, list);
			Assert.IsFalse(list.Remove(10)); // `Remove` when `BinarySearch` returns negative
		}

		[TestMethod]
		public void TestSortedListContains()
		{
			var list = new SortedList<int>(new[] { 1, 3, 4, 7, 8, 9, 11 });
			CollectionAssert.DoesNotContain(list, 6, "`Contains` when `BinarySearch` returns negative");
			CollectionAssert.Contains(list, 11, "`Contains` when `BinarySearch` returns non-negative");
		}

		[TestMethod]
		public void TestSortedListInsert()
		{
			SortedList<int> list = new([ 1, 4, 7 ]);
			_ = Assert.ThrowsExactly<NotSupportedException>(
				() => list.Insert(index: 3, item: 0),
				"setting [^0] (appending) out-of-order should throw");
			list.Insert(index: 3, item: 10);
			CollectionAssert.AreEqual(new[] { 1, 4, 7, 10 }, list, "expecting [ 1, 4, 7, 10 ]");
			_ = Assert.ThrowsExactly<NotSupportedException>(
				() => list.Insert(index: 3, item: 0),
				"setting [^1] out-of-order should throw");
			list.Insert(index: 3, item: 9);
			CollectionAssert.AreEqual(new[] { 1, 4, 7, 9, 10 }, list, "expecting [ 1, 4, 7, 9, 10 ]");
			_ = Assert.ThrowsExactly<NotSupportedException>(
				() => list.Insert(index: 1, item: 9),
				"setting [1] out-of-order should throw");
			list.Insert(index: 1, item: 3);
			CollectionAssert.AreEqual(new[] { 1, 3, 4, 7, 9, 10 }, list, "expecting [ 1, 3, 4, 7, 9, 10 ]");
			_ = Assert.ThrowsExactly<NotSupportedException>(
				() => list.Insert(index: 0, item: 9),
				"setting [0] out-of-order should throw");
			list.Insert(index: 0, item: 0);
			CollectionAssert.AreEqual(new[] { 0, 1, 3, 4, 7, 9, 10 }, list, "expecting [ 0, 1, 3, 4, 7, 9, 10 ]");
		}

		[TestMethod]
		[DataRow(new[] {1, 5, 9, 10, 11, 12}, new[] {1, 5, 9}, 9)]
		[DataRow(new[] { 2, 3 }, new[] { 2, 3 }, 5)]
		[DataRow(new[] { 4, 7 }, new int[] { }, 0)]
		public void TestSortedListRemoveAfter<T>(T[] before, T[] after, T removeItem)
			where T : IComparable<T>
		{
			SortedList<T> sortlist = new(before);
			sortlist.RemoveAfter(removeItem);
			CollectionAssert.AreEqual(after, sortlist);
		}

		[TestMethod]
		public void TestSortedListSetIndexer()
		{
			SortedList<int> list = new([ 1, 3, 4 ]);
			_ = Assert.ThrowsExactly<NotSupportedException>(
				() => list[1] = 9,
				"setting [1] out-of-order should throw");
			list[1] = 2;
			CollectionAssert.AreEqual(new[] { 1, 2, 4 }, list, "expecting [ 1, 2, 4 ]");
			_ = Assert.ThrowsExactly<NotSupportedException>(
				() => list[0] = 9,
				"setting [0] out-of-order should throw");
			list[0] = 0;
			_ = Assert.ThrowsExactly<NotSupportedException>(
				() => list[2] = 0,
				"setting [^1] out-of-order should throw");
			list[2] = 9;
			_ = Assert.Throws</*NotSupportedException*/ArgumentException>(
				() => list[3] = 0,
				"setting [^0] (appending) out-of-order should throw");
			_ = Assert.Throws<ArgumentException>(
				() => list[3] = 10,
				"setting [^0] (appending) properly should throw"); // to match BCL `List<T>`
			CollectionAssert.AreEqual(new[] { 0, 2, 9 }, list, "expecting [ 0, 2, 9 ]");
		}
	}
}
