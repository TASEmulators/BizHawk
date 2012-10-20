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

		public void StartNewMovie(Movie m, bool record)
		{
			//If a movie is already loaded, save it before starting a new movie
			if (Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.Movie.WriteMovie();
			}

			Global.MovieSession = new MovieSession();
			Global.MovieSession.Movie = m;
			RewireInputChain();

			LoadRom(Global.MainForm.CurrentlyOpenRom, true);
			if (!record)
			{
				Global.MovieSession.Movie.LoadMovie();
			}

			Global.Config.RecentMovies.Add(m.Filename);
			if (Global.MovieSession.Movie.StartsFromSavestate)
			{
				LoadStateFile(Global.MovieSession.Movie.Filename, Path.GetFileName(Global.MovieSession.Movie.Filename));
				Global.Emulator.ResetFrameCounter();
			}
			if (record)
			{
				Global.MovieSession.Movie.StartRecording();
				ReadOnly = false;
			}
			else
			{
				Global.MovieSession.Movie.StartPlayback();
			}
			SetMainformMovieInfo();
			TAStudio1.Restart();
		}

		public void SetMainformMovieInfo()
		{
			if (Global.MovieSession.Movie.IsPlaying)
			{
				Text = DisplayNameForSystem(Global.Game.System) + " - " + Global.Game.Name + " - " + Path.GetFileName(Global.MovieSession.Movie.Filename);
				PlayRecordStatus.Image = BizHawk.MultiClient.Properties.Resources.Play;
				PlayRecordStatus.ToolTipText = "Movie is in playback mode";
				PlayRecordStatus.Visible = true;
			}
			else if (Global.MovieSession.Movie.IsRecording)
			{
				Text = DisplayNameForSystem(Global.Game.System) + " - " + Global.Game.Name + " - " + Path.GetFileName(Global.MovieSession.Movie.Filename);
				PlayRecordStatus.Image = BizHawk.MultiClient.Properties.Resources.RecordHS;
				PlayRecordStatus.ToolTipText = "Movie is in record mode";
				PlayRecordStatus.Visible = true;
			}
			else if (!Global.MovieSession.Movie.IsActive)
			{
				Text = DisplayNameForSystem(Global.Game.System) + " - " + Global.Game.Name;
				PlayRecordStatus.Image = BizHawk.MultiClient.Properties.Resources.Blank;
				PlayRecordStatus.ToolTipText = "No movie is active";
				PlayRecordStatus.Visible = false;
			}
		}

		public void PlayMovie()
		{
			RunLoopBlocked = true;
			PlayMovie p = new PlayMovie();
			DialogResult d = p.ShowDialog();
			RunLoopBlocked = false;
		}

		public void RecordMovie()
		{
			RunLoopBlocked = true;
			RecordMovie r = new RecordMovie();
			r.ShowDialog();
			RunLoopBlocked = false;
		}

		public void PlayMovieFromBeginning()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				LoadRom(CurrentlyOpenRom, true);
				if (Global.MovieSession.Movie.StartsFromSavestate)
				{
					LoadStateFile(Global.MovieSession.Movie.Filename, Path.GetFileName(Global.MovieSession.Movie.Filename));
					Global.Emulator.ResetFrameCounter();
				}
				Global.MovieSession.Movie.StartPlayback();
				SetMainformMovieInfo();
				Global.OSD.AddMessage("Replaying movie file in read-only mode");
				Global.MainForm.ReadOnly = true;
			}
		}

		public void StopMovie()
		{
			string message = "Movie ";
			if (Global.MovieSession.Movie.IsRecording)
			{
				message += "recording ";
			}
			else if (Global.MovieSession.Movie.IsPlaying)
			{
				message += "playback ";
			}

			message += "stopped.";

			if (Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.Movie.Stop();
				Global.OSD.AddMessage(message);
				Global.MainForm.ReadOnly = true;
				SetMainformMovieInfo();
			}
		}

		private bool HandleMovieLoadState(string path)
		{
			//Note, some of the situations in these IF's may be identical and could be combined but I intentionally separated it out for clarity
			if (!Global.MovieSession.Movie.IsActive)
			{
				return true;
			}

			else if (Global.MovieSession.Movie.IsRecording)
			{

				if (ReadOnly)
				{
					if (!Global.MovieSession.Movie.CheckTimeLines(path, false))
					{
						return false;	//Timeline/GUID error
					}
					else
					{
						Global.MovieSession.Movie.WriteMovie();
						Global.MovieSession.Movie.StartPlayback();
						SetMainformMovieInfo();
					}
				}
				else
				{
					if (!Global.MovieSession.Movie.CheckTimeLines(path, true))
					{
						return false;	//GUID Error
					}
					Global.MovieSession.Movie.LoadLogFromSavestateText(path);
				}
			}

			else if (Global.MovieSession.Movie.IsPlaying && !Global.MovieSession.Movie.IsFinished)
			{
				if (ReadOnly)
				{
					if (!Global.MovieSession.Movie.CheckTimeLines(path, false))
					{
						return false;	//Timeline/GUID error
					}
					//Frame loop automatically handles the rewinding effect based on Global.Emulator.Frame so nothing else is needed here
				}
				else
				{
					if (!Global.MovieSession.Movie.CheckTimeLines(path, true))
					{
						return false;	//GUID Error
					}
					Global.MovieSession.Movie.SwitchToRecord();
					SetMainformMovieInfo();
					Global.MovieSession.Movie.LoadLogFromSavestateText(path);
				}
			}
			else if (Global.MovieSession.Movie.IsFinished)
			{
				if (ReadOnly)
				{
					{
						if (!Global.MovieSession.Movie.CheckTimeLines(path, false))
						{
							return false;	//Timeline/GUID error
						}
						else if (Global.MovieSession.Movie.IsFinished) //TimeLine check can change a movie to finished, hence the check here (not a good design)
						{
							Global.MovieSession.LatchInputFromPlayer(Global.MovieInputSourceAdapter);
						}
						else
						{
							Global.MovieSession.Movie.StartPlayback();
							SetMainformMovieInfo();
						}
					}
				}
				else
				{
					{
						if (!Global.MovieSession.Movie.CheckTimeLines(path, true))
						{
							return false;	//GUID Error
						}
						else
						{
							Global.MovieSession.Movie.StartRecording();
							SetMainformMovieInfo();
							Global.MovieSession.Movie.LoadLogFromSavestateText(path);
						}
					}
				}
			}
			return true;
		}

		private void HandleMovieSaveState(StreamWriter writer)
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.Movie.DumpLogIntoSavestateText(writer);
			}
		}

		private void HandleMovieOnFrameLoop()
		{
			if (!Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.LatchInputFromPlayer(Global.MovieInputSourceAdapter);
			}

			else if (Global.MovieSession.Movie.IsFinished)
			{
				if (Global.Emulator.Frame < Global.MovieSession.Movie.Frames) //This scenario can happen from rewinding (suddenly we are back in the movie, so hook back up to the movie
				{
					Global.MovieSession.Movie.StartPlayback();
					Global.MovieSession.LatchInputFromLog();
				}
				else
				{
					Global.MovieSession.LatchInputFromPlayer(Global.MovieInputSourceAdapter);
				}
			}

			else if (Global.MovieSession.Movie.IsPlaying)
			{
				if (Global.Emulator.Frame >= Global.MovieSession.Movie.Frames)
				{
					Global.MovieSession.Movie.Finish();
				}
				else
				{
					Global.MovieSession.Movie.CaptureState();
					Global.MovieSession.LatchInputFromLog();
				}
			}

			else if (Global.MovieSession.Movie.IsRecording)
			{
				Global.MovieSession.Movie.CaptureState();
				if (Global.MovieSession.MultiTrack.IsActive)
				{
					Global.MovieSession.LatchMultitrackPlayerInput(Global.MovieInputSourceAdapter, Global.MultitrackRewiringControllerAdapter);
				}
				else
				{
					Global.MovieSession.LatchInputFromPlayer(Global.MovieInputSourceAdapter);
				}
				//the movie session makes sure that the correct input has been read and merged to its MovieControllerAdapter;
				//this has been wired to Global.MovieOutputHardpoint in RewireInputChain
				Global.MovieSession.Movie.CommitFrame(Global.Emulator.Frame, Global.MovieOutputHardpoint);
			}
		}
	}
}
