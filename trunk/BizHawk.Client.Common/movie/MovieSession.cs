using System;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class MovieSession
	{
		private readonly MultitrackRecording _multiTrack = new MultitrackRecording();
		private readonly MovieControllerAdapter _movieControllerAdapter = new MovieControllerAdapter();

		public MovieSession()
		{
			ReadOnly = true;
		}

		public MultitrackRecording MultiTrack { get { return _multiTrack; } }
		public MovieControllerAdapter MovieControllerAdapter { get { return _movieControllerAdapter; } }

		public IMovie Movie { get; set; }
		public bool ReadOnly { get; set; }
		public Action<string> MessageCallback { get; set; }
		public Func<string, string, bool> AskYesNoCallback { get; set; }

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

			_movieControllerAdapter.LatchPlayerFromSource(rewiredSource, _multiTrack.CurrentPlayer);
		}

		public void LatchInputFromPlayer(IController source)
		{
			_movieControllerAdapter.LatchFromSource(source);
		}

		/// <summary>
		/// Latch input from the input log, if available
		/// </summary>
		public void LatchInputFromLog()
		{
			var input = Movie.GetInput(Global.Emulator.Frame);

			// Attempting to get a frame past the end of a movie changes the mode to finished
			if (!Movie.IsFinished)
			{
				_movieControllerAdapter.SetControllersAsMnemonic(input);
			}
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

		public void HandleMovieSaveState(StreamWriter writer)
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
						var mg = new MnemonicsGenerator();
						mg.SetSource(Global.MovieOutputHardpoint);
						if (!mg.IsEmpty)
						{
							LatchInputFromPlayer(Global.MovieInputSourceAdapter);
							Movie.PokeFrame(Global.Emulator.Frame, mg);
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
				var mg = new MnemonicsGenerator();
				mg.SetSource(Global.MovieOutputHardpoint);
				Movie.RecordFrame(Global.Emulator.Frame, mg);
			}
		}

		public bool HandleMovieLoadState(string path)
		{
			using (var sr = new StreamReader(path))
			{
				return HandleMovieLoadState(sr);
			}
		}

		public bool HandleMovieLoadState(StreamReader reader)
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

				reader.BaseStream.Position = 0;
				reader.DiscardBufferedData();
				var result = Movie.ExtractInputLog(reader, out errorMsg);
				if (!result)
				{
					Output(errorMsg);
					return false;
				}
			}

			return true;
		}
	}
}