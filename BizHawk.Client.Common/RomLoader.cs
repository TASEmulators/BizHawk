using System;
using System.Collections.Generic;
using System.Linq;
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
using BizHawk.Emulation.Cores.Computers.AppleII;

namespace BizHawk.Client.Common
{
	public class RomLoader
	{
		public enum LoadErrorType { Unknown, MissingFirmware, XML, DiscError }

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

			public RomErrorArgs(string message, string systemId, string path, bool? det, LoadErrorType type) 
				: this(message, systemId, type)
			{
				Deterministic = det;
				RomPath = path;
			}

			public string Message { get; private set; }
			public string AttemptedCoreLoad { get; private set; }
			public string RomPath { get; private set; }
			public bool? Deterministic { get; set; }
			public bool Retry { get; set; }
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

		public delegate void LoadErrorEventHandler(object sender, RomErrorArgs e);
		public event LoadErrorEventHandler OnLoadError;

		public Func<HawkFile, int?> ChooseArchive { get; set; }

		public Func<RomGame, string> ChoosePlatform { get; set; }

		// in case we get sent back through the picker more than once, use the same choice the second time
		int? previouschoice;
		private int? HandleArchive(HawkFile file)
		{
			if (previouschoice.HasValue)
				return previouschoice;

			if (ChooseArchive != null)
			{
				previouschoice = ChooseArchive(file);
				return previouschoice;
			}

			return null;
		}

		//May want to phase out this method in favor of the overload with more paramaters
		private void DoLoadErrorCallback(string message, string systemId, LoadErrorType type = LoadErrorType.Unknown)
		{
			if (OnLoadError != null)
			{
				OnLoadError(this, new RomErrorArgs(message, systemId, type));
			}
		}

		private void DoLoadErrorCallback(string message, string systemId, string path, bool det, LoadErrorType type = LoadErrorType.Unknown)
		{
			if (OnLoadError != null)
			{
				OnLoadError(this, new RomErrorArgs(message, systemId, path, det, type));
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

		public bool AsLibretro;

		bool HandleArchiveBinding(HawkFile file)
		{
			var romExtensions = new[] { "SMS", "SMC", "SFC", "PCE", "SGX", "GG", "SG", "BIN", "GEN", "MD", "SMD", "GB", "NES", "FDS", "ROM", "INT", "GBC", "UNF", "A78", "CRT", "COL", "XML", "Z64", "V64", "N64", "WS", "WSC", "GBA" };

			// try binding normal rom extensions first
			if (!file.IsBound)
			{
				file.BindSoleItemOf(romExtensions);
			}

			// if we have an archive and need to bind something, then pop the dialog
			if (file.IsArchive && !file.IsBound)
			{
				int? result = HandleArchive(file);
				if (result.HasValue)
				{
					file.BindArchiveMember(result.Value);
				}
				else
				{
					return false;
				}
			}

			CanonicalFullPath = file.CanonicalFullPath;

			return true;
		}

		public bool LoadRom(string path, CoreComm nextComm, bool forceAccurateCore = false,
			int recursiveCount = 0) // forceAccurateCore is currently just for Quicknes vs Neshawk but could be used for other situations
		{
			if (recursiveCount > 1) // hack to stop recursive calls from endlessly rerunning if we can't load it
			{
				DoLoadErrorCallback("Failed multiple attempts to load ROM.", "");
				return false;
			}

			bool cancel = false;

			if (path == null)
			{
				return false;
			}

			using (var file = new HawkFile())
			{

				//only try mounting a file if a filename was given
				if (!string.IsNullOrEmpty(path))
				{
					// lets not use this unless we need to
					// file.NonArchiveExtensions = romExtensions;
					file.Open(path);

					// if the provided file doesnt even exist, give up!
					if (!file.Exists)
					{
						return false;
					}
				}

				CanonicalFullPath = file.CanonicalFullPath;

				IEmulator nextEmulator = null;
				RomGame rom = null;
				GameInfo game = null;

				try
				{
					string ext = null;

					if (AsLibretro)
					{
						string codePathPart = Path.GetFileNameWithoutExtension(nextComm.LaunchLibretroCore);

						var retro = new LibRetroEmulator(nextComm, nextComm.LaunchLibretroCore);
						nextEmulator = retro;

						//kind of dirty.. we need to stash this, and then we can unstash it in a moment, in case the core doesnt fail
						var oldGame = Global.Game;

						if (retro.Description.SupportsNoGame && string.IsNullOrEmpty(path))
						{
							//must be done before LoadNoGame (which triggers retro_init and the paths to be consumed by the core)
							//game name == name of core
							var gameName = codePathPart;
							Global.Game = game = new GameInfo { Name = gameName, System = "Libretro" };

							//if we are allowed to run NoGame and we dont have a game, boot up the core that way
							bool ret = retro.LoadNoGame();

							Global.Game = oldGame;

							if (!ret)
							{
								DoLoadErrorCallback("LibretroNoGame failed to load. This is weird", "Libretro");
								retro.Dispose();
								return false;
							}
						}
						else
						{
							bool ret;

							//must be done before LoadNoGame (which triggers retro_init and the paths to be consumed by the core)
							//game name == name of core + extensionless_game_filename
							var gameName = Path.Combine(codePathPart, Path.GetFileNameWithoutExtension(file.Name));
							Global.Game = game = new GameInfo { Name = gameName, System = "Libretro" };

							//if the core requires an archive file, then try passing the filename of the archive
							//(but do we ever need to actually load the contents of the archive file into ram?)
							if (retro.Description.NeedsArchives)
							{
								if (file.IsArchiveMember)
									throw new InvalidOperationException("Should not have bound file member for libretro block_extract core");
								ret = retro.LoadPath(file.FullPathWithoutMember);
							}
							else
							{
								//otherwise load the data or pass the filename, as requested. but..
								if (retro.Description.NeedsRomAsPath && file.IsArchiveMember)
									throw new InvalidOperationException("Cannot pass archive member to libretro needs_fullpath core");

								if (retro.Description.NeedsRomAsPath)
									ret = retro.LoadPath(file.FullPathWithoutMember);
								else
								{
									ret = HandleArchiveBinding(file);
									if (ret)
										ret = retro.LoadData(file.ReadAllBytes());
								}
							}

							Global.Game = oldGame;

							if (!ret)
							{
								DoLoadErrorCallback("Libretro failed to load the given file. This is probably due to a core/content mismatch. Moreover, the process is now likely to be hosed. We suggest you restart the program.", "Libretro");
								retro.Dispose();
								return false;
							}
						}


					}
					else
					{
						//if not libretro:
						
						//do extension checknig
						ext = file.Extension.ToLowerInvariant();

						//do the archive binding we had to skip
						if (!HandleArchiveBinding(file))
							return false;
					}

					if (string.IsNullOrEmpty(ext)) { }
					else if (ext == ".m3u")
					{
						//HACK ZONE - currently only psx supports m3u
						M3U_File m3u;
						using(var sr = new StreamReader(path))
							m3u = M3U_File.Read(sr);
						if(m3u.Entries.Count == 0)
							throw new InvalidOperationException("Can't load an empty M3U");
						//load discs for all the m3u
						m3u.Rebase(Path.GetDirectoryName(path));
						List<Disc> discs = new List<Disc>();
						List<string> discNames = new List<string>();
						StringWriter sw = new StringWriter();
						foreach (var e in m3u.Entries)
						{
							Disc disc = null;
							string discPath = e.Path;

							//--- load the disc in a context which will let us abort if it's going to take too long
							var discMountJob = new DiscMountJob { IN_FromPath = discPath };
							discMountJob.IN_SlowLoadAbortThreshold = 8;
							discMountJob.Run();
							disc = discMountJob.OUT_Disc;

							if (discMountJob.OUT_SlowLoadAborted)
							{
								DoLoadErrorCallback("This disc would take too long to load. Run it through discohawk first, or find a new rip because this one is probably junk", "", LoadErrorType.DiscError);
								return false;
							}

							if (discMountJob.OUT_ErrorLevel)
								throw new InvalidOperationException("\r\n" + discMountJob.OUT_Log);

							if(disc == null)
								throw new InvalidOperationException("Can't load one of the files specified in the M3U");

							var discName = Path.GetFileNameWithoutExtension(discPath);
							discNames.Add(discName);
							discs.Add(disc);

							var discType = new DiscIdentifier(disc).DetectDiscType();
							sw.WriteLine("{0}", Path.GetFileName(discPath));
							if (discType == DiscType.SonyPSX)
							{
								string discHash = new DiscHasher(disc).Calculate_PSX_BizIDHash().ToString("X8");
								game = Database.CheckDatabase(discHash);
								if (game == null || game.IsRomStatusBad() || game.Status == RomStatus.NotInDatabase)
									sw.WriteLine("Disc could not be identified as known-good. Look for a better rip.");
								else
								{
									sw.WriteLine("Disc was identified (99.99% confidently) as known good with disc id hash CRC32:{0:X8}",discHash);
									sw.WriteLine("Nonetheless it could be an unrecognized romhack or patched version.");
									sw.WriteLine("According to redump.org, the ideal hash for entire disc is: CRC32:{0:X8}", game.GetStringValue("dh"));
									sw.WriteLine("The file you loaded hasn't been hashed entirely (it would take too long)");
									sw.WriteLine("Compare it with the full hash calculated by the PSX menu's Hash Discs tool");
								}
							}
							else
							{
								sw.WriteLine("Not a PSX disc");
							}
							sw.WriteLine("-------------------------");
						}

						nextEmulator = new Octoshock(nextComm, discs, discNames, null, GetCoreSettings<Octoshock>(), GetCoreSyncSettings<Octoshock>());
						nextEmulator.CoreComm.RomStatusDetails = sw.ToString();
						game = new GameInfo { Name = Path.GetFileNameWithoutExtension(file.Name) };
						game.System = "PSX";
					}
					else if (ext == ".iso" || ext == ".cue" || ext == ".ccd")
					{
						if (file.IsArchive)
						{
							throw new InvalidOperationException("Can't load CD files from archives!");
						}

						string discHash = null;

						//--- load the disc in a context which will let us abort if it's going to take too long
						var discMountJob = new DiscMountJob { IN_FromPath = path };
						discMountJob.IN_SlowLoadAbortThreshold = 8;
						discMountJob.Run();

						if (discMountJob.OUT_SlowLoadAborted)
						{
							DoLoadErrorCallback("This disc would take too long to load. Run it through discohawk first, or find a new rip because this one is probably junk", "", LoadErrorType.DiscError);
							return false;
						}

						if (discMountJob.OUT_ErrorLevel)
							throw new InvalidOperationException("\r\n" + discMountJob.OUT_Log);

						var disc = discMountJob.OUT_Disc;
						//-----------
						
						//TODO - use more sophisticated IDer
						var discType = new DiscIdentifier(disc).DetectDiscType();
						if (discType == DiscType.SonyPSX)
							discHash = new DiscHasher(disc).Calculate_PSX_BizIDHash().ToString("X8");
						else discHash = new DiscHasher(disc).OldHash();

						game = Database.CheckDatabase(discHash);
						if (game == null)
						{
							// try to use our wizard methods
							game = new GameInfo { Name = Path.GetFileNameWithoutExtension(file.Name), Hash = discHash };

							switch (new DiscIdentifier(disc).DetectDiscType())
							{
								case DiscType.SegaSaturn:
									game.System = "SAT";
									break;
								case DiscType.SonyPSP:
									game.System = "PSP";
									break;
								default: 
								case DiscType.SonyPSX:
									game.System = "PSX";
									break;
								case DiscType.MegaCD:
									game.System = "GEN";
									break;
								case DiscType.AudioDisc:
								case DiscType.TurboCD:
								case DiscType.UnknownCDFS:
								case DiscType.UnknownFormat:
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
								nextEmulator = new Octoshock(nextComm, new List<Disc>(new[]{disc}), new List<string>(new[]{Path.GetFileNameWithoutExtension(path)}), null, GetCoreSettings<Octoshock>(), GetCoreSyncSettings<Octoshock>());
								if (game.IsRomStatusBad() || game.Status == RomStatus.NotInDatabase)
									nextEmulator.CoreComm.RomStatusDetails = "Disc could not be identified as known-good. Look for a better rip.";
								else
								{
									StringWriter sw = new StringWriter();
									sw.WriteLine("Disc was identified (99.99% confidently) as known good with disc id hash CRC32:{0:X8}", discHash);
									sw.WriteLine("Nonetheless it could be an unrecognized romhack or patched version.");
									sw.WriteLine("According to redump.org, the ideal hash for entire disc is: CRC32:{0:X8}", game.GetStringValue("dh"));
									sw.WriteLine("The file you loaded hasn't been hashed entirely (it would take too long)");
									sw.WriteLine("Compare it with the full hash calculated by the PSX menu's Hash Discs tool");
									nextEmulator.CoreComm.RomStatusDetails = sw.ToString();
								}
								break;
							case "PCE":
							case "PCECD":
								nextEmulator = new PCEngine(nextComm, game, disc, GetCoreSettings<PCEngine>(), GetCoreSyncSettings<PCEngine>());
								break;
						}
					}
					else if (file.Extension.ToLowerInvariant() == ".xml")
					{
						try
						{
							var xmlGame = XmlGame.Create(file); // if load fails, are we supposed to retry as a bsnes XML????????
							game = xmlGame.GI;

							switch (game.System)
							{
								case "GB":
								case "DGB":
									// adelikat: remove need for tags to be hardcoded to left and right, we should clean this up, also maybe the DGB core should just take the xml file and handle it itself
									var leftBytes = xmlGame.Assets.First().Value;
									var rightBytes = xmlGame.Assets.Skip(1).First().Value;

									var left = Database.GetGameInfo(leftBytes, "left.gb");
									var right = Database.GetGameInfo(rightBytes, "right.gb");
									nextEmulator = new GambatteLink(
										nextComm,
										left,
										leftBytes,
										right,
										rightBytes,
										GetCoreSettings<GambatteLink>(),
										GetCoreSyncSettings<GambatteLink>(),
										Deterministic);

									// other stuff todo
									break;
								case "AppleII":
									var assets = xmlGame.Assets.Select(a => Database.GetGameInfo(a.Value, a.Key));
									var roms = xmlGame.Assets.Select(a => a.Value);
									nextEmulator = new AppleII(
										nextComm,
										assets,
										roms,
										(AppleII.Settings)GetCoreSettings<AppleII>());
									break;
								case "C64":
									nextEmulator = new C64(
										nextComm,
                                        xmlGame.Assets.Select(a => a.Value),
										(C64.C64Settings)GetCoreSettings<C64>(),
										(C64.C64SyncSettings)GetCoreSyncSettings<C64>()
									);
									break;
								case "PSX":
									var entries = xmlGame.AssetFullPaths;
									var discs = new List<Disc>();
									var discNames = new List<string>();
									var sw = new StringWriter();
									foreach (var e in entries)
									{
										Disc disc = null;
										string discPath = e;

										//--- load the disc in a context which will let us abort if it's going to take too long
										var discMountJob = new DiscMountJob { IN_FromPath = discPath };
										discMountJob.IN_SlowLoadAbortThreshold = 8;
										discMountJob.Run();
										disc = discMountJob.OUT_Disc;

										if (discMountJob.OUT_SlowLoadAborted)
										{
											DoLoadErrorCallback("This disc would take too long to load. Run it through discohawk first, or find a new rip because this one is probably junk", "PSX", LoadErrorType.DiscError);
											return false;
										}

										if (discMountJob.OUT_ErrorLevel)
											throw new InvalidOperationException("\r\n" + discMountJob.OUT_Log);

										if (disc == null)
											throw new InvalidOperationException("Can't load one of the files specified in the M3U");

										var discName = Path.GetFileNameWithoutExtension(discPath);
										discNames.Add(discName);
										discs.Add(disc);

										var discType = new DiscIdentifier(disc).DetectDiscType();
										sw.WriteLine("{0}", Path.GetFileName(discPath));
										if (discType == DiscType.SonyPSX)
										{
											string discHash = new DiscHasher(disc).Calculate_PSX_BizIDHash().ToString("X8");
											game = Database.CheckDatabase(discHash);
											if (game == null || game.IsRomStatusBad() || game.Status == RomStatus.NotInDatabase)
												sw.WriteLine("Disc could not be identified as known-good. Look for a better rip.");
											else
											{
												sw.WriteLine("Disc was identified (99.99% confidently) as known good with disc id hash CRC32:{0:X8}", discHash);
												sw.WriteLine("Nonetheless it could be an unrecognized romhack or patched version.");
												sw.WriteLine("According to redump.org, the ideal hash for entire disc is: CRC32:{0:X8}", game.GetStringValue("dh"));
												sw.WriteLine("The file you loaded hasn't been hashed entirely (it would take too long)");
												sw.WriteLine("Compare it with the full hash calculated by the PSX menu's Hash Discs tool");
											}
										}
										else
										{
											sw.WriteLine("Not a PSX disc");
										}
										sw.WriteLine("-------------------------");
									}

									// todo: copy pasta from PSX .cue section
									nextEmulator = new Octoshock(nextComm, discs, discNames, null, GetCoreSettings<Octoshock>(), GetCoreSyncSettings<Octoshock>());
									nextEmulator.CoreComm.RomStatusDetails = sw.ToString();
									game = new GameInfo { Name = Path.GetFileNameWithoutExtension(file.Name) };
									game.System = "PSX";

									break;
								default:
									return false;
							}
						}
						catch (Exception ex)
						{
							try
							{
								// need to get rid of this hack at some point
								rom = new RomGame(file);
								((CoreFileProvider)nextComm.CoreFileProvider).SubfileDirectory = Path.GetDirectoryName(path.Replace("|", string.Empty)); // Dirty hack to get around archive filenames (since we are just getting the directory path, it is safe to mangle the filename
								byte[] romData = null;
								byte[] xmlData = rom.FileData;

								game = rom.GameInfo;
								game.System = "SNES";
								
								var snes = new LibsnesCore(game, romData, Deterministic, xmlData, nextComm, GetCoreSettings<LibsnesCore>(), GetCoreSyncSettings<LibsnesCore>());
								nextEmulator = snes;
							}
							catch 
							{
								DoLoadErrorCallback(ex.ToString(), "DGB", LoadErrorType.XML);
								return false;
							}
						}
					}
					else if (file.Extension.ToLowerInvariant() == ".psf" || file.Extension.ToLowerInvariant() == ".minipsf")
					{
						Func<Stream, int, byte[]> cbDeflater = (Stream instream, int size) =>
						{
							var inflater = new ICSharpCode.SharpZipLib.Zip.Compression.Inflater(false);
							var iis = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream(instream, inflater);
							MemoryStream ret = new MemoryStream();
							iis.CopyTo(ret);
							return ret.ToArray();
						};
						PSF psf = new PSF();
						psf.Load(path, cbDeflater);
						nextEmulator = new Octoshock(nextComm, psf, GetCoreSettings<Octoshock>(), GetCoreSyncSettings<Octoshock>());
						nextEmulator.CoreComm.RomStatusDetails = "It's a PSF, what do you want. Oh, tags maybe?";

						//total garbage, this
						rom = new RomGame(file);
						game = rom.GameInfo;
					}
					else if (ext != null) // most extensions
					{
						rom = new RomGame(file);

						//hacky for now
						if (file.Extension.ToLowerInvariant() == ".exe")
							rom.GameInfo.System = "PSX";
						else if (file.Extension.ToLowerInvariant() == ".nsf")
							rom.GameInfo.System = "NES";


						if (string.IsNullOrEmpty(rom.GameInfo.System))
						{
							// Has the user picked a preference for this extension?
							if (PreferredPlatformIsDefined(rom.Extension.ToLowerInvariant()))
							{
								rom.GameInfo.System = Global.Config.PreferredPlatformsForExtensions[rom.Extension.ToLowerInvariant()];
							}
							else if (ChoosePlatform != null)
							{
								var result = ChoosePlatform(rom);
								if (!string.IsNullOrEmpty(result))
								{
									rom.GameInfo.System = result;
								}
								else
								{
									cancel = true;
								}
							}
						}

						game = rom.GameInfo;

						var isXml = false;

						// other xml has already been handled
						if (file.Extension.ToLowerInvariant() == ".xml")
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
							case "83P":
								var ti83Bios = ((CoreFileProvider)nextComm.CoreFileProvider).GetFirmware("TI83", "Rom", true);
								var ti83BiosPath = ((CoreFileProvider)nextComm.CoreFileProvider).GetFirmwarePath("TI83", "Rom", true);
								using (var ti83AsHawkFile = new HawkFile())
								{
									ti83AsHawkFile.Open(ti83BiosPath);
									var ti83BiosAsRom = new RomGame(ti83AsHawkFile);
									var ti83 = new TI83(nextComm, ti83BiosAsRom.GameInfo, ti83Bios, GetCoreSettings<TI83>());
									ti83.LinkPort.SendFileToCalc(File.OpenRead(path), false);
									nextEmulator = ti83;
								}
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
								{
									//apply main spur-of-the-moment switcheroo as lowest priority
									string preference = "neshawk";
									if(Global.Config.NES_InQuickNES) preference = "quicknes";

									//if user has saw fit to override in gamedb, apply that
									if (Global.Config.CoreForcingViaGameDB && !string.IsNullOrEmpty(game.ForcedCore))
										preference = game.ForcedCore;

									//but only neshawk is accurate
									if (forceAccurateCore) preference = "neshawk";

									if (preference == "neshawk")
									{
										core = CoreInventory.Instance["NES", "NesHawk"];
									}
									else
									{
										core = CoreInventory.Instance["NES", "QuickNes"];
									}
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
								var c64 = new C64(nextComm, Enumerable.Repeat(rom.RomData, 1), GetCoreSettings<C64>(), GetCoreSyncSettings<C64>());
								nextEmulator = c64;
								break;
							case "GBA":
								//core = CoreInventory.Instance["GBA", "Meteor"];
								if (Global.Config.GBA_UsemGBA)
								{
									core = CoreInventory.Instance["GBA", "mGBA"];
								}
								else
								{
									core = CoreInventory.Instance["GBA", "VBA-Next"];
								}
								break;
							case "PSX":
								nextEmulator = new Octoshock(nextComm, null, null, rom.FileData, GetCoreSettings<Octoshock>(), GetCoreSyncSettings<Octoshock>());
								nextEmulator.CoreComm.RomStatusDetails = "PSX etc.";
								break;
							case "GEN":
								// discard "Genplus-gx64", auto-added due to implementing IEmulator
								core = CoreInventory.Instance["GEN", "Genplus-gx"];
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
						if (!cancel)
						{
							DoLoadErrorCallback("No core could load the rom.", null);
						}
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
						if (system == "NES")
						{
							DoMessageCallback("Unable to use quicknes, using NESHawk instead");
						}
						
						return LoadRom(path, nextComm, true, recursiveCount + 1);
					}
					else if (ex is MissingFirmwareException)
					{
						DoLoadErrorCallback(ex.Message, system, path, Deterministic, LoadErrorType.MissingFirmware);
					}
					else if (ex is CGBNotSupportedException)
					{
						// Note: GB as SGB was set to false by this point, otherwise we would want to do it here
						DoMessageCallback("Failed to load a GB rom in SGB mode.  Disabling SGB Mode.");
						return LoadRom(path, nextComm, false, recursiveCount + 1);
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
