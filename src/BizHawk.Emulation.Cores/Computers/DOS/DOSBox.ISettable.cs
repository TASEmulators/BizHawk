using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	public partial class DOSBox : ISettable<object, DOSBox.SyncSettings>
	{
		public const int MAX_MEMORY_SIZE_MB = 256;

		public enum ConfigurationPreset
		{
			[Display(Name = "[1981] IBM XT 5150 (4.77Mhz, 8086, 256KB RAM, Monochrome, PC Speaker)")]
			_1981_IBM_5150,
			[Display(Name = "[1983] IBM XT 5160 (4.77Mhz, 8086, 640KB RAM, CGA, PC Speaker)")]
			_1983_IBM_5160,
			[Display(Name = "[1986] IBM XT 286 5162-286 (6Mhz, 8086, 640KB RAM, EGA, PC Speaker)")]
			_1986_IBM_5162,
			[Display(Name = "[1987] IBM PS/2 25 (8Mhz, 186, 640KB RAM, MCGA, Game Blaster)")]
			_1987_IBM_PS2_25,
			[Display(Name = "[1990] IBM PS/2 25 286 (10Mhz, 286, 4MB RAM, VGA, Sound Blaster 1)")]
			_1990_IBM_PS2_25_286,
			[Display(Name = "[1991] IBM PS/2 25 386 (25Mhz, 386, 6MB RAM, VGA, Sound Blaster 2)")]
			_1991_IBM_PS2_25_386,
			[Display(Name = "[1993] IBM PS/2 53 SLC2 486 (50Mhz, 486, 64MB RAM, SVGA, Sound Blaster Pro 2)")]
			_1993_IBM_PS2_53_SLC2_486,
			[Display(Name = "[1994] IBM PS/2 76i SLC2 486 (100Mhz, 486, 64MB RAM, SVGA, Sound Blaster 16)")]
			_1994_IBM_PS2_76i_SLC2_486,
			[Display(Name = "[1997] IBM Aptiva 2140 (233Mhz, Pentium MMX, 96MB RAM, SVGA + 3D Support, Sound Blaster 16)")]
			_1997_IBM_APTIVA_2140,
			[Display(Name = "[1999] IBM Thinkpad 240 (300Mhz, Pentium III, 128MB, SVGA + 3D Support , Sound Blaster 16) ")]
			_1999_IBM_THINKPAD_240
		}

		/// <remarks>values are the actual size in bytes for each hdd selection</remarks>
		public enum HardDiskOptions : uint
		{
			None = 0,
			[Display(Name = "21MB (FAT16)")]
			FAT16_21MB = 21411840,
			[Display(Name = "41MB (FAT16)")]
			FAT16_41MB = 42823680,
			[Display(Name = "241MB (FAT16)")]
			FAT16_241MB = 252370944,
			[Display(Name = "504MB (FAT16)")]
			FAT16_504MB = 527966208,
			[Display(Name = "2014MB (FAT16)")]
			FAT16_2014MB = 2111864832,
		}

		public enum CPUType
		{
			Auto,
			[Display(Name = "Intel 8086")]
			C8086,
			[Display(Name = "Intel 8086 + Prefetch")]
			C8086_prefetch,
			[Display(Name = "Intel 80186")]
			C80186,
			[Display(Name = "Intel 80186 + Prefetch")]
			C80186_prefetch,
			[Display(Name = "Intel 286")]
			C286,
			[Display(Name = "Intel 286 + Prefetch")]
			C286_prefetch,
			[Display(Name = "Intel 386")]
			C386,
			[Display(Name = "Intel 386 + Prefetch")]
			C386_prefetch,
			[Display(Name = "Intel 486 (Old)")]
			C486old,
			[Display(Name = "Intel 486 (Old) + Prefetch")]
			C486old_prefetch,
			[Display(Name = "Intel 486")]
			C486,
			[Display(Name = "Intel 486 + Prefetch")]
			C486_prefetch,
			[Display(Name = "Intel Pentium")]
			pentium,
			[Display(Name = "Intel Pentium MMX")]
			pentium_mmx,
			[Display(Name = "Intel Pentium Pro (Slow)")]
			ppro_slow,
			[Display(Name = "Intel Pentium II")]
			pentium_ii,
			[Display(Name = "Intel Pentium III")]
			pentium_iii,
		}

		public enum VideoCardType
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
			[Description("Establishes a base configuration for DOSBox roughly corresponding to the selected computer model. We recommend choosing a model that is roughly of the same year or above of the game / tool you plan to run. More modern models may require more CPU power to emulate.")]
			[DefaultValue(ConfigurationPreset._1993_IBM_PS2_53_SLC2_486)]
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

			[DisplayName("Mouse Sensitivity")]
			[Description("Adjusts the mouse relative speed (mickey) multiplier.")]
			[DefaultValue(3.0)]
			public float MouseSensitivity { get; set; }

			[DisplayName("Mount Formatted Hard Disk Drive")]
			[Description("Determines whether to mount an empty writable formatted hard disk in drive C:. The hard disk will be fully located in memory so make sure you have enough RAM available. Its contents can be exported to the host filesystem.\n\nThis value will be ignored if a hard disk image (.hdd) is provided.")]
			[DefaultValue(HardDiskOptions.None)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public HardDiskOptions FormattedHardDisk { get; set; }

			[DisplayName("Force FPS Numerator")]
			[Description("Forces a numerator for FPS: how many Bizhawk frames to run per second of emulation. We recommend leaving this value unmodified, to follow the core's own video refresh rate. You can set it higher if you need finer subframe inputs, and; lower, in case your game runs in lower FPS and it feels more natural.")]
			[DefaultValue(0)]
			public int forceFPSNumerator { get; set; }

			[DisplayName("Force FPS Denominator")]
			[Description("Forces denominator for FPS: how many Bizhawk frames to run per second of emulation. We recommend leaving this value unmodified, to follow the core's own video refresh rate. You can set it lower if you need finer subframe inputs, and; higher, in case your game runs in lower FPS and it feels more natural.")]
			[DefaultValue(0)]
			public int forceFPSDenominator { get; set; }

			[DisplayName("CPU Cycles")]
			[Description("How many CPU cycles to emulate per ms. Default: -1, to keep the one included in the configuration preset.")]
			[DefaultValue(-1)]
			public int CPUCycles { get; set; }

			[DisplayName("CPU Type")]
			[Description("Chooses the CPU type to emulate. Auto uses the configuration preset's default.")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			[DefaultValue(CPUType.Auto)]
			public CPUType CPUType { get; set; }

			[DisplayName("Video Card Type")]
			[Description("Chooses the video card to emulate. Auto uses the configuration preset's default.")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			[DefaultValue(VideoCardType.Auto)]
			public VideoCardType VideoCardType { get; set; }

			[DisplayName("RAM Size (MB)")]
			[Description("The size of the memory capacity (RAM) to emulate. -1 to keep the value for the machine preset. Maximum value: 256")]
			[Range(-1, 256)]
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
				=> (SyncSettings) MemberwiseClone();

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
