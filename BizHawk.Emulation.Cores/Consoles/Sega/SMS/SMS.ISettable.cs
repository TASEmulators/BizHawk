using System.ComponentModel;
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
			public bool ForceStereoSeparation { get; set; }
			public bool SpriteLimit { get; set; }

			[Description("SMS only")]
			public bool Fix3D { get; set; } = true;

			[Description("Not applicable to Game Gear")]
			public bool DisplayOverscan { get; set; }

			[Description("Game Gear only")]
			public bool ShowClippedRegions { get; set; }

			[Description("Game Gear only")]
			public bool HighlightActiveDisplayRegion { get; set; }

			// graphics settings
			public bool DispBG { get; set; } = true;
			public bool DispOBJ { get; set; } = true;

			public SmsSettings Clone() => (SmsSettings)MemberwiseClone();

			public static bool RebootNeeded(SmsSettings x, SmsSettings y) => false;
		}

		public class SmsSyncSettings
		{
			[Description("SMS only")]
			public bool EnableFm { get; set; } = true;

			[Description("SMS only")]
			public bool AllowOverClock { get; set; }

			[Description("Must be Enabled for TAS")]
			public bool UseBios { get; set; } = true;
			public Regions ConsoleRegion { get; set; } = Regions.Auto;
			public DisplayTypes DisplayType { get; set; } = DisplayTypes.Auto;
			public ControllerTypes ControllerType { get; set; } = ControllerTypes.Standard;

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

			public enum Regions
			{
				Export,
				Japan,
				Korea,
				Auto
			}

			public enum DisplayTypes
			{
				Ntsc,
				Pal,
				Auto
			}
		}
	}
}
