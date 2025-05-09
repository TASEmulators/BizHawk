using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Panasonic3DO
{
	public partial class Opera : ISettable<object, Opera.SyncSettings>
	{
		// System type determines the version of the console to run
		// The selection of the proper BIOS derives from this decision
		public enum SystemType
		{
			[Display(Name = "Panasonic FZ-1 (U)")]
			Panasonic_FZ1_U,
			[Display(Name = "Panasonic FZ-1 (E)")]
			Panasonic_FZ1_E,
			[Display(Name = "Panasonic FZ-1 (J)")]
			Panasonic_FZ1_J,
			[Display(Name = "Panasonic FZ-10 (U)")]
			Panasonic_FZ10_U,
			[Display(Name = "Panasonic FZ-10 (E)")]
			Panasonic_FZ10_E,
			[Display(Name = "Panasonic FZ-10 (J)")]
			Panasonic_FZ10_J,
			[Display(Name = "Goldstar GDO-101P")]
			Goldstar_GDO101P,
			[Display(Name = "Goldstar FC-1")]
			Goldstar_FC1,
			[Display(Name = "Sanyo IMP-21J Try")]
			Sanyo_IMP21J_Try,
			[Display(Name = "Sanyo HC-21")]
			Sanyo_HC21,
			[Display(Name = "(3DO Arcade) Shootout At Old Tucson")]
			Shootout_At_Old_Tucson,
			[Display(Name = "3DO-NTSC-1.0fc2")]
			_3DO_NTSC_1fc2,
		}

		public enum FontROM
		{
			[Display(Name = "None")]
			None,
			[Display(Name = "Kanji ROM for Panasonic FZ-1")]
			Kanji_ROM_Panasonic_FZ1,
			[Display(Name = "Kanji ROM for Panasonic FZ-10")]
			Kanji_ROM_Panasonic_FZ10,
		}

		public enum VideoStandard
		{
			[Display(Name = "NTSC")]
			NTSC = 0,
			[Display(Name = "PAL1")]
			PAL1 = 1,
			[Display(Name = "PAL2")]
			PAL2 = 2,
		}

		public enum ControllerType
		{
			[Display(Name = "None")]
			None = 0,
			[Display(Name = "Joypad")]
			Gamepad = 1,
			[Display(Name = "Mouse")]
			Mouse = 2,
			[Display(Name = "Flight Stick")]
			FlightStick = 257,
			[Display(Name = "Light Gun")]
			LightGun = 4,
			[Display(Name = "Arcade Light Gun")]
			ArcadeLightGun = 260,
			[Display(Name = "Orbatak Trackball")]
			OrbatakTrackball = 513,
		}

		public object GetSettings() => null;
		public PutSettingsDirtyBits PutSettings(object o) => PutSettingsDirtyBits.None;

		private SyncSettings _syncSettings;
		public SyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class SyncSettings
		{
			[DisplayName("System Type")]
			[Description("Sets the version of the console to emulate. This choice determines the corresponding BIOS ROM to use.")]
			[DefaultValue(SystemType.Panasonic_FZ1_U)]
			[TypeConverter(typeof(SystemType))]
			public SystemType SystemType { get; set; }

			[DisplayName("Font ROM")]
			[Description("Determines whether (if any) addition ROM to load for regional font support.")]
			[DefaultValue(FontROM.None)]
			[TypeConverter(typeof(FontROM))]
			public FontROM FontROM { get; set; }

			[DisplayName("Video Standard")]
			[Description("Determines the resolution and video timing. It should be selected according to the game and console's region.")]
			[DefaultValue(VideoStandard.NTSC)]
			[TypeConverter(typeof(VideoStandard))]
			public VideoStandard VideoStandard { get; set; }

			[DisplayName("Controller 1 Type")]
			[Description("Sets the type of controller connected to the console's port 1.")]
			[DefaultValue(ControllerType.Gamepad)]
			[TypeConverter(typeof(ControllerType))]
			public ControllerType Controller1Type { get; set; }

			[DisplayName("Controller 2 Type")]
			[Description("Sets the type of controller connected to the console's port 2.")]
			[DefaultValue(ControllerType.None)]
			[TypeConverter(typeof(ControllerType))]
			public ControllerType Controller2Type { get; set; }

			public SyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public SyncSettings Clone()
				=> (SyncSettings) MemberwiseClone();

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
