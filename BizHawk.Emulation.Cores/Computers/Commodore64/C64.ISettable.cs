using BizHawk.Emulation.Common;

using Newtonsoft.Json;

using System;
using System.ComponentModel;
using System.Drawing;


namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	// adelikat: changing settings to default object until there are actually settings, as the ui depends on it to know if there are any settings avaialable
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

		public bool PutSettings(C64Settings o)
		{
			Settings = o;
			return false;
		}

		public bool PutSyncSettings(C64SyncSettings o)
		{
			SyncSettings = o;
			return false;
		}

		internal C64Settings Settings { get; private set; }
		internal C64SyncSettings SyncSettings { get; private set; }

		public class C64Settings
		{
            [DisplayName("Border type")]
            [Description("Select how to show the border area")]
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
			[Description("Set the type of video chip to use")]
			[DefaultValue(VicType.Pal)]
			public VicType VicType { get; set; }

            [DisplayName("SID type")]
            [Description("Set the type of sound chip to use")]
            [DefaultValue(SidType.OldR2)]
            public SidType SidType { get; set; }

            [DisplayName("Tape drive type")]
            [Description("Set the type of tape drive attached")]
            [DefaultValue(TapeDriveType.None)]
            public TapeDriveType TapeDriveType { get; set; }

            [DisplayName("Disk drive type")]
            [Description("Set the type of disk drive attached")]
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
	        SmallProportional, SmallFixed, Normal, Full
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