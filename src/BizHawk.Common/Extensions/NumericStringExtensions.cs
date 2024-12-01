using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace BizHawk.Common.StringExtensions
{
	/// <remarks>TODO how many of these methods can be replaced with <see cref="int.TryParse(string,out int)">int.TryParse</see> or similar? --yoshi</remarks>
	public static class NumericStringExtensions
	{
		/// <returns><see langword="true"/> iff <paramref name="c"/> is either <c>'0'</c> or <c>'1'</c></returns>
		public static bool IsBinary(this char c) => c == '0' || c == '1';

		/// <returns><see langword="true"/> iff <paramref name="str"/> is not <see langword="null"/> and all chars of <paramref name="str"/> are either <c>'0'</c> or <c>'1'</c></returns>
		/// <remarks><paramref name="str"/> should exclude the prefix <c>0b</c></remarks>
		public static bool IsBinary(this string? str) => !string.IsNullOrWhiteSpace(str) && str.All(IsBinary);

		/// <returns><see langword="true"/> iff <paramref name="c"/> is a hex digit (<c>[0-9A-Fa-f]</c>)</returns>
		public static bool IsHex(this char c) => IsUnsigned(c) || 'A' <= char.ToUpperInvariant(c) && char.ToUpperInvariant(c) <= 'F';

		/// <returns><see langword="true"/> iff <paramref name="str"/> is not <see langword="null"/> and all chars of <paramref name="str"/> are hex digits (<c>[0-9A-Fa-f]</c>)</returns>
		/// <remarks><paramref name="str"/> should exclude the prefix <c>0x</c></remarks>
		public static bool IsHex(this string? str) => !string.IsNullOrWhiteSpace(str) && str.All(IsHex);

		/// <returns><see langword="true"/> iff <paramref name="c"/> is <c>'-'</c> or a digit</returns>
		public static bool IsSigned(this char c) => IsUnsigned(c) || c == '-';

		/// <returns><see langword="true"/> iff <paramref name="c"/> is a digit</returns>
		public static bool IsUnsigned(this char c) => char.IsDigit(c);

		/// <returns>
		/// A copy of <paramref name="raw"/> with characters removed so that the whole thing passes <see cref="IsHex(string?)">IsHex</see>.<br/>
		/// That is, all chars of the copy will be hex digits (<c>[0-9A-F]</c>).
		/// </returns>
		public static string OnlyHex(this string? raw) => string.IsNullOrWhiteSpace(raw) ? string.Empty : string.Concat(raw.Where(IsHex)).ToUpperInvariant();

		/// <returns>
		/// A copy of <paramref name="raw"/> in uppercase after removing <c>0x</c>/<c>$</c> prefixes and all whitespace, or
		/// <see cref="string.Empty"/> if <paramref name="raw"/> contains other non-hex characters.
		/// </returns>
		public static string CleanHex(this string? raw)
		{
			if (raw is not null && CleanHexRegex.Match(raw) is { Success: true} match)
			{
				return match.Groups["hex"].Value.OnlyHex();
			}
			else
			{
				return string.Empty;
			}
		}
		private static readonly Regex CleanHexRegex = new(@"^\s*(?:0x|\$)?(?<hex>[0-9A-Fa-f\s]+)\s*$");

#if NET7_0_OR_GREATER
		public static ushort ParseU16FromHex(ReadOnlySpan<char> str)
			=> ushort.Parse(str, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);

		public static byte ParseU8FromHex(ReadOnlySpan<char> str)
			=> byte.Parse(str, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
#else
		public static ushort ParseU16FromHex(ReadOnlySpan<char> str)
			=> ParseU16FromHex(str.ToString());

		public static ushort ParseU16FromHex(string str)
			=> ushort.Parse(str, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);

		public static byte ParseU8FromHex(ReadOnlySpan<char> str)
			=> ParseU8FromHex(str.ToString());

		public static byte ParseU8FromHex(string str)
			=> byte.Parse(str, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
#endif

		/// <returns><see langword="true"/> iff <paramref name="str"/> is not <see langword="null"/> and all chars of <paramref name="str"/> are digits</returns>
		public static bool IsUnsigned(this string? str) => !string.IsNullOrWhiteSpace(str) && str.All(IsUnsigned);
	}
}
