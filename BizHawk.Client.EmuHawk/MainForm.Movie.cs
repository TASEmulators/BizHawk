using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;

namespace BizHawk.Client.EmuHawk
{
	partial class MainForm
	{
		public void StartNewMovie(IMovie movie, bool record, bool fromTastudio = false) //TasStudio flag is a hack for now
		{
			//If a movie is already loaded, save it before starting a new movie
			if (!fromTastudio && Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.Movie.Save();
			}

			Global.MovieSession = new MovieSession
			{
				Movie = movie,
				MessageCallback = GlobalWin.OSD.AddMessage,
				AskYesNoCallback = StateErrorAskUser
			};

			InputManager.RewireInputChain();

			if (!record)
			{
				Global.MovieSession.Movie.Load();
			}

			try
			{
				var quicknesName = ((CoreAttributes)Attribute.GetCustomAttribute(typeof(QuickNES), typeof(CoreAttributes))).CoreName;
				var neshawkName = ((CoreAttributes)Attribute.GetCustomAttribute(typeof(NES), typeof(CoreAttributes))).CoreName;
				if (!record && Global.Emulator.SystemId == "NES") // For NES we need special logic since the movie will drive which core to load
				{
					// If either is specified use that, else use whatever is currently set
					if (Global.MovieSession.Movie.Header[HeaderKeys.CORE] == quicknesName)
					{
						Global.Config.NES_InQuickNES = true;
					}
					else if (Global.MovieSession.Movie.Header[HeaderKeys.CORE] == neshawkName)
					{
						Global.Config.NES_InQuickNES = false;
					}
				}

				string s = Global.MovieSession.Movie.Header.SyncSettingsJson;
				if (!string.IsNullOrWhiteSpace(s))
				{
					_syncSettingsHack = ConfigService.LoadWithType(s);
				}

				if (record) // This is a hack really, the movie isn't active yet unless I do this, and LoadRom wants to know if it is
				{
					Global.MovieSession.Movie.SwitchToRecord();
				}

				LoadRom(GlobalWin.MainForm.CurrentlyOpenRom);
			}
			finally
			{
				// ensure subsequent calls to LoadRom won't get the settings object created here
				this._syncSettingsHack = null;
			}

			if (!fromTastudio)
			{
				Global.Config.RecentMovies.Add(movie.Filename);
			}

			if (Global.MovieSession.Movie.Header.StartsFromSavestate)
			{
				var state = Convert.FromBase64String(Global.MovieSession.Movie.Header.SavestateBinaryBase64Blob);
				Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(state)));
				Global.Emulator.ResetCounters();
			}

			if (!fromTastudio)
			{
				if (record)
				{
					Global.MovieSession.Movie.StartNewRecording();
					Global.MovieSession.ReadOnly = false;
				}
				else
				{
					Global.MovieSession.Movie.StartNewPlayback();
				}
			}

			SetMainformMovieInfo();

			if (!fromTastudio)
			{
				GlobalWin.Tools.Restart<TAStudio>();
			}

			GlobalWin.Tools.Restart<VirtualPadForm>();
			GlobalWin.DisplayManager.NeedsToPaint = true;
		}

		public void SetMainformMovieInfo()
		{
			if (Global.MovieSession.Movie.IsPlaying)
			{
				Text = DisplayNameForSystem(Global.Game.System) + " - " + Global.Game.Name + " - " + Path.GetFileName(Global.MovieSession.Movie.Filename);
				PlayRecordStatusButton.Image = Properties.Resources.Play;
				PlayRecordStatusButton.ToolTipText = "Movie is in playback mode";
				PlayRecordStatusButton.Visible = true;
			}
			else if (Global.MovieSession.Movie.IsRecording)
			{
				Text = DisplayNameForSystem(Global.Game.System) + " - " + Global.Game.Name + " - " + Path.GetFileName(Global.MovieSession.Movie.Filename);
				PlayRecordStatusButton.Image = Properties.Resources.RecordHS;
				PlayRecordStatusButton.ToolTipText = "Movie is in record mode";
				PlayRecordStatusButton.Visible = true;
			}
			else if (!Global.MovieSession.Movie.IsActive)
			{
				Text = DisplayNameForSystem(Global.Game.System) + " - " + Global.Game.Name;
				PlayRecordStatusButton.Image = Properties.Resources.Blank;
				PlayRecordStatusButton.ToolTipText = "No movie is active";
				PlayRecordStatusButton.Visible = false;
			}
		}

		public void LoadPlayMovieDialog()
		{
			new PlayMovie().ShowDialog();
		}

		public void LoadRecordMovieDialog()
		{
			if (!Global.Emulator.Attributes().Released)
			{
				var result = MessageBox.Show
					(this, "Thanks for using Bizhawk!  The emulation core you have selected " +
					"is currently BETA-status.  We appreciate your help in testing Bizhawk. " +
					"You can record a movie on this core if you'd like to, but expect to " +
					"encounter bugs and sync problems.  Continue?", "BizHawk", MessageBoxButtons.YesNo);

				if (result != DialogResult.Yes)
				{
					return;
				}
			}
			else if (Global.Emulator is LibsnesCore)
			{
				var ss = (LibsnesCore.SnesSyncSettings)Global.Emulator.GetSyncSettings();
				if (ss.Profile == "Performance" && !Global.Config.DontAskPerformanceCoreRecordingNag)
				{
					var box = new MsgBox(
						"While the performance core is faster, it is recommended that you use the Compatibility profile when recording movies for better accuracy and stability\n\nSwitch to Compatibility?",
						"Stability Warning",
						MessageBoxIcon.Warning);

					box.SetButtons(
						new [] {"Switch", "Continue", "Cancel" },
						new DialogResult[] { DialogResult.Yes, DialogResult.No, DialogResult.Cancel });
					box.SetCheckbox("Don't ask me again");
					box.MaximumSize = new Size(450, 350);
					box.SetMessageToAutoSize();
					var result = box.ShowDialog();
					Global.Config.DontAskPerformanceCoreRecordingNag = box.CheckboxChecked;

					if (result == DialogResult.Yes)
					{
						ss.Profile = "Compatibility";
						Global.Emulator.PutSyncSettings(ss);
					}
					else if (result == DialogResult.Cancel)
					{
						return;
					}
				}
			}

			new RecordMovie().ShowDialog();
		}

		public void RestartMovie()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				LoadRom(CurrentlyOpenRom);
				if (Global.MovieSession.Movie.Header.StartsFromSavestate)
				{
					var state = Convert.FromBase64String(Global.MovieSession.Movie.Header.SavestateBinaryBase64Blob);
					Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(state)));
					Global.Emulator.ResetCounters();
				}

				Global.MovieSession.Movie.StartNewPlayback();
				SetMainformMovieInfo();
				GlobalWin.OSD.AddMessage("Replaying movie file in read-only mode");
				Global.MovieSession.ReadOnly = true;
			}
		}

		public void StopMovie(bool saveChanges = true)
		{
			Global.MovieSession.StopMovie(saveChanges);
			SetMainformMovieInfo();
		}
	}
}
