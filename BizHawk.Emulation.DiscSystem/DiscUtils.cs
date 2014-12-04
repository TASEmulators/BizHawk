namespace BizHawk.Emulation.DiscSystem
{
	public static class DiscUtils
	{
		/// <summary>
		/// converts the given byte to a BCD value
		/// </summary>
		public static byte BCD_Byte(this byte val)
		{
			byte ret = (byte)(val % 10);
			ret += (byte)(16 * (val / 10));
			return ret;
		}
	}

}