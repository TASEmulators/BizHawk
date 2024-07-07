using System.Globalization;

#pragma warning disable MA0089
namespace BizHawk.Client.Common.cheats
{
	// TODO: validate string and throw
	public static class GenesisActionReplayDecoder
	{
		public static IDecodeResult Decode(string code)
		{
			if (code == null)
			{
				throw new ArgumentNullException(nameof(code));
			}

			if (code.IndexOf(":", StringComparison.Ordinal) != 6)
			{
				return new InvalidCheatCode("Action Replay/Pro Action Replay Codes need to contain a colon after the sixth character.");
			}

			var parseString = code.Remove(0, 2);
			return code.Length switch
			{
				9 =>
				// Sample Code of 1-Byte:
				// FFF761:64
				// Becomes:
				// Address: F761
				// Value: 64
				new DecodeResult
				{
					Address = int.Parse(parseString.Remove(4, 3), NumberStyles.HexNumber)
					, Value = int.Parse(parseString.Remove(0, 5), NumberStyles.HexNumber)
					, Size = WatchSize.Byte
				},
				11 =>
				// Sample Code of 2-Byte:
				// FFF761:6411
				// Becomes:
				// Address: F761
				// Value: 6411
				new DecodeResult
				{
					Address = int.Parse(parseString.Remove(4, 5), NumberStyles.HexNumber)
					, Value = int.Parse(parseString.Remove(0, 5), NumberStyles.HexNumber)
					, Size = WatchSize.Word
				},
				_ => new InvalidCheatCode(
					"Action Replay/Pro Action Replay Codes need to be either 9 or 11 characters.")
			};
		}
	}
}
#pragma warning restore MA0089
