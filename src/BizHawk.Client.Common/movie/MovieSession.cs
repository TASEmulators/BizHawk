using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;

namespace BizHawk.Client.Common
{
	public enum MovieEndAction { Stop, Pause, Record, Finish }

	public class MovieSession : IMovieSession
	{
		private readonly Action _pauseCallback;
		private readonly Action _modeChangedCallback;
		private readonly Action<string> _messageCallback;
		private readonly Action<string> _popupCallback;

		private IMovie _queuedMovie;

		public MovieSession(
			IMovieConfig settings,
			string backDirectory,
			Action<string> messageCallback,
			Action<string> popupCallback,
			Action pauseCallback,
			Action modeChangedCallback)
		{
			Settings = settings;
			BackupDirectory = backDirectory;
			_messageCallback = messageCallback;
			_popupCallback = popupCallback;
			_pauseCallback = pauseCallback
				?? throw new ArgumentNullException($"{nameof(pauseCallback)} cannot be null.");
			_modeChangedCallback = modeChangedCallback
				?? throw new ArgumentNullException($"{nameof(modeChangedCallback)} CannotUnloadAppDomainException be null.");
		}

		public IMovieConfig Settings { get; }

		public string BackupDirectory { get; set; }

		public IMovie Movie { get; private set; }
		public bool ReadOnly { get; set; } = true;
		public bool NewMovieQueued => _queuedMovie != null;
		public string QueuedSyncSettings => _queuedMovie.SyncSettingsJson;

		public IDictionary<string, object> UserBag { get; set; } = new Dictionary<string, object>();

		public IInputAdapter MovieIn { private get; set; }
		public IInputAdapter MovieOut { get; } = new CopyControllerAdapter();
		public IStickyAdapter StickySource { get; set; }

		public IMovieController MovieController { get; private set; } = new Bk2Controller("", NullController.Instance.Definition);

		public IMovieController GenerateMovieController(ControllerDefinition definition = null)
		{
			// TODO: expose Movie.LogKey and pass in here
			return new Bk2Controller("", definition ?? MovieController.Definition);
		}

		public void HandleFrameBefore()
		{
			if (!Movie.IsActive())
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
			else if (Movie.IsPlayingOrFinished())
			{
				LatchInputToLog();

				if (Movie.IsRecording()) // The movie end situation can cause the switch to record mode, in that case we need to capture some input for this frame
				{
					HandleFrameLoopForRecordMode();
				}
			}
			else if (Movie.IsRecording())
			{
				HandleFrameLoopForRecordMode();
			}
		}

		// TODO: this is a mess, simplify
		public void HandleFrameAfter()
		{
			if (Movie is ITasMovie tasMovie)
			{
				tasMovie.GreenzoneCurrentFrame();
				if (tasMovie.IsPlayingOrFinished() && Movie.Emulator.Frame >= tasMovie.InputLogLength)
				{
					if (Settings.MovieEndAction == MovieEndAction.Record)
					{
						HandleFrameLoopForRecordMode();
					}
					else
					{
						HandlePlaybackEnd();
					}
				}
			}
			else if (Movie.IsPlaying() && Movie.Emulator.Frame >= Movie.InputLogLength)
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
					LatchInputToLog();
				}
				else if (Movie.IsFinished())
				{
					LatchInputToUser();
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

		/// <exception cref="MoviePlatformMismatchException"><paramref name="record"/> is <see langword="false"/> and <paramref name="movie"/>.<see cref="IMovie.SystemID"/> does not match <paramref name="systemId"/>.<see cref="IEmulator.SystemId"/></exception>
		public void QueueNewMovie(IMovie movie, bool record, string systemId, IDictionary<string, string> preferredCores)
		{
			if (movie.IsActive() && movie.Changes)
			{
				movie.Save();
			}

			if (!record) // The semantics of record is that we are starting a new movie, and even wiping a pre-existing movie with the same path, but non-record means we are loading an existing movie into playback mode
			{
				movie.Load(false);
				
				if (movie.SystemID != systemId)
				{
					throw new MoviePlatformMismatchException(
						$"Movie system Id ({movie.SystemID}) does not match the currently loaded platform ({systemId}), unable to load");
				}
			}

			if (!record)
			{
				if (string.IsNullOrWhiteSpace(movie.Core))
				{
					if (preferredCores.TryGetValue(systemId, out var coreName))
					{
						PopupMessage($"No core specified in the movie file, using the preferred core {preferredCores[systemId]} instead.");
					}
					else
					{
						PopupMessage($"No core specified in the movie file, using the default core instead.");
					}
				}
				else
				{
					var keys = preferredCores.Keys.ToList();
					foreach (var k in keys)
						preferredCores[k] = movie.Core;
				}
			}

			if (record) // This is a hack really, we need to set the movie to its proper state so that it will be considered active later
			{
				movie.SwitchToRecord();
			}
			else
			{
				movie.SwitchToPlay();
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

		public IMovie Get(string path)
		{
			// TODO: change IMovies to take HawkFiles only and not path
			if (Path.GetExtension(path)?.EndsWith("tasproj") ?? false)
			{
				return new TasMovie(this, path);
			}

			return new Bk2Movie(this, path);
		}

		public void PopupMessage(string message)
		{
			_popupCallback?.Invoke(message);
		}

		private void Output(string message)
		{
			_messageCallback?.Invoke(message);
		}

		private void LatchInputToUser()
		{
			MovieOut.Source = MovieIn;
		}

		// Latch input from the input log, if available
		private void LatchInputToLog()
		{
			var input = Movie.GetInputState(Movie.Emulator.Frame);
			if (input == null)
			{
				HandleFrameAfter();
				return;
			}

			MovieController.SetFrom(input);
			MovieOut.Source = MovieController;
		}

		private void HandlePlaybackEnd()
		{
			if (Movie.IsAtEnd() && Movie.Core == CoreNames.Gambatte)
			{
				var coreCycles = (ulong) ((Gameboy)Movie.Emulator).CycleCount;
				var cyclesSaved = Movie.HeaderEntries.ContainsKey(HeaderKeys.CycleCount);
				ulong previousCycles = 0;
				if (cyclesSaved)
				{
					previousCycles = Convert.ToUInt64(Movie.HeaderEntries[HeaderKeys.CycleCount]);
				}
				var cyclesMatch = previousCycles == coreCycles;
				if (!cyclesSaved || !cyclesMatch)
				{
					var previousState = !cyclesSaved ? "The saved movie is currently missing a cycle count." : $"The previous cycle count ({previousCycles}) doesn't match.";
					// TODO: Ideally, this would be a Yes/No MessageBox that saves when "Yes" is pressed.
					PopupMessage($"The end of the movie has been reached.\n\n{previousState}\n\nSave to update to the new cycle count ({coreCycles}).");
				}
			}

			// TODO: mainform callback to update on mode change
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

		private void HandleFrameLoopForRecordMode()
		{
			// we don't want TasMovie to latch user input outside its internal recording mode, so limit it to autohold
			if (Movie is ITasMovie && Movie.IsPlayingOrFinished())
			{
				MovieController.SetFromSticky(StickySource);
			}
			else
			{
				MovieController.SetFrom(MovieIn);
			}

			Movie.RecordFrame(Movie.Emulator.Frame, MovieController);
		}
	}
}