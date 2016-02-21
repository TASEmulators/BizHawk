using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;


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
			_settings = o;
			LibGPGX.gpgx_set_draw_mask(_settings.GetDrawMask());
			return false;
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
			[DisplayName("Background Layer A")]
			[Description("True to draw BG layer A")]
			[DefaultValue(true)]
			public bool DrawBGA { get; set; }

			[DisplayName("Background Layer B")]
			[Description("True to draw BG layer B")]
			[DefaultValue(true)]
			public bool DrawBGB { get; set; }

			[DisplayName("Background Layer W")]
			[Description("True to draw BG layer W")]
			[DefaultValue(true)]
			public bool DrawBGW { get; set; }

			[DisplayName("Pad screen to 320")]
			[Description("Set to True to pads the screen out to be 320 when in 256 wide video modes")]
			[DefaultValue(false)]
			public bool PadScreen320 { get; set; }

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
				return ret;
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
