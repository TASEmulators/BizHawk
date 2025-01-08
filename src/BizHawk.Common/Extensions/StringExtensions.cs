using System.Linq;
using System.Runtime.CompilerServices;

using CommunityToolkit.HighPerformance.Buffers;

namespace BizHawk.Common.StringExtensions
{
	public static class StringExtensions
	{
		public static string CharCodepointsToString(byte[] array)
		{
			var a = new char[array.Length];
			for (var i = 0; i < array.Length; i++) a[i] = char.ConvertFromUtf32(array[i])[0];
			return new(a);
		}

#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER)
		public static bool Contains(this string haystack, char needle)
			=> haystack.IndexOf(needle) >= 0;
#endif

		public static bool Contains(this string haystack, string needle, StringComparison comparisonType)
			=> haystack.IndexOf(needle, comparisonType) != -1;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ContainsOrdinal(this string haystack, char needle)
			=> haystack.Contains(needle); // already ordinal

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ContainsOrdinal(this string haystack, string needle)
			=> haystack.Contains(needle); // already ordinal

#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER)
		public static bool EndsWith(this string haystack, char needle)
			=> haystack.Length >= 1 && haystack[^1] == needle;
#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EndsWithOrdinal(this string haystack, char needle)
			=> haystack.EndsWith(needle); // already ordinal

		/// <returns>
		/// <see langword="true"/> if <paramref name="str"/> appears in <paramref name="options"/> (case-insensitive)
		/// </returns>
		public static bool In(this string str, params string[] options) =>
			options.Any(opt => string.Equals(opt, str, StringComparison.OrdinalIgnoreCase));

		/// <returns>a copy of <paramref name="raw"/> with all characters outside <c>[0-9A-Za-z]</c> removed</returns>
		public static string OnlyAlphanumeric(this string raw)
			=> string.Concat(raw.Where(static c => c is (>= '0' and <= '9') or (>= 'A' and <= 'Z') or (>= 'a' and <= 'z')));

		/// <returns>
		/// <paramref name="str"/> with the first char removed, or
		/// the original <paramref name="str"/> if the first char of <paramref name="str"/> is not <paramref name="prefix"/>
		/// </returns>
		public static string RemovePrefix(this string str, char prefix) => str.RemovePrefix(prefix, notFoundValue: str);

		/// <returns>
		/// <paramref name="str"/> with the first char removed, or
		/// <paramref name="notFoundValue"/> if the first char of <paramref name="str"/> is not <paramref name="prefix"/>
		/// </returns>
		public static string RemovePrefix(this string str, char prefix, string notFoundValue)
			=> str.StartsWith(prefix) ? str.Substring(1) : notFoundValue;

		/// <returns>
		/// <paramref name="str"/> with the leading substring <paramref name="prefix"/> removed, or
		/// the original <paramref name="str"/> if <paramref name="str"/> does not start with <paramref name="prefix"/>
		/// </returns>
		public static string RemovePrefix(this string str, string prefix) => str.RemovePrefix(prefix, notFoundValue: str);

		/// <returns>
		/// <paramref name="str"/> with the leading substring <paramref name="prefix"/> removed, or
		/// <paramref name="notFoundValue"/> if <paramref name="str"/> does not start with <paramref name="prefix"/>
		/// </returns>
		public static string RemovePrefix(this string str, string prefix, string notFoundValue) => str.StartsWith(prefix, StringComparison.Ordinal) ? str.Substring(prefix.Length, str.Length - prefix.Length) : notFoundValue;

		/// <returns>
		/// <paramref name="str"/> with the last char removed, or
		/// the original <paramref name="str"/> if the last char of <paramref name="str"/> is not <paramref name="suffix"/>
		/// </returns>
		public static string RemoveSuffix(this string str, char suffix) =>
			str.Length != 0 && str[str.Length - 1] == suffix
				? str.Substring(0, str.Length - 1)
				: str;

		/// <returns>
		/// <paramref name="str"/> with the trailing substring <paramref name="suffix"/> removed, or
		/// the original <paramref name="str"/> if <paramref name="str"/> does not end with <paramref name="suffix"/>
		/// </returns>
		public static string RemoveSuffix(this string str, string suffix) => str.RemoveSuffix(suffix, notFoundValue: str);

		/// <returns>
		/// <paramref name="str"/> with the trailing substring <paramref name="suffix"/> removed, or
		/// <paramref name="notFoundValue"/> if <paramref name="str"/> does not end with <paramref name="suffix"/>
		/// </returns>
		public static string RemoveSuffix(this string str, string suffix, string notFoundValue) => str.EndsWith(suffix, StringComparison.Ordinal) ? str.Substring(0, str.Length - suffix.Length) : notFoundValue;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool StartsWith(this ReadOnlySpan<char> str, char c)
			=> str.Length >= 1 && str[0] == c;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool StartsWith(this string str, char c)
			=> str.Length >= 1 && str[0] == c;

		/// <returns>
		/// the substring of <paramref name="str"/> after the first occurrence of <paramref name="delimiter"/>, or
		/// the original <paramref name="str"/> if not found
		/// </returns>
		public static string SubstringAfter(this string str, string delimiter) => str.SubstringAfter(delimiter, notFoundValue: str);

		/// <returns>
		/// the substring of <paramref name="str"/> after the first occurrence of <paramref name="delimiter"/>, or
		/// <paramref name="notFoundValue"/> if not found
		/// </returns>
		public static string SubstringAfter(this string str, string delimiter, string notFoundValue)
		{
			var index = str.IndexOf(delimiter, StringComparison.Ordinal);
			return index < 0 ? notFoundValue : str.Substring(index + delimiter.Length, str.Length - index - delimiter.Length);
		}

		/// <returns>
		/// the substring of <paramref name="str"/> after the last occurrence of <paramref name="delimiter"/>, or
		/// the original <paramref name="str"/> if not found
		/// </returns>
		public static string SubstringAfterLast(this string str, char delimiter)
			=> str.SubstringAfterLast(delimiter, notFoundValue: str);

		/// <returns>
		/// the substring of <paramref name="str"/> after the last occurrence of <paramref name="delimiter"/>, or
		/// <paramref name="notFoundValue"/> if not found
		/// </returns>
		public static string SubstringAfterLast(this string str, char delimiter, string notFoundValue)
		{
			var index = str.LastIndexOf(delimiter);
			return index < 0 ? notFoundValue : str.Substring(index + 1, str.Length - index - 1);
		}

		/// <returns>
		/// the substring of <paramref name="str"/> before the first occurrence of <paramref name="delimiter"/>, or
		/// the original <paramref name="str"/> if not found
		/// </returns>
		public static string SubstringBefore(this string str, char delimiter) => str.SubstringBefore(delimiter, notFoundValue: str);

		/// <returns>
		/// the substring of <paramref name="str"/> before the first occurrence of <paramref name="delimiter"/>, or
		/// <paramref name="notFoundValue"/> if not found
		/// </returns>
		public static string SubstringBefore(this string str, char delimiter, string notFoundValue)
		{
			var index = str.IndexOf(delimiter);
			return index < 0 ? notFoundValue : str.Substring(0, index);
		}

		/// <returns>
		/// the substring of <paramref name="str"/> before the last occurrence of <paramref name="delimiter"/>, or
		/// the original <paramref name="str"/> if not found
		/// </returns>
		public static string SubstringBeforeLast(this string str, char delimiter) => str.SubstringBeforeLast(delimiter, notFoundValue: str);

		/// <returns>
		/// the substring of <paramref name="str"/> before the last occurrence of <paramref name="delimiter"/>, or
		/// <paramref name="notFoundValue"/> if not found
		/// </returns>
		public static string SubstringBeforeLast(this string str, char delimiter, string notFoundValue)
		{
			var index = str.LastIndexOf(delimiter);
			return index < 0 ? notFoundValue : str.Substring(0, index);
		}

		/// <returns>
		/// the substring of <paramref name="str"/> before the first occurrence of <paramref name="delimiter"/>, or
		/// <see langword="null"/> if not found
		/// </returns>
		public static string? SubstringBeforeOrNull(this string str, string delimiter)
		{
			var index = str.IndexOf(delimiter, StringComparison.Ordinal);
			return index < 0 ? null : str.Substring(0, index);
		}

		public static byte[] ToCharCodepointArray(this string str)
		{
			var a = new byte[str.Length];
			for (var i = 0; i < str.Length; i++) a[i] = (byte) char.ConvertToUtf32(str, i);
			return a;
		}

		/// <summary>as <see cref="string.ToUpperInvariant"/>, but assumes <paramref name="str"/> is 7-bit ASCII to allow for an optimisation</summary>
		/// <remarks>allocates a new char array only when necessary</remarks>
		public static string ToUpperASCIIFast(this string str)
		{
			const ushort ASCII_UPCASE_MASK = 0b101_1111;
			for (var i = 0; i < str.Length; i++)
			{
				if (str[i] is < 'a' or > 'z') continue;
				var a = new char[str.Length];
				str.AsSpan(start: 0, length: i).CopyTo(a);
				a[i] = unchecked((char) (str[i] & ASCII_UPCASE_MASK));
				while (++i < str.Length)
				{
					var c = str[i];
					a[i] = c is >= 'a' and <= 'z' ? unchecked((char) (c & ASCII_UPCASE_MASK)) : c;
				}
				return StringPool.Shared.GetOrAdd(a);
			}
			return str;
		}

		/// <summary>
		/// splits a given <paramref name="str"/> by <paramref name="delimiter"/>,
		/// applies <paramref name="transform"/> to each part, then rejoins them
		/// </summary>
		/// <remarks><c>"abc,def,ghi".TransformFields(',', s => s.Reverse()) == "cba,fed,ihg"</c></remarks>
		public static string TransformFields(this string str, char delimiter, Func<string, string> transform)
			=> string.Join(delimiter.ToString(), str.Split(delimiter).Select(transform));

		public static bool StartsWithOrdinal(this string str, string value) => str.StartsWith(value, StringComparison.Ordinal);

		public static bool EndsWithOrdinal(this string str, string value) => str.EndsWith(value, StringComparison.Ordinal);
	}
}
