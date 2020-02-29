using System;
using System.Globalization;
using System.Linq;

namespace BizHawk.Client.Common.cheats
{
	// TODO: 
	public class GbaGameSharkDecoder
	{
		private readonly string _code;
		private readonly uint[] _gbaGameSharkSeeds = { 0x09F4FBBDU, 0x9681884AU, 0x352027E9U, 0xF3DEE5A7U };
		private readonly uint[] _gbaProActionReplaySeeds = { 0x7AA9648FU, 0x7FAE6994U, 0xC0EFAAD5U, 0x42712C57U };

		public GbaGameSharkDecoder(string code)
		{
			if (code == null)
			{
				throw new ArgumentNullException(nameof(code));
			}

			_code = code.Length == 8 ? Decrypt(code) : code;
		}

		public int Address { get; private set; }
		public int Value { get; private set; }
		public WatchSize Size { get; private set; }

		private string Decrypt(string code)
		{
			var op1 = uint.Parse(_code.Remove(8, 9), NumberStyles.HexNumber);
			var op2 = uint.Parse(_code.Remove(0, 9), NumberStyles.HexNumber);

			uint sum = 0xC6EF3720;

			// Tiny Encryption Algorithm
			for (int i = 0; i < 32; ++i)
			{
				op2 -= ((op1 << 4) + _gbaGameSharkSeeds[2]) ^ (op1 + sum) ^ ((op1 >> 5) + _gbaGameSharkSeeds[3]);
				op1 -= ((op2 << 4) + _gbaGameSharkSeeds[0]) ^ (op2 + sum) ^ ((op2 >> 5) + _gbaGameSharkSeeds[1]);
				sum -= 0x9E3779B9;
			}

			return $"{op1:X8} {op2:X8}";
		}

		// TODO: When to use this?
		private string DecryptPro(string code)
		{
			var sum = 0xC6EF3720;
			var op1 = uint.Parse(code.Remove(8, 9), NumberStyles.HexNumber);
			var op2 = uint.Parse(code.Remove(0, 9), NumberStyles.HexNumber);

			for (int j = 0; j < 32; ++j)
			{
				op2 -= ((op1 << 4) + _gbaProActionReplaySeeds[2]) ^ (op1 + sum) ^ ((op1 >> 5) + _gbaProActionReplaySeeds[3]);
				op1 -= ((op2 << 4) + _gbaProActionReplaySeeds[0]) ^ (op2 + sum) ^ ((op2 >> 5) + _gbaProActionReplaySeeds[1]);
				sum -= 0x9E3779B9;
			}

			return $"{op1:X8} {op2:X8}";
		}

		public void Decode()
		{
			if (_code.IndexOf(" ") != 9)
			{
				throw new InvalidOperationException("All GBA GameShark Codes need to contain a space after the ninth character");
			}

			if (_code.Length != 17)
			{
				throw new InvalidOperationException("All N64 GameShark Codes need to be 17 characters in length.");
			}

			Size = _code.First() switch
			{
				'0' => WatchSize.Byte,
				'1' => WatchSize.Word,
				'2' => WatchSize.DWord,
				'3' => WatchSize.DWord,
				'6' => WatchSize.Word,
				_ => WatchSize.Byte
			};

			Address = int.Parse(GetLast(_code, (int) Size), NumberStyles.HexNumber);
			Value = int.Parse(_code.Substring(1, 8));
		}

		private string GetLast(string str, int length)
		{
			return length >= str.Length ? str : str.Substring(str.Length - length);
		}
	}
}
