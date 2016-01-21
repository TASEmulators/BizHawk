using BizHawk.Emulation.Common;

using Newtonsoft.Json;

using System;
using System.ComponentModel;
using System.Drawing;


namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	// adelikat: changing settings to default object until there are actually settings, as the ui depends on it to know if there are any settings avaialable
	public partial class C64 : ISettable<object, C64.C64SyncSettings>
	{
		public object /*C64Settings*/ GetSettings()
		{
			//return Settings.Clone();
			return null;
		}

		public C64SyncSettings GetSyncSettings()
		{
			return SyncSettings.Clone();
		}

		public bool PutSettings(object /*C64Settings*/ o)
		{
			//Settings = o;
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
			[DefaultValue(VicType.PAL)]
			public VicType vicType { get; set; }

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
			PAL, NTSC, NTSC_OLD, DREAN
		}
	}
}