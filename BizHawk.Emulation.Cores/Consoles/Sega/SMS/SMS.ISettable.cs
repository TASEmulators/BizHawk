using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS : ISettable<SMS.SmsSettings, SMS.SmsSyncSettings>
	{
		public SmsSettings GetSettings() => Settings.Clone();

		public SmsSyncSettings GetSyncSettings() => SyncSettings.Clone();

		public bool PutSettings(SmsSettings o)
		{
			bool ret = SmsSettings.RebootNeeded(Settings, o);
			Settings = o;
			return ret;
		}

		public bool PutSyncSettings(SmsSyncSettings o)
		{
			bool ret = SmsSyncSettings.RebootNeeded(SyncSettings, o);
			SyncSettings = o;
			return ret;
		}

		internal SmsSettings Settings { get; private set; }
		internal SmsSyncSettings SyncSettings { get; private set; }

		public class SmsSettings
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

			public SmsSettings Clone() => (SmsSettings)MemberwiseClone();

			public static bool RebootNeeded(SmsSettings x, SmsSettings y) => false;
		}

		public class SmsSyncSettings
		{
			public bool EnableFm = true;
			public bool AllowOverClock = false;
			public bool UseBios = true;
			public string ConsoleRegion = "Auto";
			public string DisplayType = "Auto";
			public ControllerTypes ControllerType = ControllerTypes.Standard;

			public SmsSyncSettings Clone() => (SmsSyncSettings)MemberwiseClone();

			public static bool RebootNeeded(SmsSyncSettings x, SmsSyncSettings y)
			{
				return
					x.EnableFm != y.EnableFm
					|| x.AllowOverClock != y.AllowOverClock
					|| x.UseBios != y.UseBios
					|| x.ConsoleRegion != y.ConsoleRegion
					|| x.DisplayType != y.DisplayType
					|| x.ControllerType != y.ControllerType;
			}

			public enum ControllerTypes
			{
				Standard,
				Paddle,
				LightPhaser,
				SportsPad,
				Keyboard
			}
		}
	}
}
