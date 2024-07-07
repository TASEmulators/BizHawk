using System.Globalization;

#pragma warning disable MA0089
namespace BizHawk.Client.Common.cheats
{
	// TODO: cheats support comparison type, so we could support a lot more codes, by having Compare and Type properties and parsing
	public static class PsxGameSharkDecoder
	{
		// 30XXXXXX 00YY
		public static IDecodeResult Decode(string code)
		{
			if (code == null)
			{
				throw new ArgumentNullException(nameof(code));
			}

			if (code.IndexOf(" ", StringComparison.Ordinal) != 8)
			{
				return new InvalidCheatCode("All PSX GameShark Codes need to contain a space after the eighth character.");
			}

			if (code.Length != 13)
			{
				return new InvalidCheatCode("All PSX GameShark Cheats need to be 13 characters in length.");
			}

			var result = new DecodeResult();

			var type = code.Substring(0, 2);
			result.Size = type switch
			{
				"10" => WatchSize.Word,
				"11" => WatchSize.Word,
				"20" => WatchSize.Byte,
				"21" => WatchSize.Byte,
				"30" => WatchSize.Byte,
				"80" => WatchSize.Word,
				"D0" => WatchSize.Word,
				"D1" => WatchSize.Word,
				"D2" => WatchSize.Word,
				"D3" => WatchSize.Word,
				"D4" => WatchSize.Word,
				"D5" => WatchSize.Word,
				"D6" => WatchSize.Word,
				"E0" => WatchSize.Byte,
				"E1" => WatchSize.Byte,
				"E2" => WatchSize.Byte,
				"E3" => WatchSize.Byte,
				_ => WatchSize.Byte
			};

			var s = code.Remove(0, 2);
			result.Address = int.Parse(s.Remove(6, 5), NumberStyles.HexNumber);
			result.Value = int.Parse(s.Remove(0, 7), NumberStyles.HexNumber);

			return result;
		}
	}
}
#pragma warning restore MA0089
