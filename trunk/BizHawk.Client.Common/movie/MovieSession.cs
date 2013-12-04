using System;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class MovieSession
	{
		public MultitrackRecording MultiTrack = new MultitrackRecording();
		public IMovie Movie;
		public MovieControllerAdapter MovieControllerAdapter = new MovieControllerAdapter();
		public Action<string> MessageCallback; //Not Required
		public Func<string, string, bool> AskYesNoCallback; //Not Required

		public bool ReadOnly = true;

		private void Output(string message)
		{
			if (MessageCallback != null)
			{
				MessageCallback(message);
			}
		}

		private bool AskYesNo(string title, string message)
		{
			if (AskYesNoCallback != null)
			{
				return AskYesNoCallback(title, message);
			}
			else
			{
				return true;
			}
		}

		private bool HandleGuidError()
		{
			return AskYesNo(
				"GUID Mismatch error",
				"The savestate GUID does not match the current movie.  Proceed anyway?"
			);
		}

		public void LatchMultitrackPlayerInput(IController playerSource, MultitrackRewiringControllerAdapter rewiredSource)
		{
			if (MultiTrack.IsActive)
			{
				rewiredSource.PlayerSource = 1;
				rewiredSource.PlayerTargetMask = 1 << (MultiTrack.CurrentPlayer);
				if (MultiTrack.RecordAll) rewiredSource.PlayerTargetMask = unchecked((int)0xFFFFFFFF);
			}
			else rewiredSource.PlayerSource = -1;

			MovieControllerAdapter.LatchPlayerFromSource(rewiredSource, MultiTrack.CurrentPlayer);
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
			var input = Movie.GetInput(Global.Emulator.Frame);

			// Attempting to get a frame past the end of a movie changes the mode to finished
			if (!Movie.IsFinished)
			{
				MovieControllerAdapter.SetControllersAsMnemonic(input);
			}
		}

		public void StopMovie(bool saveChanges = true)
		{
			string message = "Movie ";
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

		//State handling
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
				if (Global.Emulator.Frame < Movie.FrameCount) //This scenario can happen from rewinding (suddenly we are back in the movie, so hook back up to the movie
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

				//Movie may go into finished mode as a result from latching
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
				if (MultiTrack.IsActive)
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

			string ErrorMSG = String.Empty;

			if (ReadOnly)
			{
				var result = Movie.CheckTimeLines(reader, out ErrorMSG);
				if (!result)
				{
					Output(ErrorMSG);
					return false;
				}

				if (Movie.IsRecording)
				{
					Movie.SwitchToPlay();
				}
				else if (Movie.IsPlaying && !Movie.IsFinished)
				{
					// Frame loop automatically handles the rewinding effect based on Global.Emulator.Frame so nothing else is needed here
				}
				else if (Movie.IsFinished)
				{
					if (Movie.IsFinished) // TimeLine check can change a movie to finished, hence the check here (not a good design)
					{
						LatchInputFromPlayer(Global.MovieInputSourceAdapter);
					}
					else
					{
						Movie.SwitchToPlay();
					}
				}
			}
			else
			{
				var result = Movie.CheckTimeLines(reader, out ErrorMSG);
				if (!result)
				{
					Output(ErrorMSG);
					return false;
				}

				if (Movie.IsRecording)
				{
					reader.BaseStream.Position = 0;
					reader.DiscardBufferedData();
					Movie.ExtractInputLog(reader);
				}
				else if (Movie.IsPlaying && !Movie.IsFinished)
				{
					Movie.SwitchToRecord();
					reader.BaseStream.Position = 0;
					reader.DiscardBufferedData();
					Movie.ExtractInputLog(reader);
				}
				else if (Movie.IsFinished)
				{
					Global.Emulator.ClearSaveRam();
					Movie.StartNewRecording();
					reader.BaseStream.Position = 0;
					reader.DiscardBufferedData();
					Movie.ExtractInputLog(reader);
				}
			}

			return true;
		}
	}

}