using System;
using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : ISettable<MGBAHawk.Settings, MGBAHawk.SyncSettings>
	{
		public Settings GetSettings()
		{
			return _settings.Clone();
		}

		public bool PutSettings(Settings o)
		{
			LibmGBA.Layers mask = 0;
			if (o.DisplayBG0) mask |= LibmGBA.Layers.BG0;
			if (o.DisplayBG1) mask |= LibmGBA.Layers.BG1;
			if (o.DisplayBG2) mask |= LibmGBA.Layers.BG2;
			if (o.DisplayBG3) mask |= LibmGBA.Layers.BG3;
			if (o.DisplayOBJ) mask |= LibmGBA.Layers.OBJ;
			LibmGBA.BizSetLayerMask(_core, mask);

			LibmGBA.Sounds smask = 0;
			if (o.PlayCh0) smask |= LibmGBA.Sounds.CH0;
			if (o.PlayCh1) smask |= LibmGBA.Sounds.CH1;
			if (o.PlayCh2) smask |= LibmGBA.Sounds.CH2;
			if (o.PlayCh3) smask |= LibmGBA.Sounds.CH3;
			if (o.PlayChA) smask |= LibmGBA.Sounds.CHA;
			if (o.PlayChB) smask |= LibmGBA.Sounds.CHB;
			LibmGBA.BizSetSoundMask(_core, smask);

			_settings = o;
			return false;
		}

		private Settings _settings;

		public class Settings
		{
			[DisplayName("Display BG Layer 0")]
			[DefaultValue(true)]
			public bool DisplayBG0 { get; set; }

			[DisplayName("Display BG Layer 1")]
			[DefaultValue(true)]
			public bool DisplayBG1 { get; set; }

			[DisplayName("Display BG Layer 2")]
			[DefaultValue(true)]
			public bool DisplayBG2 { get; set; }

			[DisplayName("Display BG Layer 3")]
			[DefaultValue(true)]
			public bool DisplayBG3 { get; set; }

			[DisplayName("Display Sprite Layer")]
			[DefaultValue(true)]
			public bool DisplayOBJ { get; set; }

			[DisplayName("Play Square 1")]
			[DefaultValue(true)]
			public bool PlayCh0 { get; set; }

			[DisplayName("Play Square 2")]
			[DefaultValue(true)]
			public bool PlayCh1 { get; set; }

			[DisplayName("Play Wave")]
			[DefaultValue(true)]
			public bool PlayCh2 { get; set; }

			[DisplayName("Play Noise")]
			[DefaultValue(true)]
			public bool PlayCh3 { get; set; }

			[DisplayName("Play Direct Sound A")]
			[DefaultValue(true)]
			public bool PlayChA { get; set; }

			[DisplayName("Play Direct Sound B")]
			[DefaultValue(true)]
			public bool PlayChB { get; set; }

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSyncSettings(SyncSettings o)
		{
			bool ret = SyncSettings.NeedsReboot(o, _syncSettings);
			_syncSettings = o;
			return ret;
		}

		private SyncSettings _syncSettings;

		public class SyncSettings
		{
			[DisplayName("Skip BIOS")]
			[Description("Skips the BIOS intro.  Not applicable when a BIOS is not provided.")]
			[DefaultValue(true)]
			public bool SkipBios { get; set; }

			[DisplayName("RTC Use Real Time")]
			[Description("Causes the internal clock to reflect your system clock.  Only relevant when a game has an RTC chip.  Forced to false for movie recording.")]
			[DefaultValue(true)]
			public bool RTCUseRealTime { get; set; }

			[DisplayName("RTC Initial Time")]
			[Description("The initial time of emulation.  Only relevant when a game has an RTC chip and \"RTC Use Real Time\" is false.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			public DateTime RTCInitialTime { get; set; }

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}
		}
	}
}
