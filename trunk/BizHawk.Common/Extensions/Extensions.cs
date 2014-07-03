using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace BizHawk.Common
{
	public static class Extensions
	{
		public static string GetDirectory(this Assembly asm)
		{
			string codeBase = asm.CodeBase;
			UriBuilder uri = new UriBuilder(codeBase);
			string path = Uri.UnescapeDataString(uri.Path);
			return Path.GetDirectoryName(path);
		}

		public static IEnumerable<LinkedListNode<T>> EnumerateNodes<T>(this LinkedList<T> list)
		{
			var node = list.First;
			while (node != null)
			{
				yield return node;
				node = node.Next;
			}
		}

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
				return max; //had to go all the way to max before giving up; lower bound is max
			if (mid == 0)
				return -1; //had to go all the way to min before giving up; lower bound is min

			midKey = keySelector(list[mid]);
			if (midKey.CompareTo(key) >= 0) return mid - 1;
			else return mid;
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

		public static bool Bit(this byte b, int index)
		{
			return (b & (1 << index)) != 0;
		}

		public static bool ContainsStartsWith(this IEnumerable<string> options, string str)
		{
			return options.Any(opt => opt.StartsWith(str));
		}

		public static string GetOptionValue(this IEnumerable<string> options, string str)
		{
			try
			{
				foreach (string opt in options)
				{
					if (opt.StartsWith(str))
					{
						return opt.Split('=')[1];
					}
				}
			}
			catch (Exception) { }
			return null;
		}

		public static void SaveAsHex(this byte[] buffer, TextWriter writer)
		{
			foreach (byte b in buffer)
			{
				writer.Write("{0:X2}", b);
			}
			writer.WriteLine();
		}

		public unsafe static void SaveAsHexFast(this byte[] buffer, TextWriter writer)
		{
			char* table = Util.HexConvPtr;
			if (buffer.Length > 0)
			{
				int len = buffer.Length;
				fixed (byte* src = &buffer[0])
					for (int i = 0; i < len; i++)
					{
						writer.Write(table[src[i] >> 4]);
						writer.Write(table[src[i] & 15]);
					}
			}
			writer.WriteLine();
		}

		public static void SaveAsHex(this byte[] buffer, TextWriter writer, int length)
		{
			for (int i = 0; i < length; i++)
			{
				writer.Write("{0:X2}", buffer[i]);
			}
			writer.WriteLine();
		}

		public static void SaveAsHex(this short[] buffer, TextWriter writer)
		{
			foreach (short b in buffer)
			{
				writer.Write("{0:X4}", b);
			}
			writer.WriteLine();
		}

		public static void SaveAsHex(this ushort[] buffer, TextWriter writer)
		{
			foreach (ushort b in buffer)
			{
				writer.Write("{0:X4}", b);
			}
			writer.WriteLine();
		}

		public static void SaveAsHex(this int[] buffer, TextWriter writer)
		{
			foreach (int b in buffer)
			{
				writer.Write("{0:X8}", b);
			}
			writer.WriteLine();
		}

		public static void SaveAsHex(this uint[] buffer, TextWriter writer)
		{
			foreach (uint b in buffer)
			{
				writer.Write("{0:X8}", b);
			}
			writer.WriteLine();
		}

		public static void ReadFromHex(this byte[] buffer, string hex)
		{
			if (hex.Length%2 != 0)
			{
				throw new Exception("Hex value string does not appear to be properly formatted.");
			}

			for (int i = 0; i < buffer.Length && i * 2 < hex.Length; i++)
			{
				string bytehex = "" + hex[i * 2] + hex[i * 2 + 1];
				buffer[i] = byte.Parse(bytehex, NumberStyles.HexNumber);
			}
		}

		public static unsafe void ReadFromHexFast(this byte[] buffer, string hex)
		{
			if (buffer.Length * 2 != hex.Length)
				throw new Exception("Data size mismatch");
			int count = buffer.Length;
			fixed (byte* _dst = buffer)
			fixed (char* _src = hex)
			{
				byte* dst = _dst;
				char* src = _src;
				while (count > 0)
				{
					// in my tests, replacing Hex2Int() with a 256 entry LUT slowed things down slightly
					*dst = (byte)(Hex2Int(*src++) << 4 | Hex2Int(*src++));
					count--;
				}
			}
		}

		public static void ReadFromHex(this short[] buffer, string hex)
		{
			if (hex.Length % 4 != 0)
				throw new Exception("Hex value string does not appear to be properly formatted.");
			for (int i = 0; i < buffer.Length && i * 4 < hex.Length; i++)
			{
				string shorthex = "" + hex[i * 4] + hex[(i * 4) + 1] + hex[(i * 4) + 2] + hex[(i * 4) + 3];
				buffer[i] = short.Parse(shorthex, NumberStyles.HexNumber);
			}
		}

		public static void ReadFromHex(this ushort[] buffer, string hex)
		{
			if (hex.Length % 4 != 0)
				throw new Exception("Hex value string does not appear to be properly formatted.");
			for (int i = 0; i < buffer.Length && i * 4 < hex.Length; i++)
			{
				string ushorthex = "" + hex[i * 4] + hex[(i * 4) + 1] + hex[(i * 4) + 2] + hex[(i * 4) + 3];
				buffer[i] = ushort.Parse(ushorthex, NumberStyles.HexNumber);
			}
		}

		public static void ReadFromHex(this int[] buffer, string hex)
		{
			if (hex.Length % 8 != 0)
			{
				throw new Exception("Hex value string does not appear to be properly formatted.");
			}

			for (int i = 0; i < buffer.Length && i * 8 < hex.Length; i++)
			{
				//string inthex = "" + hex[i * 8] + hex[(i * 8) + 1] + hex[(i * 4) + 2] + hex[(i * 4) + 3] + hex[(i*4
				string inthex = hex.Substring(i * 8, 8);
				buffer[i] = int.Parse(inthex, NumberStyles.HexNumber);
			}
		}

		#region Helpers

		private static int Hex2Int(char c)
		{
			if (c <= '9')
			{
				return c - '0';
			}

			if (c <= 'F')
			{
				return c - '7';
			}

			return c - 'W';
		}

		#endregion
	}
}