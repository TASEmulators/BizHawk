using System.Collections.Generic;

namespace BizHawk.Client.Common.cheats
{
	public static class SnesGameGenieDecoder
	{
		// including transposition
		// Code: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
		// Hex:  0 1 2 3 4 5 6 7 8 9 A B C D E F
		// This only applies to the SNES
		private static readonly Dictionary<char, int> SNESGameGenieTable = new Dictionary<char, int>
		{
			['D'] = 0,  // 0000
			['F'] = 1,  // 0001
			['4'] = 2,  // 0010
			['7'] = 3,  // 0011
			['0'] = 4,  // 0100
			['9'] = 5,  // 0101
			['1'] = 6,  // 0110
			['5'] = 7,  // 0111
			['6'] = 8,  // 1000
			['B'] = 9,  // 1001
			['C'] = 10, // 1010
			['8'] = 11, // 1011
			['A'] = 12, // 1100
			['2'] = 13, // 1101
			['3'] = 14, // 1110
			['E'] = 15  // 1111
		};

		public static IDecodeResult Decode(string code)
		{
			if (code == null)
			{
				throw new ArgumentNullException(nameof(code));
			}

			if (!code.Contains("-") && code.Length != 9)
			{
				return new InvalidCheatCode("Game genie codes must be 9 characters with a format of xxyy-yyyy");
			}

			// Code: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
			// Hex:  0 1 2 3 4 5 6 7 8 9 A B C D E F
			// XXYY-YYYY, where XX is the value, and YY-YYYY is the address.
			// Char # |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |
			// Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
			// maps to|     Value     |i|j|k|l|q|r|s|t|o|p|a|b|c|d|u|v|w|x|e|f|g|h|m|n|
			// order  |     Value     |a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|
			var result = new DecodeResult { Size = WatchSize.Byte };

			int x;

			// Value
			if (code.Length > 0)
			{
				_ = SNESGameGenieTable.TryGetValue(code[0], out x);
				result.Value = x << 4;
			}

			if (code.Length > 1)
			{
				_ = SNESGameGenieTable.TryGetValue(code[1], out x);
				result.Value |= x;
			}

			// Address
			if (code.Length > 2)
			{
				_ = SNESGameGenieTable.TryGetValue(code[2], out x);
				result.Address = x << 12;
			}

			if (code.Length > 3)
			{
				_ = SNESGameGenieTable.TryGetValue(code[3], out x);
				result.Address |= x << 4;
			}

			if (code.Length > 4)
			{
				_ = SNESGameGenieTable.TryGetValue(code[4], out x);
				result.Address |= (x & 0xC) << 6;
				result.Address |= (x & 0x3) << 22;
			}

			if (code.Length > 5)
			{
				_ = SNESGameGenieTable.TryGetValue(code[5], out x);
				result.Address |= (x & 0xC) << 18;
				result.Address |= (x & 0x3) << 2;
			}

			if (code.Length > 6)
			{
				_ = SNESGameGenieTable.TryGetValue(code[6], out x);
				result.Address |= (x & 0xC) >> 2;
				result.Address |= (x & 0x3) << 18;
			}

			if (code.Length > 7)
			{
				_ = SNESGameGenieTable.TryGetValue(code[7], out x);
				result.Address |= (x & 0xC) << 14;
				result.Address |= (x & 0x3) << 10;
			}

			return result;
		}
	}
}
