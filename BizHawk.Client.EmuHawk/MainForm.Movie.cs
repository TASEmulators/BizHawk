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

			if (movie.StartsFromSavestate)
			{
				if (movie.TextSavestate != null)
				{
					Global.Emulator.LoadStateText(new StringReader(movie.TextSavestate));
				}
				else
				{
					Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(movie.BinarySavestate, false)));
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

		public void RestartMovie()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				GlobalWin.MainForm.StartNewMovie(Global.MovieSession.Movie, true);
				GlobalWin.OSD.AddMessage("Replaying movie file in read-only mode");
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
