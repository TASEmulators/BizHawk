using BizHawk.Emulation.Common;

using Newtonsoft.Json;

using System;
using System.ComponentModel;
using System.Drawing;


namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
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