using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
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
using BizHawk.Emulation.Cores.Waterbox;

using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

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
			=> xmlGame.AssetFullPaths.Where(path => Disc.IsValidExtension(Path.GetExtension(path)))
				.Select(path => diskType.Create(path, str => DoLoadErrorCallback(str, systemId, LoadErrorType.DiscError)))
				.Where(disc => disc != null)
				.ToList();

		private bool LoadDisc(string path, CoreComm nextComm, HawkFile file, string ext, out IEmulator nextEmulator, out GameInfo game)
		{
			//--- load the disc in a context which will let us abort if it's going to take too long
			var discMountJob = new DiscMountJob { IN_FromPath = path, IN_SlowLoadAbortThreshold = 8 };
			discMountJob.Run();

			if (discMountJob.OUT_SlowLoadAborted)
			{
				DoLoadErrorCallback("This disc would take too long to load. Run it through DiscoHawk first, or find a new rip because this one is probably junk", "", LoadErrorType.DiscError);
				nextEmulator = null;
				game = null;
				return false;
			}
			if (discMountJob.OUT_ErrorLevel) throw new InvalidOperationException($"\r\n{discMountJob.OUT_Log}");

			var disc = discMountJob.OUT_Disc;

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
				case "GEN":
					nextEmulator = new GPGX(
						nextComm,
						game,
						null,
						new[] { disc },
						GetCoreSettings<GPGX>(),
						GetCoreSyncSettings<GPGX>()
					);
					break;
				case "SAT":
					nextEmulator = new Saturnus(
						nextComm,
						new[] { disc },
						Deterministic,
						(Saturnus.Settings) GetCoreSettings<Saturnus>(),
						(Saturnus.SyncSettings) GetCoreSyncSettings<Saturnus>()
					);
					break;
				case "PSX":
					nextEmulator = new Octoshock(
						nextComm,
						new List<Disc> { disc },
						new List<string> { Path.GetFileNameWithoutExtension(path) },
						null,
						GetCoreSettings<Octoshock>(),
						GetCoreSyncSettings<Octoshock>(),
						DiscHashWarningText(game, discHash)
					);
					break;
				case "PCFX":
					nextEmulator = new Tst(
						nextComm,
						new[] { disc },
						(Tst.Settings) GetCoreSettings<Tst>(),
						(Tst.SyncSettings) GetCoreSyncSettings<Tst>()
					);
					break;
				case "PCE": // TODO: this is clearly not used, its set to PCE by code above
				case "PCECD":
					var core = _config.PreferredCores.TryGetValue("PCECD", out var preferredCore) ? preferredCore : CoreNames.PceHawk;
					nextEmulator = core switch
					{
						CoreNames.PceHawk => new PCEngine(
							nextComm,
							game,
							disc,
							GetCoreSettings<PCEngine>(),
							GetCoreSyncSettings<PCEngine>()
						),
//						CoreNames.TurboTurboNyma => new TerboGrafixSanic(
//							game,
//							new[] { disc },
//							nextComm,
//							(NymaCore.NymaSettings) GetCoreSettings<TerboGrafixSanic>(),
//							(NymaCore.NymaSyncSettings) GetCoreSyncSettings<TerboGrafixSanic>(),
//							Deterministic
//						),
						_ => new TerboGrafix(
							game,
							new[] { disc },
							nextComm,
							(NymaCore.NymaSettings) GetCoreSettings<TerboGrafix>(),
							(NymaCore.NymaSyncSettings) GetCoreSyncSettings<TerboGrafix>(),
							Deterministic
						)
					};
					break;
				default:
					nextEmulator = null;
					break;
			}
			return true;
		}

		private void LoadM3U(string path, CoreComm nextComm, HawkFile file, out IEmulator nextEmulator, out GameInfo game)
		{
			// HACK ZONE - currently only psx supports m3u
			using var sr = new StreamReader(path);
			var m3u = M3U_File.Read(sr);
			if (m3u.Entries.Count == 0) throw new InvalidOperationException("Can't load an empty M3U");

			// load discs for all the m3u
			m3u.Rebase(Path.GetDirectoryName(path));
			var discs = new List<Disc>();
			var discNames = new List<string>();
			var swRomDetails = new StringWriter();
			foreach (var e in m3u.Entries)
			{
				var disc = DiscType.SonyPSX.Create(e.Path, str => DoLoadErrorCallback(str, "PSX", LoadErrorType.DiscError));
				discs.Add(disc);
				discNames.Add(Path.GetFileNameWithoutExtension(e.Path));
				var discHash = new DiscHasher(disc).Calculate_PSX_BizIDHash().ToString("X8");
				swRomDetails.WriteLine(Path.GetFileName(e.Path));
				swRomDetails.WriteLine(DiscHashWarningText(Database.CheckDatabase(discHash), discHash));
				swRomDetails.WriteLine("-------------------------");
			}

			game = new GameInfo
			{
				Name = Path.GetFileNameWithoutExtension(file.Name),
				System = "PSX"
			};
			nextEmulator = new Octoshock(
				nextComm,
				discs,
				discNames,
				null,
				GetCoreSettings<Octoshock>(),
				GetCoreSyncSettings<Octoshock>(),
				swRomDetails.ToString()
			);
		}

		private void LoadOther(string path, CoreComm nextComm, bool forceAccurateCore, HawkFile file, out IEmulator nextEmulator, out RomGame rom, out GameInfo game, out bool cancel)
		{
			cancel = false;
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

			Debug.WriteLine(rom.GameInfo.System);

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
			if (file.Extension.ToLowerInvariant() == ".xml")
			{
				game.System = "SNES"; // other xml has already been handled
				isXml = true;
			}

			nextEmulator = null;
			if (game.System == null) return; // The user picked nothing in the Core picker

			CoreInventory.Core core;
			switch (game.System)
			{
				case "83P":
					var ti83Bios = nextComm.CoreFileProvider.GetFirmware("TI83", "Rom", true);
					var ti83BiosPath = _firmwareManager.Request(_config.PathEntries, _config.FirmwareUserSpecifications, "TI83", "Rom"); // TODO: make the ti-83 a proper firmware file
					using (var ti83AsHawkFile = new HawkFile(ti83BiosPath))
					{
						var ti83BiosAsRom = new RomGame(ti83AsHawkFile);
						var ti83 = new TI83(
							ti83BiosAsRom.GameInfo,
							ti83Bios,
							GetCoreSettings<TI83>()
						);
						ti83.LinkPort.SendFileToCalc(File.OpenRead(path.SubstringBefore('|')), false);
						nextEmulator = ti83;
					}
					return;
				case "SNES":
					var name = game.ForcedCore?.ToLower() switch
					{
						"snes9x" => CoreNames.Snes9X,
						"bsnes" => CoreNames.Bsnes,
						_ => _config.PreferredCores["SNES"]
					};
					try
					{
						core = CoreInventory.Instance["SNES", name];
					}
					catch // TODO: CoreInventory should support some sort of trygetvalue
					{
						// need to get rid of this hack at some point
						nextEmulator = new LibsnesCore(
							game,
							isXml ? null : rom.FileData,
							isXml ? rom.FileData : null,
							Path.GetDirectoryName(path.SubstringBefore('|')),
							nextComm,
							GetCoreSettings<LibsnesCore>(),
							GetCoreSyncSettings<LibsnesCore>()
						);
						return;
					}
					break;
				case "NES":
					// apply main spur-of-the-moment switcheroo as lowest priority
					var preference = _config.PreferredCores["NES"];

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
					break;
				case "GB":
				case "GBC":
					if (_config.GbAsSgb)
					{
						if (_config.SgbUseBsnes)
						{
							game.System = "SNES";
							game.AddOption("SGB");
							nextEmulator = new LibsnesCore(
								game,
								rom.FileData,
								null,
								null,
								nextComm,
								GetCoreSettings<LibsnesCore>(),
								GetCoreSyncSettings<LibsnesCore>()
							);
							return;
						}
						core = CoreInventory.Instance["SGB", CoreNames.SameBoy];
					}
					else
					{
						core = CoreInventory.Instance["GB", _config.PreferredCores["GB"]];
					}
					break;
				case "C64":
					nextEmulator = new C64(
						nextComm,
						new[] { rom.FileData },
						rom.GameInfo,
						GetCoreSettings<C64>(),
						GetCoreSyncSettings<C64>()
					);
					return;
				case "ZXSpectrum":
					nextEmulator = new ZXSpectrum(
						nextComm,
						new[] { rom.RomData },
						new List<GameInfo> { rom.GameInfo },
						GetCoreSettings<ZXSpectrum>(),
						GetCoreSyncSettings<ZXSpectrum>(),
						Deterministic
					);
					return;
				case "ChannelF":
					nextEmulator = new ChannelF(
						nextComm,
						game,
						rom.FileData,
						GetCoreSettings<ChannelF>(),
						GetCoreSyncSettings<ChannelF>()
					);
					return;
				case "AmstradCPC":
					nextEmulator = new AmstradCPC(
						nextComm,
						new[] { rom.RomData },
						new List<GameInfo> { rom.GameInfo },
						GetCoreSettings<AmstradCPC>(),
						GetCoreSyncSettings<AmstradCPC>()
					);
					return;
				case "PSX":
					nextEmulator = new Octoshock(
						nextComm,
						null,
						null,
						rom.FileData,
						GetCoreSettings<Octoshock>(),
						GetCoreSyncSettings<Octoshock>(),
						"PSX etc."
					);
					return;
				case "Arcade":
					nextEmulator = new MAME(
						file.Directory,
						file.CanonicalName,
						GetCoreSyncSettings<MAME>(),
						out var gameName
					);
					rom.GameInfo.Name = gameName;
					return;
				case "GEN":
					core = CoreInventory.Instance["GEN", game.ForcedCore?.ToLower() == "pico" ? CoreNames.PicoDrive : CoreNames.Gpgx];
					break;
				case "32X":
					core = CoreInventory.Instance["GEN", CoreNames.PicoDrive];
					break;
				default:
					core = _config.PreferredCores.TryGetValue(game.System, out var coreName)
						? CoreInventory.Instance[game.System, coreName]
						: CoreInventory.Instance[game.System];
					break;
			}

			nextEmulator = core.Create(
				nextComm,
				game,
				rom.RomData,
				rom.FileData,
				Deterministic,
				GetCoreSettings(core.Type),
				GetCoreSyncSettings(core.Type),
				rom.Extension
			);
		}

		private void LoadPSF(string path, CoreComm nextComm, HawkFile file, out IEmulator nextEmulator, out RomGame rom, out GameInfo game)
		{
			static byte[] CbDeflater(Stream instream, int size)
			{
				var ret = new MemoryStream();
				new InflaterInputStream(instream, new Inflater(false)).CopyTo(ret);
				return ret.ToArray();
			}
			var psf = new PSF();
			psf.Load(path, CbDeflater);
			nextEmulator = new Octoshock(
				nextComm,
				psf,
				GetCoreSettings<Octoshock>(),
				GetCoreSyncSettings<Octoshock>()
			);

			// total garbage, this
			rom = new RomGame(file);
			game = rom.GameInfo;
		}

		private bool LoadXML(string path, CoreComm nextComm, HawkFile file, out IEmulator nextEmulator, out RomGame rom, out GameInfo game)
		{
			nextEmulator = null;
			rom = null;
			game = null;
			try
			{
				var xmlGame = XmlGame.Create(file); // if load fails, are we supposed to retry as a bsnes XML????????
				game = xmlGame.GI;

				switch (game.System)
				{
					case "GB":
					case "DGB":
						// adelikat: remove need for tags to be hardcoded to left and right, we should clean this up, also maybe the DGB core should just take the xml file and handle it itself
						var leftBytes = xmlGame.Assets[0].Value;
						var rightBytes = xmlGame.Assets[1].Value;
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
								GetCoreSyncSettings<GBHawkLink>()
							);
							// other stuff todo
							return true;
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
								Deterministic
							);
							// other stuff todo
							return true;
						}
					case "GB3x":
						var leftBytes3x = xmlGame.Assets[0].Value;
						var centerBytes3x = xmlGame.Assets[1].Value;
						var rightBytes3x = xmlGame.Assets[2].Value;
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
							GetCoreSyncSettings<GBHawkLink3x>()
						);
						return true;
					case "GB4x":
						var A_Bytes4x = xmlGame.Assets[0].Value;
						var B_Bytes4x = xmlGame.Assets[1].Value;
						var C_Bytes4x = xmlGame.Assets[2].Value;
						var D_Bytes4x = xmlGame.Assets[3].Value;
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
							GetCoreSyncSettings<GBHawkLink4x>()
						);
						return true;
					case "AppleII":
						nextEmulator = new AppleII(
							nextComm,
							xmlGame.Assets.Select(a => a.Value),
							(AppleII.Settings) GetCoreSettings<AppleII>()
						);
						return true;
					case "C64":
						nextEmulator = new C64(
							nextComm,
							xmlGame.Assets.Select(a => a.Value),
							GameInfo.NullInstance,
							(C64.C64Settings) GetCoreSettings<C64>(),
							(C64.C64SyncSettings) GetCoreSyncSettings<C64>()
						);
						return true;
					case "ZXSpectrum":
						nextEmulator = new ZXSpectrum(
							nextComm,
							xmlGame.Assets.Select(kvp => kvp.Value),
							xmlGame.Assets.Select(kvp => new GameInfo { Name = Path.GetFileNameWithoutExtension(kvp.Key) }).ToList(),
							(ZXSpectrum.ZXSpectrumSettings) GetCoreSettings<ZXSpectrum>(),
							(ZXSpectrum.ZXSpectrumSyncSettings) GetCoreSyncSettings<ZXSpectrum>(),
							Deterministic
						);
						return true;
					case "AmstradCPC":
						nextEmulator = new AmstradCPC(
							nextComm,
							xmlGame.Assets.Select(kvp => kvp.Value),
							xmlGame.Assets.Select(kvp => new GameInfo { Name = Path.GetFileNameWithoutExtension(kvp.Key) }).ToList(),
							(AmstradCPC.AmstradCPCSettings) GetCoreSettings<AmstradCPC>(),
							(AmstradCPC.AmstradCPCSyncSettings) GetCoreSyncSettings<AmstradCPC>()
						);
						return true;
					case "PSX":
						var entries = xmlGame.AssetFullPaths;
						var discs = new List<Disc>();
						var discNames = new List<string>();
						var swRomDetails = new StringWriter();
						foreach (var e in entries)
						{
							var disc = DiscType.SonyPSX.Create(e, str => DoLoadErrorCallback(str, "PSX", LoadErrorType.DiscError));
							discs.Add(disc);
							discNames.Add(Path.GetFileNameWithoutExtension(e));
							var discHash = new DiscHasher(disc).Calculate_PSX_BizIDHash().ToString("X8");
							swRomDetails.WriteLine(Path.GetFileName(e));
							swRomDetails.WriteLine(DiscHashWarningText(Database.CheckDatabase(discHash), discHash));
							swRomDetails.WriteLine("-------------------------");
						}
						// todo: copy pasta from PSX .cue section
						game = new GameInfo
						{
							Name = Path.GetFileNameWithoutExtension(file.Name),
							System = "PSX"
						};
						nextEmulator = new Octoshock(
							nextComm,
							discs,
							discNames,
							null,
							GetCoreSettings<Octoshock>(),
							GetCoreSyncSettings<Octoshock>(),
							swRomDetails.ToString()
						);
						return true;
					case "SAT":
						var saturnDiscs = DiscsFromXml(xmlGame, "SAT", DiscType.SegaSaturn);
						if (saturnDiscs.Count == 0) return false;
						nextEmulator = new Saturnus(
							nextComm,
							saturnDiscs,
							Deterministic,
							(Saturnus.Settings) GetCoreSettings<Saturnus>(),
							(Saturnus.SyncSettings) GetCoreSyncSettings<Saturnus>()
						);
						return true;
					case "PCFX":
						var pcfxDiscs = DiscsFromXml(xmlGame, "PCFX", DiscType.PCFX);
						if (pcfxDiscs.Count == 0) return false;
						nextEmulator = new Tst(
							nextComm,
							pcfxDiscs,
							(Tst.Settings) GetCoreSettings<Tst>(),
							(Tst.SyncSettings) GetCoreSyncSettings<Tst>()
						);
						return true;
					case "GEN":
						var genDiscs = DiscsFromXml(xmlGame, "GEN", DiscType.MegaCD);
						var romBytes = xmlGame.Assets.FirstOrDefault(kvp => !Disc.IsValidExtension(kvp.Key)).Value;
						if (genDiscs.Count == 0 && romBytes == null) return false;
						nextEmulator = new GPGX(
							nextComm,
							game,
							romBytes,
							genDiscs,
							GetCoreSettings<GPGX>(),
							GetCoreSyncSettings<GPGX>()
						);
						return true;
					case "Game Gear":
						var leftBytesGG = xmlGame.Assets[0].Value;
						var rightBytesGG = xmlGame.Assets[1].Value;
						var leftGG = Database.GetGameInfo(leftBytesGG, "left.gg");
						var rightGG = Database.GetGameInfo(rightBytesGG, "right.gg");
						nextEmulator = new GGHawkLink(
							nextComm,
							leftGG,
							leftBytesGG,
							rightGG,
							rightBytesGG,
							GetCoreSettings<GGHawkLink>(),
							GetCoreSyncSettings<GGHawkLink>()
						);
						return true;
				}
				return false;
			}
			catch (Exception ex)
			{
				try
				{
					// need to get rid of this hack at some point
					rom = new RomGame(file);
					game = rom.GameInfo;
					game.System = "SNES";
					nextEmulator = new LibsnesCore(
						game,
						null,
						rom.FileData,
						Path.GetDirectoryName(path.SubstringBefore('|')),
						nextComm,
						GetCoreSettings<LibsnesCore>(),
						GetCoreSyncSettings<LibsnesCore>()
					);
					return true;
				}
				catch
				{
					DoLoadErrorCallback(ex.ToString(), "DGB", LoadErrorType.Xml);
					return false;
				}
			}
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

			IEmulator nextEmulator;
			RomGame rom = null;
			GameInfo game = null;

			try
			{
				var cancel = false;

				if (OpenAdvanced is OpenAdvanced_Libretro)
				{
					// must be done before LoadNoGame (which triggers retro_init and the paths to be consumed by the core)
					// game name == name of core
					Game = game = new GameInfo { Name = Path.GetFileNameWithoutExtension(launchLibretroCore), System = "Libretro" };
					var retro = new LibretroCore(nextComm, game, launchLibretroCore);
					nextEmulator = retro;

					if (retro.Description.SupportsNoGame && string.IsNullOrEmpty(path))
					{
						// if we are allowed to run NoGame and we don't have a game, boot up the core that way
						if (!retro.LoadNoGame())
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

							ret = retro.Description.NeedsRomAsPath
								? retro.LoadPath(file.FullPathWithoutMember)
								: HandleArchiveBinding(file) && retro.LoadData(file.ReadAllBytes(), file.Name);
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
					// do the archive binding we had to skip
					if (!HandleArchiveBinding(file))
					{
						return false;
					}

					// not libretro: do extension checking
					var ext = file.Extension.ToLowerInvariant();
					switch (ext)
					{
						case ".m3u":
							LoadM3U(path, nextComm, file, out nextEmulator, out game);
							break;
						case ".xml":
							if (!LoadXML(path, nextComm, file, out nextEmulator, out rom, out game)) return false;
							break;
						case ".psf":
						case ".minipsf":
							LoadPSF(path, nextComm, file, out nextEmulator, out rom, out game);
							break;
						default:
							if (Disc.IsValidExtension(ext))
							{
								if (file.IsArchive) throw new InvalidOperationException("Can't load CD files from archives!");
								if (!LoadDisc(path, nextComm, file, ext, out nextEmulator, out game)) return false;
							}
							else
							{
								LoadOther(path, nextComm, forceAccurateCore, file, out nextEmulator, out rom, out game, out cancel); // must be called after LoadXML because of SNES hacks
							}
							break;
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
				var system = game?.System;

				// all of the specific exceptions we're trying to catch here aren't expected to have inner exceptions,
				// so drill down in case we got a TargetInvocationException or something like that
				while (ex.InnerException != null) ex = ex.InnerException;

				// Specific hack here, as we get more cores of the same system, this isn't scalable
				if (ex is UnsupportedGameException)
				{
					if (system == "NES")
					{
						DoMessageCallback("Unable to use quicknes, using NESHawk instead");
					}

					return LoadRom(path, nextComm, launchLibretroCore, true, recursiveCount + 1);
				}
				else if (ex is MissingFirmwareException)
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
				else if (ex is NoAvailableCoreException)
				{
					// handle exceptions thrown by the new detected systems that BizHawk does not have cores for
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

		private static string DiscHashWarningText(GameInfo game, string discHash)
			=> game == null || game.IsRomStatusBad() || game.Status == RomStatus.NotInDatabase
				? "Disc could not be identified as known-good. Look for a better rip."
				: $"Disc was identified (99.99% confidently) as known good with disc id hash CRC32:{discHash}\nNonetheless it could be an unrecognized romhack or patched version.\nAccording to redump.org, the ideal hash for entire disc is: CRC32:{game.GetStringValue("dh")}\nThe file you loaded hasn't been hashed entirely (it would take too long)\nCompare it with the full hash calculated by the PSX menu's Hash Discs tool";
	}
}
