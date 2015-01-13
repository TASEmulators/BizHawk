using System;
using System.ComponentModel;
using Newtonsoft.Json;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.Saturn
{
	public partial class Yabause : ISettable<object, Yabause.SaturnSyncSettings>
	{
		public object GetSettings()
		{
			return null;
		}

		public SaturnSyncSettings GetSyncSettings()
		{
			return SyncSettings.Clone();
		}

		public bool PutSettings(object o)
		{
			return false;
		}

		public bool PutSyncSettings(SaturnSyncSettings o)
		{
			bool ret = SaturnSyncSettings.NeedsReboot(SyncSettings, o);

			SyncSettings = o;

			if (GLMode && SyncSettings.UseGL)
			{
				if (SyncSettings.DispFree)
				{
					SetGLRes(0, SyncSettings.GLW, SyncSettings.GLH);
				}
				else
				{
					SetGLRes(SyncSettings.DispFactor, 0, 0);
				}
			}

			return ret;
		}

		private SaturnSyncSettings SyncSettings;

		public class SaturnSyncSettings
		{
			[DisplayName("Open GL Mode")]
			[Description("Use OpenGL mode for rendering instead of software.")]
			[DefaultValue(false)]
			public bool UseGL { get; set; }

			[DisplayName("Display Factor")]
			[Description("In OpenGL mode, the internal resolution as a multiple of the normal internal resolution (1x, 2x, 3x, 4x).  Ignored in software mode or when a custom resolution is used.")]
			[DefaultValue(1)]
			public int DispFactor
			{
				get { return _DispFactor; }
				set { _DispFactor = Math.Max(1, Math.Min(value, 4)); }
			}

			[JsonIgnore]
			[DeepEqualsIgnore]
			private int _DispFactor;

			[DisplayName("Display Free")]
			[Description("In OpenGL mode, set to true to use a custom resolution and ignore DispFactor.")]
			[DefaultValue(false)]
			public bool DispFree { get { return _DispFree; } set { _DispFree = value; } }
			[JsonIgnore]
			[DeepEqualsIgnore]
			private bool _DispFree;

			[DisplayName("DispFree Final Width")]
			[Description("In OpenGL mode and when DispFree is true, the width of the final resolution.")]
			[DefaultValue(640)]
			public int GLW { get { return _GLW; } set { _GLW = Math.Max(320, Math.Min(value, 2048)); } }
			[JsonIgnore]
			[DeepEqualsIgnore]
			private int _GLW;

			[DisplayName("DispFree Final Height")]
			[Description("In OpenGL mode and when DispFree is true, the height of the final resolution.")]
			[DefaultValue(480)]
			public int GLH
			{
				get { return _GLH; }
				set { _GLH = Math.Max(224, Math.Min(value, 1024)); }
			}

			[JsonIgnore]
			[DeepEqualsIgnore]
			private int _GLH;

			[DisplayName("Ram Cart Type")]
			[Description("The type of the attached RAM cart.  Most games will not use this.")]
			[DefaultValue(LibYabause.CartType.NONE)]
			public LibYabause.CartType CartType { get; set; }

			[DisplayName("Skip BIOS")]
			[Description("Skip the Bios Intro screen.")]
			[DefaultValue(false)]
			public bool SkipBios { get; set; }

			[DisplayName("Use RealTime RTC")]
			[Description("If true, the real time clock will reflect real time, instead of emulated time.  Ignored (forced to false) when a movie is recording.")]
			[DefaultValue(false)]
			public bool RealTimeRTC { get; set; }

			[DisplayName("RTC intiial time")]
			[Description("Set the initial RTC time.  Only used when RealTimeRTC is false.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			public DateTime RTCInitialTime { get; set; }

			public static bool NeedsReboot(SaturnSyncSettings x, SaturnSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public SaturnSyncSettings Clone()
			{
				return (SaturnSyncSettings)MemberwiseClone();
			}

			public SaturnSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}
	}
}
