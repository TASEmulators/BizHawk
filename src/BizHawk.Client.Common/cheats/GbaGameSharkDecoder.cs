using System.Globalization;
using System.Linq;

#pragma warning disable MA0089
namespace BizHawk.Client.Common.cheats
{
	// TODO:
	public static class GbaGameSharkDecoder
	{
		private static readonly uint[] GameSharkSeeds = { 0x09F4FBBDU, 0x9681884AU, 0x352027E9U, 0xF3DEE5A7U };
		private static readonly uint[] ProActionReplaySeeds = { 0x7AA9648FU, 0x7FAE6994U, 0xC0EFAAD5U, 0x42712C57U };

		private static string Decrypt(string code)
		{
			var op1 = uint.Parse(code.Remove(8, 9), NumberStyles.HexNumber);
			var op2 = uint.Parse(code.Remove(0, 9), NumberStyles.HexNumber);

			uint sum = 0xC6EF3720;

			// Tiny Encryption Algorithm
			for (int i = 0; i < 32; ++i)
			{
				op2 -= ((op1 << 4) + GameSharkSeeds[2]) ^ (op1 + sum) ^ ((op1 >> 5) + GameSharkSeeds[3]);
				op1 -= ((op2 << 4) + GameSharkSeeds[0]) ^ (op2 + sum) ^ ((op2 >> 5) + GameSharkSeeds[1]);
				sum -= 0x9E3779B9;
			}

			return $"{op1:X8} {op2:X8}";
		}

		// TODO: When to use this?
		private static string DecryptPro(string code)
		{
			var sum = 0xC6EF3720;
			var op1 = uint.Parse(code.Remove(8, 9), NumberStyles.HexNumber);
			var op2 = uint.Parse(code.Remove(0, 9), NumberStyles.HexNumber);

			for (int j = 0; j < 32; ++j)
			{
				op2 -= ((op1 << 4) + ProActionReplaySeeds[2]) ^ (op1 + sum) ^ ((op1 >> 5) + ProActionReplaySeeds[3]);
				op1 -= ((op2 << 4) + ProActionReplaySeeds[0]) ^ (op2 + sum) ^ ((op2 >> 5) + ProActionReplaySeeds[1]);
				sum -= 0x9E3779B9;
			}

			return $"{op1:X8} {op2:X8}";
		}

		public static IDecodeResult Decode(string code)
		{
			if (code == null)
			{
				throw new ArgumentNullException(nameof(code));
			}

			if (code.Length != 17)
			{
				code = Decrypt(code);
			}

			if (code.IndexOf(" ", StringComparison.Ordinal) != 8 || code.Length != 17) // not a redundant length check, `code` was overwritten
			{
				return new InvalidCheatCode("All GBA GameShark Codes need to be 17 characters in length with a space after the first eight.");
			}

			var result = new DecodeResult
			{
				Size = code.First() switch
				{
					'0' => WatchSize.Byte,
					'1' => WatchSize.Word,
					'2' => WatchSize.DWord,
					'3' => WatchSize.DWord,
					'6' => WatchSize.Word,
					_ => WatchSize.Byte
				}
			};

			result.Address = int.Parse(GetLast(code, (int)result.Size), NumberStyles.HexNumber);
			result.Value = int.Parse(code.Substring(1, 7), NumberStyles.HexNumber);
#if false // doing this at callsite (in unit test) for now, should we throw out the unused data here? probably affects other converters too --yoshi
			result.Value = result.Size switch
			{
				WatchSize.Byte => result.Value & 0xFF,
				WatchSize.Word => result.Value & 0xFFFF,
				_ => result.Value
			};
#endif

			return result;
		}

		private static string GetLast(string str, int length)
		{
			return length >= str.Length ? str : str.Substring(str.Length - length);
		}
	}
}
#pragma warning restore MA0089
