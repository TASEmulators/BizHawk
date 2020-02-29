using System;
using System.Globalization;

namespace BizHawk.Client.Common.cheats
{
	// TODO: cheats support comparison type, so we could support a lot more codes, by having Compare and Type properties and parsing
	public class PsxGameSharkDecoder
	{
		private readonly string _code;

		public PsxGameSharkDecoder(string code)
		{
			_code = code;
			Decode();
		}

		public int Address { get; private set; }
		public int Value { get; private set; }

		public WatchSize Size { get; private set; } = WatchSize.Word;

		// 30XXXXXX 00YY
		public void Decode()
		{
			if (_code == null)
			{
				throw new ArgumentNullException(nameof(_code));
			}

			if (_code.IndexOf(" ") != 8)
			{
				throw new InvalidOperationException("All PSX GameShark Codes need to contain a space after the eighth character.");
			}

			if (_code.Length != 13)
			{
				throw new InvalidOperationException("All PSX GameShark Cheats need to be 13 characters in length.");
			}

			var type = _code.Substring(0, 2);
			Size = type switch
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

			var s = _code.Remove(0, 2);
			Address = int.Parse(s.Remove(6, 5), NumberStyles.HexNumber);
			Value = int.Parse(s.Remove(0, 7), NumberStyles.HexNumber);
		}
	}
}
