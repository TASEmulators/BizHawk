using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Client.Common.MovieConversionExtensions;
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

		// Previous saved core preferences. Stored here so that when a movie
		// overrides the values, they can be restored to user preferences 
		private readonly IDictionary<string, string> _preferredCores = new Dictionary<string, string>();

		public MovieSession(
			IMovieConfig settings,
			Action<string> messageCallback,
			Action<string> popupCallback,
			Action pauseCallback,
			Action modeChangedCallback)
		{
			Settings = settings;
			_messageCallback = messageCallback;
			_popupCallback = popupCallback;
			_pauseCallback = pauseCallback
				?? throw new ArgumentNullException($"{nameof(pauseCallback)} cannot be null.");
			_modeChangedCallback = modeChangedCallback
				?? throw new ArgumentNullException($"{nameof(modeChangedCallback)} CannotUnloadAppDomainException be null.");
		}

		public IMovieConfig Settings { get; }

		public IMovie Movie { get; private set; }
		public bool ReadOnly { get; set; } = true;
		public bool NewMovieQueued => _queuedMovie != null;
		public string QueuedSyncSettings => _queuedMovie.SyncSettingsJson;

		public IMovieController MovieController { get; set; } = new Bk2Controller("", NullController.Instance.Definition);

		public MultitrackRecorder MultiTrack { get; } = new MultitrackRecorder();

		public void RecreateMovieController(ControllerDefinition definition)
		{
			MovieController = new Bk2Controller(definition);
		}

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
				else
				{
					// Movie may go into finished mode as a result from latching
					if (!Movie.IsFinished())
					{
						if (Global.InputManager.ClientControls.IsPressed("Scrub Input"))
						{
							LatchInputToUser();
							ClearFrame();
						}
					}
				}
			}
			else if (Movie.IsRecording())
			{
				HandleFrameLoopForRecordMode();
			}
		}

		public void HandleFrameAfter()
		{
			if (Movie is ITasMovie tasMovie)
			{
				tasMovie.GreenzoneCurrentFrame();
				if (tasMovie.IsPlayingOrFinished() && Movie.Emulator.Frame >= tasMovie.InputLogLength)
				{
					HandleFrameLoopForRecordMode();
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

			// Note: this populates MovieControllerAdapter's Type with the appropriate controller
			// Don't set it to a movie instance of the adapter or you will lose the definition!
			Global.InputManager.RewireInputChain();

			if (!record)
			{
				if (preferredCores.ContainsKey(systemId))
				{
					string movieCore = preferredCores[systemId];
					if (string.IsNullOrWhiteSpace(movie.Core))
					{
						PopupMessage($"No core specified in the movie file, using the preferred core {preferredCores[systemId]} instead.");
					}
					else
					{
						movieCore = movie.Core;
					}

					_preferredCores[systemId] = preferredCores[systemId];
					preferredCores[systemId] = movieCore;
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

		public void RunQueuedMovie(bool recordMode, IEmulator emulator, IDictionary<string, string> preferredCores)
		{
			_queuedMovie.Attach(this, emulator);
			foreach (var previousPref in _preferredCores)
			{
				preferredCores[previousPref.Key] = previousPref.Value;
			}

			Movie = _queuedMovie;
			_queuedMovie = null;
			MultiTrack.Restart(Movie.Emulator.ControllerDefinition.PlayerCount);

			Movie.ProcessSavestate(Movie.Emulator);
			Movie.ProcessSram(Movie.Emulator);

			if (recordMode)
			{
				Movie.StartNewRecording();
				ReadOnly = false;
			}
			else
			{
				Movie.StartNewPlayback();
			}

			MultiTrack.Restart(emulator.ControllerDefinition.PlayerCount);
		}

		public void ToggleMultitrack()
		{
			if (Movie.IsActive())
			{
				if (Settings.VBAStyleMovieLoadState)
				{
					Output("Multi-track can not be used in Full Movie Loadstates mode");
				}
				else if (Movie is ITasMovie)
				{
					Output("Multi-track can not be used with tasproj movies");
				}
				else
				{
					MultiTrack.IsActive ^= true;
					MultiTrack.SelectNone();
					Output(MultiTrack.IsActive ? "MultiTrack Enabled" : "MultiTrack Disabled");
				}
			}
			else
			{
				Output("MultiTrack cannot be enabled while not recording.");
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

				MultiTrack.Restart(1);

				var result = Movie.Stop(saveChanges);
				if (result)
				{
					Output($"{Path.GetFileName(Movie.Filename)} written to disk.");
				}

				Output(message);
				ReadOnly = true;

				_modeChangedCallback();
			}

			// TODO: we aren't ready for this line, keeping the old movie hanging around masks a lot of Tastudio problems
			// Uncommenting this can cause drawing crashes in tastudio since it depends on a ITasMovie and doesn't have one between closing and opening a rom
			//Movie = null;
		}

		public void ConvertToTasProj()
		{
			Movie.Save();
			Movie = Movie.ToTasMovie();
			Movie.Save();
			Movie.SwitchToPlay();
		}

		private void ClearFrame()
		{
			if (Movie.IsPlayingOrFinished())
			{
				Movie.ClearFrame(Movie.Emulator.Frame);
				Output($"Scrubbed input at frame {Movie.Emulator.Frame}");
			}
		}

		private void PopupMessage(string message)
		{
			_popupCallback?.Invoke(message);
		}

		private void Output(string message)
		{
			_messageCallback?.Invoke(message);
		}

		private void LatchInputToMultitrackUser()
		{
			var rewiredSource = Global.InputManager.MultitrackRewiringAdapter;
			if (MultiTrack.IsActive)
			{
				rewiredSource.PlayerSource = 1;
				rewiredSource.PlayerTargetMask = 1 << MultiTrack.CurrentPlayer;
				if (MultiTrack.RecordAll)
				{
					rewiredSource.PlayerTargetMask = unchecked((int)0xFFFFFFFF);
				}

				if (Movie.InputLogLength > Movie.Emulator.Frame)
				{
					var input = Movie.GetInputState(Movie.Emulator.Frame);
					MovieController.SetFrom(input);
				}

				MovieController.SetPlayerFrom(rewiredSource, MultiTrack.CurrentPlayer);
			}
		}

		private void LatchInputToUser()
		{
			MovieController.SetFrom(Global.InputManager.MovieInputSourceAdapter);
		}

		// Latch input from the input log, if available
		private void LatchInputToLog()
		{
			var input = Movie.GetInputState(Movie.Emulator.Frame);

			// adelikat: TODO: this is likely the source of frame 0 TAStudio bugs, I think the intent is to check if the movie is 0 length?
			if (Movie.Emulator.Frame == 0) // Hacky
			{
				HandleFrameAfter(); // Frame 0 needs to be handled.
			}

			if (input == null)
			{
				HandleFrameAfter();
				return;
			}

			MovieController.SetFrom(input);
			if (MultiTrack.IsActive)
			{
				Global.InputManager.MultitrackRewiringAdapter.Source = MovieController;
			}
		}

		private void HandlePlaybackEnd()
		{
			if (Movie.Core ==  CoreNames.Gambatte)
			{
				var movieCycles = Convert.ToUInt64(Movie.HeaderEntries[HeaderKeys.CycleCount]);
				var coreCycles = ((Gameboy)Movie.Emulator).CycleCount;
				if (movieCycles != (ulong)coreCycles)
				{
					PopupMessage($"Cycle count in the movie ({movieCycles}) doesn't match the emulated value ({coreCycles}).");
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
				MovieController.SetFromSticky(Global.InputManager.AutofireStickyXorAdapter);
			}
			else
			{
				if (MultiTrack.IsActive)
				{
					LatchInputToMultitrackUser();
				}
				else
				{
					LatchInputToUser();
				}
			}

			// the movie session makes sure that the correct input has been read and merged to its MovieControllerAdapter;
			// this has been wired to Global.MovieOutputHardpoint in RewireInputChain
			Movie.RecordFrame(Movie.Emulator.Frame, Global.InputManager.MovieOutputHardpoint);
		}
	}
}