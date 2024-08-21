using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Arcades.MAME;
using BizHawk.Emulation.Cores.Atari.Jaguar;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Consoles.Sega.PicoDrive;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SubNESHawk;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm
	{
		public bool StartNewMovie(IMovie movie, bool newMovie)
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
				if (newMovie)
				{
					PopulateWithDefaultHeaderValues(movie, Emulator, GetSettingsAdapterForLoadedCoreUntyped(), Game, FirmwareManager);
					if (movie is ITasMovie tasMovie)
						tasMovie.ClearChanges();
				}
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

				MovieSession.RunQueuedMovie(newMovie, Emulator);
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
					MovieSession.ReadOnly = !MovieSession.ReadOnly;
					AddOnScreenMessage(MovieSession.ReadOnly ? "Movie read-only mode" : "Movie read+write mode");
				}
				else
				{
					AddOnScreenMessage("No movie active");
				}
			}
		}

		/// <summary>
		/// Sets default header values for the given <paramref name="movie"/>.
		/// </summary>
		/// <param name="movie">The movie to fill with values</param>
		private static void PopulateWithDefaultHeaderValues(
			IMovie movie,
			IEmulator emulator,
			ISettingsAdapter settable,
			IGameInfo game,
			FirmwareManager firmwareManager)
		{
			movie.EmulatorVersion = VersionInfo.GetEmuVersion();
			movie.OriginalEmulatorVersion = VersionInfo.GetEmuVersion();
			movie.SystemID = emulator.SystemId;

			if (settable.HasSyncSettings)
			{
				movie.SyncSettingsJson = ConfigService.SaveWithType(settable.GetSyncSettings());
			}

			movie.GameName = game.FilesystemSafeName();
			movie.Hash = game.Hash;
			if (game.FirmwareHash != null)
			{
				movie.FirmwareHash = game.FirmwareHash;
			}

			if (emulator.HasBoardInfo())
			{
				movie.BoardName = emulator.AsBoardInfo().BoardName;
			}

			if (emulator.HasRegions())
			{
				var region = emulator.AsRegionable().Region;
				if (region == DisplayType.PAL)
				{
					movie.HeaderEntries.Add(HeaderKeys.Pal, "1");
				}
			}

			if (firmwareManager.RecentlyServed.Count != 0)
			{
				foreach (var firmware in firmwareManager.RecentlyServed)
				{
					var key = firmware.ID.MovieHeaderKey;
					if (!movie.HeaderEntries.ContainsKey(key))
					{
						movie.HeaderEntries.Add(key, firmware.Hash);
					}
				}
			}

			if (emulator is NDS nds && nds.IsDSi)
			{
				movie.HeaderEntries.Add("IsDSi", "1");

				if (nds.IsDSiWare)
				{
					movie.HeaderEntries.Add("IsDSiWare", "1");
				}
			}

			if ((emulator is NES nes && nes.IsVS)
				|| (emulator is SubNESHawk subnes && subnes.IsVs))
			{
				movie.HeaderEntries.Add("IsVS", "1");
			}

			if (emulator is IGameboyCommon gb)
			{
				//TODO doesn't IsCGBDMGMode imply IsCGBMode?
				if (gb.IsCGBMode) movie.HeaderEntries.Add(gb.IsCGBDMGMode ? "IsCGBDMGMode" : "IsCGBMode", "1");
			}

			if (emulator is SMS sms)
			{
				if (sms.IsSG1000)
				{
					movie.HeaderEntries.Add("IsSGMode", "1");
				}

				if (sms.IsGameGear)
				{
					movie.HeaderEntries.Add("IsGGMode", "1");
				}
			}

			if (emulator is GPGX gpgx && gpgx.IsMegaCD)
			{
				movie.HeaderEntries.Add("IsSegaCDMode", "1");
			}

			if (emulator is PicoDrive pico && pico.Is32XActive)
			{
				movie.HeaderEntries.Add("Is32X", "1");
			}

			if (emulator is VirtualJaguar jag && jag.IsJaguarCD)
			{
				movie.HeaderEntries.Add("IsJaguarCD", "1");
			}

			if (emulator is Ares64 ares && ares.IsDD)
			{
				movie.HeaderEntries.Add("IsDD", "1");
			}

			if (emulator is MAME mame)
			{
				movie.HeaderEntries.Add(HeaderKeys.VsyncAttoseconds, mame.VsyncAttoseconds.ToString());
			}

			if (emulator.HasCycleTiming())
			{
				movie.HeaderEntries.Add(HeaderKeys.CycleCount, "0");
				movie.HeaderEntries.Add(HeaderKeys.ClockRate, "0");
			}

			movie.Core = emulator.Attributes().CoreName;
		}
	}
}
