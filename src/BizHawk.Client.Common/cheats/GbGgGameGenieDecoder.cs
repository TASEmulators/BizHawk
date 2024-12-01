using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;

using static BizHawk.Common.StringExtensions.NumericStringExtensions;

namespace BizHawk.Client.Common.cheats
{
	/// <summary>
	/// Decodes Gameboy and Game Gear Game Genie codes
	/// </summary>
	/// <remarks><see href="https://www.devrs.com/gb/files/gg.html"/></remarks>
	public static class GbGgGameGenieDecoder
	{
		public static IDecodeResult Decode(string code)
		{
			if (code is null) throw new ArgumentNullException(nameof(code));
			const string ERR_MSG_MALFORMED = "expecting code in the format XXX-XXX or XXX-XXX-XXX";
			if (code.Length is not (7 or 11)) return new InvalidCheatCode(ERR_MSG_MALFORMED);
			const char SEP = '-';
			if (code[3] is not SEP || !(code[0].IsHex() && code[1].IsHex() && code[2].IsHex()
				&& code[4].IsHex() && code[5].IsHex() && code[6].IsHex()))
			{
				return new InvalidCheatCode(ERR_MSG_MALFORMED);
			}
			DecodeResult result = new() { Size = WatchSize.Byte };
			Span<char> toParse = stackalloc char[4];
			if (code.Length is 11)
			{
				if (code[7] is not SEP || !(code[8].IsHex() && code[9].IsHex() && code[10].IsHex()))
				{
					return new InvalidCheatCode(ERR_MSG_MALFORMED);
				}
				// code is VVA-AAA-CCC, parse the compare value and fall through
				toParse[0] = code[8];
				_ = code[9]; // "undetermined but if you XOR it with encoded nibble A the result must not be any of the values 1 through 7 or else you will receive a bad code message"
				toParse[1] = code[10];
				var compareValue = ParseU8FromHex(toParse[..2]);
				compareValue ^= 0xFF;
				NumberExtensions.RotateRightU8(ref compareValue, 2);
				result.Compare = compareValue ^ 0x45;
			}
			// else code is VVA-AAA
			toParse[0] = code[6];
			toParse[1] = code[2];
			toParse[2] = code[4];
			toParse[3] = code[5];
			result.Address = ParseU16FromHex(toParse) ^ 0xF000;
#pragma warning disable CA1846 // Span overload just calls ToString
			result.Value = ParseU8FromHex(
#if NET7_0_OR_GREATER
				code.AsSpan(start: 0, length: 2)
#else
				code.Substring(startIndex: 0, length: 2)
#endif
			);
#pragma warning restore CA1846
			return result;
		}
	}
}
