using System;
using System.Globalization;
using System.Linq;

namespace BizHawk.Client.Common.cheats
{
	public class GbaGameSharkDecoder
	{
		private readonly string _code;

		public GbaGameSharkDecoder(string code)
		{
			_code = code;
			Decode();
		}

		public int Address { get; private set; }
		public int Value { get; private set; }
		public WatchSize Size { get; private set; }

		public void Decode()
		{
			if (_code == null)
			{
				throw new ArgumentNullException(nameof(_code));
			}
			
			if (_code.IndexOf(" ") != 9)
			{
				throw new InvalidOperationException("All GBA GameShark Codes need to contain a space after the ninth character");
			}

			if (_code.Length != 17)
			{
				throw new InvalidOperationException("All N64 GameShark Codes need to be 17 characters in length.");
			}

			Size = _code.First() switch
			{
				'0' => WatchSize.Byte,
				'1' => WatchSize.Word,
				'2' => WatchSize.DWord,
				'3' => WatchSize.DWord,
				'6' => WatchSize.Word,
				_ => WatchSize.Byte
			};


		}
	}
}
