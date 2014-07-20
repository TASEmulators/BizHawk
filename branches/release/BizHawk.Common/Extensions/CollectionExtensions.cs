using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Common.CollectionExtensions
{
	public static class CollectionExtensions
	{
		public static int LowerBoundBinarySearch<T, TKey>(this IList<T> list, Func<T, TKey> keySelector, TKey key) where TKey : IComparable<TKey>
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

			//did we find it exactly?
			if (min == max && keySelector(list[min]).CompareTo(key) == 0)
			{
				return min;
			}

			mid = min;

			//we didnt find it. return something corresponding to lower_bound semantics

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

		// http://stackoverflow.com/questions/1766328/can-linq-use-binary-search-when-the-collection-is-ordered
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

		public static byte[] ToByteArray(this IEnumerable<bool> list)
		{
			var bits = new BitArray(list.ToArray());
			byte [] bytes = new byte[bits.Length / 8 + ( bits.Length % 8 == 0 ? 0 : 1 )];
			bits.CopyTo(bytes, 0);
			return bytes;
		}

		/// <summary>
		/// Converts any byte array into a bit array represented as a list of bools
		/// </summary>
		public static IEnumerable<bool> ToBools(this byte[] bytes)
		{
			var bits = new BitArray(bytes);
			var bools = new bool[bits.Length];
			bits.CopyTo(bools, 0);

			return bools;
		}

		
	}
}
