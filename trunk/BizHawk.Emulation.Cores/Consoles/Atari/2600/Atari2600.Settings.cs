using System;
using System.ComponentModel;
using System.Drawing;
using Newtonsoft.Json;

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
			Settings = (A2600Settings)o;
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
			private int _ntscTopLine = 24;

			[JsonIgnore]
			private int _ntscBottomLine = 248;

			[JsonIgnore]
			private int _palTopLine = 24;

			[JsonIgnore]
			private int _palBottomLine = 296;

			[Description("Sets whether or not the Background layer will be displayed")]
			public bool ShowBG { get; set; }

			[Description("Sets whether or not the Player 1 layer will be displayed")]
			public bool ShowPlayer1 { get; set; }

			[Description("Sets whether or not the Player 2 layer will be displayed")]
			public bool ShowPlayer2 { get; set; }

			[Description("Sets whether or not the Missle 1 layer will be displayed")]
			public bool ShowMissle1 { get; set; }

			[Description("Sets whether or not the Missle 2 layer will be displayed")]
			public bool ShowMissle2 { get; set; }

			[Description("Sets whether or not the Ball layer will be displayed")]
			public bool ShowBall { get; set; }

			[Description("Sets whether or not the Playfield layer will be displayed")]
			public bool ShowPlayfield { get; set; }

			public int NTSCTopLine
			{
				get { return this._ntscTopLine; }
				set { _ntscTopLine = Math.Min(64, Math.Max(value, 0)); }
			}

			public int NTSCBottomLine
			{
				get { return _ntscBottomLine; }
				set { _ntscBottomLine = Math.Min(260, Math.Max(value, 192)); }
			}

			public int PALTopLine
			{
				get { return this._palTopLine; }
				set { this._palTopLine = Math.Min(64, Math.Max(value, 0)); }
			}

			public int PALBottomLine
			{
				get { return this._palBottomLine; }
				set { this._palBottomLine = Math.Min(310, Math.Max(value, 192)); }
			}

			public Color BackgroundColor { get; set; }

			public A2600Settings Clone()
			{
				return (A2600Settings)MemberwiseClone();
			}

			public static A2600Settings GetDefaults()
			{
				return new A2600Settings
				{
					ShowBG = true,
					ShowPlayer1 = true,
					ShowPlayer2 = true,
					ShowMissle1 = true,
					ShowMissle2 = true,
					ShowBall = true,
					ShowPlayfield = true,
					BackgroundColor = Color.Black
				};
			}
		}

		public class A2600SyncSettings
		{
			[Description("Set the TV Type switch on the console to B&W or Color")]
			public bool BW { get; set; }

			[Description("Set the Left Difficulty switch on the console")]
			public bool LeftDifficulty { get; set; }

			[Description("Set the Right Difficulty switch on the console")]
			public bool RightDifficulty { get; set; }

			public A2600SyncSettings Clone()
			{
				return (A2600SyncSettings)MemberwiseClone();
			}

			public static A2600SyncSettings GetDefaults()
			{
				return new A2600SyncSettings
				{
					BW = false,
					LeftDifficulty = true,
					RightDifficulty = true
				};
			}
		}
	}
}
