using System;
using System.Globalization;

namespace BizHawk.Client.Common.cheats
{
	public static class SmsActionReplayDecoder
	{
		public static IDecodeResult Decode(string code)
		{
			if (code == null)
			{
				throw new ArgumentNullException(nameof(code));
			}

			if (code.IndexOf("-", StringComparison.Ordinal) != 3 && code.Length != 9)
			{
				return new InvalidCheatCode("Action Replay Codes must be 9 characters with a dash after the third character");
			}

			DecodeResult result = new() { Size = WatchSize.Byte };

			string s = code.Remove(0, 2);
			string ramAddress = s.Remove(4, 2).Replace("-", "");
			string ramValue = s.Remove(0, 5);
			result.Address = int.Parse(ramAddress, NumberStyles.HexNumber);
			result.Value = int.Parse(ramValue, NumberStyles.HexNumber);
			return result;
		}
	}
}
