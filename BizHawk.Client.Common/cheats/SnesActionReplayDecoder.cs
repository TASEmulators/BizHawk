using System;
using System.Globalization;

namespace BizHawk.Client.Common.cheats
{
	public class SnesActionReplayDecoder
	{
		private readonly string _code;

		public SnesActionReplayDecoder(string code)
		{
			_code = code;
			Decode();
		}

		public int Address { get; private set; }
		public int Value { get; private set; }
		public int ByteSize => 2;

		// Sample Code:
		// 7E18A428
		// Address: 7E18A4
		// Value: 28
		public void Decode()
		{
			if (_code.Length != 8)
			{
				throw new InvalidOperationException("Pro Action Replay Codes need to be eight characters in length.");
			}

			Address = int.Parse(_code.Remove(6, 2), NumberStyles.HexNumber);
			Value = int.Parse(_code.Remove(0, 6), NumberStyles.HexNumber);
		}
	}
}
