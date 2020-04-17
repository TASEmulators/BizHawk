using System;
using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.GBHawk;

namespace BizHawk.Client.Common
{
	public enum MovieEndAction { Stop, Pause, Record, Finish }

	public class MovieSession : IMovieSession
	{
		private readonly Action _pauseCallback;
		private readonly Action _modeChangedCallback;
		private readonly Action<string> _messageCallback;
		private readonly Action<string> _popupCallback;

		public MovieSession(
			Action<string> messageCallback,
			Action<string> popupCallback,
			Action pauseCallback,
			Action modeChangedCallback)
		{
			_messageCallback = messageCallback;
			_popupCallback = popupCallback;
			_pauseCallback = pauseCallback
				?? throw new ArgumentNullException($"{nameof(pauseCallback)} cannot be null.");
			_modeChangedCallback = modeChangedCallback
				?? throw new ArgumentNullException($"{nameof(modeChangedCallback)} CannotUnloadAppDomainException be null.");

			Movie = MovieService.DefaultInstance;
		}

		public IMovie Movie { get; set; }
		public IMovie QueuedMovie { get; private set; }

		public bool MovieIsQueued => QueuedMovie != null;
		public bool ReadOnly { get; set; } = true;

		public IMovieController MovieController { get; set; } = new Bk2Controller("", NullController.Instance.Definition);

		public MultitrackRecorder MultiTrack { get; } = new MultitrackRecorder();

		public IController CurrentInput
		{
			get
			{
				if (Movie.IsPlayingOrRecording() && Global.Emulator.Frame > 0)
				{
					return Movie.GetInputState(Global.Emulator.Frame - 1);
				}

				return null;
			}
		}

		public IController PreviousFrame
		{
			get
			{
				if (Movie.IsPlayingOrRecording() && Global.Emulator.Frame > 1)
				{
					return Movie.GetInputState(Global.Emulator.Frame - 2);
				}

				return null;
			}
		}

		// The behavior here is to only temporarily override these settings when playing a movie and then restore the user's preferred settings
		// A more elegant approach would be appreciated
		public bool? PreviousNesInQuickNES { get; set; }
		public bool? PreviousSnesInSnes9x { get; set; }
		public bool? PreviousGbaUsemGba { get; set; }
		public bool? PreviousGbUseGbHawk { get; set; }

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
				if (Global.Emulator.Frame < Movie.FrameCount) // This scenario can happen from rewinding (suddenly we are back in the movie, so hook back up to the movie
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
						else if (Global.Config.MoviePlaybackPokeMode)
						{
							LatchInputToUser();
							var lg = Movie.LogGeneratorInstance(Global.InputManager.MovieOutputHardpoint);
							if (!lg.IsEmpty)
							{
								LatchInputToUser();
								Movie.PokeFrame(Global.Emulator.Frame, Global.InputManager.MovieOutputHardpoint);
							}
							else
							{
								// Why, this was already done?
								LatchInputToLog();
							}
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
			if (Movie is TasMovie tasMovie)
			{
				tasMovie.GreenzoneCurrentFrame();
				if (tasMovie.IsPlaying() && Global.Emulator.Frame >= tasMovie.InputLogLength)
				{
					HandleFrameLoopForRecordMode();
				}
			}
			else if (Movie.Mode == MovieMode.Play && Global.Emulator.Frame >= Movie.InputLogLength)
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
				else if (Movie.IsPlaying())
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
				else if (Movie.IsPlaying())
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

		/// <exception cref="MoviePlatformMismatchException"><paramref name="record"/> is <see langword="false"/> and <paramref name="movie"/>.<see cref="IMovie.SystemID"/> does not match <paramref name="emulator"/>.<see cref="IEmulator.SystemId"/></exception>
		public void QueueNewMovie(IMovie movie, bool record, IEmulator emulator)
		{
			if (!record) // The semantics of record is that we are starting a new movie, and even wiping a pre-existing movie with the same path, but non-record means we are loading an existing movie into playback mode
			{
				movie.Load(false);
				
				if (movie.SystemID != emulator.SystemId)
				{
					throw new MoviePlatformMismatchException(
						$"Movie system Id ({movie.SystemID}) does not match the currently loaded platform ({emulator.SystemId}), unable to load");
				}
			}

			// Note: this populates MovieControllerAdapter's Type with the appropriate controller
			// Don't set it to a movie instance of the adapter or you will lose the definition!
			Global.InputManager.RewireInputChain();

			if (!record)
			{
				switch (emulator.SystemId)
				{
					case "NES":
						if (movie.Core == CoreNames.QuickNes)
						{
							PreviousNesInQuickNES = Global.Config.NesInQuickNes;
							Global.Config.NesInQuickNes = true;
						}
						else if (movie.Core == CoreNames.NesHawk)
						{
							PreviousNesInQuickNES = Global.Config.NesInQuickNes;
							Global.Config.NesInQuickNes = false;
						}
						break;
					case "SNES":
						if (movie.Core == CoreNames.Snes9X)
						{
							PreviousSnesInSnes9x = Global.Config.SnesInSnes9x;
							Global.Config.SnesInSnes9x = true;
						}
						else if (movie.Core == CoreNames.Bsnes)
						{
							PreviousSnesInSnes9x = Global.Config.SnesInSnes9x;
							Global.Config.SnesInSnes9x = false;
						}
						break;
					case "GBA":
						if (movie.Core == CoreNames.Mgba)
						{
							PreviousGbaUsemGba = Global.Config.GbaUsemGba;
							Global.Config.GbaUsemGba = true;
						}
						else if (movie.Core == CoreNames.VbaNext)
						{
							PreviousGbaUsemGba = Global.Config.GbaUsemGba;
							Global.Config.GbaUsemGba = false;
						}
						break;
					case "GB":
					case "GBC":
						if (movie.Core == CoreNames.GbHawk)
						{
							PreviousGbUseGbHawk = Global.Config.GbUseGbHawk;
							Global.Config.GbUseGbHawk = true;
						}
						else if (movie.Core == CoreNames.Gambatte)
						{
							PreviousGbUseGbHawk = Global.Config.GbUseGbHawk;
							Global.Config.GbUseGbHawk = false;
						}
						break;
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

			QueuedMovie = movie;
		}

		public void RunQueuedMovie(bool recordMode)
		{
			Movie = QueuedMovie;
			QueuedMovie = null;
			MultiTrack.Restart(Global.Emulator.ControllerDefinition.PlayerCount);

			if (recordMode)
			{
				Movie.StartNewRecording();
				ReadOnly = false;
			}
			else
			{
				Movie.StartNewPlayback();
			}
		}

		public void ToggleMultitrack()
		{
			if (Movie.IsActive())
			{
				if (Global.Config.VBAStyleMovieLoadState)
				{
					Output("Multi-track can not be used in Full Movie Loadstates mode");
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
			var message = "Movie ";
			if (Movie.IsRecording())
			{
				message += "recording ";
			}
			else if (Movie.IsPlaying())
			{
				message += "playback ";
			}

			message += "stopped.";

			if (Movie.IsActive())
			{
				var result = Movie.Stop(saveChanges);
				if (result)
				{
					Output($"{Path.GetFileName(Movie.Filename)} written to disk.");
				}

				Output(message);
				ReadOnly = true;
			}

			MultiTrack.Restart(Global.Emulator.ControllerDefinition.PlayerCount);
			_modeChangedCallback();
		}

		private void ClearFrame()
		{
			if (Movie.IsPlaying())
			{
				Movie.ClearFrame(Global.Emulator.Frame);
				Output($"Scrubbed input at frame {Global.Emulator.Frame}");
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

				if (Movie.InputLogLength > Global.Emulator.Frame)
				{
					var input = Movie.GetInputState(Global.Emulator.Frame);
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
			var input = Movie.GetInputState(Global.Emulator.Frame);

			// adelikat: TODO: this is likely the source of frame 0 TAStudio bugs, I think the intent is to check if the movie is 0 length?
			if (Global.Emulator.Frame == 0) // Hacky
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
			var gambatteName = ((CoreAttribute)Attribute.GetCustomAttribute(typeof(Gameboy), typeof(CoreAttribute))).CoreName;
			if (Movie.Core == gambatteName)
			{
				var movieCycles = Convert.ToUInt64(Movie.HeaderEntries[HeaderKeys.CycleCount]);
				var coreCycles = ((Gameboy)Global.Emulator).CycleCount;
				if (movieCycles != (ulong)coreCycles)
				{
					PopupMessage($"Cycle count in the movie ({movieCycles}) doesn't match the emulated value ({coreCycles}).");
				}
			}

			// TODO: mainform callback to update on mode change
			switch (Global.Config.MovieEndAction)
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
			if (Movie is TasMovie && Movie.IsPlaying())
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
			Movie.RecordFrame(Global.Emulator.Frame, Global.InputManager.MovieOutputHardpoint);
		}
	}
}