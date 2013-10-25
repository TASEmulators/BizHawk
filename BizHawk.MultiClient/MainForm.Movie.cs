using System;
using System.IO;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.MultiClient
{
	partial class MainForm
	{
		public bool ReadOnly = true;	//Global Movie Read only setting

		public void ClearFrame()
		{
			if (Global.MovieSession.Movie.IsPlaying)
			{
				Global.MovieSession.Movie.ClearFrame(Global.Emulator.Frame);
				GlobalWinF.OSD.AddMessage("Scrubbed input at frame " + Global.Emulator.Frame.ToString());
			}
		}

		public void StartNewMovie(Movie m, bool record)
		{
			//If a movie is already loaded, save it before starting a new movie
			if (Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.Movie.WriteMovie();
			}

			Global.MovieSession = new MovieSession { Movie = m };
			RewireInputChain();

			if (!record)
			{
				Global.MovieSession.Movie.LoadMovie();
				SetSyncDependentSettings();
			}

			LoadRom(GlobalWinF.MainForm.CurrentlyOpenRom, true, !record);

			Global.Config.RecentMovies.Add(m.Filename);
			if (Global.MovieSession.Movie.StartsFromSavestate)
			{
				LoadStateFile(Global.MovieSession.Movie.Filename, Path.GetFileName(Global.MovieSession.Movie.Filename));
				Global.Emulator.ResetFrameCounter();
			}
			if (record)
			{
				GlobalWinF.MainForm.ClearSaveRAM();
				Global.MovieSession.Movie.StartRecording();
				ReadOnly = false;
			}
			else
			{
				GlobalWinF.MainForm.ClearSaveRAM();
				Global.MovieSession.Movie.StartPlayback();
			}
			SetMainformMovieInfo();
			TAStudio1.Restart();
			VirtualPadForm1.Restart();
			GlobalWinF.DisplayManager.NeedsToPaint = true;
		}

		public void SetMainformMovieInfo()
		{
			if (Global.MovieSession.Movie.IsPlaying)
			{
				Text = DisplayNameForSystem(Global.Game.System) + " - " + Global.Game.Name + " - " + Path.GetFileName(Global.MovieSession.Movie.Filename);
				PlayRecordStatus.Image = Properties.Resources.Play;
				PlayRecordStatus.ToolTipText = "Movie is in playback mode";
				PlayRecordStatus.Visible = true;
			}
			else if (Global.MovieSession.Movie.IsRecording)
			{
				Text = DisplayNameForSystem(Global.Game.System) + " - " + Global.Game.Name + " - " + Path.GetFileName(Global.MovieSession.Movie.Filename);
				PlayRecordStatus.Image = Properties.Resources.RecordHS;
				PlayRecordStatus.ToolTipText = "Movie is in record mode";
				PlayRecordStatus.Visible = true;
			}
			else if (!Global.MovieSession.Movie.IsActive)
			{
				Text = DisplayNameForSystem(Global.Game.System) + " - " + Global.Game.Name;
				PlayRecordStatus.Image = Properties.Resources.Blank;
				PlayRecordStatus.ToolTipText = "No movie is active";
				PlayRecordStatus.Visible = false;
			}
		}

		public void PlayMovie()
		{
			new PlayMovie().ShowDialog();
		}

		public void RecordMovie()
		{
			// put any BEETA quality cores here
			if (Global.Emulator is Emulation.Consoles.Nintendo.GBA.GBA ||
				Global.Emulator is Emulation.Consoles.Sega.Genesis ||
				Global.Emulator is Emulation.Consoles.Sega.Saturn.Yabause ||
                Global.Emulator is Emulation.Consoles.Sony.PSP.PSP)
			{
				var result = MessageBox.Show
					(this, "Thanks for using Bizhawk!  The emulation core you have selected " +
					"is currently BETA-status.  We appreciate your help in testing Bizhawk. " +
					"You can record a movie on this core if you'd like to, but expect to " +
					"encounter bugs and sync problems.  Continue?", "BizHawk", MessageBoxButtons.YesNo);
				if (result != DialogResult.Yes)
					return;
			}
			new RecordMovie().ShowDialog();
		}

		public void PlayMovieFromBeginning()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				LoadRom(CurrentlyOpenRom, true, true);
				if (Global.MovieSession.Movie.StartsFromSavestate)
				{
					LoadStateFile(Global.MovieSession.Movie.Filename, Path.GetFileName(Global.MovieSession.Movie.Filename));
					Global.Emulator.ResetFrameCounter();
				}
				GlobalWinF.MainForm.ClearSaveRAM();
				Global.MovieSession.Movie.StartPlayback();
				SetMainformMovieInfo();
				GlobalWinF.OSD.AddMessage("Replaying movie file in read-only mode");
				GlobalWinF.MainForm.ReadOnly = true;
			}
		}

		public void StopMovie(bool abortchanges = false)
		{
			string message = "Movie ";
			if (Global.MovieSession.Movie.IsRecording)
			{
				message += "recording ";
			}
			else if (Global.MovieSession.Movie.IsPlaying)
			{
				message += "playback ";
			}

			message += "stopped.";

			if (Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.Movie.Stop(abortchanges);
				if (!abortchanges)
				{
					GlobalWinF.OSD.AddMessage(Path.GetFileName(Global.MovieSession.Movie.Filename) + " written to disk.");
				}
				GlobalWinF.OSD.AddMessage(message);
				GlobalWinF.MainForm.ReadOnly = true;
				SetMainformMovieInfo();
			}
		}

		private void ShowError(string error)
		{
			if (!String.IsNullOrWhiteSpace(error))
			{
				MessageBox.Show(error, "Loadstate Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private bool HandleMovieLoadState(string path)
		{
			using (var sr = new StreamReader(path))
			{
				return HandleMovieLoadState(sr);
			}
		}

		//OMG this needs to be refactored!
		private bool HandleMovieLoadState(StreamReader reader)
		{
			string ErrorMSG = String.Empty;
			//Note, some of the situations in these IF's may be identical and could be combined but I intentionally separated it out for clarity
			if (!Global.MovieSession.Movie.IsActive)
			{
				return true;
			}

			else if (Global.MovieSession.Movie.IsRecording)
			{
				if (ReadOnly)
				{
					var result = Global.MovieSession.Movie.CheckTimeLines(reader, OnlyGUID: false, IgnoreGuidMismatch: false, ErrorMessage: out ErrorMSG);
					if (result == Movie.LoadStateResult.Pass)
					{
						Global.MovieSession.Movie.WriteMovie();
						Global.MovieSession.Movie.SwitchToPlay();
						SetMainformMovieInfo();
						return true;
					}
					else
					{
						if (result == Movie.LoadStateResult.GuidMismatch)
						{
							var dresult = MessageBox.Show("The savestate GUID does not match the current movie.  Proceed anyway?",
								"GUID Mismatch error",
								MessageBoxButtons.YesNo, MessageBoxIcon.Question);
							if (dresult == DialogResult.Yes)
							{
								var newresult = Global.MovieSession.Movie.CheckTimeLines(reader, OnlyGUID: false, IgnoreGuidMismatch: true, ErrorMessage: out ErrorMSG);
								if (newresult == Movie.LoadStateResult.Pass)
								{
									Global.MovieSession.Movie.WriteMovie();
									Global.MovieSession.Movie.SwitchToPlay();
									SetMainformMovieInfo();
									return true;
								}
								else
								{
									ShowError(ErrorMSG);
									return false;
								}
							}
							else
							{
								return false;
							}
						}
						else
						{
							ShowError(ErrorMSG);
							return false;
						}
					}
				}
				else
				{
					var result = Global.MovieSession.Movie.CheckTimeLines(reader, OnlyGUID: true, IgnoreGuidMismatch: false, ErrorMessage: out ErrorMSG);
					if (result == Movie.LoadStateResult.Pass)
					{
						reader.BaseStream.Position = 0;
						reader.DiscardBufferedData();
						Global.MovieSession.Movie.LoadLogFromSavestateText(reader, Global.MovieSession.MultiTrack.IsActive);
					}
					else
					{
						if (result == Movie.LoadStateResult.GuidMismatch)
						{
							var dresult = MessageBox.Show("The savestate GUID does not match the current movie.  Proceed anyway?",
								"GUID Mismatch error",
								MessageBoxButtons.YesNo, MessageBoxIcon.Question);
							if (dresult == DialogResult.Yes)
							{
								var newresult = Global.MovieSession.Movie.CheckTimeLines(reader, OnlyGUID: false, IgnoreGuidMismatch: true, ErrorMessage: out ErrorMSG);
								if (newresult == Movie.LoadStateResult.Pass)
								{
									reader.BaseStream.Position = 0;
									reader.DiscardBufferedData();
									Global.MovieSession.Movie.LoadLogFromSavestateText(reader, Global.MovieSession.MultiTrack.IsActive);
									return true;
								}
								else
								{
									ShowError(ErrorMSG);
									return false;
								}
							}
							else
							{
								return false;
							}
						}
						else
						{
							ShowError(ErrorMSG);
							return false;
						}
					}
				}
			}

			else if (Global.MovieSession.Movie.IsPlaying && !Global.MovieSession.Movie.IsFinished)
			{
				if (ReadOnly)
				{
					var result = Global.MovieSession.Movie.CheckTimeLines(reader, OnlyGUID: !ReadOnly, IgnoreGuidMismatch: false, ErrorMessage: out ErrorMSG);
					if (result == Movie.LoadStateResult.Pass)
					{
						//Frame loop automatically handles the rewinding effect based on Global.Emulator.Frame so nothing else is needed here
						return true;
					}
					else
					{
						if (result == Movie.LoadStateResult.GuidMismatch)
						{
							var dresult = MessageBox.Show("The savestate GUID does not match the current movie.  Proceed anyway?",
								"GUID Mismatch error",
								MessageBoxButtons.YesNo, MessageBoxIcon.Question);
							if (dresult == DialogResult.Yes)
							{
								var newresult = Global.MovieSession.Movie.CheckTimeLines(reader, OnlyGUID: !ReadOnly, IgnoreGuidMismatch: true, ErrorMessage: out ErrorMSG);
								if (newresult == Movie.LoadStateResult.Pass)
								{
									return true;
								}
								else
								{
									ShowError(ErrorMSG);
									return false;
								}
							}
							else
							{
								return false;
							}
						}
						else
						{
							ShowError(ErrorMSG);
							return false;
						}
					}
				}
				else
				{
					var result = Global.MovieSession.Movie.CheckTimeLines(reader, OnlyGUID: !ReadOnly, IgnoreGuidMismatch: false, ErrorMessage: out ErrorMSG);
					if (result == Movie.LoadStateResult.Pass)
					{
						Global.MovieSession.Movie.SwitchToRecord();
						SetMainformMovieInfo();
						reader.BaseStream.Position = 0;
						reader.DiscardBufferedData();
						Global.MovieSession.Movie.LoadLogFromSavestateText(reader, Global.MovieSession.MultiTrack.IsActive);
						return true;
					}
					else
					{
						if (result == Movie.LoadStateResult.GuidMismatch)
						{
							var dresult = MessageBox.Show("The savestate GUID does not match the current movie.  Proceed anyway?",
								"GUID Mismatch error",
								MessageBoxButtons.YesNo, MessageBoxIcon.Question);
							if (dresult == DialogResult.Yes)
							{
								var newresult = Global.MovieSession.Movie.CheckTimeLines(reader, OnlyGUID: !ReadOnly, IgnoreGuidMismatch: true, ErrorMessage: out ErrorMSG);
								if (newresult == Movie.LoadStateResult.Pass)
								{
									Global.MovieSession.Movie.SwitchToRecord();
									SetMainformMovieInfo();
									reader.BaseStream.Position = 0;
									reader.DiscardBufferedData();
									Global.MovieSession.Movie.LoadLogFromSavestateText(reader, Global.MovieSession.MultiTrack.IsActive);
									return true;
								}
								else
								{
									ShowError(ErrorMSG);
									return false;
								}
							}
							else
							{
								return false;
							}
						}
						else
						{
							ShowError(ErrorMSG);
							return false;
						}
					}
				}
			}
			else if (Global.MovieSession.Movie.IsFinished)
			{
				if (ReadOnly)
				{
					var result = Global.MovieSession.Movie.CheckTimeLines(reader, OnlyGUID: !ReadOnly, IgnoreGuidMismatch: false, ErrorMessage: out ErrorMSG);
					if (result != Movie.LoadStateResult.Pass)
					{
						if (result == Movie.LoadStateResult.GuidMismatch)
						{
							var dresult = MessageBox.Show("The savestate GUID does not match the current movie.  Proceed anyway?",
								"GUID Mismatch error",
								MessageBoxButtons.YesNo, MessageBoxIcon.Question);
							if (dresult == DialogResult.Yes)
							{
								var newresult = Global.MovieSession.Movie.CheckTimeLines(reader, OnlyGUID: !ReadOnly, IgnoreGuidMismatch: true, ErrorMessage: out ErrorMSG);
								if (newresult == Movie.LoadStateResult.Pass)
								{
									Global.MovieSession.Movie.SwitchToPlay();
									SetMainformMovieInfo();
									return true;
								}
								else
								{
									ShowError(ErrorMSG);
									return false;
								}
							}
							else
							{
								return false;
							}
						}
						else
						{
							ShowError(ErrorMSG);
							return false;
						}
					}
					else if (Global.MovieSession.Movie.IsFinished) //TimeLine check can change a movie to finished, hence the check here (not a good design)
					{
						Global.MovieSession.LatchInputFromPlayer(GlobalWinF.MovieInputSourceAdapter);
					}
					else
					{
						Global.MovieSession.Movie.SwitchToPlay();
						SetMainformMovieInfo();
					}
				}
				else
				{
					var result = Global.MovieSession.Movie.CheckTimeLines(reader, OnlyGUID: !ReadOnly, IgnoreGuidMismatch: false, ErrorMessage: out ErrorMSG);
					if (result == Movie.LoadStateResult.Pass)
					{
						GlobalWinF.MainForm.ClearSaveRAM();
						Global.MovieSession.Movie.StartRecording();
						SetMainformMovieInfo();
						reader.BaseStream.Position = 0;
						reader.DiscardBufferedData();
						Global.MovieSession.Movie.LoadLogFromSavestateText(reader, Global.MovieSession.MultiTrack.IsActive);
						return true;
					}
					else
					{
						if (result == Movie.LoadStateResult.GuidMismatch)
						{
							var dresult = MessageBox.Show("The savestate GUID does not match the current movie.  Proceed anyway?",
								"GUID Mismatch error",
								MessageBoxButtons.YesNo, MessageBoxIcon.Question);
							if (dresult == DialogResult.Yes)
							{
								var newresult = Global.MovieSession.Movie.CheckTimeLines(reader, OnlyGUID: !ReadOnly, IgnoreGuidMismatch: true, ErrorMessage: out ErrorMSG);
								if (newresult == Movie.LoadStateResult.Pass)
								{
									GlobalWinF.MainForm.ClearSaveRAM();
									Global.MovieSession.Movie.StartRecording();
									SetMainformMovieInfo();
									reader.BaseStream.Position = 0;
									reader.DiscardBufferedData();
									Global.MovieSession.Movie.LoadLogFromSavestateText(reader, Global.MovieSession.MultiTrack.IsActive);
									return true;
								}
								else
								{
									ShowError(ErrorMSG);
									return false;
								}
							}
							else
							{
								return false;
							}
						}
						else
						{
							ShowError(ErrorMSG);
							return false;
						}
					}
				}
			}

			return true;
		}

		private void HandleMovieSaveState(StreamWriter writer)
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.Movie.DumpLogIntoSavestateText(writer);
			}
		}

		private void HandleMovieOnFrameLoop()
		{
			if (!Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.LatchInputFromPlayer(GlobalWinF.MovieInputSourceAdapter);
			}

			else if (Global.MovieSession.Movie.IsFinished)
			{
				if (Global.Emulator.Frame < Global.MovieSession.Movie.Frames) //This scenario can happen from rewinding (suddenly we are back in the movie, so hook back up to the movie
				{
					Global.MovieSession.Movie.SwitchToPlay();
					Global.MovieSession.LatchInputFromLog();
				}
				else
				{
					Global.MovieSession.LatchInputFromPlayer(GlobalWinF.MovieInputSourceAdapter);
				}
			}

			else if (Global.MovieSession.Movie.IsPlaying)
			{
				if (Global.Emulator.Frame >= Global.MovieSession.Movie.Frames)
				{
					if (TAStudio1.IsHandleCreated && !TAStudio1.IsDisposed)
					{
						Global.MovieSession.Movie.CaptureState();
						Global.MovieSession.LatchInputFromLog();
						Global.MovieSession.Movie.CommitFrame(Global.Emulator.Frame, GlobalWinF.MovieOutputHardpoint);
					}
					else
					{
						Global.MovieSession.Movie.Finish();
					}
				}
				else
				{
					Global.MovieSession.Movie.CaptureState();
					Global.MovieSession.LatchInputFromLog();
					if (GlobalWinF.ClientControls["ClearFrame"])
					{
						Global.MovieSession.LatchInputFromPlayer(GlobalWinF.MovieInputSourceAdapter);
						ClearFrame();
					}
					else if (TAStudio1.IsHandleCreated && !TAStudio1.IsDisposed || Global.Config.MoviePlaybackPokeMode)
					{
						Global.MovieSession.LatchInputFromPlayer(GlobalWinF.MovieInputSourceAdapter);
						MnemonicsGenerator mg = new MnemonicsGenerator();
						mg.SetSource( GlobalWinF.MovieOutputHardpoint);
						if (!mg.IsEmpty)
						{
							Global.MovieSession.LatchInputFromPlayer(GlobalWinF.MovieInputSourceAdapter);
							Global.MovieSession.Movie.PokeFrame(Global.Emulator.Frame, mg.GetControllersAsMnemonic());
						}
						else
						{
							Global.MovieSession.LatchInputFromLog();
						}
					}
				}
			}

			else if (Global.MovieSession.Movie.IsRecording)
			{
				Global.MovieSession.Movie.CaptureState();
				if (Global.MovieSession.MultiTrack.IsActive)
				{
					Global.MovieSession.LatchMultitrackPlayerInput(GlobalWinF.MovieInputSourceAdapter, Global.MultitrackRewiringControllerAdapter);
				}
				else
				{
					Global.MovieSession.LatchInputFromPlayer(GlobalWinF.MovieInputSourceAdapter);
				}
				//the movie session makes sure that the correct input has been read and merged to its MovieControllerAdapter;
				//this has been wired to Global.MovieOutputHardpoint in RewireInputChain
				Global.MovieSession.Movie.CommitFrame(Global.Emulator.Frame, GlobalWinF.MovieOutputHardpoint);
			}
		}

		//On movie load, these need to be set based on the contents of the movie file
		private void SetSyncDependentSettings()
		{
			switch (Global.Emulator.SystemId)
			{
				case "Coleco":
					string str = Global.MovieSession.Movie.Header.GetHeaderLine(MovieHeader.SKIPBIOS);
					if (!String.IsNullOrWhiteSpace(str))
					{
						if (str.ToLower() == "true")
						{
							Global.Config.ColecoSkipBiosIntro = true;
						}
						else
						{
							Global.Config.ColecoSkipBiosIntro = false;
						}
					}
					break;
			}
		}
	}
}
