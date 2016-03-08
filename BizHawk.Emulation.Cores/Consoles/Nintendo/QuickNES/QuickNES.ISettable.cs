using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using Newtonsoft.Json;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public partial class QuickNES : ISettable<QuickNES.QuickNESSettings, QuickNES.QuickNESSyncSettings>
	{
		public QuickNESSettings GetSettings()
		{
			return _settings.Clone();
		}

		public QuickNESSyncSettings GetSyncSettings()
		{
			return _syncSettingsNext.Clone();
		}

		public bool PutSettings(QuickNESSettings o)
		{
			_settings = o;
			QN.qn_set_sprite_limit(Context, _settings.NumSprites);
			RecalculateCrops();
			CalculatePalette();

			CoreComm.ScreenLogicalOffsetX = o.ClipLeftAndRight ? 8 : 0;
			CoreComm.ScreenLogicalOffsetY = o.ClipTopAndBottom ? 8 : 0;

			return false;
		}

		public bool PutSyncSettings(QuickNESSyncSettings o)
		{
			bool ret = QuickNESSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettingsNext = o;
			return ret;
		}

		private QuickNESSettings _settings;

		/// <summary>
		/// the syncsettings that this run of emulation is using (was passed to ctor)
		/// </summary>
		private QuickNESSyncSettings _syncSettings;

		/// <summary>
		/// the syncsettings that were requested but won't be used yet
		/// </summary>
		private QuickNESSyncSettings _syncSettingsNext;

		public class QuickNESSettings
		{
			[DefaultValue(8)]
			[Description("Set the number of sprites visible per line.  0 hides all sprites, 8 behaves like a normal NES, and 64 is maximum.")]
			[DisplayName("Visible Sprites")]
			public int NumSprites
			{
				get { return _NumSprites; }
				set { _NumSprites = Math.Min(64, Math.Max(0, value)); }
			}

			[JsonIgnore]
			private int _NumSprites;

			[DefaultValue(false)]
			[Description("Clip the left and right 8 pixels of the display, which sometimes contain nametable garbage.")]
			[DisplayName("Clip Left and Right")]
			public bool ClipLeftAndRight { get; set; }

			[DefaultValue(true)]
			[Description("Clip the top and bottom 8 pixels of the display, which sometimes contain nametable garbage.")]
			[DisplayName("Clip Top and Bottom")]
			public bool ClipTopAndBottom { get; set; }

			[Browsable(false)]
			public byte[] Palette
			{
				get { return _Palette; }
				set
				{
					if (value == null)
						throw new ArgumentNullException();
					else if (value.Length == 64 * 8 * 3)
						_Palette = value;
					else
						throw new ArgumentOutOfRangeException();
				}
			}

			[JsonIgnore]
			private byte[] _Palette;

			public QuickNESSettings Clone()
			{
				var ret = (QuickNESSettings)MemberwiseClone();
				ret._Palette = (byte[])_Palette.Clone();
				return ret;
			}

			public QuickNESSettings()
			{
				SettingsUtil.SetDefaultValues(this);
				SetDefaultColors();
			}

			public void SetNesHawkPalette(byte[,] pal)
			{
				//TODO - support 512 color palettes
				int nColors = pal.GetLength(0);
				int nElems =  pal.GetLength(1);
				if (!(nColors == 64 || nColors == 512) || nElems != 3)
				{
					throw new ArgumentOutOfRangeException();
				}

				if (nColors == 512)
				{
					//just copy the palette directly
					for (int c = 0; c < nColors; c++)
					{
						_Palette[c * 3 + 0] = pal[c, 0];
						_Palette[c * 3 + 1] = pal[c, 1];
						_Palette[c * 3 + 2] = pal[c, 2];
					}
				}
				else
				{
					//use quickNES's deemph calculator
					for (int c = 0; c < 64; c++)
					{
						int a = c & 63;
						byte[] inp = { (byte)pal[a, 0], (byte)pal[a, 1], (byte)pal[a, 2] };
						byte[] outp = new byte[3];
						Nes_NTSC_Colors.Emphasis(inp, outp, c);
						_Palette[c * 3] = outp[0];
						_Palette[c * 3 + 1] = outp[1];
						_Palette[c * 3 + 2] = outp[2];
					}
				}
			}

			private static byte[] GetDefaultColors()
			{
				IntPtr src = QN.qn_get_default_colors();
				byte[] ret = new byte[1536];
				Marshal.Copy(src, ret, 0, 1536);
				return ret;
			}

			public void SetDefaultColors()
			{
				_Palette = GetDefaultColors();
			}
		}

		public class QuickNESSyncSettings
		{
			[DefaultValue(true)]
			[DisplayName("Left Port Connected")]
			[Description("Specifies whether or not the Left (Player 1) Controller is connected")]
			public bool LeftPortConnected { get; set; }

			[DefaultValue(false)]
			[DisplayName("Right Port Connected")]
			[Description("Specifies whether or not the Right (Player 2) Controller is connected")]
			public bool RightPortConnected { get; set; }

			public QuickNESSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public QuickNESSyncSettings Clone()
			{
				return (QuickNESSyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(QuickNESSyncSettings x, QuickNESSyncSettings y)
			{
				// the core can handle dynamic plugging and unplugging, but that changes
				// the controllerdefinition, and we're not ready for that
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
