namespace BizHawk.Emulation.DiscSystem
{
	public static class DiscUtils
	{
		/// <summary>
		/// converts an LBA to AMSF absolute minute:second:frame format.
		/// </summary>
		public static void Convert_LBA_To_AMSF(int lba, out byte m, out byte s, out byte f)
		{
			lba += 150; //don't do this anymore
			m = (byte)(lba / 75 / 60);
			s = (byte)((lba - (m * 75 * 60)) / 75);
			f = (byte)(lba - (m * 75 * 60) - (s * 75));
		}

		// converts MSF to LBA offset
		public static int Convert_AMSF_To_LBA(byte m, byte s, byte f)
		{
			return f + (s * 75) + (m * 75 * 60) - 150;
		}
	}
}