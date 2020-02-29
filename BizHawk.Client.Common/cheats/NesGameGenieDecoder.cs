using System.Collections.Generic;

namespace BizHawk.Client.Common.cheats
{
	public class NesGameGenieDecoder
	{
		private readonly string _code;

		private readonly Dictionary<char, int> _gameGenieTable = new Dictionary<char, int>
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

		public NesGameGenieDecoder(string code)
		{
			_code = code;
			Decode();
		}

		public int Address { get; private set; }
		public int Value { get; private set; }
		public int? Compare { get; private set; }

		public void Decode()
		{
			// char 3 bit 3 denotes the code length.
			if (_code.Length == 6)
			{
				// Char # |   1   |   2   |   3   |   4   |   5   |   6   |
				// Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
				// maps to|1|6|7|8|H|2|3|4|-|I|J|K|L|A|B|C|D|M|N|O|5|E|F|G|
				Value = 0;
				Address = 0x8000;

				_gameGenieTable.TryGetValue(_code[0], out var x);
				Value |= x & 0x07;
				Value |= (x & 0x08) << 4;

				_gameGenieTable.TryGetValue(_code[1], out x);
				Value |= (x & 0x07) << 4;
				Address |= (x & 0x08) << 4;

				_gameGenieTable.TryGetValue(_code[2], out x);
				Address |= (x & 0x07) << 4;

				_gameGenieTable.TryGetValue(_code[3], out x);
				Address |= (x & 0x07) << 12;
				Address |= x & 0x08;

				_gameGenieTable.TryGetValue(_code[4], out x);
				Address |= x & 0x07;
				Address |= (x & 0x08) << 8;

				_gameGenieTable.TryGetValue(_code[5], out x);
				Address |= (x & 0x07) << 8;
				Value |= x & 0x08;
			}
			else if (_code.Length == 8)
			{
				// Char # |   1   |   2   |   3   |   4   |   5   |   6   |   7   |   8   |
				// Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
				// maps to|1|6|7|8|H|2|3|4|-|I|J|K|L|A|B|C|D|M|N|O|%|E|F|G|!|^|&|*|5|@|#|$|
				Value = 0;
				Address = 0x8000;
				Compare = 0;

				_gameGenieTable.TryGetValue(_code[0], out var x);
				Value |= x & 0x07;
				Value |= (x & 0x08) << 4;

				_gameGenieTable.TryGetValue(_code[1], out x);
				Value |= (x & 0x07) << 4;
				Address |= (x & 0x08) << 4;

				_gameGenieTable.TryGetValue(_code[2], out x);
				Address |= (x & 0x07) << 4;

				_gameGenieTable.TryGetValue(_code[3], out x);
				Address |= (x & 0x07) << 12;
				Address |= x & 0x08;

				_gameGenieTable.TryGetValue(_code[4], out x);
				Address |= x & 0x07;
				Address |= (x & 0x08) << 8;

				_gameGenieTable.TryGetValue(_code[5], out x);
				Address |= (x & 0x07) << 8;
				Compare |= x & 0x08;

				_gameGenieTable.TryGetValue(_code[6], out x);
				Compare |= x & 0x07;
				Compare |= (x & 0x08) << 4;

				_gameGenieTable.TryGetValue(_code[7], out x);
				Compare |= (x & 0x07) << 4;
				Value |= x & 0x08;
			}
		}
	}
}
