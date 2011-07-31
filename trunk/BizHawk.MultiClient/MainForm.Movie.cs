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

		private void HandleMovieLoadState(StreamReader reader)
		{
			//Note, some of the situations in these IF's may be identical and could be combined but I intentionally separated it out for clarity
			if (UserMovie.Mode == MOVIEMODE.RECORD)
			{
				
				if (ReadOnly)
				{
					
					int x = UserMovie.CheckTimeLines(reader);
					//if (x >= 0)
					//    MessageBox.Show("Savestate input log does not match the movie at frame " + (x+1).ToString() + "!", "Timeline error", MessageBoxButtons.OK); //TODO: replace with a not annoying message once savestate logic is running smoothly
					//else
					{
						UserMovie.WriteMovie();
						UserMovie.StartPlayback();
						SetMainformMovieInfo();
						Global.MovieMode = true;
					}
				}
				else
				{
					Global.MovieMode = false;
					UserMovie.LoadLogFromSavestateText(reader);
				}
			}
			else if (UserMovie.Mode == MOVIEMODE.PLAY)
			{
				if (ReadOnly)
				{
					int x = UserMovie.CheckTimeLines(reader);
					//if (x >= 0)
					//    MessageBox.Show("Savestate input log does not match the movie at frame " + (x+1).ToString() + "!", "Timeline error", MessageBoxButtons.OK); //TODO: replace with a not annoying message once savestate logic is running smoothly
				}
				else
				{
					//QUESTIONABLE - control whether the movie gets truncated here?
					UserMovie.StartNewRecording(!Global.MovieSession.MultiTrack.IsActive);
					SetMainformMovieInfo();
					Global.MovieMode = false;
					UserMovie.LoadLogFromSavestateText(reader);
				}
			}
			else if (UserMovie.Mode == MOVIEMODE.FINISHED)
			{
				//TODO: have the input log kick in upon movie finished mode and stop upon movie resume
				if (ReadOnly)
				{
					if (Global.Emulator.Frame > UserMovie.Length())
					{
						Global.MovieMode = false;
						//Post movie savestate
						//There is no movie data to load, and the movie will stay in movie finished mode
						//So do nothing
					}
					else
					{
						int x = UserMovie.CheckTimeLines(reader);
						UserMovie.StartPlayback();
						SetMainformMovieInfo();
						Global.MovieMode = true;
						//if (x >= 0)
						//    MessageBox.Show("Savestate input log does not match the movie at frame " + (x+1).ToString() + "!", "Timeline error", MessageBoxButtons.OK); //TODO: replace with a not annoying message once savestate logic is running smoothly
					}
				}
				else
				{
					if (Global.Emulator.Frame > UserMovie.Length())
					{
						Global.MovieMode = false;
						//Post movie savestate
						//There is no movie data to load, and the movie will stay in movie finished mode
						//So do nothing
					}
					else
					{
						UserMovie.StartNewRecording();
						Global.MovieMode = false;
						SetMainformMovieInfo();
						UserMovie.LoadLogFromSavestateText(reader);
					}
				}
			}
		}

		private void HandleMovieSaveState(StreamWriter writer)
		{
			if (UserMovie.Mode != MOVIEMODE.INACTIVE)
			{
				UserMovie.DumpLogIntoSavestateText(writer);
			}
		}
	}
}
