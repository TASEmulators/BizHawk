using System;
using System.Globalization;

namespace BizHawk.Client.Common.cheats
{
	public class SaturnGameSharkDecoder
	{
		private readonly string _code;

		public SaturnGameSharkDecoder(string code)
		{
			_code = code;
			Decode();
		}

		public int Address { get; private set; }
		public int Value { get; private set; }
		public WatchSize Size { get; private set; } = WatchSize.Word;

		// Sample Input for Saturn:
		// 160949FC 0090
		// Address: 0949FC
		// Value:  90
		// Note, 3XXXXXXX are Big Endian
		// Remove first two octets
		public void Decode()
		{
			if (_code == null)
			{
				throw new ArgumentNullException(nameof(_code));
			}

			if (_code.IndexOf(" ") != 8)
			{
				throw new InvalidOperationException("All Saturn GameShark Codes need to contain a space after the eighth character.");
			}

			if (_code.Length != 13)
			{
				throw new InvalidOperationException("All Saturn GameShark Cheats need to be 13 characters in length.");
			}

			//  Only the first character really matters?  16 or 36?
			var test = _code.Remove(2, 11).Remove(1, 1);
			if (test == "3")
			{
				Size = WatchSize.Byte;
			}
			
			var s = _code.Remove(0, 2);
			Address = int.Parse(s.Remove(6, 5), NumberStyles.HexNumber);
			Value = int.Parse(s.Remove(0, 7));
		}
	}
}
