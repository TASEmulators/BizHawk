using System.Globalization;

namespace BizHawk.Client.Common.cheats
{
	public class GbGameSharkDecoder
	{
		private readonly string _code;

		public GbGameSharkDecoder(string code)
		{
			_code = code;
			Decode();
		}

		public int Address { get; private set; }
		public int Value { get; private set; }

		// Sample Input for GB/GBC:
		// 010FF6C1
		// Becomes:
		// Address C1F6
		// Value 0F
		public void Decode()
		{
			string code = _code.Remove(0, 2);

			var valueStr = code.Remove(2, 4);
			code = code.Remove(0, 2);

			var addrStr = code.Remove(0, 2);
			addrStr = addrStr + code.Remove(2, 2);

			Value = int.Parse(valueStr, NumberStyles.HexNumber);
			Address = int.Parse(addrStr, NumberStyles.HexNumber);
		}
	}
}
