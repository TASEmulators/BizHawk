using System;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class MovieSession
	{
		private readonly MultitrackRecording _multiTrack = new MultitrackRecording();

		public MovieSession()
		{
			ReadOnly = true;
			MovieControllerAdapter = MovieService.DefaultInstance.LogGeneratorInstance().MovieControllerAdapter;
		}

		public MultitrackRecording MultiTrack { get { return _multiTrack; } }
		public IMovieController MovieControllerAdapter{ get; set; }

		public IMovie Movie { get; set; }
		public bool ReadOnly { get; set; }
		public Action<string> MessageCallback { get; set; }
		public Func<string, string, bool> AskYesNoCallback { get; set; }

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
			adapter.Type = MovieControllerAdapter.Type;
			return adapter;
		}

		// Convenience property that gets the controller state from the movie for the most recent frame
		public IController CurrentInput
		{
			get
			{
				if (Global.MovieSession.Movie.IsActive && !Global.MovieSession.Movie.IsFinished && Global.Emulator.Frame > 0)
				{
					return Global.MovieSession.Movie.GetInputState(Global.Emulator.Frame - 1);
				}

				return null;
			}
		}

		public IController PreviousFrame
		{
			get
			{
				if (Global.MovieSession.Movie.IsActive && !Global.MovieSession.Movie.IsFinished && Global.Emulator.Frame > 1)
				{
					return Global.MovieSession.Movie.GetInputState(Global.Emulator.Frame - 2);
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
			if (_multiTrack.IsActive)
			{
				rewiredSource.PlayerSource = 1;
				rewiredSource.PlayerTargetMask = 1 << _multiTrack.CurrentPlayer;
				if (_multiTrack.RecordAll)
				{
					rewiredSource.PlayerTargetMask = unchecked((int)0xFFFFFFFF);
				}
			}
			else
			{
				rewiredSource.PlayerSource = -1;
			}

			MovieControllerAdapter.LatchPlayerFromSource(rewiredSource, _multiTrack.CurrentPlayer);
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
			if (Global.Emulator.Frame < Movie.InputLogLength)
			{
				var input = Movie.GetInputState(Global.Emulator.Frame);
				MovieControllerAdapter.LatchFromSource(input);
			}
			else
			{
				Movie.FinishedMode();
			}
		}

		public bool MovieLoad()
		{
			MovieControllerAdapter = Movie.LogGeneratorInstance().MovieControllerAdapter;
			return Movie.Load();
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
				Movie.Stop(saveChanges);
				if (saveChanges)
				{
					Output(Path.GetFileName(Movie.Filename) + " written to disk.");
				}

				Output(message);
				ReadOnly = true;
			}
		}

		public void HandleMovieSaveState(TextWriter writer)
		{
			if (Movie.IsActive)
			{
				writer.Write(Movie.GetInputLog());
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

				// Movie may go into finished mode as a result from latching
				if (!Movie.IsFinished)
				{
					if (Global.ClientControls["Scrub Input"])
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
						{
							LatchInputFromLog();
						}
					}
				}
			}
			else if (Movie.IsRecording)
			{
				if (_multiTrack.IsActive)
				{
					LatchMultitrackPlayerInput(Global.MovieInputSourceAdapter, Global.MultitrackRewiringControllerAdapter);
				}
				else
				{
					LatchInputFromPlayer(Global.MovieInputSourceAdapter);
				}

				// the movie session makes sure that the correct input has been read and merged to its MovieControllerAdapter;
				// this has been wired to Global.MovieOutputHardpoint in RewireInputChain
				Movie.RecordFrame(Global.Emulator.Frame, Global.MovieOutputHardpoint);
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
					MessageCallback("Multi-track can not be used in Full Movie Loadstates mode");
				}
				else
				{
					Global.MovieSession.MultiTrack.IsActive = !Global.MovieSession.MultiTrack.IsActive;
					if (Global.MovieSession.MultiTrack.IsActive)
					{
						MessageCallback("MultiTrack Enabled");
						MultiTrack.CurrentState = "Recording None";
					}
					else
					{
						MessageCallback("MultiTrack Disabled");
					}

					Global.MovieSession.MultiTrack.SelectNone();
				}
			}
			else
			{
				MessageCallback("MultiTrack cannot be enabled while not recording.");
			}
		}
	}
}