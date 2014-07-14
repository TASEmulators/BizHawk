using System;
using System.ComponentModel;
using System.Drawing;
using Newtonsoft.Json;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600
	{
		public object GetSettings()
		{
			return Settings.Clone();
		}

		public object GetSyncSettings()
		{
			return SyncSettings.Clone();
		}

		public bool PutSettings(object o)
		{
			A2600Settings newSettings = (A2600Settings)o;
			if (Settings == null || Settings.SECAMColors != newSettings.SECAMColors)
			{
				if (_tia != null)
					_tia.SetSECAM(newSettings.SECAMColors);
			}

			Settings = newSettings;
			return false;
		}

		public bool PutSyncSettings(object o)
		{
			SyncSettings = (A2600SyncSettings)o;
			return false;
		}

		public class A2600Settings
		{
			[JsonIgnore]
			private int _ntscTopLine;

			[JsonIgnore]
			private int _ntscBottomLine;

			[JsonIgnore]
			private int _palTopLine;

			[JsonIgnore]
			private int _palBottomLine;

			[Description("Sets whether or not the Background layer will be displayed")]
			[DefaultValue(true)]
			public bool ShowBG { get; set; }

			[Description("Sets whether or not the Player 1 layer will be displayed")]
			[DefaultValue(true)]
			public bool ShowPlayer1 { get; set; }

			[Description("Sets whether or not the Player 2 layer will be displayed")]
			[DefaultValue(true)]
			public bool ShowPlayer2 { get; set; }

			[Description("Sets whether or not the Missle 1 layer will be displayed")]
			[DefaultValue(true)]
			public bool ShowMissle1 { get; set; }

			[Description("Sets whether or not the Missle 2 layer will be displayed")]
			[DefaultValue(true)]
			public bool ShowMissle2 { get; set; }

			[Description("Sets whether or not the Ball layer will be displayed")]
			[DefaultValue(true)]
			public bool ShowBall { get; set; }

			[Description("Sets whether or not the Playfield layer will be displayed")]
			[DefaultValue(true)]
			public bool ShowPlayfield { get; set; }

			[Description("If true, PAL mode will show with SECAM (French) colors.")]
			[DefaultValue(false)]
			public bool SECAMColors { get; set; }

			[Description("First line of the video image to display in NTSC mode.")]
			[DefaultValue(24)]
			public int NTSCTopLine
			{
				get { return this._ntscTopLine; }
				set { _ntscTopLine = Math.Min(64, Math.Max(value, 0)); }
			}

			[Description("Last line of the video image to display in NTSC mode.")]
			[DefaultValue(248)]
			public int NTSCBottomLine
			{
				get { return _ntscBottomLine; }
				set { _ntscBottomLine = Math.Min(260, Math.Max(value, 192)); }
			}

			[Description("First line of the video image to display in PAL mode.")]
			[DefaultValue(24)]
			public int PALTopLine
			{
				get { return this._palTopLine; }
				set { this._palTopLine = Math.Min(64, Math.Max(value, 0)); }
			}

			[Description("Last line of the video image to display in PAL mode.")]
			[DefaultValue(296)]
			public int PALBottomLine
			{
				get { return this._palBottomLine; }
				set { this._palBottomLine = Math.Min(310, Math.Max(value, 192)); }
			}

			[DefaultValue(typeof(Color), "Black")]
			public Color BackgroundColor { get; set; }

			public A2600Settings Clone()
			{
				return (A2600Settings)MemberwiseClone();
			}

			public A2600Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public class A2600SyncSettings
		{
			[Description("Set the TV Type switch on the console to B&W or Color.  This only affects the displayed image if the game supports it.")]
			[DefaultValue(false)]
			public bool BW { get; set; }

			[Description("Set the Left Difficulty switch on the console")]
			[DefaultValue(true)]
			public bool LeftDifficulty { get; set; }

			[Description("Set the Right Difficulty switch on the console")]
			[DefaultValue(true)]
			public bool RightDifficulty { get; set; }

			[Description("Skip the BIOS intro (Super Charger only)")]
			[DefaultValue(false)]
			public bool FastScBios { get; set; }

			public A2600SyncSettings Clone()
			{
				return (A2600SyncSettings)MemberwiseClone();
			}

			public A2600SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}
	}
}
