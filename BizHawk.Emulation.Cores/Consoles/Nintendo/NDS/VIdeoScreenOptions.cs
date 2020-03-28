using System.Drawing;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public enum VideoScreenOptions
	{
		Default, TopOnly, SideBySideLR, SideBySideRL /*, Reverse */
	}

	public static class VideoScreenOptionExtensions
	{
		public static Point? TouchScreenStart(this VideoScreenOptions option)
		{
			switch (option)
			{
				default:
					return new Point(0, MelonDS.NativeHeight);
				case VideoScreenOptions.TopOnly:
					return null;
				case VideoScreenOptions.SideBySideLR:
					return new Point(MelonDS.NativeWidth, 0);
				case VideoScreenOptions.SideBySideRL:
					return new Point(0, 0);
			}
		}

		public static int Width(this VideoScreenOptions option)
		{
			switch (option)
			{
				default:
					return MelonDS.NativeWidth;
				case VideoScreenOptions.SideBySideLR:
				case VideoScreenOptions.SideBySideRL:
					return MelonDS.NativeWidth * 2;
			}
		}

		// TODO: padding
		public static int Height(this VideoScreenOptions option)
		{
			switch (option)
			{
				default:
					return MelonDS.NativeHeight * 2;
				case VideoScreenOptions.TopOnly:
				case VideoScreenOptions.SideBySideLR:
				case VideoScreenOptions.SideBySideRL:
					return MelonDS.NativeHeight;
			}
		}
	}
}
