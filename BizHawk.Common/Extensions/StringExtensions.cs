using System;
using System.Linq;
using System.Text;

namespace BizHawk.Common.StringExtensions
{
	/// <remarks>TODO how many of these <c>Is*</c>/<c>Only*</c> methods' callers can use <see cref="int.TryParse(string,out int)">int.TryParse</see> or similar instead? --yoshi</remarks>
	public static class StringExtensions
	{
#pragma warning disable CS8602 // no idea --yoshi

		/// <returns>the substring of <paramref name="str"/> before the first occurrence of <paramref name="value"/>, or <see langword="null"/> if not found</returns>
		public static string? GetPrecedingString(this string str, string value)
		{
			var index = str.IndexOf(value);
			return index < 0 ? null : str.Substring(0, index);
		}

		/// <returns><see langword="true"/> iff <paramref name="str"/> appears in <paramref name="options"/> (case-insensitive)</returns>
		public static bool In(this string str, params string[] options) => options.Any(opt => string.Equals(opt, str, StringComparison.InvariantCultureIgnoreCase));

		/// <returns>how many times <paramref name="c"/> appears in <paramref name="str"/>, or <c>0</c> if <paramref name="str"/> is null</returns>
		public static int HowMany(this string? str, char c) => string.IsNullOrEmpty(str) ? 0 : str.Count(t => t == c);

		/// <returns>how many times <paramref name="sub"/> appears in <paramref name="str"/>, or <c>0</c> if <paramref name="str"/> is null</returns>
		/// <remarks>
		/// occurrences may overlap, for example <c>"AAA".HowMany("AA")</c> returns <c>2</c><br/>
		/// TODO except it doesn't, but <c>"AAAB".HowMany("AA")</c> does. I left this bug in so as to not break anything. --yoshi
		/// </remarks>
		public static int HowMany(this string? str, string sub)
		{
			if (string.IsNullOrEmpty(str)) return 0;

			var count = 0;
			var substrLength = sub.Length;
			for (int i = 0, l = str.Length - substrLength; i < l; i++)
			{
				if (string.Equals(str.Substring(i, substrLength), sub, StringComparison.InvariantCulture)) count++;
			}
			return count;
		}

		/// <returns><see langword="true"/> iff <paramref name="str"/> is not <see langword="null"/> and all chars of <paramref name="str"/> are digits</returns>
		public static bool IsUnsigned(this string? str) => !string.IsNullOrWhiteSpace(str) && str.All(IsUnsigned);

		/// <returns><see langword="true"/> iff <paramref name="c"/> is a digit</returns>
		public static bool IsUnsigned(this char c) => char.IsDigit(c);

		/// <returns>
		/// <see langword="true"/> iff <paramref name="str"/> is not <see langword="null"/>,
		/// the first char of <paramref name="str"/> is <c>'-'</c> or a digit, and
		/// all subsequent chars of <paramref name="str"/> are digits
		/// </returns>
		public static bool IsSigned(this string? str) => !string.IsNullOrWhiteSpace(str) && str[0].IsSigned() && str.Substring(1).All(IsUnsigned);

		/// <returns><see langword="true"/> iff <paramref name="c"/> is <c>'-'</c> or a digit</returns>
		public static bool IsSigned(this char c) => IsUnsigned(c) || c == '-';

		/// <returns><see langword="true"/> iff <paramref name="str"/> is not <see langword="null"/> and all chars of <paramref name="str"/> are hex digits (<c>[0-9A-Fa-f]</c>)</returns>
		/// <remarks><paramref name="str"/> should exclude the prefix <c>0x</c></remarks>
		public static bool IsHex(this string? str) => !string.IsNullOrWhiteSpace(str) && str.All(IsHex);

		/// <returns><see langword="true"/> iff <paramref name="c"/> is a hex digit (<c>[0-9A-Fa-f]</c>)</returns>
		public static bool IsHex(this char c) => IsUnsigned(c) || 'A' <= char.ToUpperInvariant(c) && char.ToUpperInvariant(c) <= 'F';

		/// <returns><see langword="true"/> iff <paramref name="str"/> is not <see langword="null"/> and all chars of <paramref name="str"/> are either <c>'0'</c> or <c>'1'</c></returns>
		/// <remarks><paramref name="str"/> should exclude the prefix <c>0b</c></remarks>
		public static bool IsBinary(this string? str) => !string.IsNullOrWhiteSpace(str) && str.All(IsBinary);

		/// <returns><see langword="true"/> iff <paramref name="c"/> is either <c>'0'</c> or <c>'1'</c></returns>
		public static bool IsBinary(this char c) => c == '0' || c == '1';

		/// <returns>
		/// <see langword="true"/> iff <paramref name="str"/> is not <see langword="null"/>,<br/>
		/// all chars of <paramref name="str"/> are <c>'.'</c> or a digit, and<br/>
		/// <paramref name="str"/> contains at most <c>1</c> decimal separator <c>'.'</c>
		/// </returns>
		/// <remarks>
		/// <paramref name="str"/> should exclude the suffix <c>M</c>.<br/>
		/// This method returning <see langword="true"/> for some <paramref name="str"/> does not imply that <see cref="float.TryParse(string,out float)">float.TryParse</see> will also return <see langword="true"/>.<br/>
		/// Also this has nothing to do with fixed- vs. floating-point numbers, a better name would be <c>IsUnsignedDecimal</c>.
		/// </remarks>
		public static bool IsFixedPoint(this string? str) => !string.IsNullOrWhiteSpace(str) && str.HowMany('.') <= 1 && str.All(IsFixedPoint);

		/// <returns><see langword="true"/> iff <paramref name="c"/> is <c>'.'</c> or a digit</returns>
		/// <remarks>Also this has nothing to do with fixed- vs. floating-point numbers, a better name would be <c>IsUnsignedDecimal</c>.</remarks>
		public static bool IsFixedPoint(this char c) => IsUnsigned(c) || c == '.';

		/// <returns>
		/// <see langword="true"/> iff <paramref name="str"/> is not <see langword="null"/>,<br/>
		/// the first char of <paramref name="str"/> is <c>'-'</c>, <c>'.'</c>, or a digit,<br/>
		/// all subsequent chars of <paramref name="str"/> are <c>'.'</c> or a digit, and<br/>
		/// <paramref name="str"/> contains at most <c>1</c> decimal separator <c>'.'</c>
		/// </returns>
		/// <remarks>
		/// <paramref name="str"/> should exclude the suffix <c>f</c>.<br/>
		/// This method returning <see langword="true"/> for some <paramref name="str"/> does not imply that <see cref="float.TryParse(string,out float)">float.TryParse</see> will also return <see langword="true"/>.<br/>
		/// Also this has nothing to do with fixed- vs. floating-point numbers, a better name would be <c>IsSignedDecimal</c>.
		/// </remarks>
		public static bool IsFloat(this string? str) => !string.IsNullOrWhiteSpace(str) && str.HowMany('.') <= 1 && str[0].IsFloat() && str.Substring(1).All(IsFixedPoint);

		/// <returns><see langword="true"/> iff <paramref name="c"/> is <c>'-'</c>, <c>'.'</c>, or a digit</returns>
		/// <remarks>Also this has nothing to do with fixed- vs. floating-point numbers, a better name would be <c>IsSignedDecimal</c>.</remarks>
		public static bool IsFloat(this char c) => c.IsFixedPoint() || c == '-';

		/// <returns>
		/// A copy of <paramref name="raw"/> with characters removed so that the whole thing passes <see cref="IsBinary(string?)">IsBinary</see>.<br/>
		/// That is, all chars of the copy will be either <c>'0'</c> or <c>'1'</c>.
		/// </returns>
		public static string OnlyBinary(this string? raw) => string.IsNullOrWhiteSpace(raw) ? string.Empty : string.Concat(raw.Where(IsBinary));

		/// <returns>
		/// A copy of <paramref name="raw"/> with characters removed so that the whole thing passes <see cref="IsUnsigned(string?)">IsUnsigned</see>.<br/>
		/// That is, all chars of the copy will be digits.
		/// </returns>
		public static string OnlyUnsigned(this string? raw) => string.IsNullOrWhiteSpace(raw) ? string.Empty : string.Concat(raw.Where(IsUnsigned));

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

		/// <returns>
		/// A copy of <paramref name="raw"/> with characters removed so that the whole thing passes <see cref="IsHex(string?)">IsHex</see>.<br/>
		/// That is, all chars of the copy will be hex digits (<c>[0-9A-F]</c>).
		/// </returns>
		public static string OnlyHex(this string? raw) => string.IsNullOrWhiteSpace(raw) ? string.Empty : string.Concat(raw.Where(IsHex)).ToUpperInvariant();

		/// <returns>
		/// A copy of <paramref name="raw"/> with characters removed so that the whole thing passes <see cref="IsFixedPoint(string?)">IsFixedPoint</see>.<br/>
		/// That is, the all chars of the copy will be <c>'.'</c> or a digit and the copy will contain at most <c>1</c> decimal separator <c>'.'</c>.
		/// </returns>
		/// <remarks>
		/// The returned value may not be parseable by <see cref="float.TryParse(string,out float)">float.TryParse</see>.<br/>
		/// Also this has nothing to do with fixed- vs. floating-point numbers, a better name would be <c>IsUnsignedDecimal</c>.
		/// </remarks>
		public static string OnlyFixedPoint(this string? raw)
		{
			if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
			var output = new StringBuilder();
			var usedDot = false;
			foreach (var chr in raw)
			{
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
		/// A copy of <paramref name="raw"/> with characters removed so that the whole thing passes <see cref="IsFloat(string?)">IsFloat</see>.<br/>
		/// That is, the first char of the copy will be <c>'-'</c>, <c>'.'</c>, or a digit,<br/>
		/// all subsequent chars of the copy will be <c>'.'</c> or a digit, and<br/>
		/// the copy will contain at most <c>1</c> decimal separator <c>'.'</c>.
		/// </returns>
		/// <remarks>
		/// If <paramref name="raw"/> contains a serialized negative decimal, it must be at the start (<paramref name="raw"/><c>[0] == '-'</c>) or the sign will be dropped.<br/>
		/// The returned value may not be parseable by <see cref="float.TryParse(string,out float)">float.TryParse</see>.<br/>
		/// Also this has nothing to do with fixed- vs. floating-point numbers, a better name would be <c>IsSignedDecimal</c>.
		/// </remarks>
		public static string OnlyFloat(this string? raw)
		{
			if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

			var output = new StringBuilder();
			var usedDot = false;

			var first = raw[0];
			if (first.IsFloat())
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

#pragma warning restore CS8602

		/// <returns><paramref name="str"/> with the last char removed (iff the last char is <paramref name="suffix"/>, otherwise <paramref name="str"/> unmodified)</returns>
		public static string RemoveSuffix(this string str, char suffix) => str[str.Length - 1] == suffix ? str.Substring(0, str.Length - 1) : str;

		/// <returns><paramref name="str"/> with the trailing substring <paramref name="suffix"/> removed (iff <paramref name="str"/> ends with <paramref name="suffix"/>, otherwise <paramref name="str"/> unmodified)</returns>
		public static string RemoveSuffix(this string str, string suffix) => str.EndsWith(suffix) ? str.Substring(0, str.Length - suffix.Length) : str;
	}
}
