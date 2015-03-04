using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class VBANext : ISettable<object, VBANext.SyncSettings>
	{
		public object GetSettings()
		{
			return null;
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSettings(object o)
		{
			return false;
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
			[Description("Skips the BIOS intro.  A BIOS file is still required.")]
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

			public enum DayOfWeek
			{
				Sunday = 0,
				Monday,
				Tuesday,
				Wednesday,
				Thursday,
				Friday,
				Saturday
			}

			[DisplayName("RTC Initial Day")]
			[Description("The day of the week to go with \"RTC Initial Time\".  Due to peculiarities in the RTC chip, this can be set indepedently of the year, month, and day of month.")]
			[DefaultValue(DayOfWeek.Friday)]
			public DayOfWeek RTCInitialDay { get; set; }

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
