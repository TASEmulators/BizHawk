using System.Globalization;

#pragma warning disable MA0089
namespace BizHawk.Client.Common.cheats
{
	// TODO: support comparison cheat codes
	public static class N64GameSharkDecoder
	{
		public static IDecodeResult Decode(string code)
		{
			if (code == null)
			{
				throw new ArgumentNullException(nameof(code));
			}
			
			if (code.IndexOf(" ", StringComparison.Ordinal) != 8)
			{
				return new InvalidCheatCode("GameShark Codes need to contain a space after the eighth character.");
			}

			if (code.Length != 13)
			{
				return new InvalidCheatCode("GameShark Codes need to be 13 characters in length.");
			}

			switch (code.Substring(0, 2))
			{
				case "50":
				case "D0":
				case "D1":
				case "D2":
				case "D3":
					return new InvalidCheatCode("This code is not yet supported by BizHawk.");
				case "EE":
				case "DD":
				case "CC":
					return new InvalidCheatCode("This code is for Disabling the Expansion Pak. This is not allowed.");
				case "DE":
				// Single Write ON-Boot code.
				// Not Necessary?  Think so?
				case "F0":
				case "F1":
				case "2A":
				case "3C":
				case "FF":
					return new InvalidCheatCode("This code is not needed by Bizhawk.");
			}

			var s = code.Remove(0, 2);

			return new DecodeResult
			{
				Size = code.Substring(0, 2) switch
				{
					"80" => WatchSize.Byte,
					"81" => WatchSize.Word,
					"88" => WatchSize.Byte,
					"89" => WatchSize.Word,
					"A0" => WatchSize.Byte,
					"A1" => WatchSize.Word,
					_ => WatchSize.Byte,
				},
				Address = int.Parse(s.Remove(6, 5), NumberStyles.HexNumber),
				Value = int.Parse(s.Remove(0, 7), NumberStyles.HexNumber)
			};
		}
	}
}
#pragma warning restore MA0089
