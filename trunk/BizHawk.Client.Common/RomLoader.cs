using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Atari.Atari2600;
using BizHawk.Emulation.Cores.Atari.Atari7800;
using BizHawk.Emulation.Cores.Calculators;
using BizHawk.Emulation.Cores.ColecoVision;
using BizHawk.Emulation.Cores.Computers.Commodore64;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Intellivision;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.Sega.Saturn;
using BizHawk.Emulation.Cores.Sony.PSP;
using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.Emulation.DiscSystem;
using BizHawk.Emulation.Cores.WonderSwan;

namespace BizHawk.Client.Common
{
	public class RomLoader
	{
		public enum LoadErrorType { Unknown, MissingFirmware, XML }

		// helper methods for the settings events
		private object GetCoreSettings<T>()
			where T : IEmulator
		{
			return GetCoreSettings(typeof(T));
		}

		private object GetCoreSyncSettings<T>()
			where T : IEmulator
		{
			return GetCoreSyncSettings(typeof(T));
		}

		private object GetCoreSettings(Type t)
		{
			var e = new SettingsLoadArgs(t);
			if (OnLoadSettings != null)
			{
				OnLoadSettings(this, e);
			}
			return e.Settings;
		}
		private object GetCoreSyncSettings(Type t)
		{
			var e = new SettingsLoadArgs(t);
			if (OnLoadSyncSettings != null)
			{
				OnLoadSyncSettings(this, e);
			}
			return e.Settings;
		}

		public RomLoader()
		{

		}

		// For not throwing errors but simply outputing information to the screen
		public Action<string> MessageCallback { get; set; }

		private void DoMessageCallback(string message)
		{
			if (MessageCallback != null)
			{
				MessageCallback(message);
			}
		}

		// TODO: reconsider the need for exposing these;
		public IEmulator LoadedEmulator { get; private set; }
		public GameInfo Game { get; private set; }
		public RomGame Rom { get; private set; }
		public string CanonicalFullPath { get; private set; }

		public bool Deterministic { get; set; }

		public class RomErrorArgs : EventArgs
		{
			// TODO: think about naming here, what to pass, a lot of potential good information about what went wrong could go here!
			public RomErrorArgs(string message, string systemId, LoadErrorType type)
			{
				Message = message;
				AttemptedCoreLoad = systemId;
				Type = type;
			}

			public string Message { get; private set; }
			public string AttemptedCoreLoad { get; private set; }
			public LoadErrorType Type { get; private set; }
		}

		public class SettingsLoadArgs : EventArgs
		{
			public object Settings { get; set; }
			public Type Core { get; private set; }
			public SettingsLoadArgs(Type t)
			{
				Core = t;
				Settings = null;
			}
		}

		public delegate void SettingsLoadEventHandler(object sender, SettingsLoadArgs e);
		public event SettingsLoadEventHandler OnLoadSettings;
		public event SettingsLoadEventHandler OnLoadSyncSettings;

		public delegate void LoadErrorEventHandler(object sener, RomErrorArgs e);
		public event LoadErrorEventHandler OnLoadError;

		public Func<HawkFile, int?> ChooseArchive { get; set; }

		public Func<RomGame, string> ChoosePlatform { get; set; }

		private int? HandleArchive(HawkFile file)
		{
			if (ChooseArchive != null)
			{
				return ChooseArchive(file);
			}

			return null;
		}

		private void DoLoadErrorCallback(string message, string systemId, LoadErrorType type = LoadErrorType.Unknown)
		{
			if (OnLoadError != null)
			{
				OnLoadError(this, new RomErrorArgs(message, systemId, type));
			}
		}

		private bool PreferredPlatformIsDefined(string extension)
		{
			if (Global.Config.PreferredPlatformsForExtensions.ContainsKey(extension))
			{
				return !string.IsNullOrEmpty(Global.Config.PreferredPlatformsForExtensions[extension]);
			}

			return false;
		}

		public bool LoadRom(string path, CoreComm nextComm, bool forceAccurateCore = false) // forceAccurateCore is currently just for Quicknes vs Neshawk but could be used for other situations
		{
			if (path == null)
			{
				return false;
			}

			using (var file = new HawkFile())
			{
				var romExtensions = new[] { "SMS", "SMC", "SFC", "PCE", "SGX", "GG", "SG", "BIN", "GEN", "MD", "SMD", "GB", "NES", "FDS", "ROM", "INT", "GBC", "UNF", "A78", "CRT", "COL", "XML", "Z64", "V64", "N64", "WS", "WSC", "GBA" };

				// lets not use this unless we need to
				// file.NonArchiveExtensions = romExtensions;
				file.Open(path);

				// if the provided file doesnt even exist, give up!
				if (!file.Exists)
				{
					return false;
				}

				// try binding normal rom extensions first
				if (!file.IsBound)
				{
					file.BindSoleItemOf(romExtensions);
				}

				// if we have an archive and need to bind something, then pop the dialog
				if (file.IsArchive && !file.IsBound)
				{
					var result = HandleArchive(file);
					if (result.HasValue)
					{
						file.BindArchiveMember(result.Value);
					}
					else
					{
						return false;
					}
				}

				// set this here so we can see what file we tried to load even if an error occurs
				CanonicalFullPath = file.CanonicalFullPath;

				IEmulator nextEmulator = null;
				RomGame rom = null;
				GameInfo game = null;

				try
				{
					var ext = file.Extension.ToLower();
					if (ext == ".iso" || ext == ".cue" || ext == ".ccd")
					{
						Disc disc = null;
						if(ext == ".iso")
							disc = Disc.FromIsoPath(path);
						if(ext == ".cue")
							disc = Disc.FromCuePath(path, new CueBinPrefs());
						if (ext == ".ccd")
							disc = Disc.FromCCDPath(path);

						var hash = disc.GetHash();
						game = Database.CheckDatabase(hash);
						if (game == null)
						{
							// try to use our wizard methods
							game = new GameInfo { Name = Path.GetFileNameWithoutExtension(file.Name), Hash = hash };

							switch (disc.DetectDiscType())
							{
								case DiscType.SegaSaturn:
									game.System = "SAT";
									break;
								case DiscType.SonyPSP:
									game.System = "PSP";
									break;
								case DiscType.SonyPSX:
									game.System = "PSX";
									break;
								case DiscType.MegaCD:
									game.System = "GEN";
									break;
								case DiscType.TurboCD:
								case DiscType.UnknownCDFS:
								case DiscType.UnknownFormat:
								default: // PCECD was bizhawk's first CD core,
									// and during that time, all CDs were blindly sent to it
									// so this prevents regressions
									game.System = "PCECD";
									break;
							}
						}

						switch (game.System)
						{
							case "GEN":
								var genesis = new GPGX(
										nextComm, null, disc, GetCoreSettings<GPGX>(), GetCoreSyncSettings<GPGX>());
								nextEmulator = genesis;
								break;
							case "SAT":
								nextEmulator = new Yabause(nextComm, disc, GetCoreSyncSettings<Yabause>());
								break;
							case "PSP":
								nextEmulator = new PSP(nextComm, file.Name);
								break;
							case "PSX":
								nextEmulator = new Octoshock(nextComm, disc);
								nextEmulator.CoreComm.RomStatusDetails = "PSX etc.";
								break;
							case "PCE":
							case "PCECD":
								nextEmulator = new PCEngine(nextComm, game, disc, GetCoreSettings<PCEngine>(), GetCoreSyncSettings<PCEngine>());
								break;
						}
					}
					else if (file.Extension.ToLower() == ".xml")
					{
						try
						{
							var xmlGame = XmlGame.Create(file); // if load fails, are we supposed to retry as a bsnes XML????????
							game = xmlGame.GI;

							switch (game.System)
							{
								case "DGB":
									var left = Database.GetGameInfo(xmlGame.Assets["LeftRom"], "left.gb");
									var right = Database.GetGameInfo(xmlGame.Assets["RightRom"], "right.gb");
									nextEmulator = new GambatteLink(
										nextComm,
										left,
										xmlGame.Assets["LeftRom"],
										right,
										xmlGame.Assets["RightRom"],
										GetCoreSettings<GambatteLink>(),
										GetCoreSyncSettings<GambatteLink>(),
										Deterministic);

									// other stuff todo
									break;
								default:
									return false;
							}
						}
						catch (Exception ex)
						{
							DoLoadErrorCallback(ex.ToString(), "DGB", LoadErrorType.XML);
							return false;
						}
					}
					else // most extensions
					{
						rom = new RomGame(file);

						if (string.IsNullOrEmpty(rom.GameInfo.System))
						{
							// Has the user picked a preference for this extension?
							if (PreferredPlatformIsDefined(rom.Extension.ToLower()))
							{
								rom.GameInfo.System = Global.Config.PreferredPlatformsForExtensions[rom.Extension.ToLower()];
							}
							else if (ChoosePlatform != null)
							{
								rom.GameInfo.System = ChoosePlatform(rom);
							}
						}

						game = rom.GameInfo;

						var isXml = false;

						// other xml has already been handled
						if (file.Extension.ToLower() == ".xml")
						{
							game.System = "SNES";
							isXml = true;
						}


						CoreInventory.Core core = null;

						switch (game.System)
						{
							default:
								core = CoreInventory.Instance[game.System];
								break;

							case null:
								// The user picked nothing in the Core picker
								break;
							case "SNES":
								if (Global.Config.SNES_InSnes9x && VersionInfo.DeveloperBuild)
								{
									core = CoreInventory.Instance["SNES", "Snes9x"];
								}
								else
								{
									// need to get rid of this hack at some point
									((CoreFileProvider)nextComm.CoreFileProvider).SubfileDirectory = Path.GetDirectoryName(path.Replace("|", String.Empty)); // Dirty hack to get around archive filenames (since we are just getting the directory path, it is safe to mangle the filename
									var romData = isXml ? null : rom.FileData;
									var xmlData = isXml ? rom.FileData : null;
									var snes = new LibsnesCore(game, romData, Deterministic, xmlData, nextComm, GetCoreSettings<LibsnesCore>(), GetCoreSyncSettings<LibsnesCore>());
									nextEmulator = snes;
								}

								break;
							case "NES":
								if (!Global.Config.NES_InQuickNES || forceAccurateCore)
								{
									core = CoreInventory.Instance["NES", "NesHawk"];
								}
								else
								{
									core = CoreInventory.Instance["NES", "QuickNes"];
								}

								break;
							case "GB":
							case "GBC":
								if (!Global.Config.GB_AsSGB)
								{
									core = CoreInventory.Instance["GB", "Gambatte"];
								}
								else
								{
									try
									{
										game.System = "SNES";
										game.AddOption("SGB");
										var snes = new LibsnesCore(game, rom.FileData, Deterministic, null, nextComm, GetCoreSettings<LibsnesCore>(), GetCoreSyncSettings<LibsnesCore>());
										nextEmulator = snes;
									}
									catch
									{
										// failed to load SGB bios or game does not support SGB mode. 
										// To avoid catch-22, disable SGB mode
										Global.Config.GB_AsSGB = false;
										throw;
									}
								}

								break;
							case "A78":
								var gamedbpath = Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb", "EMU7800.csv");
								nextEmulator = new Atari7800(nextComm, game, rom.RomData, gamedbpath);
								break;
							case "C64":
								var c64 = new C64(nextComm, game, rom.RomData, rom.Extension);
								nextEmulator = c64;
								break;
							case "GBA":
								//core = CoreInventory.Instance["GBA", "Meteor"];
								core = CoreInventory.Instance["GBA", "VBA-Next"];
								break;
							case "DEBUG":
								if (VersionInfo.DeveloperBuild)
								{
									nextEmulator = LibRetroEmulator.CreateDebug(nextComm, rom.RomData);
								}

								break;
						}

						if (core != null)
						{
							// use coreinventory
							nextEmulator = core.Create(nextComm, game, rom.RomData, rom.FileData, Deterministic, GetCoreSettings(core.Type), GetCoreSyncSettings(core.Type));
						}
					}

					if (nextEmulator == null)
					{
						DoLoadErrorCallback("No core could load the rom.", null);
						return false;
					}
				}
				catch (Exception ex)
				{
					string system = null;
					if (game != null)
					{
						system = game.System;
					}

					// all of the specific exceptions we're trying to catch here aren't expected to have inner exceptions,
					// so drill down in case we got a TargetInvocationException or something like that
					while (ex.InnerException != null)
						ex = ex.InnerException;

					// Specific hack here, as we get more cores of the same system, this isn't scalable
					if (ex is UnsupportedGameException)
					{
						return LoadRom(path, nextComm, forceAccurateCore: true);
					}
					else if (ex is MissingFirmwareException)
					{
						DoLoadErrorCallback(ex.Message, system, LoadErrorType.MissingFirmware);
					}
					else if (ex is CGBNotSupportedException)
					{
						// Note: GB as SGB was set to false by this point, otherwise we would want to do it here
						DoMessageCallback("Failed to load a GB rom in SGB mode.  Disabling SGB Mode.");
						return LoadRom(path, nextComm);
					}
					else
					{
						DoLoadErrorCallback("A core accepted the rom, but threw an exception while loading it:\n\n" + ex, system);
					}

					return false;
				}

				Rom = rom;
				LoadedEmulator = nextEmulator;
				Game = game;
				return true;
			}
		}
	}
}
