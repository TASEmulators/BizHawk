using System;
using System.IO;
using System.Windows.Forms;

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
		public bool StartNewMovie(IMovie movie, bool record)
		{
			if (movie.IsActive)
			{
				movie.Save();
			}

			try
			{
				Global.MovieSession.QueueNewMovie(movie, record);
			}
			catch (MoviePlatformMismatchException ex)
			{
				MessageBox.Show(this, ex.Message, "Movie/Platform Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

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

			GlobalWin.Tools.Restart<VirtualpadTool>();
			GlobalWin.DisplayManager.NeedsToPaint = true;

			return true;
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
			UpdateStatusSlots();
		}

		public void RestartMovie()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				GlobalWin.MainForm.StartNewMovie(Global.MovieSession.Movie, false);
				GlobalWin.OSD.AddMessage("Replaying movie file in read-only mode");
			}
		}
	}
}
