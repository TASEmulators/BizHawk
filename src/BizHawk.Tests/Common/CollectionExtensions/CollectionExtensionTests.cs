using System.Collections;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common.CollectionExtensions;

using CE = BizHawk.Common.CollectionExtensions.CollectionExtensions;

namespace BizHawk.Tests.Common.CollectionExtensions
{
	[TestClass]
	public class CollectionExtensionTests
	{
		/// <summary>there isn't really an <see cref="IList{T}"/> implementor which isn't <see cref="List{T}"/> and isn't immutable... so I made one</summary>
		private readonly struct ListImpl : IList<int>
		{
			private readonly IList<int> _wrapped;

			public int Count => _wrapped.Count;

			public bool IsReadOnly => false;

			public ListImpl(params int[] init) => _wrapped = new List<int>(init);

			public readonly int this[int index]
			{
				get => _wrapped[index];
				set => throw new NotImplementedException();
			}

			public readonly void Add(int item) => throw new NotImplementedException();

			public readonly void Clear() => throw new NotImplementedException();

			public readonly bool Contains(int item) => throw new NotImplementedException();

			public readonly void CopyTo(int[] array, int arrayIndex) => throw new NotImplementedException();

			public readonly IEnumerator<int> GetEnumerator() => throw new NotImplementedException();

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			public readonly int IndexOf(int item) => throw new NotImplementedException();

			public readonly void Insert(int index, int item) => throw new NotImplementedException();

			public readonly bool Remove(int item) => throw new NotImplementedException();

			public readonly void RemoveAt(int index) => _wrapped.RemoveAt(index);
		}

		[TestMethod]
		public void TestAddRange()
		{
			List<int> a = new(new[] { 1, 2 });
			CE.AddRange(a, new[] { 3, 4 });
			Assert.AreEqual(4, a.Count, nameof(CE.AddRange) + " failed on List<int>");

			SortedSet<int> b = new(new[] { 1, 2 });
			b.AddRange(new[] { 3, 4 });
			Assert.AreEqual(4, b.Count, nameof(CE.AddRange) + " failed on (ICollection<int> not List<int>)");
		}

		[TestMethod]
		public void TestConcatArray()
		{
			var a123 = new[] { 1, 2, 3 };
			var a456 = new[] { 4, 5, 6 };
			Assert.IsTrue(a123.ConcatArray(a456).SequenceEqual(new[] { 1, 2, 3, 4, 5, 6 }));
			Assert.AreSame(a123, a123.ConcatArray(Array.Empty<int>()));
			Assert.AreSame(a456, Array.Empty<int>().ConcatArray(a456));
			Assert.AreEqual(0, Array.Empty<int>().ConcatArray(Array.Empty<int>()).Length);
		}

		[TestMethod]
		public void TestLowerBoundBinarySearch()
		{
			List<string> a = new(new[] { "a", "abc", "abcde", "abcdef", "abcdefg" });
			Assert.AreEqual(-1, a.LowerBoundBinarySearch(static s => s.Length, 0), "length 0");
			Assert.AreEqual(0, a.LowerBoundBinarySearch(static s => s.Length, 1), "length 1");
			Assert.AreEqual(1, a.LowerBoundBinarySearch(static s => s.Length, 4), "length 4");
			Assert.AreEqual(2, a.LowerBoundBinarySearch(static s => s.Length, 5), "length 5");
			Assert.AreEqual(4, a.LowerBoundBinarySearch(static s => s.Length, 7), "length 7");
			Assert.AreEqual(4, a.LowerBoundBinarySearch(static s => s.Length, 8), "length 8");

			List<int> emptyList = new List<int>();
			Assert.AreEqual(-1, emptyList.LowerBoundBinarySearch(static i => i, 13));
		}

		[TestMethod]
		public void TestBinarySearchExtension()
		{
			List<string> testList = new(new[] { "a", "abc", "abcdef" });
			Assert.ThrowsException<InvalidOperationException>(() => testList.BinarySearch(static s => s.Length, 4));
			Assert.ThrowsException<InvalidOperationException>(() => testList.BinarySearch(static s => s.Length, 7));
			Assert.AreEqual("abc", testList.BinarySearch(static s => s.Length, 3));

			List<int> emptyList = new List<int>();
			Assert.ThrowsException<InvalidOperationException>(() => emptyList.BinarySearch(i => i, 15));
		}

		[TestMethod]
		public void TestRemoveAll()
		{
			static bool Predicate(int i) => 2 <= i && i <= 3;

			List<int> a = new(new[] { 1, 2, 3, 4 });
			CE.RemoveAll(a, Predicate);
			Assert.AreEqual(2, a.Count, nameof(CE.RemoveAll) + " failed on List<int>");

			ListImpl b = new(1, 2, 3, 4);
			b.RemoveAll(Predicate);
			Assert.AreEqual(2, b.Count, nameof(CE.RemoveAll) + " failed on (IList<int> not List<int>)");

			SortedSet<int> c = new(new[] { 1, 2, 3, 4 });
			c.RemoveAll(Predicate);
			Assert.AreEqual(2, c.Count, nameof(CE.RemoveAll) + " failed on (ICollection<int> not IList<int>)");
		}
	}
}
