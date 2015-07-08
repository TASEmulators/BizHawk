namespace BizHawk.Emulation.DiscSystem
{
	public static class DiscUtils
	{
		/// <summary>
		/// converts the given int to a BCD value
		/// </summary>
		public static int BCD_Byte(this int val)
		{
			byte ret = (byte)(val % 10);
			ret += (byte)(16 * (val / 10));
			return ret;
		}
	}

}