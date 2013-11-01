using System;
using System.IO;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.MultiClient
{
	partial class MainForm
	{
		public void StartNewMovie(Movie m, bool record)
		{
			//If a movie is already loaded, save it before starting a new movie
			if (Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.Movie.WriteMovie();
			}

			Global.MovieSession = new MovieSession
			{
				Movie = m,
				ClearSRAMCallback = ClearSaveRAM,
				MessageCallback = GlobalWinF.OSD.AddMessage,
				AskYesNoCallback = StateErrorAskUser
			};

			RewireInputChain();

			if (!record)
			{
				Global.MovieSession.Movie.LoadMovie();
				SetSyncDependentSettings();
			}

			LoadRom(GlobalWinF.MainForm.CurrentlyOpenRom, true, !record);

			Global.Config.RecentMovies.Add(m.Filename);
			if (Global.MovieSession.Movie.StartsFromSavestate)
			{
				LoadStateFile(Global.MovieSession.Movie.Filename, Path.GetFileName(Global.MovieSession.Movie.Filename));
				Global.Emulator.ResetFrameCounter();
			}
			if (record)
			{
				GlobalWinF.MainForm.ClearSaveRAM();
				Global.MovieSession.Movie.StartRecording();
				Global.ReadOnly = false;
			}
			else
			{
				GlobalWinF.MainForm.ClearSaveRAM();
				Global.MovieSession.Movie.StartPlayback();
			}
			SetMainformMovieInfo();
			TAStudio1.Restart();
			VirtualPadForm1.Restart();
			GlobalWinF.DisplayManager.NeedsToPaint = true;
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
			// put any BEETA quality cores here
			if (Global.Emulator is Emulation.Consoles.Nintendo.GBA.GBA ||
				Global.Emulator is Emulation.Consoles.Sega.Genesis ||
				Global.Emulator is Emulation.Consoles.Sega.Saturn.Yabause ||
				Global.Emulator is Emulation.Consoles.Sony.PSP.PSP)
			{
				var result = MessageBox.Show
					(this, "Thanks for using Bizhawk!  The emulation core you have selected " +
					"is currently BETA-status.  We appreciate your help in testing Bizhawk. " +
					"You can record a movie on this core if you'd like to, but expect to " +
					"encounter bugs and sync problems.  Continue?", "BizHawk", MessageBoxButtons.YesNo);
				if (result != DialogResult.Yes) return;
			}
			new RecordMovie().ShowDialog();
		}

		public void RestartMovie()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				LoadRom(CurrentlyOpenRom, true, true);
				if (Global.MovieSession.Movie.StartsFromSavestate)
				{
					LoadStateFile(Global.MovieSession.Movie.Filename, Path.GetFileName(Global.MovieSession.Movie.Filename));
					Global.Emulator.ResetFrameCounter();
				}
				GlobalWinF.MainForm.ClearSaveRAM();
				Global.MovieSession.Movie.StartPlayback();
				SetMainformMovieInfo();
				GlobalWinF.OSD.AddMessage("Replaying movie file in read-only mode");
				Global.ReadOnly = true;
			}
		}

		public void StopMovie(bool abortchanges = false)
		{
			Global.MovieSession.StopMovie();
			SetMainformMovieInfo();
		}

		//On movie load, these need to be set based on the contents of the movie file
		private void SetSyncDependentSettings()
		{
			switch (Global.Emulator.SystemId)
			{
				case "Coleco":
					string str = Global.MovieSession.Movie.Header.GetHeaderLine(MovieHeader.SKIPBIOS);
					if (!String.IsNullOrWhiteSpace(str))
					{
						if (str.ToLower() == "true")
						{
							Global.Config.ColecoSkipBiosIntro = true;
						}
						else
						{
							Global.Config.ColecoSkipBiosIntro = false;
						}
					}
					break;
			}
		}
	}
}
