using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class MovieSession
	{
		private readonly MultitrackRecording _multiTrack = new MultitrackRecording();
		private readonly BkmControllerAdapter _movieControllerAdapter = new BkmControllerAdapter();

		public MovieSession()
		{
			ReadOnly = true;
		}

		public MultitrackRecording MultiTrack { get { return _multiTrack; } }
		public BkmControllerAdapter MovieControllerAdapter { get { return _movieControllerAdapter; } }

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
	}
}