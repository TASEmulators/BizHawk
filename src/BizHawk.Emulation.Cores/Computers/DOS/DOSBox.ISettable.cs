using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	public partial class DOSBox : ISettable<object, DOSBox.SyncSettings>
	{
		public enum ConfigurationPreset
		{
			[Display(Name = "Early 80s")]
			Early80s,
			[Display(Name = "Late 80s")]
			Late80s,
			[Display(Name = "Early 90s")]
			Early90s,
			[Display(Name = "Mid 90s")]
			Mid90s,
			[Display(Name = "Late 90s")]
			Late90s,
		}

		/// <remarks>values are the actual size in bytes for each hdd selection</remarks>
		public enum WriteableHardDiskOptions : ulong
		{
			None = 0UL,
			[Display(Name = "21Mb (FAT16)")]
			FAT16_21Mb = 21411840UL,
			[Display(Name = "41Mb (FAT16)")]
			FAT16_41Mb = 42823680UL,
			[Display(Name = "241Mb (FAT16)")]
			FAT16_241Mb = 252370944UL,
			[Display(Name = "504Mb (FAT16)")]
			FAT16_504Mb = 527966208UL,
			[Display(Name = "2014Mb (FAT16)")]
			FAT16_2014Mb = 2111864832UL,
			[Display(Name = "4091Mb (FAT32)")]
			FAT32_4091Mb = 4289725440UL,
		}

		public enum MachineType
		{
			Auto,
			mda,
			cga,
			cga_mono,
			cga_rgb,
			cga_composite,
			cga_composite2,
			hercules,
			hercules_plus,
			hercules_incolor,
			hercules_color,
			tandy,
			pcjr,
			pcjr_composite,
			pcjr_composite2,
			amstrad,
			ega,
			ega200,
			jega,
			mcga,
			vgaonly,
			svga_s3,
			svga_s386c928,
			svga_s3vision864,
			svga_s3vision868,
			svga_s3vision964,
			svga_s3vision968,
			svga_s3trio32,
			svga_s3trio64,
			svga_s3trio64vP,
			svga_s3virge,
			svga_s3virgevx,
			svga_et3000,
			svga_et4000,
			svga_paradise,
			vesa_nolfb,
			vesa_oldvbe,
			vesa_oldvbe10,
			pc98,
			pc9801,
			pc9821,
			svga_ati_egavgawonder,
			svga_ati_vgawonder,
			svga_ati_vgawonderplus,
			svga_ati_vgawonderxl,
			svga_ati_vgawonderxl24,
			svga_ati_mach8,
			svga_ati_mach32,
			svga_ati_mach64,
			fm_towns
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
			[DisplayName("Configuration Preset")]
			[Description("Establishes a base configuration for DOSBox roughly corresponding to the selected era.")]
			[DefaultValue(ConfigurationPreset.Early90s)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public ConfigurationPreset ConfigurationPreset { get; set; }

			[DisplayName("Enable Joystick 1")]
			[Description("Determines whether a joystick will be plugged in the IBM PC Gameport 1")]
			[DefaultValue(true)]
			public bool EnableJoystick1 { get; set; }

			[DisplayName("Enable Joystick 2")]
			[Description("Determines whether a joystick will be plugged in the IBM PC Gameport 2")]
			[DefaultValue(true)]
			public bool EnableJoystick2 { get; set; }

			[DisplayName("Enable Mouse")]
			[Description("Determines whether a mouse will be plugged in")]
			[DefaultValue(true)]
			public bool EnableMouse { get; set; }

			[DisplayName("Writeable Hard Disk Drive")]
			[Description("Determines whether to mount an empty writable formatted hard disk in drive C:. This hard disk will be fully located in memory so make sure you have enough RAM available. Its contents can be saved and loaded as SaveRAM.")]
			[DefaultValue(WriteableHardDiskOptions.FAT16_241Mb)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public WriteableHardDiskOptions WriteableHardDisk { get; set; }

			[DisplayName("CPU Cycles")]
			[Description("How many CPU cycles to emulate per ms. Default: -1, to keep the one included in the configuration preset.")]
			[DefaultValue(-1)]
			public int CPUCycles { get; set; }

			[DisplayName("Machine Type")]
			[Description("Chooses the machine type (CPU/GPU) to emulate. Auto uses the configuration preset's default.")]
			[DefaultValue(MachineType.Auto)]
			public MachineType MachineType { get; set; }

			public SyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public SyncSettings Clone()
				=> (SyncSettings)MemberwiseClone();

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
