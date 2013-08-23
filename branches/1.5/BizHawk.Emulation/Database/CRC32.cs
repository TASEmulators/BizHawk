//we could get a little list of crcs from here and make it clear which crc this class was for, and expose others
//http://www.ross.net/crc/download/crc_v3.txt

namespace BizHawk
{
	public static class CRC32
	{
		// Lookup table for speed.
		private static uint[] CRC32Table;

		static CRC32()
		{
			CRC32Table = new uint[256];
			for (uint i = 0; i < 256; ++i)
			{
				uint crc = i;
				for (int j = 8; j > 0; --j)
				{
					if ((crc & 1) == 1)
						crc = ((crc >> 1) ^ 0xEDB88320);
					else
						crc >>= 1;
				}
				CRC32Table[i] = crc;
			}
		}

		public static int Calculate(byte[] data)
		{
			uint Result = 0xFFFFFFFF;
			foreach (var b in data)
				Result = (((Result) >> 8) ^ CRC32Table[b ^ ((Result) & 0xFF)]);
			return (int) ~Result;
		}
	}

	
}
