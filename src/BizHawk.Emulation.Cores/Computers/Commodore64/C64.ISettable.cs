using System.ComponentModel;

using BizHawk.API.ApiHawk;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	// adelikat: changing settings to default object until there are actually settings, as the ui depends on it to know if there are any settings available
	public partial class C64 : ISettable<C64.C64Settings, C64.C64SyncSettings>
	{
		public C64Settings GetSettings()
		{
			return Settings.Clone();
		}

		public C64SyncSettings GetSyncSettings()
		{
			return SyncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(C64Settings o)
		{
			Settings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(C64SyncSettings o)
		{
			SyncSettings = o;
			return PutSettingsDirtyBits.None;
		}

		internal C64Settings Settings { get; private set; }
		internal C64SyncSettings SyncSettings { get; private set; }

		public class C64Settings
		{
			[DisplayName("Border type")]
			[Description("Select how to show the border area\n" +
				"NORMAL:\t Horizontal and Vertical border both set to 32 pixels (although horizontal will appear narrower due to pixel density)\n" +
				"SMALL PROPORTIONAL:\t Horizontal and Vertical border both set to 16 pixels (although horizontal will appear narrower due to pixel density)\n" +
				"SMALL FIXED:\t Horizontal border is set to 16 pixels and vertical is made slightly smaller so as to appear horizontal and vertical are the same after pixel density has been applied\n" +
				"NONE:\t Only the pixel buffer is rendered"
				)]
			[DefaultValue(BorderType.SmallProportional)]
			public BorderType BorderType { get; set; }

			public C64Settings Clone()
			{
				return (C64Settings)MemberwiseClone();
			}

			public C64Settings()
			{
				BizHawk.Common.SettingsUtil.SetDefaultValues(this);
			}
		}

		public class C64SyncSettings
		{
			[DisplayName("VIC type")]
			[Description("Set the type of video chip to use\n" +
				"PAL: ~50hz. All PAL games will expect this configuration.\n" +
				"NTSC: ~60hz.This is the most common NTSC configuration. Every NTSC game should work with this configuration.\n" +
				"NTSCOld: ~60hz.This was used in the very earliest systems and will exhibit problems on most modern games.\n" +
				"Drean: ~60hz.This was manufactured for a very specific market and is not very compatible with timing sensitive games.\n")]
			[DefaultValue(VicType.Pal)]
			public VicType VicType { get; set; }

			[DisplayName("SID type")]
			[Description("Set the type of sound chip to use\n" +
				"OldR2, OldR3, OldR4AR: Original 6581 SID chip.\n" +
				"NewR5: Updated 8580 SID chip.\n" +
				"")]
			[DefaultValue(SidType.OldR2)]
			public SidType SidType { get; set; }

			[DisplayName("Tape drive type")]
			[Description("Set the type of tape drive attached\n" +
				"1531: Original Datasette device.")]
			[DefaultValue(TapeDriveType.None)]
			public TapeDriveType TapeDriveType { get; set; }

			[DisplayName("Disk drive type")]
			[Description("Set the type of disk drive attached\n" +
				"1541: Original disk drive and ROM.\n" +
				"1541 - II: Improved model with some ROM bugfixes.")]
			[DefaultValue(DiskDriveType.None)]
			public DiskDriveType DiskDriveType { get; set; }

			public C64SyncSettings Clone()
			{
				return (C64SyncSettings)MemberwiseClone();
			}

			public C64SyncSettings()
			{
				BizHawk.Common.SettingsUtil.SetDefaultValues(this);
			}
		}

		public enum VicType
		{
			Pal, Ntsc, NtscOld, Drean
		}

		public enum CiaType
		{
			Pal, Ntsc, PalRevA, NtscRevA
		}

		public enum BorderType
		{
			None, SmallProportional, SmallFixed, Normal, Full
		}

		public enum SidType
		{
			OldR2, OldR3, OldR4AR, NewR5
		}

		public enum TapeDriveType
		{
			None, Commodore1530
		}

		public enum DiskDriveType
		{
			None, Commodore1541, Commodore1541II
		}
	}
}