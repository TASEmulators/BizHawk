using System.Linq;
using System.Text;

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

		/// <returns><see langword="true"/> iff <paramref name="c"/> is <c>'-'</c>, <c>'.'</c> or a digit</returns>
		public static bool IsSignedDecimal(this char c) => IsSigned(c) || c is '.';

		/// <returns><see langword="true"/> iff <paramref name="c"/> is a digit</returns>
		public static bool IsUnsigned(this char c) => char.IsDigit(c);

		/// <returns><see langword="true"/> iff <paramref name="c"/> is <c>'.'</c> or a digit</returns>
		public static bool IsUnsignedDecimal(this char c) => IsUnsigned(c) || c is '.';

		/// <returns>
		/// A copy of <paramref name="raw"/> with characters removed so that the whole thing passes <see cref="IsBinary(string?)">IsBinary</see>.<br/>
		/// That is, all chars of the copy will be either <c>'0'</c> or <c>'1'</c>.
		/// </returns>
		public static string OnlyBinary(this string? raw) => string.IsNullOrWhiteSpace(raw) ? string.Empty : string.Concat(raw.Where(IsBinary));

		/// <returns>
		/// A copy of <paramref name="raw"/> with characters removed so that the whole thing passes <see cref="IsHex(string?)">IsHex</see>.<br/>
		/// That is, all chars of the copy will be hex digits (<c>[0-9A-F]</c>).
		/// </returns>
		public static string OnlyHex(this string? raw) => string.IsNullOrWhiteSpace(raw) ? string.Empty : string.Concat(raw.Where(IsHex)).ToUpperInvariant();

		/// <returns>
		/// A copy of <paramref name="raw"/> with characters removed so that the whole thing passes <see cref="IsUnsigned(string?)">IsUnsigned</see>.<br/>
		/// That is, all chars of the copy will be digits.
		/// </returns>
		public static string OnlyUnsigned(this string? raw) => string.IsNullOrWhiteSpace(raw) ? string.Empty : string.Concat(raw.Where(IsUnsigned));

#pragma warning disable CS8602

		/// <returns>
		/// <see langword="true"/> iff <paramref name="str"/> is not <see langword="null"/>,<br/>
		/// the first char of <paramref name="str"/> is <c>'-'</c>, <c>'.'</c>, or a digit,<br/>
		/// all subsequent chars of <paramref name="str"/> are <c>'.'</c> or a digit, and<br/>
		/// <paramref name="str"/> contains at most <c>1</c> decimal separator <c>'.'</c>
		/// </returns>
		/// <remarks>
		/// <paramref name="str"/> should exclude the suffix <c>f</c>.<br/>
		/// This method returning <see langword="true"/> for some <paramref name="str"/> does not imply that <see cref="float.TryParse(string,out float)">float.TryParse</see> will also return <see langword="true"/>.<br/>
		/// </remarks>
		public static bool IsSignedDecimal(this string? str) => !string.IsNullOrWhiteSpace(str) && str.Count(c => c == '.') <= 1 && str[0].IsSignedDecimal() && str.Substring(1).All(IsUnsignedDecimal);

		/// <returns>
		/// <see langword="true"/> iff <paramref name="str"/> is not <see langword="null"/>,
		/// the first char of <paramref name="str"/> is <c>'-'</c> or a digit, and
		/// all subsequent chars of <paramref name="str"/> are digits
		/// </returns>
		public static bool IsSigned(this string? str) => !string.IsNullOrWhiteSpace(str) && str[0].IsSigned() && str.Substring(1).All(IsUnsigned);

		/// <returns><see langword="true"/> iff <paramref name="str"/> is not <see langword="null"/> and all chars of <paramref name="str"/> are digits</returns>
		public static bool IsUnsigned(this string? str) => !string.IsNullOrWhiteSpace(str) && str.All(IsUnsigned);

		/// <returns>
		/// A copy of <paramref name="raw"/> with characters removed so that the whole thing passes <see cref="IsSignedDecimal(string?)">IsSignedDecimal</see>.<br/>
		/// That is, the first char of the copy will be <c>'-'</c>, <c>'.'</c>, or a digit,<br/>
		/// all subsequent chars of the copy will be <c>'.'</c> or a digit, and<br/>
		/// the copy will contain at most <c>1</c> decimal separator <c>'.'</c>.
		/// </returns>
		/// <remarks>
		/// If <paramref name="raw"/> contains a serialized negative decimal, it must be at the start (<paramref name="raw"/><c>[0] == '-'</c>) or the sign will be dropped.<br/>
		/// The returned value may not be parseable by <see cref="float.TryParse(string,out float)">float.TryParse</see>.<br/>
		/// </remarks>
		public static string OnlySignedDecimal(this string? raw)
		{
			if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

			var output = new StringBuilder();
			var usedDot = false;

			var first = raw[0];
			if (first.IsSignedDecimal())
			{
				output.Append(first);
				if (first == '.') usedDot = true;
			}

			for (int i = 1, l = raw.Length; i < l; i++)
			{
				var chr = raw[i];
				if (chr == '.')
				{
					if (usedDot) continue;
					output.Append(chr);
					usedDot = true;
				}
				else if (chr.IsUnsigned()) output.Append(chr);
			}

			return output.ToString();
		}

		/// <returns>
		/// A copy of <paramref name="raw"/> with characters removed so that the whole thing passes <see cref="IsSigned(string?)">IsSigned</see>.<br/>
		/// That is, the first char of the copy will be <c>'-'</c> or a digit, and all subsequent chars of the copy will be digits.
		/// </returns>
		/// <remarks>If <paramref name="raw"/> contains a serialized negative integer, it must be at the start (<paramref name="raw"/><c>[0] == '-'</c>) or the sign will be dropped.</remarks>
		public static string OnlySigned(this string? raw)
		{
			if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
			return raw[0].IsSigned()
				? raw[0] + string.Concat(raw.Skip(1).Where(IsUnsigned))
				: string.Concat(raw.Skip(1).Where(IsUnsigned));
		}

#pragma warning restore CS8602
	}
}
