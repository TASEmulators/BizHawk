using System;
using System.IO;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
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

			if (Global.MovieSession.PreviousNES_InQuickNES.HasValue)
			{
				Global.Config.NES_InQuickNES = Global.MovieSession.PreviousNES_InQuickNES.Value;
				Global.MovieSession.PreviousNES_InQuickNES = null;
			}

			if (Global.MovieSession.PreviousSNES_InSnes9x.HasValue)
			{
				Global.Config.SNES_InSnes9x = Global.MovieSession.PreviousSNES_InSnes9x.Value;
				Global.MovieSession.PreviousSNES_InSnes9x = null;
			}

			Global.Config.RecentMovies.Add(movie.Filename);

			if (Global.Emulator.HasSavestates() && movie.StartsFromSavestate)
			{
				if (movie.TextSavestate != null)
				{
					(Global.Emulator as IStatable).LoadStateText(new StringReader(movie.TextSavestate));
				}
				else
				{
					(Global.Emulator as IStatable).LoadStateBinary(new BinaryReader(new MemoryStream(movie.BinarySavestate, false)));
				}
				if (movie.SavestateFramebuffer != null)
				{
					var b1 = movie.SavestateFramebuffer;
					var b2 = Global.Emulator.VideoProvider.GetVideoBuffer();
					int len = Math.Min(b1.Length, b2.Length);
					for (int i = 0; i < len; i++)
					{
						b2[i] = b1[i];
					}
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
			if (IsSlave && master.WantsToControlRestartMovie)
			{
				master.RestartMovie();
			}
			else
			{
				if (Global.MovieSession.Movie.IsActive)
				{
					GlobalWin.MainForm.StartNewMovie(Global.MovieSession.Movie, false);
					GlobalWin.OSD.AddMessage("Replaying movie file in read-only mode");
				}
			}
		}
	}
}
