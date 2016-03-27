using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using Newtonsoft.Json;


namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : ISettable<GPGX.GPGXSettings, GPGX.GPGXSyncSettings>
	{
		public GPGXSettings GetSettings()
		{
			return _settings.Clone();
		}

		public GPGXSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSettings(GPGXSettings o)
		{
			bool ret = GPGXSettings.NeedsReboot(_settings, o);
			_settings = o;
			Core.gpgx_set_draw_mask(_settings.GetDrawMask());
			return ret;
		}

		public bool PutSyncSettings(GPGXSyncSettings o)
		{
			bool ret = GPGXSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret;
		}

		private GPGXSyncSettings _syncSettings;
		private GPGXSettings _settings;

		public class GPGXSettings
		{
			[DeepEqualsIgnore]
			[JsonIgnore]
			private bool _DrawBGA;

			[DisplayName("Background Layer A")]
			[Description("True to draw BG layer A")]
			[DefaultValue(true)]
			public bool DrawBGA { get { return _DrawBGA; } set { _DrawBGA = value; } }

			[DeepEqualsIgnore]
			[JsonIgnore]
			private bool _DrawBGB;

			[DisplayName("Background Layer B")]
			[Description("True to draw BG layer B")]
			[DefaultValue(true)]
			public bool DrawBGB { get { return _DrawBGB; } set { _DrawBGB = value; } }

			[DeepEqualsIgnore]
			[JsonIgnore]
			private bool _DrawBGW;

			[DisplayName("Background Layer W")]
			[Description("True to draw BG layer W")]
			[DefaultValue(true)]
			public bool DrawBGW { get { return _DrawBGW; } set { _DrawBGW = value; } }

			[DeepEqualsIgnore]
			[JsonIgnore]
			private bool _DrawObj;

			[DisplayName("Sprite Layer")]
			[Description("True to draw sprite layer")]
			[DefaultValue(true)]
			public bool DrawObj { get { return _DrawObj; } set { _DrawObj = value; } }

			[DeepEqualsIgnore]
			[JsonIgnore]
			private bool _PadScreen320;

			[DisplayName("Pad screen to 320")]
			[Description("When using 1:1 aspect ratio, enable to make screen width constant (320) between game modes")]
			[DefaultValue(false)]
			public bool PadScreen320 { get { return _PadScreen320; } set { _PadScreen320 = value; } }

			[DisplayName("Audio Filter")]
			[DefaultValue(LibGPGX.InitSettings.FilterType.LowPass)]
			public LibGPGX.InitSettings.FilterType Filter { get; set; }

			[DisplayName("Low Pass Range")]
			[Description("Only active when filter type is lowpass")]
			[DefaultValue((ushort)39321)]
			public ushort LowPassRange { get; set; }

			[DisplayName("Three band low cutoff")]
			[Description("Only active when filter type is three band")]
			[DefaultValue((short)880)]
			public short LowFreq { get; set; }

			[DisplayName("Three band high cutoff")]
			[Description("Only active when filter type is three band")]
			[DefaultValue((short)5000)]
			public short HighFreq { get; set; }

			[DisplayName("Three band low gain")]
			[Description("Only active when filter type is three band")]
			[DefaultValue((short)1)]
			public short LowGain { get; set; }

			[DisplayName("Three band mid gain")]
			[Description("Only active when filter type is three band")]
			[DefaultValue((short)1)]
			public short MidGain { get; set; }

			[DisplayName("Three band high gain")]
			[Description("Only active when filter type is three band")]
			[DefaultValue((short)1)]
			public short HighGain { get; set; }

			[DeepEqualsIgnore]
			[JsonIgnore]
			private bool _Backdrop;

			[DisplayName("Use custom backdrop color")]
			[Description("Filler when layers are off")]
			[DefaultValue((bool)false)]
			public bool Backdrop { get { return _Backdrop; } set { _Backdrop = value; } }

			[DisplayName("Custom backdrop color")]
			[Description("Magic pink (0xffff00ff) by default")]
			[DefaultValue((uint)0xffff00ff)]
			public uint BackdropColor { get; set; }

			public GPGXSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public GPGXSettings Clone()
			{
				return (GPGXSettings)MemberwiseClone();
			}

			public LibGPGX.DrawMask GetDrawMask()
			{
				LibGPGX.DrawMask ret = 0;
				if (DrawBGA) ret |= LibGPGX.DrawMask.BGA;
				if (DrawBGB) ret |= LibGPGX.DrawMask.BGB;
				if (DrawBGW) ret |= LibGPGX.DrawMask.BGW;
				if (DrawObj) ret |= LibGPGX.DrawMask.Obj;
				if (Backdrop) ret |= LibGPGX.DrawMask.Backdrop;
				return ret;
			}

			public static bool NeedsReboot(GPGXSettings x, GPGXSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public LibGPGX.InitSettings GetNativeSettings()
			{
				return new LibGPGX.InitSettings
				{
					Filter = Filter,
					LowPassRange = LowPassRange,
					LowFreq = LowFreq,
					HighFreq = HighFreq,
					LowGain = LowGain,
					MidGain = MidGain,
					HighGain = HighGain,
					BackdropColor = BackdropColor
				};
			}
		}

		public class GPGXSyncSettings
		{
			[DisplayName("Use Six Button Controllers")]
			[Description("Controls the type of any attached normal controllers; six button controllers are used if true, otherwise three button controllers.  Some games don't work correctly with six button controllers.  Not relevant if other controller types are connected.")]
			[DefaultValue(true)]
			public bool UseSixButton { get; set; }

			[DisplayName("Control Type")]
			[Description("Sets the type of controls that are plugged into the console.  Some games will automatically load with a different control type.")]
			[DefaultValue(ControlType.Normal)]
			public ControlType ControlType { get; set; }

			[DisplayName("Autodetect Region")]
			[Description("Sets the region of the emulated console.  Many games can run on multiple regions and will behave differently on different ones.  Some games may require a particular region.")]
			[DefaultValue(LibGPGX.Region.Autodetect)]
			public LibGPGX.Region Region { get; set; }

			public GPGXSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public GPGXSyncSettings Clone()
			{
				return (GPGXSyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(GPGXSyncSettings x, GPGXSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
