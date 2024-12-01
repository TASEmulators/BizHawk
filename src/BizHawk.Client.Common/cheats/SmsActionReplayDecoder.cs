using System.Globalization;

#pragma warning disable MA0089
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

			var result = new DecodeResult { Size = WatchSize.Byte };

			var s = code.Remove(0, 2);
			var ramAddress = s.Remove(4, 2).Replace("-", "");
			var ramValue = s.Remove(0, 5);
			result.Address = int.Parse(ramAddress, NumberStyles.HexNumber);
			result.Value = int.Parse(ramValue, NumberStyles.HexNumber);
			return result;
		}
	}
}
#pragma warning restore MA0089
