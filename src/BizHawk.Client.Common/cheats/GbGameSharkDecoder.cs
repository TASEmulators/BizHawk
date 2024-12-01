using System.Globalization;

namespace BizHawk.Client.Common.cheats
{
	public static class GbGameSharkDecoder
	{
		// Sample Input for GB/GBC:
		// 010FF6C1
		// Becomes:
		// Address C1F6
		// Value 0F
		public static IDecodeResult Decode(string code)
		{
			if (code == null)
			{
				throw new ArgumentNullException(nameof(code));
			}

			if (code.Length != 8 || code.Contains("-"))
			{
				return new InvalidCheatCode("GameShark codes must be 8 characters with no dashes.");
			}

			var test = code.Substring(0, 2);
			if (test != "00" && test != "01")
			{
				return new InvalidCheatCode("All GameShark Codes for GameBoy need to start with 00 or 01");
			}

			var result = new DecodeResult { Size = WatchSize.Byte };

			code = code.Remove(0, 2);

			var valueStr = code.Remove(2, 4);
			code = code.Remove(0, 2);

			var addrStr = code.Remove(0, 2);
			addrStr += code.Remove(2, 2);

			result.Value = int.Parse(valueStr, NumberStyles.HexNumber);
			result.Address = int.Parse(addrStr, NumberStyles.HexNumber);
			return result;
		}
	}
}
