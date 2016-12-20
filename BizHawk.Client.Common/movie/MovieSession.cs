using System;
using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.GBA;

using BizHawk.Client.Common;

namespace BizHawk.Client.Common
{
	public enum MovieEndAction { Stop, Pause, Record, Finish }

	public class MovieSession : IMovieSession
	{
		public MovieSession()
		{
			ReadOnly = true;
			MovieControllerAdapter = MovieService.DefaultInstance.LogGeneratorInstance().MovieControllerAdapter;
			MultiTrack = new MultitrackRecorder();
		}

		/// <summary>
		/// When initializing a movie, it will be stored here until Rom processes have been completed, then it will be moved to the Movie property
		/// If an existing movie is still active, it will remain in the Movie property while the new movie is queued
		/// </summary>
		public IMovie QueuedMovie { get; set; }

		// This wrapper but the logic could change, don't make the client code understand these details
		public bool MovieIsQueued
		{
			get { return QueuedMovie != null; }
		}

		public MultitrackRecorder MultiTrack { get; private set; }
		public IMovieController MovieControllerAdapter{ get; set; }

		public IMovie Movie { get; set; }
		public bool ReadOnly { get; set; }
		public Action<string> MessageCallback { get; set; }
		public Func<string, string, bool> AskYesNoCallback { get; set; }

		/// <summary>
		/// Required
		/// </summary>
		public Action PauseCallback { get; set; }

		/// <summary>
		/// Required
		/// </summary>
		public Action ModeChangedCallback { get; set; }

		/// <summary>
		/// Simply shortens the verbosity necessary otherwise
		/// </summary>
		/// <returns></returns>
		public ILogEntryGenerator LogGeneratorInstance()
		{
			return Movie.LogGeneratorInstance();
		}

		public IMovieController MovieControllerInstance()
		{
			var adapter = Movie.LogGeneratorInstance().MovieControllerAdapter;
			adapter.Definition = MovieControllerAdapter.Definition;
			return adapter;
		}

		// Convenience property that gets the controller state from the movie for the most recent frame
		public IController CurrentInput
		{
			get
			{
				if (Movie.IsActive && !Movie.IsFinished && Global.Emulator.Frame > 0)
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
				if (Movie.IsActive && !Movie.IsFinished && Global.Emulator.Frame > 1)
				{
					return Movie.GetInputState(Global.Emulator.Frame - 2);
				}

				return null;
			}
		}

		private void Output(string message)
		{
			if (MessageCallback != null)
			{
				MessageCallback(message);
			}
		}

		public void LatchMultitrackPlayerInput(IController playerSource, MultitrackRewiringControllerAdapter rewiredSource)
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
					MovieControllerAdapter.LatchFromSource(input);
				}

				MovieControllerAdapter.LatchPlayerFromSource(rewiredSource, MultiTrack.CurrentPlayer);
			}
		}

		public void LatchInputFromPlayer(IController source)
		{
			MovieControllerAdapter.LatchFromSource(source);
		}

		/// <summary>
		/// Latch input from the input log, if available
		/// </summary>
		public void LatchInputFromLog()
		{
			var input = Movie.GetInputState(Global.Emulator.Frame);

			// adelikat: TODO: this is likely the source of frame 0 TAStudio bugs, I think the intent is to check if the movie is 0 length?
			if (Global.Emulator.Frame == 0) // Hacky
				HandleMovieAfterFrameLoop(); // Frame 0 needs to be handled.

			if (input == null)
			{
				HandleMovieAfterFrameLoop();
				return;
			}

			MovieControllerAdapter.LatchFromSource(input);
			if (MultiTrack.IsActive)
			{
				Global.MultitrackRewiringAdapter.Source = MovieControllerAdapter;
			}
		}

		private void HandlePlaybackEnd()
		{
			// TODO: mainform callback to update on mode change
			switch(Global.Config.MovieEndAction)
			{
				case MovieEndAction.Stop:
					Movie.Stop();
					break;
				case MovieEndAction.Record:
					Movie.SwitchToRecord();
					break;
				case MovieEndAction.Pause:
					Movie.Stop();
					PauseCallback();
					break;
				default:
				case MovieEndAction.Finish:
					Movie.FinishedMode();
					break;
			}

			ModeChangedCallback();
		}

		// Movie Refactor TODO: delete me, any code calling this is poorly designed
		public bool MovieLoad()
		{
			MovieControllerAdapter = Movie.LogGeneratorInstance().MovieControllerAdapter;
			return Movie.Load(false);
		}

		public void StopMovie(bool saveChanges = true)
		{
			var message = "Movie ";
			if (Movie.IsRecording)
			{
				message += "recording ";
			}
			else if (Movie.IsPlaying)
			{
				message += "playback ";
			}

			message += "stopped.";

			if (Movie.IsActive)
			{
				var result = Movie.Stop(saveChanges);
				if (result)
				{
					Output(Path.GetFileName(Movie.Filename) + " written to disk.");
				}

				Output(message);
				ReadOnly = true;
			}

			MultiTrack.Restart();
			ModeChangedCallback();
		}

		public void HandleMovieSaveState(TextWriter writer)
		{
			if (Movie.IsActive)
			{
				Movie.WriteInputLog(writer);
			}
		}

		public void ClearFrame()
		{
			if (Movie.IsPlaying)
			{
				Movie.ClearFrame(Global.Emulator.Frame);
				Output("Scrubbed input at frame " + Global.Emulator.Frame);
			}
		}

		public void HandleMovieOnFrameLoop()
		{
			if (!Movie.IsActive)
			{
				LatchInputFromPlayer(Global.MovieInputSourceAdapter);
			}
			else if (Movie.IsFinished)
			{
				if (Global.Emulator.Frame < Movie.FrameCount) // This scenario can happen from rewinding (suddenly we are back in the movie, so hook back up to the movie
				{
					Movie.SwitchToPlay();
					LatchInputFromLog();
				}
				else
				{
					LatchInputFromPlayer(Global.MovieInputSourceAdapter);
				}
			}
			else if (Movie.IsPlaying)
			{
				LatchInputFromLog();

				if (Movie.IsRecording) // The movie end situation can cause the switch to record mode, in that case we need to capture some input for this frame
				{
					HandleFrameLoopForRecordMode();
				}
				else
				{
					// Movie may go into finished mode as a result from latching
					if (!Movie.IsFinished)
					{
						if (Global.ClientControls.IsPressed("Scrub Input"))
						{
							LatchInputFromPlayer(Global.MovieInputSourceAdapter);
							ClearFrame();
						}
						else if (Global.Config.MoviePlaybackPokeMode)
						{
							LatchInputFromPlayer(Global.MovieInputSourceAdapter);
							var lg = Movie.LogGeneratorInstance();
							lg.SetSource(Global.MovieOutputHardpoint);
							if (!lg.IsEmpty)
							{
								LatchInputFromPlayer(Global.MovieInputSourceAdapter);
								Movie.PokeFrame(Global.Emulator.Frame, Global.MovieOutputHardpoint);
							}
							else
							{ // Why, this was already done?
								LatchInputFromLog();
							}
						}
					}
				}
			}
			else if (Movie.IsRecording)
			{
				HandleFrameLoopForRecordMode();
			}
		}

		private void HandleFrameLoopForRecordMode()
		{
			if (MultiTrack.IsActive)
			{
				LatchMultitrackPlayerInput(Global.MovieInputSourceAdapter, Global.MultitrackRewiringAdapter);
			}
			else
			{
				LatchInputFromPlayer(Global.MovieInputSourceAdapter);
			}

			// the movie session makes sure that the correct input has been read and merged to its MovieControllerAdapter;
			// this has been wired to Global.MovieOutputHardpoint in RewireInputChain
			Movie.RecordFrame(Global.Emulator.Frame, Global.MovieOutputHardpoint);
		}

		public void HandleMovieAfterFrameLoop()
		{
			if (Movie is TasMovie)
			{
				(Movie as TasMovie).GreenzoneCurrentFrame();
				if (Movie.IsPlaying && Global.Emulator.Frame > Movie.InputLogLength)
				{
					HandleFrameLoopForRecordMode();
				}
			}
			else if (Movie.IsPlaying && !Movie.IsFinished && Global.Emulator.Frame >= Movie.InputLogLength)
			{
				HandlePlaybackEnd();
			}
		}

		public bool HandleMovieLoadState(string path)
		{
			using (var sr = new StreamReader(path))
			{
				return HandleMovieLoadState(sr);
			}
		}

		//TODO: maybe someone who understands more about what's going on here could rename these step1 and step2 into something more descriptive
		public bool HandleMovieLoadState_HackyStep2(TextReader reader)
		{
			if (!Movie.IsActive)
			{
				return true;
			}


			if (ReadOnly)
			{

			}
			else
			{

				string errorMsg;

				//// fixme: this is evil (it causes crashes in binary states because InflaterInputStream can't have its position set, even to zero.
				//((StreamReader)reader).BaseStream.Position = 0;
				//((StreamReader)reader).DiscardBufferedData();
				//edit: zero 18-apr-2014 - this was solved by HackyStep1 and HackyStep2, so that the zip stream can be re-acquired instead of needing its position reset

				var result = Movie.ExtractInputLog(reader, out errorMsg);
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
			if (!HandleMovieLoadState_HackyStep1(reader))
				return false;
			return HandleMovieLoadState_HackyStep2(reader);
		}

		public bool HandleMovieLoadState_HackyStep1(TextReader reader)
		{
			if (!Movie.IsActive)
			{
				return true;
			}

			string errorMsg;

			if (ReadOnly)
			{
				var result = Movie.CheckTimeLines(reader, out errorMsg);
				if (!result)
				{
					Output(errorMsg);
					return false;
				}

				if (Movie.IsRecording)
				{
					Movie.SwitchToPlay();
				}
				else if (Movie.IsFinished)
				{
					LatchInputFromPlayer(Global.MovieInputSourceAdapter);
				}
			}
			else
			{
				if (Movie.IsFinished)
				{
					Movie.StartNewRecording(); 
				}
				else if (Movie.IsPlaying)
				{
					Movie.SwitchToRecord();
				}

			}

			return true;
		}

		public void ToggleMultitrack()
		{
			if (Movie.IsActive)
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

		// Movie Load Refactor TODO: a better name
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
		public bool? PreviousNES_InQuickNES { get; set; }
		public bool? PreviousSNES_InSnes9x { get; set; }
		public bool? PreviousGBA_UsemGBA { get; set; }

		public void QueueNewMovie(IMovie movie, bool record, IEmulator emulator)
		{
			if (!record) // The semantics of record is that we are starting a new movie, and even wiping a pre-existing movie with the same path, but non-record means we are loading an existing movie into playback mode
			{
				movie.Load(false);
				
				if (movie.SystemID != emulator.SystemId)
				{
					throw new MoviePlatformMismatchException(
						string.Format(
						"Movie system Id ({0}) does not match the currently loaded platform ({1}), unable to load",
						movie.SystemID,
						emulator.SystemId));
				}
			}

			// TODO: Delete this, this save is utterly useless.
			// Movie was saved immediately before calling QueeuNewMovie. (StartNewMovie)
			//if (Movie.IsActive && !string.IsNullOrEmpty(Movie.Filename))
			//{
			//	Movie.Save();
			//}

			// Note: this populates MovieControllerAdapter's Type with the approparite controller
			// Don't set it to a movie instance of the adapter or you will lose the definition!
			InputManager.RewireInputChain();

			if (!record && emulator.SystemId == "NES") // For NES we need special logic since the movie will drive which core to load
			{
				var quicknesName = ((CoreAttributes)Attribute.GetCustomAttribute(typeof(QuickNES), typeof(CoreAttributes))).CoreName;
				var neshawkName = ((CoreAttributes)Attribute.GetCustomAttribute(typeof(NES), typeof(CoreAttributes))).CoreName;

				// If either is specified use that, else use whatever is currently set
				if (movie.Core == quicknesName)
				{
					PreviousNES_InQuickNES = Global.Config.NES_InQuickNES;
					Global.Config.NES_InQuickNES = true;
				}
				else if (movie.Core == neshawkName)
				{
					PreviousNES_InQuickNES = Global.Config.NES_InQuickNES;
					Global.Config.NES_InQuickNES = false;
				}
			}
			else if (!record && emulator.SystemId == "SNES") // ditto with snes9x vs bsnes
			{
				var snes9xName = ((CoreAttributes)Attribute.GetCustomAttribute(typeof(Snes9x), typeof(CoreAttributes))).CoreName;
				var bsnesName = ((CoreAttributes)Attribute.GetCustomAttribute(typeof(LibsnesCore), typeof(CoreAttributes))).CoreName;

				if (movie.Core == snes9xName)
				{
					PreviousSNES_InSnes9x = Global.Config.SNES_InSnes9x;
					Global.Config.SNES_InSnes9x = true;
				}
				else
				{
					PreviousSNES_InSnes9x = Global.Config.SNES_InSnes9x;
					Global.Config.SNES_InSnes9x = false;
				}
			}
			else if (!record && emulator.SystemId == "GBA") // ditto with GBA, we should probably architect this at some point, this isn't sustainable
			{
				var mGBAName = ((CoreAttributes)Attribute.GetCustomAttribute(typeof(MGBAHawk), typeof(CoreAttributes))).CoreName;
				var vbaNextName = ((CoreAttributes)Attribute.GetCustomAttribute(typeof(VBANext), typeof(CoreAttributes))).CoreName;

				if (movie.Core == mGBAName)
				{
					PreviousGBA_UsemGBA = Global.Config.GBA_UsemGBA;
					Global.Config.GBA_UsemGBA = true;
				}
				else
				{
					PreviousSNES_InSnes9x = Global.Config.GBA_UsemGBA;
					Global.Config.GBA_UsemGBA = false;
				}
			}

			if (record) // This is a hack really, we need to set the movie to its propert state so that it will be considered active later
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