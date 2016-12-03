using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.ComponentModel;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES : ISettable<NES.NESSettings, NES.NESSyncSettings>
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
			CoreComm.ScreenLogicalOffsetY = Region == DisplayType.NTSC ? Settings.NTSC_TopLine : Settings.PAL_TopLine;

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

			public List<byte> InitialWRamStatePattern = null;

			public NESSyncSettings Clone()
			{
				var ret = (NESSyncSettings)MemberwiseClone();
				ret.BoardProperties = new Dictionary<string, string>(BoardProperties);
				ret.Controls = Controls.Clone();
				ret.VSDipswitches = VSDipswitches.Clone();
				return ret;
			}

			public static bool NeedsReboot(NESSyncSettings x, NESSyncSettings y)
			{
				return !(Util.DictionaryEqual(x.BoardProperties, y.BoardProperties)
					&& x.RegionOverride == y.RegionOverride
					&& !NESControlSettings.NeedsReboot(x.Controls, y.Controls)
					&& ((x.InitialWRamStatePattern ?? new List<byte>()).SequenceEqual(y.InitialWRamStatePattern ?? new List<byte>()))
					&& x.VSDipswitches.Equals(y.VSDipswitches));
			}

			public class VSDipswitchSettings
			{
				public bool Dip_Switch_1 { get; set; }
				public bool Dip_Switch_2 { get; set; }
				public bool Dip_Switch_3 { get; set; }
				public bool Dip_Switch_4 { get; set; }
				public bool Dip_Switch_5 { get; set; }
				public bool Dip_Switch_6 { get; set; }
				public bool Dip_Switch_7 { get; set; }
				public bool Dip_Switch_8 { get; set; }

				public VSDipswitchSettings Clone()
				{
					return (VSDipswitchSettings)MemberwiseClone();
				}

				public override bool Equals(object obj)
				{
					if (obj == null)
					{
						return false;
					}

					if (obj is VSDipswitchSettings)
					{
						var settings = obj as VSDipswitchSettings;
						return Dip_Switch_1 == settings.Dip_Switch_1
							&& Dip_Switch_2 == settings.Dip_Switch_2
							&& Dip_Switch_3 == settings.Dip_Switch_3
							&& Dip_Switch_4 == settings.Dip_Switch_4
							&& Dip_Switch_5 == settings.Dip_Switch_5
							&& Dip_Switch_6 == settings.Dip_Switch_6
							&& Dip_Switch_7 == settings.Dip_Switch_7
							&& Dip_Switch_8 == settings.Dip_Switch_8;
					}

					return base.Equals(obj);
				}

				public override int GetHashCode()
				{
					return base.GetHashCode();
				}
			}

			public VSDipswitchSettings VSDipswitches = new VSDipswitchSettings();
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

			public byte[,] Palette;

			public int Square1 = 376;
			public int Square2 = 376;
			public int Triangle = 426;
			public int Noise = 247;
			public int DMC = 167;

			public NESSettings Clone()
			{
				var ret = (NESSettings)MemberwiseClone();
				ret.Palette = (byte[,])ret.Palette.Clone();
				return ret;
			}

			public NESSettings()
			{
				Palette = (byte[,])Palettes.QuickNESPalette.Clone();
			}

			[Newtonsoft.Json.JsonConstructor]
			public NESSettings(byte[,] Palette)
			{
				if (Palette == null)
					// only needed for SVN purposes
					// edit: what does this mean?
					this.Palette = (byte[,])Palettes.QuickNESPalette.Clone();
				else
					this.Palette = Palette;
			}
		}
	}
}
