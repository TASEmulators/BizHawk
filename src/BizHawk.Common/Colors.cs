namespace BizHawk.Common
{
#pragma warning disable MA0104 // unlikely to conflict with System.Windows.Media.Colors
	public static class Colors
#pragma warning restore MA0104
	{
		/// <remarks>This is just <c>Color.FromArgb(alpha, red, green, blue).ToArgb()</c> with extra steps.</remarks>
		public static int ARGB(byte red, byte green, byte blue, byte alpha = 0xFF) => unchecked((int) ((uint) (alpha << 24) | (uint) (red << 16) | (uint) (green << 8) | blue));

#if false
		public static int Luminosity(byte lum) => ARGB(lum, lum, lum);
#endif
	}
}
