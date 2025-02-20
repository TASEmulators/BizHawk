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
		public void TestSortedListInsert()
		{
			SortedList<int> list = new([ 1, 4, 7 ]);
			Assert.ThrowsException<NotSupportedException>(() => list.Insert(index: 3, item: 0), "setting [^0] (appending) out-of-order should throw");
			list.Insert(index: 3, item: 10);
			Assert.IsTrue(list.SequenceEqual([ 1, 4, 7, 10 ]), "expecting [ 1, 4, 7, 10 ]");
			Assert.ThrowsException<NotSupportedException>(() => list.Insert(index: 3, item: 0), "setting [^1] out-of-order should throw");
			list.Insert(index: 3, item: 9);
			Assert.IsTrue(list.SequenceEqual([ 1, 4, 7, 9, 10 ]), "expecting [ 1, 4, 7, 9, 10 ]");
			Assert.ThrowsException<NotSupportedException>(() => list.Insert(index: 1, item: 9), "setting [1] out-of-order should throw");
			list.Insert(index: 1, item: 3);
			Assert.IsTrue(list.SequenceEqual([ 1, 3, 4, 7, 9, 10 ]), "expecting [ 1, 3, 4, 7, 9, 10 ]");
			Assert.ThrowsException<NotSupportedException>(() => list.Insert(index: 0, item: 9), "setting [0] out-of-order should throw");
			list.Insert(index: 0, item: 0);
			Assert.IsTrue(list.SequenceEqual([ 0, 1, 3, 4, 7, 9, 10 ]), "expecting [ 0, 1, 3, 4, 7, 9, 10 ]");
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

		[TestMethod]
		public void TestSortedListSetIndexer()
		{
			SortedList<int> list = new([ 1, 3, 4 ]);
			Assert.ThrowsException<NotSupportedException>(() => list[1] = 9, "setting [1] out-of-order should throw");
			list[1] = 2;
			Assert.IsTrue(list.SequenceEqual([ 1, 2, 4 ]), "expecting [ 1, 2, 4 ]");
			Assert.ThrowsException<NotSupportedException>(() => list[0] = 9, "setting [0] out-of-order should throw");
			list[0] = 0;
			Assert.ThrowsException<NotSupportedException>(() => list[2] = 0, "setting [^1] out-of-order should throw");
			list[2] = 9;
			Assert.ThrowsException</*NotSupportedException*/ArgumentOutOfRangeException>(() => list[3] = 0, "setting [^0] (appending) out-of-order should throw");
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => list[3] = 10, "setting [^0] (appending) properly should throw"); // to match BCL `List<T>`
			Assert.IsTrue(list.SequenceEqual([ 0, 2, 9 ]), "expecting [ 0, 2, 9 ]");
		}
	}
}
