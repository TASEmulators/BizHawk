using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Common.CollectionExtensions
{
	public static class CollectionExtensions
	{
		public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			bool desc)
		{
			return desc ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);
		}

		public static int LowerBoundBinarySearch<T, TKey>(this IList<T> list, Func<T, TKey> keySelector, TKey key)
			where TKey : IComparable<TKey>
		{
			int min = 0;
			int max = list.Count;
			int mid;
			TKey midKey;
			while (min < max)
			{
				mid = (max + min) / 2;
				T midItem = list[mid];
				midKey = keySelector(midItem);
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

			// did we find it exactly?
			if (min == max && keySelector(list[min]).CompareTo(key) == 0)
			{
				return min;
			}

			mid = min;

			// we didn't find it. return something corresponding to lower_bound semantics
			if (mid == list.Count)
			{
				return max; // had to go all the way to max before giving up; lower bound is max
			}

			if (mid == 0)
			{
				return -1; // had to go all the way to min before giving up; lower bound is min
			}

			midKey = keySelector(list[mid]);
			if (midKey.CompareTo(key) >= 0)
			{
				return mid - 1;
			}

			return mid;
		}

		/// <exception cref="InvalidOperationException"><paramref name="key"/> not found after mapping <paramref name="keySelector"/> over <paramref name="list"/></exception>
		/// <remarks>implementation from https://stackoverflow.com/a/1766369/7467292</remarks>
		public static T BinarySearch<T, TKey>(this IList<T> list, Func<T, TKey> keySelector, TKey key)
			where TKey : IComparable<TKey>
		{
			int min = 0;
			int max = list.Count;
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

			if (min == max &&
				keySelector(list[min]).CompareTo(key) == 0)
			{
				return list[min];
			}

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

		public static T? FirstOrNull<T>(this IEnumerable<T> list, Func<T, bool> predicate)
			where T : struct
		{
			foreach (var t in list)
				if (predicate(t))
					return t;
			return null;
		}

		/// <inheritdoc cref="List{T}.RemoveAll"/>
		/// <remarks>
		/// (This is an extension method which reimplements <see cref="List{T}.RemoveAll"/> for other <see cref="ICollection{T}">collections</see>.
		/// It defers to the existing <see cref="List{T}.RemoveAll">RemoveAll</see> if the receiver's type is <see cref="List{T}"/> or a subclass.)
		/// </remarks>
		public static int RemoveAll<T>(this ICollection<T> list, Predicate<T> match)
		{
			if (list is List<T> listImpl) return listImpl.RemoveAll(match);
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
				foreach (var item in list.Where(item => match(item)) // can't simply cast to Func<T, bool>
					.ToList()) // very important
				{
					list.Remove(item);
				}
			}
			return c - list.Count;
		}
	}
}
