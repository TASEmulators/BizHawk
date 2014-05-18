using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Sega.Genesis;
using BizHawk.Emulation.Cores.Sega.Saturn;
using BizHawk.Emulation.Cores.Sony.PSP;

using Newtonsoft.Json;

namespace BizHawk.Client.EmuHawk
{
	partial class MainForm
	{
		public void StartNewMovie(IMovie movie, bool record, bool fromTastudio = false) //TasStudio flag is a hack for now
		{
			//If a movie is already loaded, save it before starting a new movie
			if (!fromTastudio && Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.Movie.Save();
			}

			Global.MovieSession = new MovieSession
			{
				Movie = movie,
				MessageCallback = GlobalWin.OSD.AddMessage,
				AskYesNoCallback = StateErrorAskUser
			};

			InputManager.RewireInputChain();

			if (!record)
			{
				Global.MovieSession.Movie.Load();
			}

			try
			{
				// Get Sync settings and put into hack variable
				_syncSettingsHack = JsonConvert.DeserializeObject(Global.MovieSession.Movie.Header.SyncSettingsJson, Global.Emulator.GetSyncSettings().GetType());

				// movie 1.0 hack: restore sync settings for the only core that fully supported them in movie 1.0
				if (!record && Global.Emulator.SystemId == "Coleco")
				{
					string str = Global.MovieSession.Movie.Header[HeaderKeys.SKIPBIOS];
					if (!String.IsNullOrWhiteSpace(str))
					{
						this._syncSettingsHack = new Emulation.Cores.ColecoVision.ColecoVision.ColecoSyncSettings
						{
							SkipBiosIntro = str.ToLower() == "true"
						};
					}
				}
				else if (!record && Global.Emulator.SystemId == "NES")
				{
					var quicknesName = ((CoreAttributes)Attribute.GetCustomAttribute(typeof(QuickNES), typeof(CoreAttributes))).CoreName;

					if (Global.MovieSession.Movie.Header[HeaderKeys.CORE] == quicknesName)
					{
						Global.Config.NES_InQuickNES = true;
						var qs = new QuickNES.QuickNESSettings();
						this._syncSettingsHack = qs;
					}
					else //Else assume Neshawk
					{
						var s = new Emulation.Cores.Nintendo.NES.NES.NESSyncSettings();
						s.BoardProperties = new System.Collections.Generic.Dictionary<string, string>(Global.MovieSession.Movie.Header.BoardProperties);
						this._syncSettingsHack = s;
					}
				}
				else if (!record && Global.Emulator is Emulation.Cores.Consoles.Sega.gpgx.GPGX)
				{
					// unfortunately, gpgx is being released with movie 1.0
					// we don't save the control settings there, so hack and assume a particular configuration
					var s = new Emulation.Cores.Consoles.Sega.gpgx.GPGX.GPGXSyncSettings
					{
						ControlType = Emulation.Cores.Consoles.Sega.gpgx.GPGX.ControlType.Normal,
						UseSixButton = true,
					};
					this._syncSettingsHack = s;
				}
				// load the rom in any case
				LoadRom(GlobalWin.MainForm.CurrentlyOpenRom, true, !record);
			}
			finally
			{
				// ensure subsequent calls to LoadRom won't get the settings object created here
				this._syncSettingsHack = null;
			}

			if (!fromTastudio)
			{
				Global.Config.RecentMovies.Add(movie.Filename);
			}

			if (Global.MovieSession.Movie.Header.StartsFromSavestate)
			{
				byte[] state = Convert.FromBase64String(Global.MovieSession.Movie.Header.SavestateBinaryBase64Blob);
				Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(state)));
				Global.Emulator.ResetCounters();
			}

			if (!fromTastudio)
			{
				if (record)
				{
					Global.MovieSession.Movie.StartNewRecording();
					Global.MovieSession.ReadOnly = false;
				}
				else
				{
					Global.MovieSession.Movie.StartNewPlayback();
				}
			}

			SetMainformMovieInfo();

			if (!fromTastudio)
			{
				GlobalWin.Tools.Restart<TAStudio>();
			}

			GlobalWin.Tools.Restart<VirtualPadForm>();
			GlobalWin.DisplayManager.NeedsToPaint = true;
		}

		public void SetMainformMovieInfo()
		{
			// TODO: this shoudln't be here it is copy paste from MainForm LoadRom
			string gamename = string.Empty;
			if (!string.IsNullOrWhiteSpace(Global.Game.Name)) // Prefer Game db name, else use the path
			{
				gamename = Global.Game.Name;
			}
			else
			{
				gamename = Path.GetFileNameWithoutExtension(GlobalWin.MainForm.CurrentlyOpenRom.Split('|').Last());
			}

			if (Global.MovieSession.Movie.IsPlaying)
			{
				Text = DisplayNameForSystem(Global.Game.System) + " - " + gamename + " - " + Path.GetFileName(Global.MovieSession.Movie.Filename);
				PlayRecordStatusButton.Image = Properties.Resources.Play;
				PlayRecordStatusButton.ToolTipText = "Movie is in playback mode";
				PlayRecordStatusButton.Visible = true;
			}
			else if (Global.MovieSession.Movie.IsRecording)
			{
				Text = DisplayNameForSystem(Global.Game.System) + " - " + gamename + " - " + Path.GetFileName(Global.MovieSession.Movie.Filename);
				PlayRecordStatusButton.Image = Properties.Resources.RecordHS;
				PlayRecordStatusButton.ToolTipText = "Movie is in record mode";
				PlayRecordStatusButton.Visible = true;
			}
			else if (!Global.MovieSession.Movie.IsActive)
			{
				Text = DisplayNameForSystem(Global.Game.System) + " - " + gamename;
				PlayRecordStatusButton.Image = Properties.Resources.Blank;
				PlayRecordStatusButton.ToolTipText = "No movie is active";
				PlayRecordStatusButton.Visible = false;
			}
		}

		public void LoadPlayMovieDialog()
		{
			new PlayMovie().ShowDialog();
		}

		public void LoadRecordMovieDialog()
		{
			// put any BEETA quality cores here
			if (Global.Emulator is GBA ||
				Global.Emulator is Genesis ||
				Global.Emulator is Yabause ||
				Global.Emulator is PSP)
			{
				var result = MessageBox.Show
					(this, "Thanks for using Bizhawk!  The emulation core you have selected " +
					"is currently BETA-status.  We appreciate your help in testing Bizhawk. " +
					"You can record a movie on this core if you'd like to, but expect to " +
					"encounter bugs and sync problems.  Continue?", "BizHawk", MessageBoxButtons.YesNo);
				if (result != DialogResult.Yes) return;
			}
			new RecordMovie().ShowDialog();
		}

		public void RestartMovie()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				LoadRom(CurrentlyOpenRom, true, true);
				if (Global.MovieSession.Movie.Header.StartsFromSavestate)
				{
					byte[] state = Convert.FromBase64String(Global.MovieSession.Movie.Header.SavestateBinaryBase64Blob);
					Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(state)));
					Global.Emulator.ResetCounters();
				}

				Global.MovieSession.Movie.StartNewPlayback();
				SetMainformMovieInfo();
				GlobalWin.OSD.AddMessage("Replaying movie file in read-only mode");
				Global.MovieSession.ReadOnly = true;
			}
		}

		public void StopMovie(bool saveChanges = true)
		{
			Global.MovieSession.StopMovie(saveChanges);
			SetMainformMovieInfo();
		}
	}
}
