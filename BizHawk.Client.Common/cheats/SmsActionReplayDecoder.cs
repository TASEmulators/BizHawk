using System;
using System.Globalization;

namespace BizHawk.Client.Common.cheats
{
	// TODO: validate string and throw
	public class SmsActionReplayDecoder
	{
		private readonly string _code;

		public SmsActionReplayDecoder(string code)
		{
			_code = code;
			Decode();
		}

		public int Address { get; private set; }
		public int Value { get; private set; }

		public void Decode()
		{
			if (_code.IndexOf("-") != 3 && _code.Length != 9)
			{
				throw new InvalidOperationException("Invalid Action Replay Code");
			}

			var parseString = _code.Remove(0, 2);
			var ramAddress = parseString.Remove(4, 2).Replace("-", "");
			var ramValue = parseString.Remove(0, 5);
			Address = int.Parse(ramAddress, NumberStyles.HexNumber);
			Value = int.Parse(ramValue, NumberStyles.HexNumber);
		}
	}
}
