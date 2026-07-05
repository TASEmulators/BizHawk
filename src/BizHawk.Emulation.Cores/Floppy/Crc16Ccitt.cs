namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// CRC-16/CCITT (polynomial 0x1021, init 0xFFFF, no reflection, no final XOR) as used by the IBM
	/// System-34 MFM (and FM) floppy track format for both the ID field and the data field. The three A1
	/// sync bytes and the address-mark byte are part of the CRC, so a caller seeds with 0xFFFF and feeds
	/// A1, A1, A1, the mark byte, then the field bytes, before comparing the trailing two CRC bytes.
	/// Part of the shared floppy disk subsystem.
	/// </summary>
	public static class Crc16Ccitt
	{
		public const ushort Init = 0xFFFF;

		/// <summary>Fold one byte into a running CRC (MSB-first).</summary>
		public static ushort Update(ushort crc, byte b)
		{
			crc ^= (ushort)(b << 8);
			for (int i = 0; i < 8; i++)
			{
				crc = (crc & 0x8000) != 0
					? (ushort)((crc << 1) ^ 0x1021)
					: (ushort)(crc << 1);
			}
			return crc;
		}

		/// <summary>Compute the CRC over a span of bytes.</summary>
		public static ushort Compute(System.ReadOnlySpan<byte> data, ushort init = Init)
		{
			ushort crc = init;
			foreach (var b in data)
				crc = Update(crc, b);
			return crc;
		}
	}
}
