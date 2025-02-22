using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	public partial class DOSBox : ISettable<object, DOSBox.SyncSettings>
	{
		public enum MachineConfig
		{
			[Display(Name = "A500 OCS KS1.3 512K 512K")]
			A500_OCS_130_512K_512K,
			[Display(Name = "A600 ECS KS2.05 2M")]
			A600_ECS_205_2M,
			[Display(Name = "A1200 AGA KS3.1 2M 8M")]
			A1200_AGA_310_2M_8M,
			[Display(Name = "A4000 AGA KS3.1 2M 8M")]
			A4000_AGA_310_2M_8M,
		//	CD32
		}

		public enum CpuModel
		{
			[Display(Name = "68000")]
			_68000 = 68000,
			[Display(Name = "68010")]
			_68010 = 68010,
			[Display(Name = "68020")]
			_68020 = 68020,
			[Display(Name = "68030")]
			_68030 = 68030,
			[Display(Name = "68040")]
			_68040 = 68040,
			[Display(Name = "68060")]
			_68060 = 68060,
			Auto
		}

		public enum ChipsetCompatible
		{
			A500,
			A600,
			A1200,
			A4000,
			Auto
		}

		public enum Chipset
		{
			OCS,
			[Display(Name = "ECS Agnus")]
			ECS_Agnus,
			[Display(Name = "ECS Denise")]
			ECS_Denise,
			ECS,
			AGA,
			Auto
		}

		public enum ChipMemory
		{
			[Display(Name = "512KB")]
			KB_512 = 1,
			[Display(Name = "1MB")]
			MB_1,
			[Display(Name = "1.5MB")]
			MB_1_5,
			[Display(Name = "2MB")]
			MB_2,
			[Display(Name = "2.5MB")]
			MB_2_5,
			[Display(Name = "3MB")]
			MB_3,
			[Display(Name = "3.5MB")]
			MB_3_5,
			[Display(Name = "4MB")]
			MB_4,
			[Display(Name = "4.5MB")]
			MB_4_5,
			[Display(Name = "5MB")]
			MB_5,
			[Display(Name = "5.5MB")]
			MB_5_5,
			[Display(Name = "6MB")]
			MB_6,
			[Display(Name = "6.5MB")]
			MB_6_5,
			[Display(Name = "7MB")]
			MB_7,
			[Display(Name = "7.5MB")]
			MB_7_5,
			[Display(Name = "8MB")]
			MB_8,
			Auto
		}

		public enum SlowMemory
		{
			[Display(Name = "0")]
			KB_0 = 0,
			[Display(Name = "512KB")]
			KB_512 = 2,
			[Display(Name = "1MB")]
			MB_1 = 4,
			[Display(Name = "1.5MB")]
			MB_1_5 = 6,
			Auto
		}

		public enum DriveType
		{
			DRV_NONE = -1,
			DRV_35_DD = 0,
			DRV_35_HD,
			DRV_525_SD,
			DRV_35_DD_ESCOM,
			DRV_PC_525_ONLY_40,
			DRV_PC_35_ONLY_80,
			DRV_PC_525_40_80,
			DRV_525_DD,
			DRV_FB
		}

		public enum FloppySpeed
		{
			[Display(Name = "100%")]
			_100 = 100,
			[Display(Name = "200%")]
			_200 = 200,
			[Display(Name = "400%")]
			_400 = 400,
			[Display(Name = "800%")]
			_800 = 800,
			Turbo = 0
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
			[DisplayName("Machine configuration")]
			[Description("")]
			[DefaultValue(MachineConfig.A500_OCS_130_512K_512K)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public MachineConfig MachineConfig { get; set; }

			[DisplayName("CPU model")]
			[Description("Overrides machine configuration.")]
			[DefaultValue(CpuModel.Auto)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public CpuModel CpuModel { get; set; }

			[DisplayName("Chipset compatible")]
			[Description("Overrides machine configuration.")]
			[DefaultValue(ChipsetCompatible.Auto)]
			public ChipsetCompatible ChipsetCompatible { get; set; }

			[DisplayName("Chipset")]
			[Description("Overrides machine configuration.")]
			[DefaultValue(Chipset.Auto)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public Chipset Chipset { get; set; }

			[DisplayName("Chip memory")]
			[Description("Size of chip-memory.  Overrides machine configuration.")]
			[DefaultValue(ChipMemory.Auto)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public ChipMemory ChipMemory { get; set; }

			[DisplayName("Slow memory")]
			[Description("Size of bogo-memory at 0xC00000.  Overrides machine configuration.")]
			[DefaultValue(SlowMemory.Auto)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SlowMemory SlowMemory { get; set; }

			[DisplayName("Fast memory")]
			[Description("Size in megabytes of fast-memory.  -1 means Auto.  Overrides machine configuration.")]
			[Range(LibDOSBox.FASTMEM_AUTO, 512)]
			[DefaultValue(LibDOSBox.FASTMEM_AUTO)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int FastMemory { get; set; }

			[DisplayName("Enable Joystick 1")]
			[Description("")]
			[DefaultValue(true)]
			public bool EnableJoystick1 { get; set; }

			[DisplayName("Enable Joystick 2")]
			[Description("")]
			[DefaultValue(true)]
			public bool EnableJoystick2 { get; set; }

			[DisplayName("Mouse speed")]
			[Description("Mouse speed in percents (1% - 1000%).  Adjust if there's mismatch between emulated and host mouse movement.  Note that maximum mouse movement is still 127 pixels due to Amiga hardware limitations.")]
			[Range(1, 1000)]
			[DefaultValue(100)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int MouseSpeed { get; set; }

			[DisplayName("Stereo separation")]
			[Description("Stereo separation in percents.  100% is full separation, 0% is mono mode.")]
			[Range(0, 100)]
			[DefaultValue(70)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int StereoSeparation { get; set; }

			[DisplayName("Floppy disk drives")]
			[Description("How many floppy disk drives to emulate (0 - 4).")]
			[Range(0, 4)]
			[DefaultValue(1)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int FloppyDrives { get; set; }

			[DisplayName("Floppy drive speed")]
			[Description("Default speed is 300RPM.  'Turbo' removes disk rotation emulation.  This is a speedhack, not available for movies.")]
			[DefaultValue(FloppySpeed._100)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public FloppySpeed FloppySpeed { get; set; }

			public SyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public SyncSettings Clone()
				=> (SyncSettings)MemberwiseClone();

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
