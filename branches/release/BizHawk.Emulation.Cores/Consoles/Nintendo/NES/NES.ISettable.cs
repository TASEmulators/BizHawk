using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES : IStatable
	{
		public NESSettings GetSettings()
		{
			return Settings.Clone();
		}

		public NESSyncSettings GetSyncSettings()
		{
			return SyncSettings.Clone();
		}

		public bool PutSettings(NESSettings o)
		{
			Settings = o;
			if (Settings.ClipLeftAndRight)
			{
				videoProvider.left = 8;
				videoProvider.right = 247;
			}
			else
			{
				videoProvider.left = 0;
				videoProvider.right = 255;
			}

			CoreComm.ScreenLogicalOffsetX = videoProvider.left;
			CoreComm.ScreenLogicalOffsetY = DisplayType == DisplayType.NTSC ? Settings.NTSC_TopLine : Settings.PAL_TopLine;

			SetPalette(Settings.Palette);

			apu.Square1V = Settings.Square1;
			apu.Square2V = Settings.Square2;
			apu.TriangleV = Settings.Triangle;
			apu.NoiseV = Settings.Noise;
			apu.DMCV = Settings.DMC;

			return false;
		}

		public bool PutSyncSettings(NESSyncSettings o)
		{
			bool ret = NESSyncSettings.NeedsReboot(SyncSettings, o);
			SyncSettings = o;
			return ret;
		}

		internal NESSettings Settings = new NESSettings();
		internal NESSyncSettings SyncSettings = new NESSyncSettings();

		public class NESSyncSettings
		{
			public Dictionary<string, string> BoardProperties = new Dictionary<string, string>();

			public enum Region
			{
				Default,
				NTSC,
				PAL,
				Dendy
			};

			public Region RegionOverride = Region.Default;

			public NESControlSettings Controls = new NESControlSettings();

			public NESSyncSettings Clone()
			{
				var ret = (NESSyncSettings)MemberwiseClone();
				ret.BoardProperties = new Dictionary<string, string>(BoardProperties);
				ret.Controls = Controls.Clone();
				return ret;
			}

			public static bool NeedsReboot(NESSyncSettings x, NESSyncSettings y)
			{
				return !(Util.DictionaryEqual(x.BoardProperties, y.BoardProperties) &&
					x.RegionOverride == y.RegionOverride &&
					!NESControlSettings.NeedsReboot(x.Controls, y.Controls));
			}
		}

		public class NESSettings
		{
			public bool AllowMoreThanEightSprites = false;
			public bool ClipLeftAndRight = false;
			public bool DispBackground = true;
			public bool DispSprites = true;
			public int BackgroundColor = 0;

			public int NTSC_TopLine = 8;
			public int NTSC_BottomLine = 231;
			public int PAL_TopLine = 0;
			public int PAL_BottomLine = 239;

			public int[,] Palette;

			public int Square1 = 376;
			public int Square2 = 376;
			public int Triangle = 426;
			public int Noise = 247;
			public int DMC = 167;

			public NESSettings Clone()
			{
				var ret = (NESSettings)MemberwiseClone();
				ret.Palette = (int[,])ret.Palette.Clone();
				return ret;
			}

			public NESSettings()
			{
				Palette = (int[,])Palettes.QuickNESPalette.Clone();
			}

			[Newtonsoft.Json.JsonConstructor]
			public NESSettings(int[,] Palette)
			{
				if (Palette == null)
					// only needed for SVN purposes
					this.Palette = (int[,])Palettes.QuickNESPalette.Clone();
				else
					this.Palette = Palette;
			}
		}
	}
}
