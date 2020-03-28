using System.Drawing;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public enum VideoScreenOptions
	{
		Default, TopOnly, BottomOnly, SideBySideLR, SideBySideRL /*, Reverse */
	}

	public static class VideoScreenOptionExtensions
	{
		public static ScreenLayoutSettings ToLayout(this VideoScreenOptions option)
		{
			return option switch
			{
				VideoScreenOptions.Default => Default,
				VideoScreenOptions.TopOnly => TopOnly,
				VideoScreenOptions.BottomOnly => BottomOnly,
				VideoScreenOptions.SideBySideLR => SideBySideLR,
				VideoScreenOptions.SideBySideRL => SideBySideRL,
				_ => Default
			};
		}

		private static ScreenLayoutSettings Default => new ScreenLayoutSettings
		{
			Locations = new[] { new Point(0, 0), new Point(0, MelonDS.NativeHeight) },
			Order = new[] { 0, 1 },
			FinalSize = new Size(MelonDS.NativeWidth, MelonDS.NativeHeight * 2)
		};

		private static ScreenLayoutSettings TopOnly => new ScreenLayoutSettings
		{
			Locations = new[] { new Point(0, 0), new Point(0, MelonDS.NativeHeight) },
			Order = new[] { 0, 1 },
			FinalSize = new Size(MelonDS.NativeWidth, MelonDS.NativeHeight * 2)
		};

		private static ScreenLayoutSettings BottomOnly => new ScreenLayoutSettings
		{
			Locations = new[] { new Point(0, 0), new Point(0, MelonDS.NativeHeight) },
			Order = new[] { 0, 1 },
			FinalSize = new Size(MelonDS.NativeWidth, MelonDS.NativeHeight * 2)
		};

		private static ScreenLayoutSettings SideBySideLR => new ScreenLayoutSettings
		{
			Locations = new[] { new Point(0, 0), new Point(0, MelonDS.NativeHeight) },
			Order = new[] { 0, 1 },
			FinalSize = new Size(MelonDS.NativeWidth, MelonDS.NativeHeight * 2)
		};

		private static ScreenLayoutSettings SideBySideRL => new ScreenLayoutSettings
		{
			Locations = new[] { new Point(0, 0), new Point(0, MelonDS.NativeHeight) },
			Order = new[] { 0, 1 },
			FinalSize = new Size(MelonDS.NativeWidth, MelonDS.NativeHeight * 2)
		};

		private static ScreenLayoutSettings Reverse => new ScreenLayoutSettings
		{
			Locations = new[] { new Point(0, 0), new Point(0, MelonDS.NativeHeight) },
			Order = new[] { 0, 1 },
			FinalSize = new Size(MelonDS.NativeWidth, MelonDS.NativeHeight * 2)
		};
	}
}
