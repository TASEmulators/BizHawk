using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm
	{
		public bool StartNewMovie(IMovie movie, bool record)
		{
			if (movie is null) throw new ArgumentNullException(paramName: nameof(movie));

			if (CheatList.AnyActive)
			{
				var result = this.ModalMessageBox3(
					caption: "Cheats warning",
					text: "Continue playback with cheats enabled?\nChoosing \"No\" will disable cheats but not remove them.",
					icon: EMsgBoxIcon.Question);
				if (result is null) return false;
				if (result is false) CheatList.DisableAll();
			}
			var oldPreferredCores = new Dictionary<string, string>(Config.PreferredCores);
			try
			{
				try
				{
					MovieSession.QueueNewMovie(
						movie,
						systemId: Emulator.SystemId,
						loadedRomHash: Game.Hash,
						Config.PathEntries,
						Config.PreferredCores);
				}
				catch (MoviePlatformMismatchException ex)
				{
					using var ownerForm = new Form { TopMost = true };
					MessageBox.Show(ownerForm, ex.Message, "Movie/Platform Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}

				if (!_isLoadingRom)
				{
					var rebootSucceeded = RebootCore();
					if (!rebootSucceeded) return false;
				}

				Config.RecentMovies.Add(movie.Filename);

				MovieSession.RunQueuedMovie(record, Emulator);
			}
			finally
			{
				MovieSession.AbortQueuedMovie();
				Config.PreferredCores = oldPreferredCores;
			}

			SetMainformMovieInfo();

			// turns out this was too late for .tasproj autoloading and restoring playback position (loads savestate but wasn't checking game match)
			if (string.IsNullOrEmpty(MovieSession.Movie.Hash))
			{
				AddOnScreenMessage("Movie is missing hash, skipping hash check");
			}
			else if (MovieSession.Movie.Hash != Game.Hash)
			{
				AddOnScreenMessage("Warning: Movie hash does not match the ROM");
			}

			return !Emulator.IsNull();
		}

		public void SetMainformMovieInfo()
		{
			if (MovieSession.Movie.IsPlayingOrFinished())
			{
				PlayRecordStatusButton.Image = Properties.Resources.Play;
				PlayRecordStatusButton.ToolTipText = "Movie is in playback mode";
				PlayRecordStatusButton.Visible = true;
			}
			else if (MovieSession.Movie.IsRecording())
			{
				PlayRecordStatusButton.Image = Properties.Resources.Record;
				PlayRecordStatusButton.ToolTipText = "Movie is in record mode";
				PlayRecordStatusButton.Visible = true;
			}
			else if (MovieSession.Movie.NotActive())
			{
				PlayRecordStatusButton.Image = Properties.Resources.Blank;
				PlayRecordStatusButton.ToolTipText = "No movie is active";
				PlayRecordStatusButton.Visible = false;
			}

			UpdateWindowTitle();
			UpdateStatusSlots();
			Tools.UpdateValues<VirtualpadTool>();
		}

		public void StopMovie(bool saveChanges = true)
		{
			if (ToolControllingStopMovie is { } tool)
			{
				tool.StopMovie(!saveChanges);
			}
			else
			{
				MovieSession.StopMovie(saveChanges);
				SetMainformMovieInfo();
			}
		}

		public bool RestartMovie()
		{
			if (ToolControllingRestartMovie is { } tool) return tool.RestartMovie();
			if (!MovieSession.Movie.IsActive()) return false;
			var success = StartNewMovie(MovieSession.Movie, false);
			if (success) AddOnScreenMessage("Replaying movie file in read-only mode");
			return success;
		}

		private void ToggleReadOnly()
		{
			if (ToolControllingReadOnly is { } tool)
			{
				tool.ToggleReadOnly();
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
