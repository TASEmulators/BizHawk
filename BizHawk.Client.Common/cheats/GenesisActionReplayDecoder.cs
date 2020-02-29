using System;
using System.Globalization;

namespace BizHawk.Client.Common.cheats
{
	// TODO: validate string and throw
	public class GenesisActionReplayDecoder
	{
		private readonly string _code;

		public GenesisActionReplayDecoder(string code)
		{
			_code = code;
			Decode();
		}

		public int Address { get; private set; }
		public int Value { get; private set; }
		public WatchSize Size { get; private set; } = WatchSize.Byte;

		public void Decode()
		{
			var parseString = _code.Remove(0, 2);
			switch (_code.Length)
			{
				case 9:
					// Sample Code of 1-Byte:
					// FFF761:64
					// Becomes:
					// Address: F761
					// Value: 64
					Address = int.Parse(parseString.Remove(4, 3), NumberStyles.HexNumber);
					Value = int.Parse(parseString.Remove(0, 5), NumberStyles.HexNumber);
					Size = WatchSize.Byte;
					break;
				case 11:
					// Sample Code of 2-Byte:
					// FFF761:6411
					// Becomes:
					// Address: F761
					// Value: 6411
					Address = int.Parse(parseString.Remove(4, 5), NumberStyles.HexNumber);
					Value = int.Parse(parseString.Remove(0, 5), NumberStyles.HexNumber);
					Size = WatchSize.Word;
					break;
				default:
					// We could have checked above but here is fine, since it's a quick check due to one of three possibilities.
					throw new InvalidOperationException(
						"All Genesis Action Replay/Pro Action Replay Codes need to be either 9 or 11 characters in length");
			}
		}
	}
}
