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
		public void StartNewMovie(string path, bool record)
		{
			StartNewMovie(MovieService.Get(Global.Config.RecentMovies.MostRecent), false);
		}

		public bool StartNewMovie(IMovie movie, bool record)
		{
			// SuuperW: Check changes. adelikat: this could break bk2 movies
			// TODO: Clean up the saving process
			if (movie.IsActive && (movie.Changes || !(movie is TasMovie)))
			{
				movie.Save();
			}

			try
			{
				var tasmovie = (movie as TasMovie);
				if (tasmovie != null)
					tasmovie.TasStateManager.MountWriteAccess();
				Global.MovieSession.QueueNewMovie(movie, record, Emulator);
			}
			catch (MoviePlatformMismatchException ex)
			{
				MessageBox.Show(this, ex.Message, "Movie/Platform Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			RebootCore();

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

			if (Global.MovieSession.PreviousGBA_UsemGBA.HasValue)
			{
				Global.Config.GBA_UsemGBA = Global.MovieSession.PreviousGBA_UsemGBA.Value;
				Global.MovieSession.PreviousGBA_UsemGBA = null;
			}

			Global.Config.RecentMovies.Add(movie.Filename);

			if (Emulator.HasSavestates() && movie.StartsFromSavestate)
			{
				if (movie.TextSavestate != null)
				{
					Emulator.AsStatable().LoadStateText(new StringReader(movie.TextSavestate));
				}
				else
				{
					Emulator.AsStatable().LoadStateBinary(new BinaryReader(new MemoryStream(movie.BinarySavestate, false)));
				}

				if (movie.SavestateFramebuffer != null && Emulator.HasVideoProvider())
				{
					var b1 = movie.SavestateFramebuffer;
					var b2 = Emulator.AsVideoProvider().GetVideoBuffer();
					int len = Math.Min(b1.Length, b2.Length);
					for (int i = 0; i < len; i++)
					{
						b2[i] = b1[i];
					}
				}

				Emulator.ResetCounters();
			}
			else if (Emulator.HasSaveRam() && movie.StartsFromSaveRam)
			{
				Emulator.AsSaveRam().StoreSaveRam(movie.SaveRam);
			}

			Global.MovieSession.RunQueuedMovie(record);

			SetMainformMovieInfo();

			GlobalWin.Tools.Restart<VirtualpadTool>();


			if (Global.MovieSession.Movie.Hash != Global.Game.Hash)
			{
				GlobalWin.OSD.AddMessage("Warning: Movie hash does not match the ROM");
			}

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
					StartNewMovie(Global.MovieSession.Movie, false);
					GlobalWin.OSD.AddMessage("Replaying movie file in read-only mode");
				}
			}
		}
	}
}
