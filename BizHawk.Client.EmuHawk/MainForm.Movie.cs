using System;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	partial class MainForm
	{
		public bool StartNewMovie(IMovie movie, bool record)
		{
			if (movie == null)
			{
				throw new ArgumentNullException($"{nameof(movie)} cannot be null.");
			}

			try
			{
				MovieSession.QueueNewMovie(movie, record, Emulator);
			}
			catch (MoviePlatformMismatchException ex)
			{
				using var ownerForm = new Form { TopMost = true };
				MessageBox.Show(ownerForm, ex.Message, "Movie/Platform Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			RebootCore();

			Config.RecentMovies.Add(movie.Filename);

			MovieSession.RunQueuedMovie(record);

			SetMainformMovieInfo();

			if (MovieSession.Movie.Hash != Game.Hash)
			{
				AddOnScreenMessage("Warning: Movie hash does not match the ROM");
			}

			return !Emulator.IsNull();
		}

		public void SetMainformMovieInfo()
		{
			if (MovieSession.Movie.IsPlaying())
			{
				PlayRecordStatusButton.Image = Properties.Resources.Play;
				PlayRecordStatusButton.ToolTipText = "Movie is in playback mode";
				PlayRecordStatusButton.Visible = true;
			}
			else if (MovieSession.Movie.IsRecording())
			{
				PlayRecordStatusButton.Image = Properties.Resources.RecordHS;
				PlayRecordStatusButton.ToolTipText = "Movie is in record mode";
				PlayRecordStatusButton.Visible = true;
			}
			else if (!MovieSession.Movie.IsActive())
			{
				PlayRecordStatusButton.Image = Properties.Resources.Blank;
				PlayRecordStatusButton.ToolTipText = "No movie is active";
				PlayRecordStatusButton.Visible = false;
			}

			SetWindowText();
			UpdateStatusSlots();
		}

		private void StopMovie(bool saveChanges = true)
		{
			if (IsSlave && Master.WantsToControlStopMovie)
			{
				Master.StopMovie(!saveChanges);
			}
			else
			{
				MovieSession.StopMovie(saveChanges);
				SetMainformMovieInfo();
			}
		}

		private void RestartMovie()
		{
			if (IsSlave && Master.WantsToControlRestartMovie)
			{
				Master.RestartMovie();
			}
			else if (MovieSession.Movie.IsActive())
			{
				StartNewMovie(MovieSession.Movie, false);
				AddOnScreenMessage("Replaying movie file in read-only mode");
			}
		}

		private void ToggleReadOnly()
		{
			if (IsSlave && Master.WantsToControlReadOnly)
			{
				Master.ToggleReadOnly();
			}
			else
			{
				if (MovieSession.Movie.IsActive())
				{
					MovieSession.ReadOnly ^= true;
					AddOnScreenMessage(MovieSession.ReadOnly ? "Movie read-only mode" : "Movie read+write mode");
				}
				else
				{
					AddOnScreenMessage("No movie active");
				}
			}
		}
	}
}
