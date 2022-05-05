using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Libretro;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.Emulation.Cores.Arcades.MAME;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Client.Common
{
	public class RomLoader
	{
		private class DiscAsset : IDiscAsset
		{
			public Disc DiscData { get; set; }
			public DiscType DiscType { get; set; }
			public string DiscName { get; set; }
		}
		private class RomAsset : IRomAsset
		{
			public byte[] RomData { get; set; }
			public byte[] FileData { get; set; }
			public string Extension { get; set; }
			public string RomPath { get; set; }
			public GameInfo Game { get; set; }
		}
		private class CoreInventoryParameters : ICoreInventoryParameters
		{
			private readonly RomLoader _parent;
			public CoreInventoryParameters(RomLoader parent)
			{
				_parent = parent;
			}
			public CoreComm Comm { get; set; }

			public GameInfo Game { get; set; }

			public List<IRomAsset> Roms { get; set; } = new List<IRomAsset>();

			public List<IDiscAsset> Discs { get; set; } = new List<IDiscAsset>();

			public bool DeterministicEmulationRequested => _parent.Deterministic;

			public object FetchSettings(Type emulatorType, Type settingsType)
				=> _parent.GetCoreSettings(emulatorType, settingsType);

			public object FetchSyncSettings(Type emulatorType, Type syncSettingsType)
				=> _parent.GetCoreSyncSettings(emulatorType, syncSettingsType);
		}
		private readonly Config _config;

		public RomLoader(Config config)
		{
			_config = config;
		}

		public enum LoadErrorType
		{
			Unknown, MissingFirmware, Xml, DiscError
		}

		// helper methods for the settings events
		private TSetting GetCoreSettings<TCore, TSetting>()
			where TCore : IEmulator
		{
			return (TSetting)GetCoreSettings(typeof(TCore), typeof(TSetting));
		}

		private TSync GetCoreSyncSettings<TCore, TSync>()
			where TCore : IEmulator
		{
			return (TSync)GetCoreSyncSettings(typeof(TCore), typeof(TSync));
		}

		private object GetCoreSettings(Type t, Type settingsType)
		{
			var e = new SettingsLoadArgs(t, settingsType);
			if (OnLoadSettings == null)
				throw new InvalidOperationException("Frontend failed to provide a settings getter");
			OnLoadSettings(this, e);
			if (e.Settings != null && e.Settings.GetType() != settingsType)
				throw new InvalidOperationException($"Frontend did not provide the requested settings type: Expected {settingsType}, got {e.Settings.GetType()}");
			return e.Settings;
		}

		private object GetCoreSyncSettings(Type t, Type syncSettingsType)
		{
			var e = new SettingsLoadArgs(t, syncSettingsType);
			if (OnLoadSyncSettings == null)
				throw new InvalidOperationException("Frontend failed to provide a sync settings getter");
			OnLoadSyncSettings(this, e);
			if (e.Settings != null && e.Settings.GetType() != syncSettingsType)
				throw new InvalidOperationException($"Frontend did not provide the requested sync settings type: Expected {syncSettingsType}, got {e.Settings.GetType()}");
			return e.Settings;
		}

		// For not throwing errors but simply outputting information to the screen
		public Action<string> MessageCallback { get; set; }

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
			public Type SettingsType { get; }
			public SettingsLoadArgs(Type t, Type s)
			{
				Core = t;
				SettingsType = s;
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

		public IOpenAdvanced OpenAdvanced { get; set; }

		private bool HandleArchiveBinding(HawkFile file)
		{
			// try binding normal rom extensions first
			if (!file.IsBound)
			{
				file.BindSoleItemOf(RomFileExtensions.AutoloadFromArchive);
			}
			// ...including unrecognised extensions that the user has set a platform for
			if (!file.IsBound)
			{
				var exts = _config.PreferredPlatformsForExtensions.Where(static kvp => !string.IsNullOrEmpty(kvp.Value))
					.Select(static kvp => kvp.Key)
					.ToList();
				if (exts.Count is not 0) file.BindSoleItemOf(exts);
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

		private GameInfo MakeGameFromDisc(Disc disc, string ext, string name)
		{
			// TODO - use more sophisticated IDer
			var discType = new DiscIdentifier(disc).DetectDiscType();
			var discHasher = new DiscHasher(disc);
			var discHash = discType == DiscType.SonyPSX
				? discHasher.Calculate_PSX_BizIDHash()
				: discHasher.OldHash();

			var game = Database.CheckDatabase(discHash);
			if (game == null)
			{
				// try to use our wizard methods
				game = new GameInfo { Name = name, Hash = discHash };

				switch (discType)
				{
					case DiscType.SegaSaturn:
						game.System = VSystemID.Raw.SAT;
						break;
					case DiscType.SonyPSP:
						game.System = "PSP";
						break;
					case DiscType.SonyPS2:
						game.System = VSystemID.Raw.PS2;
						break;
					case DiscType.MegaCD:
						game.System = VSystemID.Raw.GEN;
						break;
					case DiscType.PCFX:
						game.System = VSystemID.Raw.PCFX;
						break;

					case DiscType.TurboGECD:
					case DiscType.TurboCD:
						game.System = VSystemID.Raw.PCE;
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
						game.System = _config.TryGetChosenSystemForFileExt(ext, out var sysID) ? sysID : VSystemID.Raw.NULL;
						break;

					default: //"for an unknown disc, default to psx instead of pce-cd, since that is far more likely to be what they are attempting to open" [5e07ab3ec3b8b8de9eae71b489b55d23a3909f55, year 2015]
					case DiscType.SonyPSX:
						game.System = VSystemID.Raw.PSX;
						break;
				}
			}
			return game;
		}

		private bool LoadDisc(string path, CoreComm nextComm, HawkFile file, string ext, string forcedCoreName, out IEmulator nextEmulator, out GameInfo game)
		{
			var disc = DiscExtensions.CreateAnyType(path, str => DoLoadErrorCallback(str, "???", LoadErrorType.DiscError));
			if (disc == null)
			{
				game = null;
				nextEmulator = null;
				return false;
			}

			game = MakeGameFromDisc(disc, ext, Path.GetFileNameWithoutExtension(file.Name));

			var cip = new CoreInventoryParameters(this)
			{
				Comm = nextComm,
				Game = game,
				Discs =
					{
						new DiscAsset
						{
							DiscData = disc,
							DiscType = new DiscIdentifier(disc).DetectDiscType(),
							DiscName = Path.GetFileNameWithoutExtension(path)
						}
					},
			};
			nextEmulator = MakeCoreFromCoreInventory(cip, forcedCoreName);
			return true;
		}

		private void LoadM3U(string path, CoreComm nextComm, HawkFile file, string forcedCoreName, out IEmulator nextEmulator, out GameInfo game)
		{
			M3U_File m3u;
			using (var sr = new StreamReader(path))
				m3u = M3U_File.Read(sr);
			if (m3u.Entries.Count == 0)
				throw new InvalidOperationException("Can't load an empty M3U");
			m3u.Rebase(Path.GetDirectoryName(path));

			var discs = m3u.Entries
				.Select(e => e.Path)
				.Where(p => Disc.IsValidExtension(Path.GetExtension(p)))
				.Select(path => new
				{
					d = DiscExtensions.CreateAnyType(path, str => DoLoadErrorCallback(str, "???", LoadErrorType.DiscError)),
					p = path,
				})
				.Where(a => a.d != null)
				.Select(a => (IDiscAsset)new DiscAsset
				{
					DiscData = a.d,
					DiscType = new DiscIdentifier(a.d).DetectDiscType(),
					DiscName = Path.GetFileNameWithoutExtension(a.p)
				})
				.ToList();
			if (m3u.Entries.Count == 0)
				throw new InvalidOperationException("Couldn't load any contents of the M3U as discs");

			game = MakeGameFromDisc(discs[0].DiscData, Path.GetExtension(m3u.Entries[0].Path), discs[0].DiscName);
			var cip = new CoreInventoryParameters(this)
			{
				Comm = nextComm,
				Game = game,
				Discs = discs
			};
			nextEmulator = MakeCoreFromCoreInventory(cip, forcedCoreName);
		}

		private IEmulator MakeCoreFromCoreInventory(CoreInventoryParameters cip, string forcedCoreName = null)
		{
			IReadOnlyCollection<CoreInventory.Core> cores;
			if (forcedCoreName != null)
			{
				var singleCore = CoreInventory.Instance.GetCores(cip.Game.System).SingleOrDefault(c => c.Name == forcedCoreName);
				cores = singleCore != null ? new[] { singleCore } : Array.Empty<CoreInventory.Core>();
			}
			else
			{
				_config.PreferredCores.TryGetValue(cip.Game.System, out var preferredCore);
				var dbForcedCoreName = cip.Game.ForcedCore;
				cores = CoreInventory.Instance.GetCores(cip.Game.System)
					.OrderBy(c =>
					{
						if (c.Name == preferredCore)
						{
							return (int)CorePriority.UserPreference;
						}

						if (string.Equals(c.Name, dbForcedCoreName, StringComparison.InvariantCultureIgnoreCase))
						{
							return (int)CorePriority.GameDbPreference;
						}

						return (int)c.Priority;
					})
					.ToList();
				if (cores.Count == 0) throw new InvalidOperationException("No core was found to try on the game");
			}
			var exceptions = new List<Exception>();
			foreach (var core in cores)
			{
				try
				{
					return core.Create(cip);
				}
				catch (Exception e)
				{
					if (_config.DontTryOtherCores || e is MissingFirmwareException || e.InnerException is MissingFirmwareException)
						throw;
					exceptions.Add(e);
				}
			}
			throw new AggregateException("No core could load the game", exceptions);
		}

		private void LoadOther(CoreComm nextComm, HawkFile file, string forcedCoreName, out IEmulator nextEmulator, out RomGame rom, out GameInfo game, out bool cancel)
		{
			cancel = false;
			rom = new RomGame(file);

			// hacky for now
			if (file.Extension == ".exe")
			{
				rom.GameInfo.System = VSystemID.Raw.PSX;
			}
			else if (file.Extension == ".nsf")
			{
				rom.GameInfo.System = VSystemID.Raw.NES;
			}

			Util.DebugWriteLine(rom.GameInfo.System);

			if (string.IsNullOrEmpty(rom.GameInfo.System))
			{
				// Has the user picked a preference for this extension?
				if (_config.TryGetChosenSystemForFileExt(rom.Extension.ToLowerInvariant(), out var systemID))
				{
					rom.GameInfo.System = systemID;
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

			nextEmulator = null;
			if (game.System == null)
				return; // The user picked nothing in the Core picker

			switch (game.System)
			{
				case VSystemID.Raw.GB:
				case VSystemID.Raw.GBC:
					if (_config.GbAsSgb)
					{
						game.System = VSystemID.Raw.SGB;
					}
					break;
				case VSystemID.Raw.MAME:
					nextEmulator = new MAME(
						file.Directory,
						file.CanonicalName,
						GetCoreSyncSettings<MAME, MAME.MAMESyncSettings>(),
						out var gameName
					);
					rom.GameInfo.Name = gameName;
					return;
			}
			var cip = new CoreInventoryParameters(this)
			{
				Comm = nextComm,
				Game = game,
				Roms =
				{
					new RomAsset
					{
						RomData = rom.RomData,
						FileData = rom.FileData,
						Extension = rom.Extension,
						RomPath = file.FullPathWithoutMember,
						Game = game
					}
				},
			};
			nextEmulator = MakeCoreFromCoreInventory(cip, forcedCoreName);
		}

		private void LoadPSF(string path, CoreComm nextComm, HawkFile file, out IEmulator nextEmulator, out RomGame rom, out GameInfo game)
		{
			// TODO: Why does the PSF loader need CbDeflater provided?  Surely this is a matter internal to it.
			static byte[] CbDeflater(Stream instream, int size)
			{
				var ret = new MemoryStream();
				new GZipStream(instream, CompressionMode.Decompress).CopyTo(ret);
				return ret.ToArray();
			}
			var psf = new PSF();
			psf.Load(path, CbDeflater);
			nextEmulator = new Octoshock(
				nextComm,
				psf,
				GetCoreSettings<Octoshock, Octoshock.Settings>(),
				GetCoreSyncSettings<Octoshock, Octoshock.SyncSettings>()
			);

			// total garbage, this
			rom = new RomGame(file);
			game = rom.GameInfo;
		}

		private bool LoadXML(string path, CoreComm nextComm, HawkFile file, string forcedCoreName, out IEmulator nextEmulator, out RomGame rom, out GameInfo game)
		{
			nextEmulator = null;
			rom = null;
			game = null;
			try
			{
				var xmlGame = XmlGame.Create(file); // if load fails, are we supposed to retry as a bsnes XML????????
				game = xmlGame.GI;

				var system = game.System;
				var cip = new CoreInventoryParameters(this)
				{
					Comm = nextComm,
					Game = game,
					Roms = xmlGame.Assets
						.Where(kvp => !Disc.IsValidExtension(Path.GetExtension(kvp.Key)))
						.Select(kvp => (IRomAsset)new RomAsset
						{
							RomData = kvp.Value,
							FileData = kvp.Value, // TODO: Hope no one needed anything special here
							Extension = Path.GetExtension(kvp.Key),
							Game = Database.GetGameInfo(kvp.Value, Path.GetFileName(kvp.Key))
						})
						.ToList(),
					Discs = xmlGame.AssetFullPaths
						.Where(p => Disc.IsValidExtension(Path.GetExtension(p)))
						.Select(path => new
						{
							d = DiscExtensions.CreateAnyType(path, str => DoLoadErrorCallback(str, system, LoadErrorType.DiscError)),
							p = path,
						})
						.Where(a => a.d != null)
						.Select(a => (IDiscAsset)new DiscAsset
						{
							DiscData = a.d,
							DiscType = new DiscIdentifier(a.d).DetectDiscType(),
							DiscName = Path.GetFileNameWithoutExtension(a.p)
						})
						.ToList(),
				};
				nextEmulator = MakeCoreFromCoreInventory(cip, forcedCoreName);
				return true;
			}
			catch (Exception ex)
			{
				try
				{
					// need to get rid of this hack at some point
					rom = new RomGame(file);
					game = rom.GameInfo;
					game.System = VSystemID.Raw.SNES;
					nextEmulator = new LibsnesCore(
						game,
						null,
						rom.FileData,
						Path.GetDirectoryName(path.SubstringBefore('|')),
						nextComm,
						GetCoreSettings<LibsnesCore, LibsnesCore.SnesSettings>(),
						GetCoreSyncSettings<LibsnesCore, LibsnesCore.SnesSyncSettings>()
					);
					return true;
				}
				catch
				{
					DoLoadErrorCallback(ex.ToString(), VSystemID.Raw.GBL, LoadErrorType.Xml);
					return false;
				}
			}
		}

		public bool LoadRom(string path, CoreComm nextComm, string launchLibretroCore, string forcedCoreName = null, int recursiveCount = 0)
		{
			if (path == null) return false;

			if (recursiveCount > 1) // hack to stop recursive calls from endlessly rerunning if we can't load it
			{
				DoLoadErrorCallback("Failed multiple attempts to load ROM.", "");
				return false;
			}

			bool allowArchives = true;
			if (OpenAdvanced is OpenAdvanced_MAME) allowArchives = false;
			using var file = new HawkFile(path, false, allowArchives);
			if (!file.Exists && OpenAdvanced is not OpenAdvanced_LibretroNoGame) return false; // if the provided file doesn't even exist, give up! (unless libretro no game is used)

			CanonicalFullPath = file.CanonicalFullPath;

			IEmulator nextEmulator;
			RomGame rom = null;
			GameInfo game = null;

			try
			{
				var cancel = false;

				if (OpenAdvanced is OpenAdvanced_Libretro or OpenAdvanced_LibretroNoGame)
				{
					// must be done before LoadNoGame (which triggers retro_init and the paths to be consumed by the core)
					// game name == name of core
					Game = game = new GameInfo { Name = Path.GetFileNameWithoutExtension(launchLibretroCore), System = VSystemID.Raw.Libretro };
					var retro = new LibretroEmulator(nextComm, game, launchLibretroCore);
					nextEmulator = retro;

					if (retro.Description.SupportsNoGame && string.IsNullOrEmpty(path))
					{
						// if we are allowed to run NoGame and we don't have a game, boot up the core that way
						if (!retro.LoadNoGame())
						{
							DoLoadErrorCallback("LibretroNoGame failed to load. This is weird", VSystemID.Raw.Libretro);
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
							DoLoadErrorCallback("Libretro failed to load the given file. This is probably due to a core/content mismatch. Moreover, the process is now likely to be hosed. We suggest you restart the program.", VSystemID.Raw.Libretro);
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
					var ext = file.Extension;
					switch (ext)
					{
						case ".m3u":
							LoadM3U(path, nextComm, file, forcedCoreName, out nextEmulator, out game);
							break;
						case ".xml":
							if (!LoadXML(path, nextComm, file, forcedCoreName, out nextEmulator, out rom, out game))
								return false;
							break;
						case ".psf":
						case ".minipsf":
							LoadPSF(path, nextComm, file, out nextEmulator, out rom, out game);
							break;
						default:
							if (Disc.IsValidExtension(ext))
							{
								if (file.IsArchive)
									throw new InvalidOperationException("Can't load CD files from archives!");
								if (!LoadDisc(path, nextComm, file, ext, forcedCoreName, out nextEmulator, out game))
									return false;
							}
							else
							{
								LoadOther(nextComm, file, forcedCoreName, out nextEmulator, out rom, out game, out cancel); // must be called after LoadXML because of SNES hacks
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

				DispatchErrorMessage(ex, system: system, path: path);
				return false;
			}

			Rom = rom;
			LoadedEmulator = nextEmulator;
			Game = game;
			return true;
		}

		private void DispatchErrorMessage(Exception ex, string system, string path)
		{
			if (ex is AggregateException agg)
			{
				// all cores failed to load a game, so tell the user everything that went wrong and maybe they can fix it
				if (agg.InnerExceptions.Count > 1)
				{
					DoLoadErrorCallback("Multiple cores failed to load the rom:", system);
				}
				foreach (Exception e in agg.InnerExceptions)
				{
					DispatchErrorMessage(e, system: system, path: path);
				}

				return;
			}

			// all of the specific exceptions we're trying to catch here aren't expected to have inner exceptions,
			// so drill down in case we got a TargetInvocationException or something like that
			while (ex.InnerException != null)
				ex = ex.InnerException;

			if (ex is MissingFirmwareException)
			{
				DoLoadErrorCallback(ex.Message, system, path, Deterministic, LoadErrorType.MissingFirmware);
			}
			else if (ex is CGBNotSupportedException)
			{
				// failed to load SGB bios or game does not support SGB mode.
				DoLoadErrorCallback("Failed to load a GB rom in SGB mode.  You might try disabling SGB Mode.", system);
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
		}

		/// <remarks>roms ONLY; when an archive is loaded with a single file whose extension is one of these, the user prompt is skipped</remarks>
		private static class RomFileExtensions
		{
			public static readonly IReadOnlyCollection<string> A26 = new[] { "a26" };

			public static readonly IReadOnlyCollection<string> A78 = new[] { "a78" };

			public static readonly IReadOnlyCollection<string> AppleII = new[] { "dsk", "do", "po" };

			public static readonly IReadOnlyCollection<string> C64 = new[] { "prg", "d64", "g64", "crt", "tap" };

			public static readonly IReadOnlyCollection<string> Coleco = new[] { "col" };

			public static readonly IReadOnlyCollection<string> GB = new[] { "gb", "gbc", "sgb" };

			public static readonly IReadOnlyCollection<string> GBA = new[] { "gba" };

			public static readonly IReadOnlyCollection<string> GEN = new[] { "gen", "md", "smd", "32x" };

			public static readonly IReadOnlyCollection<string> INTV = new[] { "int", "bin", "rom" };

			public static readonly IReadOnlyCollection<string> Lynx = new[] { "lnx" };

			public static readonly IReadOnlyCollection<string> N64 = new[] { "z64", "v64", "n64" };

			public static readonly IReadOnlyCollection<string> NDS = new[] { "nds" };

			public static readonly IReadOnlyCollection<string> NES = new[] { "nes", "fds", "unf" };

			public static readonly IReadOnlyCollection<string> NGP = new[] { "ngp", "ngc" };

			public static readonly IReadOnlyCollection<string> O2 = new[] { "o2" };

			public static readonly IReadOnlyCollection<string> PCE = new[] { "pce", "sgx" };

			public static readonly IReadOnlyCollection<string> SMS = new[] { "sms", "gg", "sg" };

			public static readonly IReadOnlyCollection<string> SNES = new[] { "smc", "sfc", "xml" };

			public static readonly IReadOnlyCollection<string> TI83 = new[] { "83g", "83l", "83p" };

			public static readonly IReadOnlyCollection<string> UZE = new[] { "uze" };

			public static readonly IReadOnlyCollection<string> VB = new[] { "vb" };

			public static readonly IReadOnlyCollection<string> VEC = new[] { "vec" };

			public static readonly IReadOnlyCollection<string> WSWAN = new[] { "ws", "wsc" };

			public static readonly IReadOnlyCollection<string> ZXSpectrum = new[] { "tzx", "tap", "dsk", "pzx" };

			public static readonly IReadOnlyCollection<string> AutoloadFromArchive = Array.Empty<string>()
				.Concat(A26)
				.Concat(A78)
				.Concat(AppleII)
				.Concat(C64)
				.Concat(Coleco)
				.Concat(GB)
				.Concat(GBA)
				.Concat(GEN)
				.Concat(INTV)
				.Concat(Lynx)
				.Concat(N64)
				.Concat(NDS)
				.Concat(NES)
				.Concat(NGP)
				.Concat(O2)
				.Concat(PCE)
				.Concat(SMS)
				.Concat(SNES)
				.Concat(TI83)
				.Concat(UZE)
				.Concat(VB)
				.Concat(VEC)
				.Concat(WSWAN)
				.Concat(ZXSpectrum)
				.Select(static s => $".{s}") // this is what's expected at call-site
				.ToArray();
		}

		/// <remarks>TODO add and handle <see cref="FilesystemFilter.LuaScripts"/> (you can drag-and-drop scripts and there are already non-rom things in this list, so why not?)</remarks>
		private static readonly FilesystemFilterSet RomFSFilterSet = new FilesystemFilterSet(
			new FilesystemFilter("Music Files", Array.Empty<string>(), devBuildExtraExts: new[] { "psf", "minipsf", "sid", "nsf" }),
			new FilesystemFilter("Disc Images", new[] { "cue", "ccd", "mds", "m3u" }),
			new FilesystemFilter("NES", RomFileExtensions.NES.Concat(new[] { "nsf" }).ToList(), addArchiveExts: true),
			new FilesystemFilter("Super NES", RomFileExtensions.SNES, addArchiveExts: true),
			new FilesystemFilter("PlayStation", new[] { "cue", "ccd", "mds", "m3u" }),
			new FilesystemFilter("PSX Executables (experimental)", Array.Empty<string>(), devBuildExtraExts: new[] { "exe" }),
			new FilesystemFilter("PSF Playstation Sound File", new[] { "psf", "minipsf" }),
			new FilesystemFilter("Nintendo 64", RomFileExtensions.N64),
			new FilesystemFilter("Gameboy", RomFileExtensions.GB, addArchiveExts: true),
			new FilesystemFilter("Gameboy Advance", RomFileExtensions.GBA, addArchiveExts: true),
			new FilesystemFilter("Nintendo DS", RomFileExtensions.NDS),
			new FilesystemFilter("Master System", RomFileExtensions.SMS, addArchiveExts: true),
			new FilesystemFilter("PC Engine", RomFileExtensions.PCE.Concat(new[] { "cue", "ccd", "mds" }).ToList(), addArchiveExts: true),
			new FilesystemFilter("Atari 2600", RomFileExtensions.A26, devBuildExtraExts: new[] { "bin" }, addArchiveExts: true),
			new FilesystemFilter("Atari 7800", RomFileExtensions.A78, devBuildExtraExts: new[] { "bin" }, addArchiveExts: true),
			new FilesystemFilter("Atari Lynx", RomFileExtensions.Lynx, addArchiveExts: true),
			new FilesystemFilter("ColecoVision", RomFileExtensions.Coleco, addArchiveExts: true),
			new FilesystemFilter("IntelliVision", RomFileExtensions.INTV, addArchiveExts: true),
			new FilesystemFilter("TI-83", RomFileExtensions.TI83, addArchiveExts: true),
			FilesystemFilter.Archives,
			new FilesystemFilter("Genesis", RomFileExtensions.GEN.Concat(new[] { "bin", "cue", "ccd" }).ToList(), addArchiveExts: true),
			new FilesystemFilter("SID Commodore 64 Music File", Array.Empty<string>(), devBuildExtraExts: new[] { "sid" }, devBuildAddArchiveExts: true),
			new FilesystemFilter("WonderSwan", RomFileExtensions.WSWAN, addArchiveExts: true),
			new FilesystemFilter("Apple II", RomFileExtensions.AppleII, addArchiveExts: true),
			new FilesystemFilter("Virtual Boy", RomFileExtensions.VB, addArchiveExts: true),
			new FilesystemFilter("Neo Geo Pocket", RomFileExtensions.NGP, addArchiveExts: true),
			new FilesystemFilter("Commodore 64", RomFileExtensions.C64, addArchiveExts: true),
			new FilesystemFilter("Amstrad CPC", Array.Empty<string>(), devBuildExtraExts: new[] { "cdt", "dsk" }, devBuildAddArchiveExts: true),
			new FilesystemFilter("Sinclair ZX Spectrum", RomFileExtensions.ZXSpectrum.Concat(new[] { "csw", "wav" }).ToList(), addArchiveExts: true),
			new FilesystemFilter("Odyssey 2", RomFileExtensions.O2),
			new FilesystemFilter("Uzebox", RomFileExtensions.UZE),
			new FilesystemFilter("Vectrex", RomFileExtensions.VEC),
			FilesystemFilter.EmuHawkSaveStates
		);

		public static readonly IReadOnlyCollection<string> KnownRomExtensions = RomFSFilterSet.Filters
			.SelectMany(f => f.Extensions)
			.Distinct()
			.Except(FilesystemFilter.ArchiveExtensions.Concat(new[] { "State" }))
			.Select(s => $".{s.ToUpperInvariant()}") // this is what's expected at call-site
			.ToList();

		public static readonly string RomFilter = RomFSFilterSet.ToString("Everything");
	}
}
