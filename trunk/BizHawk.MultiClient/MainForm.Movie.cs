using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	partial class MainForm
	{
		public bool ReadOnly = true;	//Global Movie Read only setting
		public Movie UserMovie = new Movie();

		public void StartNewMovie(Movie m, bool record)
		{
			Global.MovieSession = new MovieSession();
			Global.MovieSession.Movie = m;
			UserMovie = m; //TODO - maybe get rid of UserMovie?
			RewireInputChain();

			LoadRom(Global.MainForm.CurrentlyOpenRom);
			UserMovie.LoadMovie();
			Global.Config.RecentMovies.Add(m.Filename);
			if (UserMovie.StartsFromSavestate)
			{
				LoadStateFile(m.Filename, Path.GetFileName(m.Filename));
				Global.Emulator.ResetFrameCounter();
			}
			if (record)
			{
				UserMovie.StartNewRecording();
				ReadOnly = false;
			}
			else
			{
				UserMovie.StartPlayback();
			}
			SetMainformMovieInfo();
		}

		public void SetMainformMovieInfo()
		{
			if (UserMovie.Mode == MOVIEMODE.PLAY || UserMovie.Mode == MOVIEMODE.FINISHED)
			{
				Text = DisplayNameForSystem(Global.Game.System) + " - " + Global.Game.Name + " - " + Path.GetFileName(UserMovie.Filename);
				PlayRecordStatus.Image = BizHawk.MultiClient.Properties.Resources.Play;
				PlayRecordStatus.ToolTipText = "Movie is in playback mode";
			}
			else if (UserMovie.Mode == MOVIEMODE.RECORD)
			{
				Text = DisplayNameForSystem(Global.Game.System) + " - " + Global.Game.Name + " - " + Path.GetFileName(UserMovie.Filename);
				PlayRecordStatus.Image = BizHawk.MultiClient.Properties.Resources.RecordHS;
				PlayRecordStatus.ToolTipText = "Movie is in record mode";
			}
			else
			{
				Text = DisplayNameForSystem(Global.Game.System) + " - " + Global.Game.Name;
				PlayRecordStatus.Image = BizHawk.MultiClient.Properties.Resources.Blank;
				PlayRecordStatus.ToolTipText = "";
			}
		}

		public bool MovieActive()
		{
			if (UserMovie.Mode != MOVIEMODE.INACTIVE)
				return true;
			else
				return false;
		}

		private void PlayMovie()
		{
			PlayMovie p = new PlayMovie();
			DialogResult d = p.ShowDialog();
		}

		private void RecordMovie()
		{
			RecordMovie r = new RecordMovie();
			r.ShowDialog();
		}

		public void PlayMovieFromBeginning()
		{
			if (UserMovie.Mode != MOVIEMODE.INACTIVE)
			{
				LoadRom(CurrentlyOpenRom);
				UserMovie.StartPlayback();
				SetMainformMovieInfo();
			}
		}

		public void StopMovie()
		{
			string message = "Movie ";
			if (UserMovie.Mode == MOVIEMODE.RECORD)
				message += "recording ";
			else if (UserMovie.Mode == MOVIEMODE.PLAY
				|| UserMovie.Mode == MOVIEMODE.FINISHED)
				message += "playback ";
			message += "stopped.";
			if (UserMovie.Mode != MOVIEMODE.INACTIVE)
			{
				UserMovie.StopMovie();
				Global.MovieMode = false;
				Global.RenderPanel.AddMessage(message);
				SetMainformMovieInfo();
			}
		}
	}
}
