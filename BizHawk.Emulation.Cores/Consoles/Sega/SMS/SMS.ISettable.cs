using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public sealed partial class SMS : ISettable<SMS.SMSSettings, SMS.SMSSyncSettings>
	{
		public SMSSettings GetSettings()
		{
			return Settings.Clone();
		}

		public SMSSyncSettings GetSyncSettings()
		{
			return SyncSettings.Clone();
		}

		public bool PutSettings(SMSSettings o)
		{
			bool ret = SMSSettings.RebootNeeded(Settings, o);
			Settings = o;
			return ret;
		}

		public bool PutSyncSettings(SMSSyncSettings o)
		{
			bool ret = SMSSyncSettings.RebootNeeded(SyncSettings, o);
			SyncSettings = o;
			return ret;
		}

		internal SMSSettings Settings { get; private set; }
		internal SMSSyncSettings SyncSettings { get; private set; }

		public class SMSSettings
		{
			// Game settings
			public bool ForceStereoSeparation = false;
			public bool SpriteLimit = false;
			public bool Fix3D = true;
			public bool DisplayOverscan = false;

			// GG settings
			public bool ShowClippedRegions = false;
			public bool HighlightActiveDisplayRegion = false;

			// graphics settings
			public bool DispBG = true;
			public bool DispOBJ = true;

			public SMSSettings Clone()
			{
				return (SMSSettings)MemberwiseClone();
			}

			public static bool RebootNeeded(SMSSettings x, SMSSettings y)
			{
				return false;
			}
		}

		public class SMSSyncSettings
		{
			public bool EnableFM = true;
			public bool AllowOverlock = false;
			public bool UseBIOS = false;
			public string ConsoleRegion = "Export";
			public string DisplayType = "NTSC";

			public SMSSyncSettings Clone()
			{
				return (SMSSyncSettings)MemberwiseClone();
			}

			public static bool RebootNeeded(SMSSyncSettings x, SMSSyncSettings y)
			{
				return
					x.EnableFM != y.EnableFM ||
					x.AllowOverlock != y.AllowOverlock ||
					x.UseBIOS != y.UseBIOS ||
					x.ConsoleRegion != y.ConsoleRegion ||
					x.DisplayType != y.DisplayType;
			}
		}
	}
}
