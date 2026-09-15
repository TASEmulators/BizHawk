namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// Standard CRC-32/ISO-HDLC (the common zlib CRC32): reflected polynomial 0xEDB88320, initial value
	/// 0xFFFFFFFF, final XOR 0xFFFFFFFF. Used to verify IPF records and their data blocks.
	/// </summary>
	public static class Crc32Iso
	{
		private static readonly uint[] Table = BuildTable();

		private static uint[] BuildTable()
		{
			var t = new uint[256];
			for (uint i = 0; i < 256; i++)
			{
				uint c = i;
				for (int k = 0; k < 8; k++)
					c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
				t[i] = c;
			}
			return t;
		}

		public static uint Compute(byte[] data, int offset, int length)
		{
			uint crc = 0xFFFFFFFF;
			int end = offset + length;
			for (int i = offset; i < end; i++)
				crc = Table[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
			return crc ^ 0xFFFFFFFF;
		}

		/// <summary>
		/// CRC over a record whose own 4-byte CRC field (at crcFieldOffset) is treated as zero, matching how
		/// IPF stores the value (computed with the field cleared, then written back).
		/// </summary>
		public static uint ComputeWithZeroedField(byte[] data, int offset, int length, int crcFieldOffset)
		{
			uint crc = 0xFFFFFFFF;
			int end = offset + length;
			for (int i = offset; i < end; i++)
			{
				byte b = i >= crcFieldOffset && i < crcFieldOffset + 4 ? (byte)0 : data[i];
				crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
			}
			return crc ^ 0xFFFFFFFF;
		}
	}
}
