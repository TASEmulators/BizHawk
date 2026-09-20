using System.Collections.Generic;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.Common.cheats
{
	public static class SnesGameGenieDecoder
	{
		private const string ERR_MSG_MALFORMED = "Game genie codes must be 9 characters with a format of xxyy-yyyy";

		private static void RotLeft16(ref uint value, int offset)
			=> value = ((value << offset) & 0xFFFF) | (value >> (16 - offset));

		/// <remarks>
		/// encr: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
		/// decr: 0 1 2 3 4 5 6 7 8 9 A B C D E F
		/// </remarks>
		private static readonly Dictionary<char, byte> NybbleDecodeLookup = new()
		{
			['0'] = 0x4,
			['1'] = 0x6,
			['2'] = 0xD,
			['3'] = 0xE,
			['4'] = 0x2,
			['5'] = 0x7,
			['6'] = 0x8,
			['7'] = 0x3,
			['8'] = 0xB,
			['9'] = 0x5,
			['A'] = 0xC,
			['B'] = 0x9,
			['C'] = 0xA,
			['D'] = 0x0,
			['E'] = 0xF,
			['F'] = 0x1,
		};

		public static IDecodeResult Decode(string code)
		{
			if (code == null)
			{
				throw new ArgumentNullException(nameof(code));
			}
			if (code.Length is not 9 || code[4] is not '-') return new InvalidCheatCode(ERR_MSG_MALFORMED);
			code = code.OnlyHex();
			if (code.Length is not 8) return new InvalidCheatCode(ERR_MSG_MALFORMED);

			// Code: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
			// Hex:  0 1 2 3 4 5 6 7 8 9 A B C D E F
			// XXYY-YYYY, where XX is the value, and YY-YYYY is the address.
			// Char # |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |
			// Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
			// maps to|     Value     |i|j|k|l|q|r|s|t|o|p|a|b|c|d|u|v|w|x|e|f|g|h|m|n|
			// order  |     Value     |a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|
			var addrBitsIJKL = (uint) NybbleDecodeLookup[code[2]];
			var addrBitsQRST = (uint) NybbleDecodeLookup[code[3]];
			var bunchaBits = unchecked((uint) ((NybbleDecodeLookup[code[4]] << 12)
				| (NybbleDecodeLookup[code[5]] << 8)
				| (NybbleDecodeLookup[code[6]] << 4)
				| NybbleDecodeLookup[code[7]]));
			RotLeft16(ref bunchaBits, 2);
			var addr = ((bunchaBits & 0xF000U) << 8) // offset 12 to 20
				| ((bunchaBits & 0x00F0U) << 12) // offset 4 to 16
				| (addrBitsIJKL << 12)
				| ((bunchaBits & 0x000FU) << 8) // offset 0 to 8
				| (addrBitsQRST << 4)
				| ((bunchaBits & 0x0F00U) >> 8); // offset 8 to 0
			return new DecodeResult
			{
				Address = unchecked((int) addr),
				Size = WatchSize.Byte,
				Value = (NybbleDecodeLookup[code[0]] << 4) | NybbleDecodeLookup[code[1]],
			};
		}
	}
}
