using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Libretro;
using BizHawk.Emulation.Cores.Calculators;
using BizHawk.Emulation.Cores.Computers.AppleII;
using BizHawk.Emulation.Cores.Computers.Commodore64;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink3x;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.GGHawkLink;
using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;
using BizHawk.Emulation.Cores.Arcades.MAME;
using BizHawk.Emulation.DiscSystem;

using BizHawk.Emulation.Cores.Consoles.Sega.Saturn;
using BizHawk.Emulation.Cores.Consoles.NEC.PCFX;
using BizHawk.Emulation.Cores.Computers.AmstradCPC;
using BizHawk.Emulation.Cores.Consoles.ChannelF;
using BizHawk.Emulation.Cores.Consoles.NEC.PCE;

namespace BizHawk.Client.Common
{
	public class RomLoader
	{
		private readonly Config _config;
		private readonly FirmwareManager _firmwareManager;

		public RomLoader(Config config, FirmwareManager firmwareManager)
		{
			_config = config;
			_firmwareManager = firmwareManager;
		}

		public enum LoadErrorType
		{
			Unknown, MissingFirmware, Xml, DiscError
		}

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
			OnLoadSettings?.Invoke(this, e);
			return e.Settings;
		}

		private object GetCoreSyncSettings(Type t)
		{
			var e = new SettingsLoadArgs(t);
			OnLoadSyncSettings?.Invoke(this, e);
			return e.Settings;
		}

		// For not throwing errors but simply outputting information to the screen
		public Action<string> MessageCallback { get; set; }

		private void DoMessageCallback(string message)
		{
			MessageCallback?.Invoke(message);
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

			public string Message { get; }
			public string AttemptedCoreLoad { get; }
			public string RomPath { get; }
			public bool? Deterministic { get; set; }
			public bool Retry { get; set; }
			public LoadErrorType Type { get; }
		}

		public class SettingsLoadArgs : EventArgs
		{
			public object Settings { get; set; }
			public Type Core { get; }
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
		private int? _previousChoice;
		private int? HandleArchive(HawkFile file)
		{
			if (_previousChoice.HasValue)
			{
				return _previousChoice;
			}

			if (ChooseArchive != null)
			{
				_previousChoice = ChooseArchive(file);
				return _previousChoice;
			}

			return null;
		}

		// May want to phase out this method in favor of the overload with more parameters
		private void DoLoadErrorCallback(string message, string systemId, LoadErrorType type = LoadErrorType.Unknown)
		{
			OnLoadError?.Invoke(this, new RomErrorArgs(message, systemId, type));
		}

		private void DoLoadErrorCallback(string message, string systemId, string path, bool det, LoadErrorType type = LoadErrorType.Unknown)
		{
			OnLoadError?.Invoke(this, new RomErrorArgs(message, systemId, path, det, type));
		}

		private bool PreferredPlatformIsDefined(string extension)
		{
			if (_config.PreferredPlatformsForExtensions.ContainsKey(extension))
			{
				return !string.IsNullOrEmpty(_config.PreferredPlatformsForExtensions[extension]);
			}

			return false;
		}

		public IOpenAdvanced OpenAdvanced { get; set; }

		private bool HandleArchiveBinding(HawkFile file)
		{
			var romExtensions = new[]
			{
				"SMS", "SMC", "SFC", "PCE", "SGX", "GG", "SG", "BIN", "GEN", "MD", "SMD", "GB",
				"NES", "FDS", "ROM", "INT", "GBC", "UNF", "A78", "CRT", "COL", "XML", "Z64",
				"V64", "N64", "WS", "WSC", "GBA", "32X", "VEC", "O2"
			};

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

		private List<Disc> DiscsFromXml(XmlGame xmlGame, string systemId, DiscType diskType)
		{
			var discs = new List<Disc>();
			foreach (var e in xmlGame.AssetFullPaths.Where(a => Disc.IsValidExtension(Path.GetExtension(a))))
			{
				var disc = diskType.Create(e, str => { DoLoadErrorCallback(str, systemId, LoadErrorType.DiscError); });
				if (disc != null)
				{
					discs.Add(disc);
				}
			}

			return discs;
		}

		public bool LoadRom(string path, CoreComm nextComm, string launchLibretroCore, bool forceAccurateCore = false, int recursiveCount = 0)
		{
			if (path == null) return false;

			if (recursiveCount > 1) // hack to stop recursive calls from endlessly rerunning if we can't load it
			{
				DoLoadErrorCallback("Failed multiple attempts to load ROM.", "");
				return false;
			}

			using var file = new HawkFile(
				path,
				nonArchiveExtensions: OpenAdvanced is OpenAdvanced_MAME
					? new[] { ".zip", ".7z" } // MAME uses these extensions for arcade ROMs, but also accepts all sorts of variations of archives, folders, and files. if we let archive loader handle this, it won't know where to stop, since it'd require MAME's ROM database (which contains ROM names and blob hashes) to look things up, and even then it might be confused by archive/folder structure. so assume the user provides the proper ROM directly, and handle possible errors later
					: null
			);
			if (!file.Exists) return false; // if the provided file doesn't even exist, give up!

			CanonicalFullPath = file.CanonicalFullPath;

			IEmulator nextEmulator = null;
			RomGame rom = null;
			GameInfo game = null;

			try
			{
				string ext = null;
				var cancel = false;

				if (OpenAdvanced is OpenAdvanced_Libretro)
				{
					// must be done before LoadNoGame (which triggers retro_init and the paths to be consumed by the core)
					// game name == name of core
					string codePathPart = Path.GetFileNameWithoutExtension(launchLibretroCore);
					Game = game = new GameInfo { Name = codePathPart, System = "Libretro" };
					var retro = new LibretroCore(nextComm, game, launchLibretroCore);
					nextEmulator = retro;

					if (retro.Description.SupportsNoGame && string.IsNullOrEmpty(path))
					{
						// if we are allowed to run NoGame and we don't have a game, boot up the core that way
						bool ret = retro.LoadNoGame();

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

						// if the core requires an archive file, then try passing the filename of the archive
						// (but do we ever need to actually load the contents of the archive file into ram?)
						if (retro.Description.NeedsArchives)
						{
							if (file.IsArchiveMember)
							{
								throw new InvalidOperationException("Should not have bound file member for libretro block_extract core");
							}

							ret = retro.LoadPath(file.FullPathWithoutMember);
						}
						else
						{
							// otherwise load the data or pass the filename, as requested. but..
							if (retro.Description.NeedsRomAsPath && file.IsArchiveMember)
							{
								throw new InvalidOperationException("Cannot pass archive member to libretro needs_fullpath core");
							}

							if (retro.Description.NeedsRomAsPath)
							{
								ret = retro.LoadPath(file.FullPathWithoutMember);
							}
							else
							{
								ret = HandleArchiveBinding(file);
								if (ret)
								{
									ret = retro.LoadData(file.ReadAllBytes(), file.Name);
								}
							}
						}

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
					// at this point, file is either assigned to the ROM path, if it exists,
					// or is empty and CoreComm is not a libretro core
					// so, we still need to check path here before continuing
					if (string.IsNullOrEmpty(path))
					{
						Console.WriteLine("No ROM to Load");
						return false;
					}

					// if not libretro:
					// do extension checking
					ext = file.Extension.ToLowerInvariant();

					// do the archive binding we had to skip
					if (!HandleArchiveBinding(file))
					{
						return false;
					}
				}

				if (string.IsNullOrEmpty(ext))
				{
				}
				else if (ext == ".m3u")
				{
					// HACK ZONE - currently only psx supports m3u
					M3U_File m3u;
					using (var sr = new StreamReader(path))
					{
						m3u = M3U_File.Read(sr);
					}

					if (m3u.Entries.Count == 0)
					{
						throw new InvalidOperationException("Can't load an empty M3U");
					}

					// load discs for all the m3u
					m3u.Rebase(Path.GetDirectoryName(path));
					var discs = new List<Disc>();
					var discNames = new List<string>();
					var sw = new StringWriter();
					foreach (var e in m3u.Entries)
					{
						var disc = DiscType.SonyPSX.Create(e.Path, str => { DoLoadErrorCallback(str, "PSX", LoadErrorType.DiscError); });
						var discName = Path.GetFileNameWithoutExtension(e.Path);
						discNames.Add(discName);
						discs.Add(disc);

						sw.WriteLine("{0}", Path.GetFileName(e.Path));

						string discHash = new DiscHasher(disc).Calculate_PSX_BizIDHash().ToString("X8");
						game = Database.CheckDatabase(discHash);
						if (game == null || game.IsRomStatusBad() || game.Status == RomStatus.NotInDatabase)
						{
							sw.WriteLine("Disc could not be identified as known-good. Look for a better rip.");
						}
						else
						{
							sw.WriteLine("Disc was identified (99.99% confidently) as known good with disc id hash CRC32:{0:X8}", discHash);
							sw.WriteLine("Nonetheless it could be an unrecognized romhack or patched version.");
							sw.WriteLine("According to redump.org, the ideal hash for entire disc is: CRC32:{0:X8}", game.GetStringValue("dh"));
							sw.WriteLine("The file you loaded hasn't been hashed entirely (it would take too long)");
							sw.WriteLine("Compare it with the full hash calculated by the PSX menu's Hash Discs tool");
						}
						
						sw.WriteLine("-------------------------");
					}

					nextEmulator = new Octoshock(nextComm, discs, discNames, null, GetCoreSettings<Octoshock>(), GetCoreSyncSettings<Octoshock>(), sw.ToString());
					game = new GameInfo
					{
						Name = Path.GetFileNameWithoutExtension(file.Name),
						System = "PSX"
					};
				}
				else if (Disc.IsValidExtension(ext))
				{
					if (file.IsArchive)
					{
						throw new InvalidOperationException("Can't load CD files from archives!");
					}

					//--- load the disc in a context which will let us abort if it's going to take too long
					var discMountJob = new DiscMountJob { IN_FromPath = path, IN_SlowLoadAbortThreshold = 8 };
					discMountJob.Run();

					if (discMountJob.OUT_SlowLoadAborted)
					{
						DoLoadErrorCallback("This disc would take too long to load. Run it through DiscoHawk first, or find a new rip because this one is probably junk", "", LoadErrorType.DiscError);
						return false;
					}

					if (discMountJob.OUT_ErrorLevel)
					{
						throw new InvalidOperationException($"\r\n{discMountJob.OUT_Log}");
					}

					var disc = discMountJob.OUT_Disc;

					// -----------
					// TODO - use more sophisticated IDer
					var discType = new DiscIdentifier(disc).DetectDiscType();
					var discHasher = new DiscHasher(disc);
					var discHash = discType == DiscType.SonyPSX
						? discHasher.Calculate_PSX_BizIDHash().ToString("X8")
						: discHasher.OldHash();

					game = Database.CheckDatabase(discHash);
					if (game == null)
					{
						// try to use our wizard methods
						game = new GameInfo { Name = Path.GetFileNameWithoutExtension(file.Name), Hash = discHash };

						switch (discType)
						{
							case DiscType.SegaSaturn:
								game.System = "SAT";
								break;
							case DiscType.SonyPSP:
								game.System = "PSP";
								break;
							case DiscType.MegaCD:
								game.System = "GEN";
								break;
							case DiscType.PCFX:
								game.System = "PCFX";
								break;

							case DiscType.TurboGECD:
							case DiscType.TurboCD:
								game.System = "PCECD";
								break;

							case DiscType.Amiga:
							case DiscType.CDi:
							case DiscType.Dreamcast:
							case DiscType.GameCube:
							case DiscType.NeoGeoCD:
							case DiscType.Panasonic3DO:
							case DiscType.Playdia:
							case DiscType.Wii:
								// no supported emulator core for these (yet)
								game.System = discType.ToString();
								throw new NoAvailableCoreException(discType.ToString());

							case DiscType.AudioDisc:
							case DiscType.UnknownCDFS:
							case DiscType.UnknownFormat:
								game.System = PreferredPlatformIsDefined(ext)
									? _config.PreferredPlatformsForExtensions[ext]
									: "NULL";
								break;

							default: //"for an unknown disc, default to psx instead of pce-cd, since that is far more likely to be what they are attempting to open" [5e07ab3ec3b8b8de9eae71b489b55d23a3909f55, year 2015]
							case DiscType.SonyPSX:
								game.System = "PSX";
								break;
						}
					}

					switch (game.System)
					{
						case "NULL":
							nextEmulator = null;
							break;
						case "GEN":
							var genesis = new GPGX(nextComm, game, null, new[] { disc }, GetCoreSettings<GPGX>(), GetCoreSyncSettings<GPGX>());
							nextEmulator = genesis;
							break;
						case "SAT":
							nextEmulator = new Saturnus(nextComm, new[] { disc }, Deterministic,
								(Saturnus.Settings)GetCoreSettings<Saturnus>(), (Saturnus.SyncSettings)GetCoreSyncSettings<Saturnus>());
							break;
						case "PSX":
							string romDetails;
							if (game.IsRomStatusBad() || game.Status == RomStatus.NotInDatabase)
							{
								romDetails = "Disc could not be identified as known-good. Look for a better rip.";
							}
							else
							{
								var sw = new StringWriter();
								sw.WriteLine("Disc was identified (99.99% confidently) as known good with disc id hash CRC32:{0:X8}", discHash);
								sw.WriteLine("Nonetheless it could be an unrecognized romhack or patched version.");
								sw.WriteLine("According to redump.org, the ideal hash for entire disc is: CRC32:{0:X8}", game.GetStringValue("dh"));
								sw.WriteLine("The file you loaded hasn't been hashed entirely (it would take too long)");
								sw.WriteLine("Compare it with the full hash calculated by the PSX menu's Hash Discs tool");
								romDetails = sw.ToString();
							}

							nextEmulator = new Octoshock(nextComm, new List<Disc>(new[] { disc }), new List<string>(new[] { Path.GetFileNameWithoutExtension(path) }), null, GetCoreSettings<Octoshock>(), GetCoreSyncSettings<Octoshock>(), romDetails);
							break;
						case "PCFX":
							nextEmulator = new Tst(nextComm, new[] { disc },
								(Tst.Settings)GetCoreSettings<Tst>(), (Tst.SyncSettings)GetCoreSyncSettings<Tst>());
							break;
						case "PCE": // TODO: this is clearly not used, its set to PCE by code above
						case "PCECD":
							string core = CoreNames.PceHawk;
							if (_config.PreferredCores.TryGetValue("PCECD", out string preferredCore))
							{
								core = preferredCore;
							}

							if (core == CoreNames.PceHawk)
							{
								nextEmulator = new PCEngine(nextComm, game, disc, GetCoreSettings<PCEngine>(), GetCoreSyncSettings<PCEngine>());
							}
							else
							{
								nextEmulator = new TerboGrafix(game, new[] { disc }, nextComm,
									(Emulation.Cores.Waterbox.NymaCore.NymaSettings)GetCoreSettings<TerboGrafix>(),
									(Emulation.Cores.Waterbox.NymaCore.NymaSyncSettings)GetCoreSyncSettings<TerboGrafix>(), Deterministic);
								// nextEmulator = new TerboGrafixSanic(game, new[] { disc }, nextComm,
								// 	(Emulation.Cores.Waterbox.NymaCore.NymaSettings)GetCoreSettings<TerboGrafixSanic>(),
								// 	(Emulation.Cores.Waterbox.NymaCore.NymaSyncSettings)GetCoreSyncSettings<TerboGrafixSanic>(), Deterministic);
							}
							
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
								if (_config.PreferredCores["GB"] == CoreNames.GbHawk)
								{
									nextEmulator = new GBHawkLink(
										nextComm,
										left,
										leftBytes,
										right,
										rightBytes,
										GetCoreSettings<GBHawkLink>(),
										GetCoreSyncSettings<GBHawkLink>());
								}
								else
								{
									nextEmulator = new GambatteLink(
										nextComm,
										left,
										leftBytes,
										right,
										rightBytes,
										GetCoreSettings<GambatteLink>(),
										GetCoreSyncSettings<GambatteLink>(),
										Deterministic);
								}
										
								// other stuff todo
								break;
							case "GB3x":
								var leftBytes3x = xmlGame.Assets.First().Value;
								var centerBytes3x = xmlGame.Assets.Skip(1).First().Value;
								var rightBytes3x = xmlGame.Assets.Skip(2).First().Value;

								var left3x = Database.GetGameInfo(leftBytes3x, "left.gb");
								var center3x = Database.GetGameInfo(centerBytes3x, "center.gb");
								var right3x = Database.GetGameInfo(rightBytes3x, "right.gb");

								nextEmulator = new GBHawkLink3x(
									nextComm,
									left3x,
									leftBytes3x,
									center3x,
									centerBytes3x,
									right3x,
									rightBytes3x,
									GetCoreSettings<GBHawkLink3x>(),
									GetCoreSyncSettings<GBHawkLink3x>());

								break;
							case "GB4x":
								var A_Bytes4x = xmlGame.Assets.First().Value;
								var B_Bytes4x = xmlGame.Assets.Skip(1).First().Value;
								var C_Bytes4x = xmlGame.Assets.Skip(2).First().Value;
								var D_Bytes4x = xmlGame.Assets.Skip(3).First().Value;

								var A_4x = Database.GetGameInfo(A_Bytes4x, "A.gb");
								var B_4x = Database.GetGameInfo(B_Bytes4x, "B.gb");
								var C_4x = Database.GetGameInfo(C_Bytes4x, "C.gb");
								var D_4x = Database.GetGameInfo(D_Bytes4x, "D.gb");

								nextEmulator = new GBHawkLink4x(
									nextComm,
									A_4x,
									A_Bytes4x,
									B_4x,
									B_Bytes4x,
									C_4x,
									C_Bytes4x,
									D_4x,
									D_Bytes4x,
									GetCoreSettings<GBHawkLink4x>(),
									GetCoreSyncSettings<GBHawkLink4x>());

								break;
							case "AppleII":
								var roms = xmlGame.Assets.Select(a => a.Value);
								nextEmulator = new AppleII(
									nextComm,
									roms,
									(AppleII.Settings)GetCoreSettings<AppleII>());
								break;
							case "C64":
								nextEmulator = new C64(
									nextComm,
									xmlGame.Assets.Select(a => a.Value),
									GameInfo.NullInstance,
									(C64.C64Settings)GetCoreSettings<C64>(),
									(C64.C64SyncSettings)GetCoreSyncSettings<C64>());
								break;
							case "ZXSpectrum":

								var zxGI = new List<GameInfo>();
								foreach (var a in xmlGame.Assets)
								{
									zxGI.Add(new GameInfo { Name = Path.GetFileNameWithoutExtension(a.Key) });
								}

								nextEmulator = new ZXSpectrum(
									nextComm,
									xmlGame.Assets.Select(a => a.Value),
									zxGI,
									(ZXSpectrum.ZXSpectrumSettings)GetCoreSettings<ZXSpectrum>(),
									(ZXSpectrum.ZXSpectrumSyncSettings)GetCoreSyncSettings<ZXSpectrum>(),
									Deterministic);
								break;
							case "AmstradCPC":

								var cpcGI = new List<GameInfo>();
								foreach (var a in xmlGame.Assets)
								{
									cpcGI.Add(new GameInfo { Name = Path.GetFileNameWithoutExtension(a.Key) });
								}

								nextEmulator = new AmstradCPC(
									nextComm,
									xmlGame.Assets.Select(a => a.Value),
									cpcGI,
									(AmstradCPC.AmstradCPCSettings)GetCoreSettings<AmstradCPC>(),
									(AmstradCPC.AmstradCPCSyncSettings)GetCoreSyncSettings<AmstradCPC>());
								break;
							case "PSX":
								var entries = xmlGame.AssetFullPaths;
								var discs = new List<Disc>();
								var discNames = new List<string>();
								var sw = new StringWriter();
								foreach (var e in entries)
								{
									var disc = DiscType.SonyPSX.Create(e, str => { DoLoadErrorCallback(str, "PSX", LoadErrorType.DiscError); });

									var discName = Path.GetFileNameWithoutExtension(e);
									discNames.Add(discName);
									discs.Add(disc);

									sw.WriteLine("{0}", Path.GetFileName(e));

									string discHash = new DiscHasher(disc).Calculate_PSX_BizIDHash().ToString("X8");
									game = Database.CheckDatabase(discHash);
									if (game == null || game.IsRomStatusBad() || game.Status == RomStatus.NotInDatabase)
									{
										sw.WriteLine("Disc could not be identified as known-good. Look for a better rip.");
									}
									else
									{
										sw.WriteLine("Disc was identified (99.99% confidently) as known good with disc id hash CRC32:{0:X8}", discHash);
										sw.WriteLine("Nonetheless it could be an unrecognized romhack or patched version.");
										sw.WriteLine("According to redump.org, the ideal hash for entire disc is: CRC32:{0:X8}", game.GetStringValue("dh"));
										sw.WriteLine("The file you loaded hasn't been hashed entirely (it would take too long)");
										sw.WriteLine("Compare it with the full hash calculated by the PSX menu's Hash Discs tool");
									}

									sw.WriteLine("-------------------------");
								}

								// todo: copy pasta from PSX .cue section
								nextEmulator = new Octoshock(nextComm, discs, discNames, null, GetCoreSettings<Octoshock>(), GetCoreSyncSettings<Octoshock>(), sw.ToString());
								game = new GameInfo
								{
									Name = Path.GetFileNameWithoutExtension(file.Name),
									System = "PSX"
								};
								break;
							case "SAT":
								var saturnDiscs = DiscsFromXml(xmlGame, "SAT", DiscType.SegaSaturn);
								if (!saturnDiscs.Any())
								{
									return false;
								}

								nextEmulator = new Saturnus(nextComm, saturnDiscs, Deterministic,
									(Saturnus.Settings)GetCoreSettings<Saturnus>(), (Saturnus.SyncSettings)GetCoreSyncSettings<Saturnus>());
								break;
							case "PCFX":
								var pcfxDiscs = DiscsFromXml(xmlGame, "PCFX", DiscType.PCFX);
								if (!pcfxDiscs.Any())
								{
									return false;
								}

								nextEmulator = new Tst(nextComm, pcfxDiscs,
									(Tst.Settings)GetCoreSettings<Tst>(), (Tst.SyncSettings)GetCoreSyncSettings<Tst>());
								break;
							case "GEN":
								var genDiscs = DiscsFromXml(xmlGame, "GEN", DiscType.MegaCD);
								var romBytes = xmlGame.Assets
									.Where(a => !Disc.IsValidExtension(a.Key))
									.Select(a => a.Value)
									.FirstOrDefault();
								if (!genDiscs.Any() && romBytes == null)
								{
									return false;
								}
								nextEmulator = new GPGX(nextComm, game, romBytes, genDiscs, GetCoreSettings<GPGX>(), GetCoreSyncSettings<GPGX>());
								break;
							case "Game Gear":
								var leftBytesGG = xmlGame.Assets.First().Value;
								var rightBytesGG = xmlGame.Assets.Skip(1).First().Value;

								var leftGG = Database.GetGameInfo(leftBytesGG, "left.gg");
								var rightGG = Database.GetGameInfo(rightBytesGG, "right.gg");

								nextEmulator = new GGHawkLink(
									nextComm,
									leftGG,
									leftBytesGG,
									rightGG,
									rightBytesGG,
									GetCoreSettings<GGHawkLink>(),
									GetCoreSyncSettings<GGHawkLink>());
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
							var basePath = Path.GetDirectoryName(path.Replace("|", "")); // Dirty hack to get around archive filenames (since we are just getting the directory path, it is safe to mangle the filename
							byte[] xmlData = rom.FileData;

							game = rom.GameInfo;
							game.System = "SNES";

							var snes = new LibsnesCore(game, null, xmlData, basePath, nextComm, GetCoreSettings<LibsnesCore>(), GetCoreSyncSettings<LibsnesCore>());
							nextEmulator = snes;
						}
						catch
						{
							DoLoadErrorCallback(ex.ToString(), "DGB", LoadErrorType.Xml);
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

					// total garbage, this
					rom = new RomGame(file);
					game = rom.GameInfo;
				}
				else
				{
					rom = new RomGame(file);

					// hacky for now
					if (file.Extension.ToLowerInvariant() == ".exe")
					{
						rom.GameInfo.System = "PSX";
					}
					else if (file.Extension.ToLowerInvariant() == ".nsf")
					{
						rom.GameInfo.System = "NES";
					}

					Console.WriteLine(rom.GameInfo.System);

					if (string.IsNullOrEmpty(rom.GameInfo.System))
					{
						// Has the user picked a preference for this extension?
						if (PreferredPlatformIsDefined(rom.Extension.ToLowerInvariant()))
						{
							rom.GameInfo.System = _config.PreferredPlatformsForExtensions[rom.Extension.ToLowerInvariant()];
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
							if (_config.PreferredCores.TryGetValue(game.System, out string coreName))
							{
								core = CoreInventory.Instance[game.System, coreName];
							}
							else
							{
								core = CoreInventory.Instance[game.System];
							}

							break;

						case null:
							// The user picked nothing in the Core picker
							break;
						case "83P":
							var ti83Bios = nextComm.CoreFileProvider.GetFirmware("TI83", "Rom", true);

							// TODO: make the ti-83 a proper firmware file
							var ti83BiosPath = _firmwareManager.Request(_config.PathEntries, _config.FirmwareUserSpecifications, "TI83", "Rom");
							using (var ti83AsHawkFile = new HawkFile(ti83BiosPath))
							{
								var ti83BiosAsRom = new RomGame(ti83AsHawkFile);
								var ti83 = new TI83(ti83BiosAsRom.GameInfo, ti83Bios, GetCoreSettings<TI83>());
								ti83.LinkPort.SendFileToCalc(File.OpenRead(path.Split('|').First()), false);
								nextEmulator = ti83;
							}

							break;
						case "SNES":
						{
							var name = _config.PreferredCores["SNES"];
							if (game.ForcedCore.ToLower() == "snes9x")
							{
								name = CoreNames.Snes9X;
							}
							else if (game.ForcedCore.ToLower() == "bsnes")
							{
								name = CoreNames.Bsnes;
							}
							
							try
							{
								core = CoreInventory.Instance["SNES", name];
							}
							catch // TODO: CoreInventory should support some sort of trygetvalue
							{
								// need to get rid of this hack at some point
								var basePath = Path.GetDirectoryName(path.Replace("|", "")); // Dirty hack to get around archive filenames (since we are just getting the directory path, it is safe to mangle the filename
								var romData = isXml ? null : rom.FileData;
								var xmlData = isXml ? rom.FileData : null;
								var snes = new LibsnesCore(game, romData, xmlData, basePath, nextComm, GetCoreSettings<LibsnesCore>(), GetCoreSyncSettings<LibsnesCore>());
								nextEmulator = snes;
							}
							break;
						}
						case "NES":
						{
							// apply main spur-of-the-moment switcheroo as lowest priority
							string preference = _config.PreferredCores["NES"];

							// if user has saw fit to override in gamedb, apply that
							if (!string.IsNullOrEmpty(game.ForcedCore))
							{
								preference = game.ForcedCore.ToLower() switch
								{
									"quicknes" => CoreNames.QuickNes,
									_ => CoreNames.NesHawk
								};
							}

							// but only neshawk is accurate
							if (forceAccurateCore)
							{
								preference = CoreNames.NesHawk;
							}

							core = CoreInventory.Instance["NES", preference];
						}
							break;

						case "GB":
						case "GBC":
							if (!_config.GbAsSgb)
							{
								core = CoreInventory.Instance["GB", _config.PreferredCores["GB"]];
							}
							else
							{
								if (_config.SgbUseBsnes)
								{
									game.System = "SNES";
									game.AddOption("SGB");
									var snes = new LibsnesCore(game, rom.FileData, null, null, nextComm, GetCoreSettings<LibsnesCore>(), GetCoreSyncSettings<LibsnesCore>());
									nextEmulator = snes;
								}
								else
								{
									core = CoreInventory.Instance["SGB", CoreNames.SameBoy];
								}
							}
							break;
						case "C64":
							var c64 = new C64(nextComm, Enumerable.Repeat(rom.FileData, 1), rom.GameInfo, GetCoreSettings<C64>(), GetCoreSyncSettings<C64>());
							nextEmulator = c64;
							break;
						case "ZXSpectrum":
							var zx = new ZXSpectrum(nextComm, 
								Enumerable.Repeat(rom.RomData, 1), 
								Enumerable.Repeat(rom.GameInfo, 1).ToList(), 
								GetCoreSettings<ZXSpectrum>(), 
								GetCoreSyncSettings<ZXSpectrum>(),
								Deterministic);
							nextEmulator = zx;
							break;
						case "ChannelF":
							nextEmulator = new ChannelF(nextComm, game, rom.FileData, GetCoreSettings<ChannelF>(), GetCoreSyncSettings<ChannelF>());
							break;
						case "AmstradCPC":
							var cpc = new AmstradCPC(nextComm, Enumerable.Repeat(rom.RomData, 1), Enumerable.Repeat(rom.GameInfo, 1).ToList(), GetCoreSettings<AmstradCPC>(), GetCoreSyncSettings<AmstradCPC>());
							nextEmulator = cpc;
							break;
						case "PSX":
							nextEmulator = new Octoshock(nextComm, null, null, rom.FileData, GetCoreSettings<Octoshock>(), GetCoreSyncSettings<Octoshock>(), "PSX etc.");
							break;
						case "Arcade":
							nextEmulator = new MAME(file.Directory, file.CanonicalName, GetCoreSyncSettings<MAME>(), out var gameName);
							rom.GameInfo.Name = gameName;
							break;
						case "GEN":
							if (game.ForcedCore?.ToLower() == "pico")
							{
								core = CoreInventory.Instance["GEN", CoreNames.PicoDrive];
							}
							else
							{
								core = CoreInventory.Instance["GEN", CoreNames.Gpgx];
							}

							break;
						case "32X":
							core = CoreInventory.Instance["GEN", CoreNames.PicoDrive];
							break;
					}

					if (core != null)
					{
						// use CoreInventory
						nextEmulator = core.Create(
							nextComm, game, rom.RomData, rom.FileData, Deterministic,
							GetCoreSettings(core.Type), GetCoreSyncSettings(core.Type), rom.Extension);
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
				{
					ex = ex.InnerException;
				}

				// Specific hack here, as we get more cores of the same system, this isn't scalable
				if (ex is UnsupportedGameException)
				{
					if (system == "NES")
					{
						DoMessageCallback("Unable to use quicknes, using NESHawk instead");
					}

					return LoadRom(path, nextComm, launchLibretroCore, true, recursiveCount + 1);
				}

				if (ex is MissingFirmwareException)
				{
					DoLoadErrorCallback(ex.Message, system, path, Deterministic, LoadErrorType.MissingFirmware);
				}
				else if (ex is CGBNotSupportedException)
				{
					// failed to load SGB bios or game does not support SGB mode. 
					// To avoid catch-22, disable SGB mode
					_config.GbAsSgb = false;
					DoMessageCallback("Failed to load a GB rom in SGB mode.  Disabling SGB Mode.");
					return LoadRom(path, nextComm, launchLibretroCore, false, recursiveCount + 1);
				}

				// handle exceptions thrown by the new detected systems that BizHawk does not have cores for
				else if (ex is NoAvailableCoreException)
				{
					DoLoadErrorCallback($"{ex.Message}\n\n{ex}", system);
				}

				else
				{
					DoLoadErrorCallback($"A core accepted the rom, but threw an exception while loading it:\n\n{ex}", system);
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
