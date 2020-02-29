using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common.cheats
{
	/// <summary>
	/// Decodes Gameboy and Game Gear Game Genie codes
	/// </summary>
	public class GbGgGameGenieDecoder
	{
		private readonly string _code;

		private readonly Dictionary<char, int> _gbGgGameGenieTable = new Dictionary<char, int>
		{
			['0'] = 0,
			['1'] = 1,
			['2'] = 2,
			['3'] = 3,
			['4'] = 4,
			['5'] = 5,
			['6'] = 6,
			['7'] = 7,
			['8'] = 8,
			['9'] = 9,
			['A'] = 10,
			['B'] = 11,
			['C'] = 12,
			['D'] = 13,
			['E'] = 14,
			['F'] = 15
		};

		public GbGgGameGenieDecoder(string code)
		{
			_code = code;
			Decode();
		}

		public int Address { get; private set; }
		public int Value { get; private set; }
		public int? Compare { get; private set; }

		public void Decode()
		{
			// No cypher on value
			// Char # |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |   8   |
			// Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
			// maps to|      Value    |A|B|C|D|E|F|G|H|I|J|K|L|XOR 0xF|a|b|c|c|NotUsed|e|f|g|h|
			// proper |      Value    |XOR 0xF|A|B|C|D|E|F|G|H|I|J|K|L|g|h|a|b|Nothing|c|d|e|f|
			int x;

			// Getting Value
			if (_code.Length > 0)
			{
				_gbGgGameGenieTable.TryGetValue(_code[0], out x);
				Value = x << 4;
			}

			if (_code.Length > 1)
			{
				_gbGgGameGenieTable.TryGetValue(_code[1], out x);
				Value |= x;
			}

			// Address
			if (_code.Length > 2)
			{
				_gbGgGameGenieTable.TryGetValue(_code[2], out x);
				Value = x << 8;
			}
			else
			{
				Value = -1;
			}

			if (_code.Length > 3)
			{
				_gbGgGameGenieTable.TryGetValue(_code[3], out x);
				Address |= x << 4;
			}

			if (_code.Length > 4)
			{
				_gbGgGameGenieTable.TryGetValue(_code[4], out x);
				Address |= x;
			}

			if (_code.Length > 5)
			{
				_gbGgGameGenieTable.TryGetValue(_code[5], out x);
				Address |= (x ^ 0xF) << 12;
			}

			// compare need to be full
			if (_code.Length > 8)
			{
				_gbGgGameGenieTable.TryGetValue(_code[6], out x);
				var comp = x << 2;

				// 8th character ignored
				_gbGgGameGenieTable.TryGetValue(_code[8], out x);
				comp |= (x & 0xC) >> 2;
				comp |= (x & 0x3) << 6;
				Compare = comp ^ 0xBA;
			}
		}
	}
}
