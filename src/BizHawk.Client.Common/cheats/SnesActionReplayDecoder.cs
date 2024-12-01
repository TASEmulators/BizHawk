using System.Globalization;

namespace BizHawk.Client.Common.cheats
{
	public static class SnesActionReplayDecoder
	{
		// Sample Code:
		// 7E18A428
		// Address: 7E18A4
		// Value: 28
		public static IDecodeResult Decode(string code)
		{
			if (code == null)
			{
				throw new ArgumentNullException(nameof(code));
			}

			if (code.Length != 8)
			{
				return new InvalidCheatCode("Pro Action Replay Codes must to be eight characters.");
			}

			return new DecodeResult
			{
				Size = WatchSize.Byte,
				Address = int.Parse(code.Remove(6, 2), NumberStyles.HexNumber),
				Value = int.Parse(code.Remove(0, 6), NumberStyles.HexNumber)
			};
		}
	}
}
