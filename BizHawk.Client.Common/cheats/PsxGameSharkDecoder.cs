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
		public int ByteSize { get; private set; } = 1;

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
			ByteSize = type switch
			{
				"10" => 2,
				"11" => 2,
				"20" => 1,
				"21" => 1,
				"30" => 1,
				"80" => 2,
				"D0" => 2,
				"D1" => 2,
				"D2" => 2,
				"D3" => 2,
				"D4" => 2,
				"D5" => 2,
				"D6" => 2,
				"E0" => 1,
				"E1" => 1,
				"E2" => 1,
				"E3" => 1,
				_ => 1
			};

			var s = _code.Remove(0, 2);
			Address = int.Parse(s.Remove(6, 5), NumberStyles.HexNumber);
			Value = int.Parse(s.Remove(0, 7), NumberStyles.HexNumber);
		}
	}
}
