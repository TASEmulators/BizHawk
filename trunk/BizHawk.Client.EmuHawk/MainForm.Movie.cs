using System;
using System.IO;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using BizHawk.Emulation.Cores.Nintendo.SNES;

namespace BizHawk.Client.EmuHawk
{
	partial class MainForm
	{
		public void StartNewMovie(IMovie movie, bool record)
		{
			Global.MovieSession.QueueNewMovie(movie, record);

			LoadRom(GlobalWin.MainForm.CurrentlyOpenRom);

			Global.Config.RecentMovies.Add(movie.Filename);

			if (Global.MovieSession.Movie.StartsFromSavestate)
			{
				if (Global.MovieSession.Movie.TextSavestate != null)
				{
					Global.Emulator.LoadStateText(new StringReader(Global.MovieSession.Movie.TextSavestate));
				}
				else
				{
					Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(Global.MovieSession.Movie.BinarySavestate, false)));
				}

				Global.Emulator.ResetCounters();
			}

			Global.MovieSession.RunQueuedMovie(record);

			SetMainformMovieInfo();
			UpdateStatusSlots();

			GlobalWin.Tools.Restart<VirtualpadTool>();
			GlobalWin.DisplayManager.NeedsToPaint = true;
		}

		public void SetMainformMovieInfo()
		{
			if (Global.MovieSession.Movie.IsPlaying)
			{
				PlayRecordStatusButton.Image = Properties.Resources.Play;
				PlayRecordStatusButton.ToolTipText = "Movie is in playback mode";
				PlayRecordStatusButton.Visible = true;
			}
			else if (Global.MovieSession.Movie.IsRecording)
			{
				PlayRecordStatusButton.Image = Properties.Resources.RecordHS;
				PlayRecordStatusButton.ToolTipText = "Movie is in record mode";
				PlayRecordStatusButton.Visible = true;
			}
			else if (!Global.MovieSession.Movie.IsActive)
			{
				PlayRecordStatusButton.Image = Properties.Resources.Blank;
				PlayRecordStatusButton.ToolTipText = "No movie is active";
				PlayRecordStatusButton.Visible = false;
			}

			SetWindowText();
		}

		// Movie Refactor TODO: this needs to be considered, and adapated to the queue system
		public void RestartMovie()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				LoadRom(CurrentlyOpenRom);
				if (Global.MovieSession.Movie.StartsFromSavestate)
				{
					// TODO: why does this code exist twice??

					if (Global.MovieSession.Movie.TextSavestate != null)
					{
						Global.Emulator.LoadStateText(new StringReader(Global.MovieSession.Movie.TextSavestate));
					}
					else
					{
						Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(Global.MovieSession.Movie.BinarySavestate, false)));
					}

					//var state = Convert.FromBase64String(Global.MovieSession.Movie.SavestateBinaryBase64Blob);
					//Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(state)));
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
			if (IsSlave && _master.WantsToCOntrolStopMovie)
			{
				_master.StopMovie();
			}
			else
			{
				Global.MovieSession.StopMovie(saveChanges);
				SetMainformMovieInfo();
				UpdateStatusSlots();
			}
		}
	}
}
