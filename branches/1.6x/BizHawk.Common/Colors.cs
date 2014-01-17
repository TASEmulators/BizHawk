namespace BizHawk.Common
{
	public static class Colors
	{
		public static int ARGB(byte red, byte green, byte blue)
		{
			return (int)((uint)((red << 0x10) | (green << 8) | blue | (0xFF << 0x18)));
		}

		public static int ARGB(byte red, byte green, byte blue, byte alpha)
		{
			return (int)((uint)((red << 0x10) | (green << 8) | blue | (alpha << 0x18)));
		}

		public static int Luminosity(byte lum)
		{
			return (int)((uint)((lum << 0x10) | (lum << 8) | lum | (0xFF << 0x18)));
		}
	}
}
