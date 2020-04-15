using System;
using System.IO;

using BizHawk.Emulation.Common;
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
		/// <summary>
		/// Gets the queued movie
		/// When initializing a movie, it will be stored here until Rom processes have been completed, then it will be moved to the Movie property
		/// If an existing movie is still active, it will remain in the Movie property while the new movie is queued
		/// </summary>
		public IMovie QueuedMovie { get; private set; }

		// This wrapper but the logic could change, don't make the client code understand these details
		public bool MovieIsQueued => QueuedMovie != null;

		public MultitrackRecorder MultiTrack { get; } = new MultitrackRecorder();
		public IMovieController MovieControllerAdapter { get; set; } = MovieService.DefaultInstance.LogGeneratorInstance().MovieControllerAdapter;

		public IMovie Movie { get; set; }
		public bool ReadOnly { get; set; } = true;
		public Action<string> MessageCallback { get; set; }
		public Action<string> PopupCallback { get; set; }
		public Func<string, string, bool> AskYesNoCallback { get; set; }

		/// <summary>
		/// Gets or sets a callback that allows the movie session to pause the emulator
		/// This is Required!
		/// </summary>
		public Action PauseCallback { get; set; }

		/// <summary>
		/// Gets or sets a callback that is invoked when the movie mode has changed
		/// This is Required!
		/// </summary>
		public Action ModeChangedCallback { get; set; }

		public void SetMovieController(ControllerDefinition definition)
		{
			MovieControllerAdapter = new Bk2Controller(definition);
		}

		/// <summary>
		/// Simply shortens the verbosity necessary otherwise
		/// </summary>
		public ILogEntryGenerator LogGeneratorInstance(IController source)
		{
			var lg = Movie.LogGeneratorInstance();
			lg.SetSource(source);
			return lg;
		}

		public IMovieController MovieControllerInstance()
		{
			return Movie.LogGeneratorInstance().MovieControllerAdapter;
		}

		// Convenience property that gets the controller state from the movie for the most recent frame
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

		private void PopupMessage(string message)
		{
			PopupCallback?.Invoke(message);
		}

		private void Output(string message)
		{
			MessageCallback?.Invoke(message);
		}

		private void LatchMultitrackPlayerInput(MultitrackRewiringControllerAdapter rewiredSource)
		{
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
					MovieControllerAdapter.LatchFrom(input);
				}

				MovieControllerAdapter.LatchPlayerFrom(rewiredSource, MultiTrack.CurrentPlayer);
			}
		}

		public void LatchInputFromPlayer(IController source)
		{
			MovieControllerAdapter.LatchFrom(source);
		}

		/// <summary>
		/// Latch input from the input log, if available
		/// </summary>
		public void LatchInputFromLog()
		{
			var input = Movie.GetInputState(Global.Emulator.Frame);

			// adelikat: TODO: this is likely the source of frame 0 TAStudio bugs, I think the intent is to check if the movie is 0 length?
			if (Global.Emulator.Frame == 0) // Hacky
			{
				HandleMovieAfterFrameLoop(); // Frame 0 needs to be handled.
			}

			if (input == null)
			{
				HandleMovieAfterFrameLoop();
				return;
			}

			MovieControllerAdapter.LatchFrom(input);
			if (MultiTrack.IsActive)
			{
				Global.InputManager.MultitrackRewiringAdapter.Source = MovieControllerAdapter;
			}
		}

		private void HandlePlaybackEnd()
		{
			var gambatteName = ((CoreAttribute)Attribute.GetCustomAttribute(typeof(Gameboy), typeof(CoreAttribute))).CoreName;
			if (Movie.Core == gambatteName)
			{
				var movieCycles = Convert.ToUInt64(Movie.HeaderEntries[HeaderKeys.CycleCount]);
				var coreCycles = (Global.Emulator as Gameboy).CycleCount;
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
					PauseCallback();
					break;
				default:
				case MovieEndAction.Finish:
					Movie.FinishedMode();
					break;
			}

			ModeChangedCallback();
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

			MultiTrack.Restart();
			ModeChangedCallback();
		}

		public void HandleMovieSaveState(TextWriter writer)
		{
			if (Movie.IsActive())
			{
				Movie.WriteInputLog(writer);
			}
		}

		public void ClearFrame()
		{
			if (Movie.IsPlaying())
			{
				Movie.ClearFrame(Global.Emulator.Frame);
				Output($"Scrubbed input at frame {Global.Emulator.Frame}");
			}
		}

		public void HandleMovieOnFrameLoop()
		{
			if (!Movie.IsActive())
			{
				LatchInputFromPlayer(Global.InputManager.MovieInputSourceAdapter);
			}
			else if (Movie.IsFinished())
			{
				if (Global.Emulator.Frame < Movie.FrameCount) // This scenario can happen from rewinding (suddenly we are back in the movie, so hook back up to the movie
				{
					Movie.SwitchToPlay();
					LatchInputFromLog();
				}
				else
				{
					LatchInputFromPlayer(Global.InputManager.MovieInputSourceAdapter);
				}
			}
			else if (Movie.IsPlaying())
			{
				LatchInputFromLog();

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
							LatchInputFromPlayer(Global.InputManager.MovieInputSourceAdapter);
							ClearFrame();
						}
						else if (Global.Config.MoviePlaybackPokeMode)
						{
							LatchInputFromPlayer(Global.InputManager.MovieInputSourceAdapter);
							var lg = Movie.LogGeneratorInstance();
							lg.SetSource(Global.InputManager.MovieOutputHardpoint);
							if (!lg.IsEmpty)
							{
								LatchInputFromPlayer(Global.InputManager.MovieInputSourceAdapter);
								Movie.PokeFrame(Global.Emulator.Frame, Global.InputManager.MovieOutputHardpoint);
							}
							else
							{
								// Why, this was already done?
								LatchInputFromLog();
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

		private void HandleFrameLoopForRecordMode()
		{
			// we don't want TasMovie to latch user input outside its internal recording mode, so limit it to autohold
			if (Movie is TasMovie && Movie.IsPlaying())
			{
				MovieControllerAdapter.LatchFromSticky(Global.InputManager.AutofireStickyXorAdapter);
			}
			else
			{
				if (MultiTrack.IsActive)
				{
					LatchMultitrackPlayerInput(Global.InputManager.MultitrackRewiringAdapter);
				}
				else
				{
					LatchInputFromPlayer(Global.InputManager.MovieInputSourceAdapter);
				}
			}

			// the movie session makes sure that the correct input has been read and merged to its MovieControllerAdapter;
			// this has been wired to Global.MovieOutputHardpoint in RewireInputChain
			Movie.RecordFrame(Global.Emulator.Frame, Global.InputManager.MovieOutputHardpoint);
		}

		public void HandleMovieAfterFrameLoop()
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

		public bool HandleMovieLoadState(TextReader reader)
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
					LatchInputFromLog();
				}
				else if (Movie.IsFinished())
				{
					LatchInputFromPlayer(Global.InputManager.MovieInputSourceAdapter);
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

		/// <summary>
		/// Sets the Movie property with the QueuedMovie, clears the queued movie, and starts the new movie
		/// </summary>
		public void RunQueuedMovie(bool recordMode)
		{
			Movie = QueuedMovie;
			QueuedMovie = null;
			MultiTrack.Restart();

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

		// The behavior here is to only temporarily override these settings when playing a movie and then restore the user's preferred settings
		// A more elegant approach would be appreciated
		public bool? PreviousNesInQuickNES { get; set; }
		public bool? PreviousSnesInSnes9x { get; set; }
		public bool? PreviousGbaUsemGba { get; set; }
		public bool? PreviousGbUseGbHawk { get; set; }

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

			if (!record && emulator.SystemId == "NES") // For NES we need special logic since the movie will drive which core to load
			{
				var quicknesName =  typeof(QuickNES).CoreName();
				var neshawkName = typeof(NES).CoreName();

				// If either is specified use that, else use whatever is currently set
				if (movie.Core == quicknesName)
				{
					PreviousNesInQuickNES = Global.Config.NesInQuickNes;
					Global.Config.NesInQuickNes = true;
				}
				else if (movie.Core == neshawkName)
				{
					PreviousNesInQuickNES = Global.Config.NesInQuickNes;
					Global.Config.NesInQuickNes = false;
				}
			}
			else if (!record && emulator.SystemId == "SNES") // ditto with snes9x vs bsnes
			{
				var snes9XName = typeof(Snes9x).CoreName();
				var bsnesName = typeof(LibsnesCore).CoreName();

				if (movie.Core == snes9XName)
				{
					PreviousSnesInSnes9x = Global.Config.SnesInSnes9x;
					Global.Config.SnesInSnes9x = true;
				}
				else if (movie.Core == bsnesName)
				{
					PreviousSnesInSnes9x = Global.Config.SnesInSnes9x;
					Global.Config.SnesInSnes9x = false;
				}
			}
			else if (!record && emulator.SystemId == "GBA") // ditto with GBA, we should probably architect this at some point, this isn't sustainable
			{
				var mGBAName = typeof(MGBAHawk).CoreName();
				var vbaNextName = typeof(VBANext).CoreName();

				if (movie.Core == mGBAName)
				{
					PreviousGbaUsemGba = Global.Config.GbaUsemGba;
					Global.Config.GbaUsemGba = true;
				}
				else if (movie.Core == vbaNextName)
				{
					PreviousGbaUsemGba = Global.Config.GbaUsemGba;
					Global.Config.GbaUsemGba = false;
				}
			}
			else if (!record && (emulator.SystemId == "GB" || emulator.SystemId == "GBC"))
			{
				var gbHawkName = typeof(GBHawk).CoreName();
				var gambatteName = typeof(Gameboy).CoreName();

				if (movie.Core == gbHawkName)
				{
					PreviousGbUseGbHawk = Global.Config.GbUseGbHawk;
					Global.Config.GbUseGbHawk = true;
				}
				else if (movie.Core == gambatteName)
				{
					PreviousGbUseGbHawk = Global.Config.GbUseGbHawk;
					Global.Config.GbUseGbHawk = false;
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
	}
}