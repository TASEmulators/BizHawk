using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Tests
{
	public static class TestAssertions
	{
		public static void AreEqual<T>(
			this CollectionAssert assert,
			ICollection<T> expected,
			ICollection<T> actual,
			string? message = null)
		{
			if (actual.SequenceEqual(expected)) return;
			Console.WriteLine($"ex{PrettyPrint(expected)}\nac{PrettyPrint(actual)}");
			Assert.Fail(message);
		}

		public static void AreEqual<T>(
			this CollectionAssert assert,
			IReadOnlyCollection<T> expected,
			IReadOnlyCollection<T> actual,
			string? message = null)
		{
			if (actual.SequenceEqual(expected)) return;
			Console.WriteLine($"ex{PrettyPrint(expected)}\nac{PrettyPrint(actual)}");
			Assert.Fail(message);
		}

		public static void AreEqual<T>(
			this CollectionAssert assert,
			ReadOnlySpan<T> expected,
			ReadOnlySpan<T> actual,
			string? message = null)
				where T : IEquatable<T>
		{
			if (actual.SequenceEqual(expected)) return;
			Console.WriteLine($"ex{PrettyPrint((ICollection<T>) expected.ToArray())}\nac{PrettyPrint((ICollection<T>) actual.ToArray())}");
			Assert.Fail(message);
		}

		public static void Contains<T>(
			this CollectionAssert assert,
			IEnumerable<T> collection,
			T element,
			string? message = null)
				=> Assert.IsTrue(collection.Contains(element), message);

		public static void ContainsKey<TKey, TValue>(
			this CollectionAssert assert,
			IReadOnlyDictionary<TKey, TValue> dict,
			TKey key,
			string? message = null)
				=> Assert.IsTrue(dict.ContainsKey(key), message);

		public static void IsEmpty<T>(
			this CollectionAssert assert,
			IReadOnlyCollection<T> collection,
			string? message = null)
#pragma warning disable MSTEST0037 // intentionally not using `Assert.AreEqual` here as the "ex: 0, ac: 4" message might be confusing
				=> Assert.IsTrue(collection.Count is 0, message);
#pragma warning restore MSTEST0037

		/// <remarks>dumb param order matches predefined method</remarks>
		public static void IsSubsetOf<T>(
			this CollectionAssert assert,
			ICollection<T> subset,
			ICollection<T> superset,
			string? message = null)
				=> Assert.IsTrue(subset.All(superset.Contains), message);

		private static string PrettyPrint<T>(ICollection<T> collection)
			=> $"[{collection.Count}]: {string.Join(", ", collection.Select(static o => o?.ToString() ?? "null"))}";

		private static string PrettyPrint<T>(IReadOnlyCollection<T> collection)
			=> $"[{collection.Count}]: {string.Join(", ", collection.Select(static o => o?.ToString() ?? "null"))}";
	}
}
