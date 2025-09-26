using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using CommunityToolkit.HighPerformance.Buffers;

namespace BizHawk.Common.StringExtensions
{
	public static partial class StringExtensions
	{
		/// <remarks>based on <see href="https://stackoverflow.com/a/35081977"/></remarks>
		public static char[] CommonPrefix(params string[] strings)
		{
			var shortest = strings.MinBy(static s => s.Length);
			return string.IsNullOrEmpty(shortest)
				? [ ]
				: shortest.TakeWhile((c, i) => Array.TrueForAll(strings, s => s[i] == c)).ToArray();
		}

		public static char[] CommonPrefix(this IEnumerable<string> strings)
			=> CommonPrefix(strings: strings as string[] ?? strings.ToArray());

		/// <inheritdoc cref="EqualsIgnoreCase"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ContainsIgnoreCase(this string haystack, string needle)
			=> haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ContainsOrdinal(this string haystack, char needle)
			=> haystack.Contains(needle); // already ordinal

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ContainsOrdinal(this string haystack, string needle)
			=> haystack.Contains(needle); // already ordinal

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EndsWithOrdinal(this string haystack, char needle)
			=> haystack.EndsWith(needle); // already ordinal

#pragma warning disable RS0030 // doc comment links to banned API
		/// <summary>performs a non-localised but case-insensitive comparison</summary>
		/// <remarks>
		/// uses <see cref="StringComparison.OrdinalIgnoreCase"/>,
		/// equivalent to <c>str.ToUpperInvariant().SequenceEqual(other.ToUpperInvariant())</c> per <see href="https://learn.microsoft.com/en-us/dotnet/api/system.stringcomparer.ordinalignorecase?view=netstandard-2.0#remarks">docs</see>;
		/// whereas <see cref="StringComparison.InvariantCultureIgnoreCase"/> is different (for non-ASCII text)
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EqualsIgnoreCase(this string str, string other)
			=> str.Equals(other, StringComparison.OrdinalIgnoreCase);
#pragma warning restore RS0030

		/// <returns>
		/// <see langword="true"/> if <paramref name="str"/> appears in <paramref name="options"/> (case-insensitive)
		/// </returns>
		public static bool In(this string str, params string[] options)
			=> options.Any(str.EqualsIgnoreCase);

		public static string InsertAfter(this string str, char needle, string insert, out bool found)
		{
			var insertPoint = str.IndexOf(needle);
			found = insertPoint >= 0;
			return found ? str.Insert(insertPoint + 1, insert) : str;
		}

		public static string InsertAfterLast(this string str, char needle, string insert, out bool found)
		{
			var insertPoint = str.LastIndexOf(needle);
			found = insertPoint >= 0;
			return found ? str.Insert(insertPoint + 1, insert) : str;
		}

		public static string InsertBefore(this string str, char needle, string insert, out bool found)
		{
			var insertPoint = str.IndexOf(needle);
			found = insertPoint >= 0;
			return found ? str.Insert(insertPoint, insert) : str;
		}

		public static string InsertBeforeLast(this string str, char needle, string insert, out bool found)
		{
			var insertPoint = str.LastIndexOf(needle);
			found = insertPoint >= 0;
			return found ? str.Insert(insertPoint, insert) : str;
		}

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

		/// <summary>a simple checksum of string contents, using MSBuild's "legacy" algorithm</summary>
		public static int StableStringHash(this string str)
		{
			if (str is null) return 0;

			// taken from .NET 9 source, MIT-licensed, specifically https://github.com/dotnet/msbuild/blob/v17.12.6/src/Shared/CommunicationsUtilities.cs#L861-L888
			// (and then cleaned up a LOT)
			static int RotateLeft(int n, int shift)
				=> (n << shift) + (n >> ((sizeof(int) * 8) - shift));
			int hash1 = 0x15051505;
			int hash2 = 0x15051505;
			var span = MemoryMarshal.AsBytes(str.AsSpan());
			while (true)
			{
				if (span.Length < sizeof(int))
				{
					if (span.Length < sizeof(ushort)) break;
					hash1 += RotateLeft(hash1, 5);
					hash1 ^= MemoryMarshal.Read<ushort>(span);
					break;
				}
				hash1 += RotateLeft(hash1, 5);
				hash1 ^= MemoryMarshal.Read<int>(span);
				span = span.Slice(sizeof(int));

				if (span.Length < sizeof(int))
				{
					if (span.Length < sizeof(ushort)) break;
					hash2 += RotateLeft(hash2, 5);
					hash2 ^= MemoryMarshal.Read<ushort>(span);
					break;
				}
				hash2 += RotateLeft(hash2, 5);
				hash2 ^= MemoryMarshal.Read<int>(span);
				span = span.Slice(sizeof(int));
			}
			return hash2 * (37 * 42326593) + hash1;
		}

		/// <inheritdoc cref="EqualsIgnoreCase"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool StartsWithIgnoreCase(this string haystack, string needle)
			=> haystack.StartsWith(needle, StringComparison.OrdinalIgnoreCase);

		/// <returns>
		/// the substring of <paramref name="str"/> after the first occurrence of <paramref name="delimiter"/>, or
		/// the original <paramref name="str"/> if not found
		/// </returns>
		public static string SubstringAfter(this string str, char delimiter)
			=> str.SubstringAfter(delimiter, notFoundValue: str);

		/// <returns>
		/// the substring of <paramref name="str"/> after the first occurrence of <paramref name="delimiter"/>, or
		/// the original <paramref name="str"/> if not found
		/// </returns>
		public static string SubstringAfter(this string str, char delimiter, string notFoundValue)
		{
			var index = str.IndexOf(delimiter);
			return index < 0 ? notFoundValue : str.Substring(index + 1, str.Length - index - 1);
		}

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
