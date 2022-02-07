using System;
using System.Collections;
using System.Collections.Generic;

using BizHawk.Common.CollectionExtensions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

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
