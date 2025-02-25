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
			[Display(Name = "Mid 80s")]
			Mid80s,
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
			[Display(Name = "MDA")]
			mda,
			[Display(Name = "CGA")]
			cga,
			[Display(Name = "CGA Mono")]
			cga_mono,
			[Display(Name = "CGA RGB")]
			cga_rgb,
			[Display(Name = "CGA Composite")]
			cga_composite,
			[Display(Name = "CGA Composite 2")]
			cga_composite2,
			[Display(Name = "Hercules")]
			hercules,
			[Display(Name = "Hercules Plus")]
			hercules_plus,
			[Display(Name = "Hercules InColor")]
			hercules_incolor,
			[Display(Name = "Hercules Color")]
			hercules_color,
			[Display(Name = "Tandy")]
			tandy,
			[Display(Name = "PCjr")]
			pcjr,
			[Display(Name = "PCjr Composite")]
			pcjr_composite,
			[Display(Name = "PCjr Composite 2")]
			pcjr_composite2,
			[Display(Name = "Amstrad")]
			amstrad,
			[Display(Name = "EGA")]
			ega,
			[Display(Name = "EGA 200")]
			ega200,
			[Display(Name = "JEGA")]
			jega,
			[Display(Name = "MCGA")]
			mcga,
			[Display(Name = "VGA Only")]
			vgaonly,
			[Display(Name = "SVGA S3")]
			svga_s3,
			[Display(Name = "SVGA S3 386 C928")]
			svga_s386c928,
			[Display(Name = "SVGA S3 Vision 864")]
			svga_s3vision864,
			[Display(Name = "SVGA S3 Vision 868")]
			svga_s3vision868,
			[Display(Name = "SVGA S3 Vision 964")]
			svga_s3vision964,
			[Display(Name = "SVGA S3 Vision 968")]
			svga_s3vision968,
			[Display(Name = "SVGA S3 Trio 32")]
			svga_s3trio32,
			[Display(Name = "SVGA S3 Trio 64")]
			svga_s3trio64,
			[Display(Name = "SVGA S3 Trio 64 VP")]
			svga_s3trio64vP,
			[Display(Name = "SVGA S3 Virge")]
			svga_s3virge,
			[Display(Name = "SVGA S3 Virge VX")]
			svga_s3virgevx,
			[Display(Name = "SVGA Tseng Labs ET3000")]
			svga_et3000,
			[Display(Name = "SVGA Tseng Labs ET4000")]
			svga_et4000,
			[Display(Name = "SVGA Paradise")]
			svga_paradise,
			[Display(Name = "VESA No LFB")]
			vesa_nolfb,
			[Display(Name = "VESA Old VBE")]
			vesa_oldvbe,
			[Display(Name = "VESA Old VBE 10")]
			vesa_oldvbe10,
			[Display(Name = "PC98")]
			pc98,
			[Display(Name = "PC9801")]
			pc9801,
			[Display(Name = "PC9821")]
			pc9821,
			[Display(Name = "SVGA ATI EGA/VGA Wonder")]
			svga_ati_egavgawonder,
			[Display(Name = "SVGA ATI VGA Wonder")]
			svga_ati_vgawonder,
			[Display(Name = "SVGA ATI VGA Wonder Plus")]
			svga_ati_vgawonderplus,
			[Display(Name = "SVGA ATI VGA Wonder XL")]
			svga_ati_vgawonderxl,
			[Display(Name = "SVGA ATI VGA Wonder XL24")]
			svga_ati_vgawonderxl24,
			[Display(Name = "SVGA ATI Mach 8")]
			svga_ati_mach8,
			[Display(Name = "SVGA ATI Mach 32")]
			svga_ati_mach32,
			[Display(Name = "SVGA ATI Mach 64")]
			svga_ati_mach64,
			[Display(Name = "FM Towns")]
			fm_towns
		}

		public enum SoundBlasterModel
		{
			Auto,
			[Display(Name = "None")]
			none,
			[Display(Name = "Sound Blaster")]
			sb1,
			[Display(Name = "Sound Blaster 2")]
			sb2,
			[Display(Name = "Sound Blaster Pro")]
			sbpro1,
			[Display(Name = "Sound Blaster Pro 2")]
			sbpro2,
			[Display(Name = "Sound Blaster 16")]
			sb16,
			[Display(Name = "Sound Blaster Vibra")]
			sb16vibra,
			[Display(Name = "Game Blaster")]
			gb,
			[Display(Name = "ESS688")]
			ess688,
			[Display(Name = "Reveal SC400")]
			reveal_sc400
		}

		public enum PCSpeaker : int
		{
			Auto = -1,
			Disabled = 0,
			Enabled = 1,
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
			[TypeConverter(typeof(DescribableEnumConverter))]
			[DefaultValue(MachineType.Auto)]
			public MachineType MachineType { get; set; }

			[DisplayName("RAM Size (Mb)")]
			[Description("The size of the memory capacity (RAM) to emulate. -1 to keep the value for the machine preset")]
			[DefaultValue(-1)]
			public int RAMSize { get; set; }

			[DisplayName("PC Speaker")]
			[Description("Chooses the whether to enable/disable the PC Speaker. Auto uses the configuration preset's default.")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			[DefaultValue(PCSpeaker.Auto)]
			public PCSpeaker PCSpeaker { get; set; }

			[DisplayName("Sound Blaster Model")]
			[Description("Chooses the Sound Blaster model to emulate. Auto uses the configuration preset's default.")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			[DefaultValue(SoundBlasterModel.Auto)]
			public SoundBlasterModel SoundBlasterModel { get; set; }

			[DisplayName("Sound Blaster IRQ")]
			[Description("Chooses the interrupt request number for the Sound Blaster. -1 for automatic.")]
			[DefaultValue(-1)]
			public int SoundBlasterIRQ { get; set; }

			public SyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public SyncSettings Clone()
				=> (SyncSettings)MemberwiseClone();

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
