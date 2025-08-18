using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BizHawk.Common.CollectionExtensions
{
#pragma warning disable MA0104 // unlikely to conflict with System.Collections.Generic.CollectionExtensions
	public static class CollectionExtensions
#pragma warning restore MA0104
	{
		private struct EnumeratorAsEnumerable<T> : IEnumerable<T>
		{
			private IEnumerator<T>? _wrapped;

			public EnumeratorAsEnumerable(IEnumerator<T> wrapped)
				=> _wrapped = wrapped;

			public override bool Equals(object? other)
				=> other is EnumeratorAsEnumerable<T> wrapper && object.Equals(_wrapped, wrapper._wrapped);

			public IEnumerator<T> GetEnumerator()
			{
				var temp = _wrapped ?? throw new InvalidOperationException("double enumeration (or `default`/zeroed struct)");
				_wrapped = null;
				return temp;
			}

			IEnumerator IEnumerable.GetEnumerator()
				=> GetEnumerator();

			public override int GetHashCode()
				=> _wrapped?.GetHashCode() ?? default;
		}

		private const string ERR_MSG_IMMUTABLE_LIST = "immutable list passed to mutating method";

		private const string WARN_NONGENERIC = "use generic overload";

		public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			bool desc)
		{
			return desc ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);
		}

		/// <summary>Implements an indirected binary search.</summary>
		/// <return>
		/// The index of the element whose key matches <paramref name="key"/>;
		/// or if none match, the index of the element whose key is closest and lower;
		/// or if all elements' keys are higher, <c>-1</c>.<br/>
		/// (Equivalently: If none match, 1 less than the index where inserting an element with the given <paramref name="key"/> would keep the list sorted)
		/// </return>
		/// <remarks>The returned index may not be accurate if <paramref name="list"/> is not sorted in ascending order with respect to <paramref name="keySelector"/>.</remarks>
		public static int LowerBoundBinarySearch<T, TKey>(this IList<T> list, Func<T, TKey> keySelector, TKey key)
			where TKey : IComparable<TKey>
		{
			if (list.Count is 0) return -1;

			int min = 0;
			int max = list.Count - 1;
			while (min < max)
			{
				int mid = (max + min) / 2;
				T midItem = list[mid];
				var midKey = keySelector(midItem);
				int comp = midKey.CompareTo(key);
				if (comp < 0)
				{
					min = mid + 1;
				}
				else if (comp > 0)
				{
					max = mid - 1;
				}
				else
				{
					return mid;
				}
			}

			int compareResult = keySelector(list[min]).CompareTo(key);

			// return something corresponding to lower_bound semantics
			// if min is higher than key, return min - 1. Otherwise, when min is <=key, return min directly.
			if (compareResult > 0)
			{
				return min - 1;
			}

			return min;
		}

		/// <remarks>for collection initializer syntax</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Add<T>(this Queue<T> q, T item)
			=> q.Enqueue(item);

		/// <exception cref="InvalidOperationException"><paramref name="key"/> not found after mapping <paramref name="keySelector"/> over <paramref name="list"/></exception>
		/// <remarks>implementation from https://stackoverflow.com/a/1766369/7467292</remarks>
		public static T BinarySearch<T, TKey>(this IList<T> list, Func<T, TKey> keySelector, TKey key)
			where TKey : IComparable<TKey>
		{
			int min = 0;
			int max = list.Count - 1;
			while (min < max)
			{
				int mid = (max + min) / 2;
				T midItem = list[mid];
				TKey midKey = keySelector(midItem);
				int comp = midKey.CompareTo(key);
				if (comp < 0)
				{
					min = mid + 1;
				}
				else if (comp > 0)
				{
					max = mid - 1;
				}
				else
				{
					return midItem;
				}
			}
			if (min == max && keySelector(list[min]).CompareTo(key) is 0) return list[min];
			throw new InvalidOperationException("Item not found");
		}

		/// <inheritdoc cref="List{T}.AddRange"/>
		/// <remarks>
		/// (This is an extension method which reimplements <see cref="List{T}.AddRange"/> for other <see cref="ICollection{T}">collections</see>.
		/// It defers to the existing <see cref="List{T}.AddRange">AddRange</see> if the receiver's type is <see cref="List{T}"/> or a subclass.)
		/// </remarks>
		public static void AddRange<T>(this ICollection<T> list, IEnumerable<T> collection)
		{
			if (list is List<T> listImpl)
			{
				listImpl.AddRange(collection);
				return;
			}
			foreach (var item in collection) list.Add(item);
		}

		public static IEnumerable<T> AsEnumerable<T>(this IEnumerator<T> enumerator)
			=> new EnumeratorAsEnumerable<T>(enumerator);

		/// <remarks>
		/// Contains method for arrays which does not need Linq, but rather uses Array.IndexOf
		/// similar to <see cref="ICollection{T}.Contains">ICollection's Contains</see>
		/// </remarks>
		public static bool Contains<T>(this T[] array, T value)
			=> Array.IndexOf(array, value) >= 0;

		/// <returns>
		/// portion of <paramref name="dest"/> that was written to,
		/// unless either span is empty, in which case the other reference is returned<br/>
		/// if <paramref name="dest"/> is too small, returns <see cref="Span{T}.Empty"/>
		/// </returns>
		public static ReadOnlySpan<T> ConcatArray<T>(this ReadOnlySpan<T> a, ReadOnlySpan<T> b, Span<T> dest)
		{
			if (b.IsEmpty) return a;
			if (a.IsEmpty) return b;
			var combinedLen = a.Length + b.Length;
			if (combinedLen < dest.Length) return Span<T>.Empty;
			a.CopyTo(dest);
			b.CopyTo(dest.Slice(start: a.Length));
			return dest.Slice(start: 0, length: combinedLen);
		}

		/// <returns>freshly-allocated array, unless either array is empty, in which case the other reference is returned</returns>
		public static T[] ConcatArray<T>(this T[] a, T[] b)
		{
			if (b.Length is 0) return a;
			if (a.Length is 0) return b;
			var combined = new T[a.Length + b.Length];
			var returned = ((ReadOnlySpan<T>) a).ConcatArray(b, combined);
			Debug.Assert(returned == combined, "expecting return value to cover all of combined since the whole thing was written to");
			return combined;
		}

		/// <returns>freshly-allocated array</returns>
#pragma warning disable RCS1224 // will be `params` when we bump `$(LangVersion)`; `this params` is nonsensical
		public static T[] ConcatArrays<T>(/*params*/ IReadOnlyCollection<T[]> arrays)
#pragma warning restore RCS1224
		{
			var combinedLength = arrays.Sum(static a => a.Length); //TODO detect overflow
			if (combinedLength is 0) return Array.Empty<T>();
			var combined = new T[combinedLength];
			var i = 0;
			foreach (var arr in arrays)
			{
				arr.AsSpan().CopyTo(combined.AsSpan(start: i));
				i += arr.Length;
			}
			return combined;
		}

		/// <returns>freshly-allocated array</returns>
#pragma warning disable RCS1224 // will be `params` when we bump `$(LangVersion)`; `this params` is nonsensical
		public static T[] ConcatArrays<T>(/*params*/ IReadOnlyCollection<ArraySegment<T>> arrays)
#pragma warning restore RCS1224
		{
			var combinedLength = arrays.Sum(static a => a.Count); //TODO detect overflow
			if (combinedLength is 0) return Array.Empty<T>();
			var combined = new T[combinedLength];
			var i = 0;
			foreach (var arr in arrays)
			{
				arr.AsSpan().CopyTo(combined.AsSpan(start: i));
				i += arr.Count;
			}
			return combined;
		}

		public static bool CountIsAtLeast<T>(this IEnumerable<T> collection, int n)
			=> collection is ICollection countable
				? countable.Count >= n
				: collection.Skip(n - 1).Any();

		public static bool CountIsExactly<T>(this IEnumerable<T> collection, int n)
			=> collection is ICollection countable
				? countable.Count == n
				: collection.Take(n + 1).Count() == n;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Fill<T>(this T[] array, T value)
			=> array.AsSpan().Fill(value);

		/// <summary>
		/// Returns the value at <paramref name="key"/>.
		/// If the key is not present, stores the result of <c>defaultValue(key)</c> in the dict, and then returns that.
		/// </summary>
		public static TValue GetValueOrPut<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> defaultValue)
			=> dictionary.TryGetValue(key, out var found) ? found : (dictionary[key] = defaultValue(key));

		/// <summary>
		/// Returns the value at <paramref name="key"/>.
		/// If the key is not present, stores the result of <c>new TValue()</c> in the dict, and then returns that.
		/// </summary>
		public static TValue GetValueOrPutNew<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
			where TValue : new()
			=> dictionary.TryGetValue(key, out var found) ? found : (dictionary[key] = new());

		/// <summary>
		/// Returns the value at <paramref name="key"/>.
		/// If the key is not present, stores the result of <c>new TValue(key)</c> in the dict, and then returns that.
		/// </summary>
		/// <remarks>
		/// Will throw if such a constructor does not exist, or exists but is not <see langword="public"/>.<br/>
		/// TODO is <see cref="Activator.CreateInstance(Type, object[])"/> fast enough?
		/// I suppose it's not that important because it's called on cache miss --yoshi
		/// </remarks>
		public static TValue GetValueOrPutNew1<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
			=> dictionary.GetValueOrPut(key, static k => (TValue) Activator.CreateInstance(typeof(TValue), k));

		/// <inheritdoc cref="IList{T}.IndexOf"/>
		/// <remarks>
		/// (This is an extension method which reimplements <see cref="IList{T}.IndexOf"/> for other <see cref="IReadOnlyList{T}">collections</see>.
		/// It defers to the existing <see cref="IList{T}.IndexOf">IndexOf</see> if the receiver's type is <see cref="IList{T}"/> or a subtype.)
		/// </remarks>
		public static int IndexOf<T>(this IReadOnlyList<T> list, T elem)
			where T : IEquatable<T>
		{
			if (list is IList<T> listImpl) return listImpl.IndexOf(elem);
			for (int i = 0, l = list.Count; i < l; i++) if (elem.Equals(list[i])) return i;
			return -1;
		}

		public static bool InsertAfter<T>(this IList<T> list, T needle, T insert)
		{
			Debug.Assert(!list.IsReadOnly, ERR_MSG_IMMUTABLE_LIST);
			var insertPoint = list.IndexOf(needle);
			if (insertPoint < 0) return false;
			list.Insert(insertPoint + 1, insert);
			return true;
		}

		[Obsolete(WARN_NONGENERIC)]
		public static bool InsertAfter<T>(this IList list, T needle, T insert)
		{
			Debug.Assert(!list.IsReadOnly, ERR_MSG_IMMUTABLE_LIST);
			var insertPoint = list.IndexOf(needle);
			if (insertPoint < 0) return false;
			list.Insert(insertPoint + 1, insert);
			return true;
		}

		public static bool InsertAfterLast<T>(this IList<T> list, T needle, T insert)
		{
			Debug.Assert(!list.IsReadOnly, ERR_MSG_IMMUTABLE_LIST);
			var insertPoint = list.LastIndexOf(needle);
			if (insertPoint < 0) return false;
			list.Insert(insertPoint + 1, insert);
			return true;
		}

		[Obsolete(WARN_NONGENERIC)]
		public static bool InsertAfterLast<T>(this IList list, T needle, T insert)
		{
			Debug.Assert(!list.IsReadOnly, ERR_MSG_IMMUTABLE_LIST);
			var insertPoint = list.LastIndexOf(needle);
			if (insertPoint < 0) return false;
			list.Insert(insertPoint + 1, insert);
			return true;
		}

		public static bool InsertBefore<T>(this IList<T> list, T needle, T insert)
		{
			Debug.Assert(!list.IsReadOnly, ERR_MSG_IMMUTABLE_LIST);
			var insertPoint = list.IndexOf(needle);
			if (insertPoint < 0) return false;
			list.Insert(insertPoint, insert);
			return true;
		}

		[Obsolete(WARN_NONGENERIC)]
		public static bool InsertBefore<T>(this IList list, T needle, T insert)
		{
			Debug.Assert(!list.IsReadOnly, ERR_MSG_IMMUTABLE_LIST);
			var insertPoint = list.IndexOf(needle);
			if (insertPoint < 0) return false;
			list.Insert(insertPoint, insert);
			return true;
		}

		public static bool InsertBeforeLast<T>(this IList<T> list, T needle, T insert)
		{
			Debug.Assert(!list.IsReadOnly, ERR_MSG_IMMUTABLE_LIST);
			var insertPoint = list.LastIndexOf(needle);
			if (insertPoint < 0) return false;
			list.Insert(insertPoint, insert);
			return true;
		}

		[Obsolete(WARN_NONGENERIC)]
		public static bool InsertBeforeLast<T>(this IList list, T needle, T insert)
		{
			Debug.Assert(!list.IsReadOnly, ERR_MSG_IMMUTABLE_LIST);
			var insertPoint = list.LastIndexOf(needle);
			if (insertPoint < 0) return false;
			list.Insert(insertPoint, insert);
			return true;
		}

		public static int LastIndexOf<T>(this IList<T> list, T item)
		{
			if (list is T[] arr) return Array.LastIndexOf(arr, item);
			if (list is List<T> bclList) return bclList.LastIndexOf(item);
			if (item is null)
			{
				for (var i = list.Count - 1; i >= 0; i--) if (list[i] is null) return i;
			}
			else
			{
				for (var i = list.Count - 1; i >= 0; i--) if (item.Equals(list[i])) return i;
			}
			return -1;
		}

		[Obsolete(WARN_NONGENERIC)]
		public static int LastIndexOf(this IList list, object? item)
		{
			if (item is null)
			{
				for (var i = list.Count - 1; i >= 0; i--) if (list[i] is null) return i;
			}
			else
			{
				for (var i = list.Count - 1; i >= 0; i--) if (item.Equals(list[i])) return i;
			}
			return -1;
		}

		public static T? FirstOrNull<T>(this IEnumerable<T> list, Func<T, bool> predicate)
			where T : struct
		{
			foreach (var t in list)
				if (predicate(t))
					return t;
			return null;
		}

		/// <returns><see langword="true"/> iff any removed</returns>
		public static bool RemoveAll<T>(this ICollection<T> collection, T item)
		{
			if (collection is ISet<T>) return collection.Remove(item);
#if false // probably not worth it, would need to benchmark
			if (collection is IList<T> list)
			{
				// remove from end
				var i = list.LastIndexOf(item);
				if (i < 0) return false;
				do
				{
					list.RemoveAt(i);
					i = list.LastIndexOf(item);
				}
				while (i < 0);
				return true;
			}
#endif
			if (!collection.Remove(item)) return false;
			while (collection.Remove(item)) {/*noop*/}
			return true;
		}

		/// <inheritdoc cref="List{T}.RemoveAll"/>
		/// <remarks>
		/// (This is an extension method which reimplements <see cref="List{T}.RemoveAll"/> for other <see cref="ICollection{T}">collections</see>.
		/// It defers to the existing <see cref="List{T}.RemoveAll">RemoveAll</see> if the receiver's type is <see cref="List{T}"/> or a subclass.)
		/// </remarks>
		public static int RemoveAll<T>(this ICollection<T> list, Func<T, bool> match)
		{
			if (list is List<T> listImpl) return listImpl.RemoveAll(item => match(item)); // can't simply cast to Predicate<T>, but thankfully we only need to allocate 1 extra delegate
			var c = list.Count;
			if (list is IList<T> iList)
			{
				for (var i = 0; i < iList.Count; i++)
				{
					if (match(iList[i])) iList.RemoveAt(i--);
				}
			}
			else
			{
				foreach (var item in list.Where(match)
							.ToArray()) // very important
				{
					list.Remove(item);
				}
			}
			return c - list.Count;
		}

		public static bool ReversedSequenceEqual<T>(this ReadOnlySpan<T> a, ReadOnlySpan<T> b)
			where T : IEquatable<T>
		{
			var len = a.Length;
			if (len != b.Length) return false;
			if (len is 0) return true;
			var i = 0;
			while (i < len)
			{
				if (!a[i].Equals(b[len - 1 - i])) return false;
				i++;
			}
			return true;
		}

		/// <summary>
		/// if <paramref name="shouldBeMember"/> is <see langword="false"/>,
		/// removes every copy of <paramref name="item"/> from <paramref name="collection"/>;
		/// else if <paramref name="shouldBeMember"/> is <see langword="true"/>
		/// and <paramref name="item"/> is not present in <paramref name="collection"/>, appends one copy;
		/// else no-op (does not limit to one copy)
		/// </summary>
		public static void SetMembership<T>(this ICollection<T> collection, T item, bool shouldBeMember)
		{
			if (!shouldBeMember) _ = collection.RemoveAll(item);
			else if (!collection.Contains(item)) collection.Add(item);
			// else noop
		}

		public static ReadOnlySpan<T> Slice<T>(this ReadOnlySpan<T> span, Range range)
		{
			var (offset, length) = range.GetOffsetAndLength(span.Length);
			return span.Slice(start: offset, length: length);
		}

		public static Span<T> Slice<T>(this Span<T> span, Range range)
		{
			var (offset, length) = range.GetOffsetAndLength(span.Length);
			return span.Slice(start: offset, length: length);
		}

		public static string Substring(this string str, Range range)
		{
			var (offset, length) = range.GetOffsetAndLength(str.Length);
			return str.Substring(startIndex: offset, length: length);
		}

#if !NET8_0_OR_GREATER
		/// <summary>shallow clone</summary>
		public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> list) where TKey : notnull
			=> list.ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value);
#endif

		/// <summary>
		/// if <paramref name="collection"/> contains <paramref name="item"/>, removes every copy;
		/// otherwise appends one copy
		/// </summary>
		/// <returns>new membership state (<see langword="true"/> iff added)</returns>
		public static bool ToggleMembership<T>(this ICollection<T> collection, T item)
		{
			var removed = collection.RemoveAll(item);
			if (!removed) collection.Add(item);
			return removed;
		}

		/// <inheritdoc cref="Unanimity(ISet{bool})"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool? Unanimity(this IEnumerable<bool> lazy)
			=> lazy is IReadOnlyCollection<bool> collection
				? Unanimity(collection)
				: Unanimity(lazy as ISet<bool> ?? lazy.ToHashSet());

		/// <inheritdoc cref="Unanimity(ISet{bool})"/>
		public static bool? Unanimity(this IReadOnlyCollection<bool> collection)
		{
			if (collection is bool[] arr) return Unanimity(arr.AsSpan());
			if (collection is List<bool> list)
			{
				return list is [ var first, .. ] && list.IndexOf(!first, index: 1) < 0 ? first : null;
			}
			using var iter = collection.GetEnumerator();
			if (!iter.MoveNext()) return null;
			var first1 = iter.Current;
			while (iter.MoveNext()) if (iter.Current != first1) return null;
			return first1;
		}

		/// <returns>
		/// <see langword="true"/> if all <see langword="true"/>,
		/// <see langword="false"/> if all <see langword="false"/>,
		/// <see langword="true"/> if mixed (or empty)
		/// </returns>
		public static bool? Unanimity(this ISet<bool> set)
			=> set.Contains(false)
				? set.Contains(true) ? null : false
				: set.Contains(true) ? true : null;

		/// <inheritdoc cref="Unanimity(ISet{bool})"/>
		public static bool? Unanimity(this ReadOnlySpan<bool> span)
			=> span is [ var first, .. ] && !span.Slice(start: 1).Contains(!first) ? first : null;

		public static bool IsSortedAsc<T>(this IReadOnlyList<T> list)
			where T : IComparable<T>
		{
			for (int i = 0, e = list.Count - 1; i < e; i++) if (list[i + 1].CompareTo(list[i]) < 0) return false;
			return true;
		}

		public static bool IsSortedAsc<T>(this ReadOnlySpan<T> span)
			where T : IComparable<T>
		{
			for (int i = 0, e = span.Length - 1; i < e; i++) if (span[i + 1].CompareTo(span[i]) < 0) return false;
			return true;
		}

		public static bool IsSortedDesc<T>(this IReadOnlyList<T> list)
			where T : IComparable<T>
		{
			for (int i = 0, e = list.Count - 1; i < e; i++) if (list[i + 1].CompareTo(list[i]) > 0) return false;
			return true;
		}

		public static bool IsSortedDesc<T>(this ReadOnlySpan<T> span)
			where T : IComparable<T>
		{
			for (int i = 0, e = span.Length - 1; i < e; i++) if (span[i + 1].CompareTo(span[i]) > 0) return false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool SequenceEqual<T>(this T[] a, ReadOnlySpan<T> b) where T : IEquatable<T> => a.AsSpan().SequenceEqual(b);
	}
}
