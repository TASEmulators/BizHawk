using System.Collections.Generic;

namespace BizHawk.Client.Common.cheats
{
	public static class NesGameGenieDecoder
	{
		private static readonly Dictionary<char, int> GameGenieTable = new Dictionary<char, int>
		{
			['A'] =  0,  // 0000
			['P'] =  1,  // 0001
			['Z'] =  2,  // 0010
			['L'] =  3,  // 0011
			['G'] =  4,  // 0100
			['I'] =  5,  // 0101
			['T'] =  6,  // 0110
			['Y'] =  7,  // 0111
			['E'] =  8,  // 1000
			['O'] =  9,  // 1001
			['X'] =  10, // 1010
			['U'] =  11, // 1011
			['K'] =  12, // 1100
			['S'] =  13, // 1101
			['V'] =  14, // 1110
			['N'] =  15  // 1111
		};

		public static IDecodeResult Decode(string code)
		{
			if (code == null)
			{
				throw new ArgumentNullException(nameof(code));
			}

			if (code.Length != 6 && code.Length != 8)
			{
				return new InvalidCheatCode("Game Genie codes need to be six or eight characters in length.");
			}

			var result = new DecodeResult { Size = WatchSize.Byte };
			// char 3 bit 3 denotes the code length.
			if (code.Length == 6)
			{
				// Char # |   1   |   2   |   3   |   4   |   5   |   6   |
				// Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
				// maps to|1|6|7|8|H|2|3|4|-|I|J|K|L|A|B|C|D|M|N|O|5|E|F|G|
				result.Value = 0;
				result.Address = 0x8000;

				_ = GameGenieTable.TryGetValue(code[0], out var x);
				result.Value |= x & 0x07;
				result.Value |= (x & 0x08) << 4;

				_ = GameGenieTable.TryGetValue(code[1], out x);
				result.Value |= (x & 0x07) << 4;
				result.Address |= (x & 0x08) << 4;

				_ = GameGenieTable.TryGetValue(code[2], out x);
				result.Address |= (x & 0x07) << 4;

				_ = GameGenieTable.TryGetValue(code[3], out x);
				result.Address |= (x & 0x07) << 12;
				result.Address |= x & 0x08;

				_ = GameGenieTable.TryGetValue(code[4], out x);
				result.Address |= x & 0x07;
				result.Address |= (x & 0x08) << 8;

				_ = GameGenieTable.TryGetValue(code[5], out x);
				result.Address |= (x & 0x07) << 8;
				result.Value |= x & 0x08;
			}
			else
			{
				// Char # |   1   |   2   |   3   |   4   |   5   |   6   |   7   |   8   |
				// Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
				// maps to|1|6|7|8|H|2|3|4|-|I|J|K|L|A|B|C|D|M|N|O|%|E|F|G|!|^|&|*|5|@|#|$|
				result.Value = 0;
				result.Address = 0x8000;
				result.Compare = 0;

				_ = GameGenieTable.TryGetValue(code[0], out var x);
				result.Value |= x & 0x07;
				result.Value |= (x & 0x08) << 4;

				_ = GameGenieTable.TryGetValue(code[1], out x);
				result.Value |= (x & 0x07) << 4;
				result.Address |= (x & 0x08) << 4;

				_ = GameGenieTable.TryGetValue(code[2], out x);
				result.Address |= (x & 0x07) << 4;

				_ = GameGenieTable.TryGetValue(code[3], out x);
				result.Address |= (x & 0x07) << 12;
				result.Address |= x & 0x08;

				_ = GameGenieTable.TryGetValue(code[4], out x);
				result.Address |= x & 0x07;
				result.Address |= (x & 0x08) << 8;

				_ = GameGenieTable.TryGetValue(code[5], out x);
				result.Address |= (x & 0x07) << 8;
				result.Compare |= x & 0x08;

				_ = GameGenieTable.TryGetValue(code[6], out x);
				result.Compare |= x & 0x07;
				result.Compare |= (x & 0x08) << 4;

				_ = GameGenieTable.TryGetValue(code[7], out x);
				result.Compare |= (x & 0x07) << 4;
				result.Value |= x & 0x08;
			}

			return result;
		}
	}
}
