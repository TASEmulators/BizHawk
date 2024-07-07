using System.Globalization;

#pragma warning disable MA0089
namespace BizHawk.Client.Common.cheats
{
	public static class SaturnGameSharkDecoder
	{
		// Sample Input for Saturn:
		// 160949FC 0090
		// Address: 0949FC
		// Value:  90
		// Note, 3XXXXXXX are Big Endian
		// Remove first two octets
		public static IDecodeResult Decode(string code)
		{
			if (code == null)
			{
				throw new ArgumentNullException(nameof(code));
			}

			if (code.IndexOf(" ", StringComparison.Ordinal) != 8)
			{
				return new InvalidCheatCode("All Saturn GameShark Codes need to contain a space after the eighth character.");
			}

			if (code.Length != 13)
			{
				return new InvalidCheatCode("All Saturn GameShark Cheats need to be 13 characters in length.");
			}

			var result = new DecodeResult { Size = WatchSize.Word };

			//  Only the first character really matters?  16 or 36?
			var test = code.Remove(2, 11).Remove(1, 1);
			if (test == "3")
			{
				result.Size = WatchSize.Byte;
			}
			
			var s = code.Remove(0, 2);
			result.Address = int.Parse(s.Remove(6, 5), NumberStyles.HexNumber);
			result.Value = int.Parse(s.Remove(0, 7));
			return result;
		}
	}
}
#pragma warning restore MA0089
