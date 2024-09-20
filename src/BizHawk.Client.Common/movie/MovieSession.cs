using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common.PathExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public enum MovieEndAction { Stop, Pause, Record, Finish }

	public class MovieSession : IMovieSession
	{
		private readonly IDialogParent _dialogParent;

		private readonly Action _pauseCallback;
		private readonly Action _modeChangedCallback;

		private IMovie _queuedMovie;

		public MovieSession(
			IMovieConfig settings,
			string backDirectory,
			IDialogParent dialogParent,
			Action pauseCallback,
			Action modeChangedCallback)
		{
			Settings = settings;
			BackupDirectory = backDirectory;
			_dialogParent = dialogParent;
			_pauseCallback = pauseCallback
				?? throw new ArgumentNullException(paramName: nameof(pauseCallback));
			_modeChangedCallback = modeChangedCallback
				?? throw new ArgumentNullException(paramName: nameof(modeChangedCallback));
		}

		public IMovieConfig Settings { get; }

		public string BackupDirectory { get; set; }

		public IMovie Movie { get; private set; }
		public bool ReadOnly { get; set; } = true;
		public bool NewMovieQueued => _queuedMovie != null;
		public string QueuedSyncSettings => _queuedMovie.SyncSettingsJson;

		public string QueuedCoreName => _queuedMovie?.Core;

		public IDictionary<string, object> UserBag { get; set; } = new Dictionary<string, object>();

		public IController MovieIn { get; set; }
		public IInputAdapter MovieOut { get; } = new CopyControllerAdapter();
		public IController StickySource { get; set; }

		public IMovieController MovieController { get; private set; } = new Bk2Controller(NullController.Instance.Definition);

		public IMovieController GenerateMovieController(ControllerDefinition definition = null, string logKey = null)
		{
			// TODO: should this fallback to Movie.LogKey?
			// this function is kinda weird
			return new Bk2Controller(definition ?? MovieController.Definition, logKey);
		}

		public void HandleFrameBefore()
		{
			if (Movie.NotActive())
			{
				LatchInputToUser();
			}
			else if (Movie.IsFinished())
			{
				if (Movie.Emulator.Frame < Movie.FrameCount) // This scenario can happen from rewinding (suddenly we are back in the movie, so hook back up to the movie
				{
					Movie.SwitchToPlay();
					LatchInputToLog();
				}
				else
				{
					LatchInputToUser();
				}
			}
			else if (Movie.IsPlaying())
			{
				LatchInputToLog();
				// if we're at the movie's end and the MovieEndAction is record, just continue recording in play mode
				// TODO change to TAStudio check
				if (Movie is ITasMovie && Movie.Emulator.Frame == Movie.FrameCount && Settings.MovieEndAction == MovieEndAction.Record)
				{
					Movie.RecordFrame(Movie.Emulator.Frame, MovieOut.Source);
				}
			}
			else if (Movie.IsRecording())
			{
				LatchInputToUser();
				Movie.RecordFrame(Movie.Emulator.Frame, MovieOut.Source);
			}
		}

		public void HandleFrameAfter()
		{
			if (Movie is ITasMovie tasMovie)
			{
				tasMovie.GreenzoneCurrentFrame();
				// TODO change to TAStudio check
				if (Settings.MovieEndAction == MovieEndAction.Record) return;
			}

			if (Movie.IsPlaying() && Movie.Emulator.Frame >= Movie.FrameCount)
			{
				HandlePlaybackEnd();
			}
		}

		public void HandleSaveState(TextWriter writer)
		{
			if (Movie.IsActive())
			{
				Movie.WriteInputLog(writer);
			}
		}

		public bool CheckSavestateTimeline(TextReader reader)
		{
			if (Movie.IsActive() && ReadOnly)
			{
				var result = Movie.CheckTimeLines(reader, out var errorMsg);
				if (!result)
				{
					Output(errorMsg);
					return false;
				}
			}

			return true;
		}

		public bool HandleLoadState(TextReader reader)
		{
			if (Movie.NotActive())
			{
				return true;
			}

			if (ReadOnly)
			{
				if (Movie.IsRecording())
				{
					Movie.SwitchToPlay();
				}
				else if (Movie.IsPlayingOrFinished())
				{
					// set the controller state to the previous frame for input display purposes
					int previousFrame = Movie.Emulator.Frame - 1;
					Movie.Session.MovieController.SetFrom(Movie.GetInputState(previousFrame));
				}
			}
			else
			{
				if (Movie.IsFinished())
				{
					Movie.StartNewRecording();
				}
				else if (Movie.IsPlayingOrFinished())
				{
					Movie.SwitchToRecord();
				}

				var result = Movie.ExtractInputLog(reader, out var errorMsg);
				if (!result)
				{
					Output(errorMsg);
					return false;
				}

				LatchInputToUser();
			}

			return true;
		}

		/// <exception cref="MoviePlatformMismatchException"><paramref name="movie"/>.<see cref="IBasicMovieInfo.SystemID"/> does not match <paramref name="systemId"/>.<see cref="IEmulator.SystemId"/></exception>
		public void QueueNewMovie(
			IMovie movie,
			string systemId,
			string loadedRomHash,
			PathEntryCollection pathEntries,
			IDictionary<string, string> preferredCores)
		{
			if (movie.SystemID != systemId)
			{
				throw new MoviePlatformMismatchException(
					$"Movie system Id ({movie.SystemID}) does not match the currently loaded platform ({systemId}), unable to load");
			}

			if (!(string.IsNullOrEmpty(movie.Hash) || loadedRomHash.Equals(movie.Hash, StringComparison.Ordinal))
				&& movie is TasMovie tasproj)
			{
				var result = _dialogParent.ModalMessageBox2(
					caption: "Discard GreenZone?",
					text: $"The TAStudio project {movie.Filename.MakeRelativeTo(pathEntries.MovieAbsolutePath())} appears to be for a different game than the one that's loaded.\n"
						+ "Choose \"No\" to continue anyway, which may lead to an invalid savestate being loaded.\n"
						+ "Choose \"Yes\" to discard the GreenZone (savestate history). This is safer, and at worst you'll only need to watch through the whole movie.");
				//TODO add abort option
				if (result)
				{
					tasproj.TasSession.UpdateValues(frame: 0, currentBranch: tasproj.TasSession.CurrentBranch); // wtf is this API --yoshi
					tasproj.InvalidateEntireGreenzone();
				}
			}

			if (string.IsNullOrWhiteSpace(movie.Core))
			{
				PopupMessage(preferredCores.TryGetValue(systemId, out var coreName)
					? $"No core specified in the movie file, using the preferred core {coreName} instead."
					: "No core specified in the movie file, using the default core instead.");
			}
			else
			{
				var keys = preferredCores.Keys.ToList();
				foreach (var k in keys)
				{
					preferredCores[k] = movie.Core;
				}
			}

			_queuedMovie = movie;
		}

		public void RunQueuedMovie(bool recordMode, IEmulator emulator)
		{
			MovieController = new Bk2Controller(emulator.ControllerDefinition);

			Movie = _queuedMovie;
			Movie.Attach(emulator);
			_queuedMovie = null;

			Movie.ProcessSavestate(Movie.Emulator);
			Movie.ProcessSram(Movie.Emulator);

			if (recordMode)
			{
				Movie.StartNewRecording();
				ReadOnly = false;
				// If we are starting a movie recording while another one is playing, we need to switch back to user input
				LatchInputToUser();
			}
			else
			{
				Movie.StartNewPlayback();
			}
		}

		public void AbortQueuedMovie()
			=> _queuedMovie = null;

		public void StopMovie(bool saveChanges = true)
		{
			if (Movie.IsActive())
			{
				var message = "Movie ";
				if (Movie.IsRecording())
				{
					message += "recording ";
				}
				else if (Movie.IsPlayingOrFinished())
				{
					message += "playback ";
				}

				message += "stopped.";

				var result = Movie.Stop(saveChanges);
				if (result)
				{
					Output($"{Path.GetFileName(Movie.Filename)} written to disk.");
				}

				Output(message);
				ReadOnly = true;

				_modeChangedCallback();
			}

			if (Movie is IDisposable d
				&& Movie != _queuedMovie) // Uberhack, remove this and Loading Tastudio with a bk2 already loaded breaks, probably other TAStudio scenarios as well
			{
				d.Dispose();
			}

			Movie = null;
		}

		public void ConvertToTasProj()
		{
			Movie = Movie.ToTasMovie();
			Movie.Save();
			Movie.SwitchToPlay();
		}

		public IMovie Get(string path, bool loadMovie)
		{
			// TODO: change IMovies to take HawkFiles only and not path
			IMovie movie = Path.GetExtension(path)?.EndsWithOrdinal("tasproj") is true
				? new TasMovie(this, path)
				: new Bk2Movie(this, path);

			if (loadMovie)
				movie.Load();

			return movie;
		}

		public void PopupMessage(string message) => _dialogParent.ModalMessageBox(message, "Warning", EMsgBoxIcon.Warning);

		private void Output(string message)
			=> _dialogParent.AddOnScreenMessage(message);

		private void LatchInputToUser()
		{
			MovieOut.Source = MovieIn;
		}

		// Latch input from the input log, if available
		private void LatchInputToLog()
		{
			var input = Movie.GetInputState(Movie.Emulator.Frame);

			MovieController.SetFrom(input ?? StickySource);
			MovieOut.Source = MovieController;
		}

		private void HandlePlaybackEnd()
		{
#if false // invariants given by single call-site
			Debug.Assert(Movie.IsPlaying());
			Debug.Assert(Movie.Emulator.Frame >= Movie.InputLogLength);
#endif
#if false // code below doesn't actually do anything as the cycle count is indiscriminately overwritten (or removed) on save anyway.
			if (Movie.IsAtEnd() && Movie.Emulator.HasCycleTiming())
			{
				const string WINDOW_TITLE_MISMATCH = "Cycle count mismatch";
				const string WINDOW_TITLE_MISSING = "Cycle count not yet saved";
				const string PFX_MISSING = "The cycle count (running time) hasn't been saved into this movie yet.\n";
				const string ERR_MSG_MISSING_READONLY = PFX_MISSING + "The movie was loaded in read-only mode. To add the cycle count, load it in read-write mode and play to the end again.";
				const string ERR_MSG_MISSING_CONFIRM = PFX_MISSING + "Add it now?";
				const string PFX_MISMATCH = "The cycle count (running time) saved into this movie ({0}) doesn't match the measured count ({1}) here at the end.\n";
				const string ERR_FMT_STR_MISMATCH_READONLY = PFX_MISMATCH + "The movie was loaded in read-only mode. To correct the cycle count, load it in read-write mode and play to the end again.";
				const string ERR_FMT_STR_MISMATCH_CONFIRM = PFX_MISMATCH + "Correct it now?";
				var coreValue = Movie.Emulator.AsCycleTiming().CycleCount;
				if (!Movie.HeaderEntries.TryGetValue(HeaderKeys.CycleCount, out var movieValueStr)
					|| !long.TryParse(movieValueStr, out var movieValue))
				{
					if (ReadOnly)
					{
						//TODO this would be annoying to encounter; detect the missing field when playback starts instead --yoshi
						_dialogParent.ModalMessageBox(
							caption: WINDOW_TITLE_MISSING,
							text: ERR_MSG_MISSING_READONLY,
							icon: EMsgBoxIcon.Info);
					}
					else if (_dialogParent.ModalMessageBox2(
						caption: WINDOW_TITLE_MISSING,
						text: ERR_MSG_MISSING_CONFIRM,
						icon: EMsgBoxIcon.Question))
					{
						Movie.SetCycleValues();
					}
				}
				else if (coreValue != movieValue)
				{
					if (ReadOnly)
					{
						//TODO this would be annoying to encounter; detect the missing field when playback starts instead --yoshi
						_dialogParent.ModalMessageBox(
							caption: WINDOW_TITLE_MISMATCH,
							text: string.Format(ERR_FMT_STR_MISMATCH_READONLY, movieValue, coreValue),
							icon: EMsgBoxIcon.Warning);
					}
					else if (_dialogParent.ModalMessageBox2(
						caption: WINDOW_TITLE_MISMATCH,
						text: string.Format(ERR_FMT_STR_MISMATCH_CONFIRM, movieValue, coreValue),
						icon: EMsgBoxIcon.Question))
					{
						Movie.SetCycleValues();
					}
				}
			}
#endif
			switch (Settings.MovieEndAction)
			{
				case MovieEndAction.Stop:
					Movie.Stop();
					break;
				case MovieEndAction.Record:
					Movie.SwitchToRecord();
					break;
				case MovieEndAction.Pause:
					Movie.FinishedMode();
					_pauseCallback();
					break;
				default:
				case MovieEndAction.Finish:
					Movie.FinishedMode();
					break;
			}

			_modeChangedCallback();
		}
	}
}
