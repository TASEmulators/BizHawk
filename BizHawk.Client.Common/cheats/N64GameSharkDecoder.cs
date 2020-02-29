using System;
using System.Globalization;

namespace BizHawk.Client.Common.cheats
{
	public class N64GameSharkDecoder
	{
		private readonly string _code;

		public N64GameSharkDecoder(string code)
		{
			_code = code;
			Decode();
		}

		public int Address { get; private set; }
		public int Value { get; private set; }
		public WatchSize Size { get; private set; } = WatchSize.Byte;

		public void Decode()
		{
			if (_code == null)
			{
				throw new ArgumentNullException(nameof(_code));
			}
			
			if (_code.IndexOf(" ") != 8)
			{
				throw new InvalidOperationException("All N64 GameShark Codes need to contain a space after the eighth character");
			}

			if (_code.Length != 13)
			{
				throw new InvalidOperationException("All N64 GameShark Codes need to be 13 characters in length.");
			}

			Size = _code.Substring(0, 2) switch
			{
				"80" => WatchSize.Byte,
				"81" => WatchSize.Word,
				"88" => WatchSize.Byte,
				"89" => WatchSize.Word,
				"A0" => WatchSize.Byte,
				"A1" => WatchSize.Word,
				_ => WatchSize.Byte,
			};

			var s = _code.Remove(0, 2);
			Address = int.Parse(s.Remove(6, 5), NumberStyles.HexNumber);
			Value = int.Parse(s.Remove(0, 7), NumberStyles.HexNumber);
		}
	}
}
