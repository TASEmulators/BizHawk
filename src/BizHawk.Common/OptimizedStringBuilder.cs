using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace BizHawk.Common
{
	/// <summary>
	/// Optimized string building utilities to reduce allocations and improve performance.
	/// Compatible with .NET Standard 2.0.
	/// </summary>
	public static class OptimizedStringBuilder
	{
		/// <summary>
		/// Concatenates two strings using span-based operations to minimize allocations.
		/// For frequent concatenations, consider using StringBuilder or string.Concat.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string Concat(string str1, string str2)
		{
			if (string.IsNullOrEmpty(str1)) return str2 ?? string.Empty;
			if (string.IsNullOrEmpty(str2)) return str1;
			
			return string.Concat(str1, str2);
		}

		/// <summary>
		/// Converts a byte array to a hex string efficiently.
		/// </summary>
		public static string ToHexString(byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0) return string.Empty;

			var chars = new char[bytes.Length * 2];
			int pos = 0;
			foreach (var b in bytes)
			{
				chars[pos++] = GetHexChar(b >> 4);
				chars[pos++] = GetHexChar(b & 0xF);
			}

			return new string(chars);
		}

		/// <summary>
		/// Converts a byte value to a 2-character hex string efficiently.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToHexString(byte value)
		{
			var chars = new char[2];
			chars[0] = GetHexChar(value >> 4);
			chars[1] = GetHexChar(value & 0xF);
			return new string(chars);
		}

		/// <summary>
		/// Converts a ushort value to a 4-character hex string efficiently.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToHexString(ushort value)
		{
			var chars = new char[4];
			chars[0] = GetHexChar((value >> 12) & 0xF);
			chars[1] = GetHexChar((value >> 8) & 0xF);
			chars[2] = GetHexChar((value >> 4) & 0xF);
			chars[3] = GetHexChar(value & 0xF);
			return new string(chars);
		}

		/// <summary>
		/// Converts a uint value to an 8-character hex string efficiently.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToHexString(uint value)
		{
			var chars = new char[8];
			for (int i = 7; i >= 0; i--)
			{
				chars[i] = GetHexChar((int)(value & 0xF));
				value >>= 4;
			}
			return new string(chars);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static char GetHexChar(int value)
		{
			return (char)(value < 10 ? '0' + value : 'A' + (value - 10));
		}

		/// <summary>
		/// Joins strings with a separator efficiently using StringBuilder with capacity estimation.
		/// </summary>
		public static string Join(char separator, string[] values)
		{
			if (values == null || values.Length == 0) return string.Empty;
			if (values.Length == 1) return values[0] ?? string.Empty;

			// Estimate capacity
			int capacity = values.Length - 1; // separators
			foreach (var str in values.Where(s => s != null))
			{
				capacity += str.Length;
			}

			var sb = new StringBuilder(capacity);
			sb.Append(values[0]);
			for (int i = 1; i < values.Length; i++)
			{
				sb.Append(separator);
				sb.Append(values[i]);
			}

			return sb.ToString();
		}

		/// <summary>
		/// Efficiently trims whitespace from a string.
		/// Returns the original string if no trimming is needed.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string TrimOptimized(string str)
		{
			if (string.IsNullOrEmpty(str)) return str;
			
			var trimmed = str.Trim();
			return trimmed.Length == str.Length ? str : trimmed;
		}

		/// <summary>
		/// Pads a string to a specific length efficiently.
		/// </summary>
		public static string PadLeft(string str, int totalWidth, char paddingChar = ' ')
		{
			if (str == null) str = string.Empty;
			if (str.Length >= totalWidth) return str;

			return str.PadLeft(totalWidth, paddingChar);
		}

		/// <summary>
		/// Reverses a string efficiently.
		/// </summary>
		public static string Reverse(string str)
		{
			if (string.IsNullOrEmpty(str) || str.Length == 1) return str;

			var chars = str.ToCharArray();
			Array.Reverse(chars);
			return new string(chars);
		}

		/// <summary>
		/// Creates a pooled StringBuilder with a specified initial capacity.
		/// Remember to return it via ReturnStringBuilder when done.
		/// </summary>
		private static readonly ObjectPool<StringBuilder> StringBuilderPool = 
			new ObjectPool<StringBuilder>(() => new StringBuilder(256), sb => { sb.Clear(); return sb; });

		/// <summary>
		/// Gets a StringBuilder from the pool for temporary use.
		/// Must call ReturnStringBuilder when done.
		/// </summary>
		public static StringBuilder GetStringBuilder()
		{
			return StringBuilderPool.Get();
		}

		/// <summary>
		/// Returns a StringBuilder to the pool after use.
		/// The StringBuilder should not be used after returning.
		/// </summary>
		public static void ReturnStringBuilder(StringBuilder sb)
		{
			if (sb != null) StringBuilderPool.Return(sb);
		}
	}

	/// <summary>
	/// Simple object pool for reusing expensive objects with a maximum size limit.
	/// </summary>
	internal class ObjectPool<T> where T : class
	{
		private readonly Func<T> _factory;
		private readonly Func<T, T> _reset;
		private readonly System.Collections.Concurrent.ConcurrentBag<T> _objects = new System.Collections.Concurrent.ConcurrentBag<T>();
		private readonly int _maxSize;
		private int _count;

		public ObjectPool(Func<T> factory, Func<T, T> reset, int maxSize = 64)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_reset = reset;
			_maxSize = maxSize;
		}

		public T Get()
		{
			if (_objects.TryTake(out var item))
			{
				System.Threading.Interlocked.Decrement(ref _count);
				return item;
			}
			return _factory();
		}

		public void Return(T obj)
		{
			if (obj == null) return;
			
			var resetObj = _reset != null ? _reset(obj) : obj;
			if (resetObj != null)
			{
				var newCount = System.Threading.Interlocked.Increment(ref _count);
				if (newCount > _maxSize)
				{
					System.Threading.Interlocked.Decrement(ref _count);
					return;
				}
				_objects.Add(resetObj);
			}
		}
	}
}
