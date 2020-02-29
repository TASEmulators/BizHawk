using System.Collections.Generic;

namespace BizHawk.Client.Common.cheats
{
	public class SnesGameGenieDecoder
	{
		private readonly string _code;

		// including transposition
		// Code: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
		// Hex:  0 1 2 3 4 5 6 7 8 9 A B C D E F
		// This only applies to the SNES
		private readonly Dictionary<char, int> _snesGameGenieTable = new Dictionary<char, int>
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

		public SnesGameGenieDecoder(string code)
		{
			_code = code?.Replace("-", "") ?? "";
			Decode();
		}

		public int Address { get; private set; }
		public int Value { get; private set; }

		public void Decode()
		{
			// Code: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
			// Hex:  0 1 2 3 4 5 6 7 8 9 A B C D E F
			// XXYY-YYYY, where XX is the value, and YY-YYYY is the address.
			// Char # |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |
			// Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
			// maps to|     Value     |i|j|k|l|q|r|s|t|o|p|a|b|c|d|u|v|w|x|e|f|g|h|m|n|
			// order  |     Value     |a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|
			int x;

			// Getting Value
			if (_code.Length > 0)
			{
				_snesGameGenieTable.TryGetValue(_code[0], out x);
				Value = x << 4;
			}

			if (_code.Length > 1)
			{
				_snesGameGenieTable.TryGetValue(_code[1], out x);
				Value |= x;
			}

			// Address
			if (_code.Length > 2)
			{
				_snesGameGenieTable.TryGetValue(_code[2], out x);
				Address = x << 12;
			}

			if (_code.Length > 3)
			{
				_snesGameGenieTable.TryGetValue(_code[3], out x);
				Address |= x << 4;
			}

			if (_code.Length > 4)
			{
				_snesGameGenieTable.TryGetValue(_code[4], out x);
				Address |= (x & 0xC) << 6;
				Address |= (x & 0x3) << 22;
			}

			if (_code.Length > 5)
			{
				_snesGameGenieTable.TryGetValue(_code[5], out x);
				Address |= (x & 0xC) << 18;
				Address |= (x & 0x3) << 2;
			}

			if (_code.Length > 6)
			{
				_snesGameGenieTable.TryGetValue(_code[6], out x);
				Address |= (x & 0xC) >> 2;
				Address |= (x & 0x3) << 18;
			}

			if (_code.Length > 7)
			{
				_snesGameGenieTable.TryGetValue(_code[7], out x);
				Address |= (x & 0xC) << 14;
				Address |= (x & 0x3) << 10;
			}
		}
	}
}
