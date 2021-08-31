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

using System.ComponentModel;

namespace BizHawk.Client.Common
{
	public class RomLoader
	{
		public static RomLoader instance;

		public static Dictionary<string, EmulatorInstance> loadedEmulators = new Dictionary<string, EmulatorInstance>();
		public static List<string> knownRoms = new List<string>();
		public static string activeRom = "";
		public static List<string> romHistory = new List<string>();
		public static Dictionary<string, bool> romActiveStates = new Dictionary<string, bool>();

		public static EmulatorInstance activeEmulator;
		public static Dictionary<string, GameTriggerDefinition> gameTriggers = new Dictionary<string, GameTriggerDefinition>();
		public static GameTriggerDefinition activeGameTriggerDef;

		public static BackgroundWorker checkFrameWorker;
		public static bool shouldCheckForSwitches;
		public static bool shouldSwitchGames;
		public static int frameIndex = 0;

		public static List<MemoryDomain> activeMemoryDomains = new List<MemoryDomain>();

		public static LoadRomArgs lastLoadRomArgs;

		public static bool infiniteLivesOn = true;

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
			RomLoader.instance = this;
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
			var romExtensions = new[]
			{
				".sms", ".smc", ".sfc", ".pce", ".sgx", ".gg", ".sg", ".bin", ".gen", ".md", ".smd", ".gb",
				".nes", ".fds", ".rom", ".int", ".gbc", ".unf", ".a78", ".crt", ".col", ".xml", ".z64",
				".v64", ".n64", ".ws", ".wsc", ".gba", ".32x", ".vec", ".o2"
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

		private GameInfo MakeGameFromDisc(Disc disc, string ext, string name)
		{
			System.Diagnostics.Debug.WriteLine("!!! CALLING MakeGameFromDisc");
			// TODO - use more sophisticated IDer
			var discType = new DiscIdentifier(disc).DetectDiscType();
			var discHasher = new DiscHasher(disc);
			var discHash = discType == DiscType.SonyPSX
				? discHasher.Calculate_PSX_BizIDHash().ToString("X8")
				: discHasher.OldHash();

			var game = Database.CheckDatabase(discHash);
			if (game == null)
			{
				// try to use our wizard methods
				game = new GameInfo { Name = name, Hash = discHash };

				switch (discType)
				{
					case DiscType.SegaSaturn:
						game.System = "SAT";
						break;
					case DiscType.SonyPSP:
						game.System = "PSP";
						break;
					case DiscType.SonyPS2:
						game.System = "PS2";
						break;
					case DiscType.MegaCD:
						game.System = "GEN";
						break;
					case DiscType.PCFX:
						game.System = "PCFX";
						break;

					case DiscType.TurboGECD:
					case DiscType.TurboCD:
						game.System = "PCE";
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
						game.System = _config.TryGetChosenSystemForFileExt(ext, out var sysID) ? sysID : "NULL";
						break;

					default: //"for an unknown disc, default to psx instead of pce-cd, since that is far more likely to be what they are attempting to open" [5e07ab3ec3b8b8de9eae71b489b55d23a3909f55, year 2015]
					case DiscType.SonyPSX:
						game.System = "PSX";
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
				rom.GameInfo.System = "PSX";
			}
			else if (file.Extension == ".nsf")
			{
				rom.GameInfo.System = "NES";
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
				case "GB":
				case "GBC":
					if (_config.GbAsSgb)
					{
						game.System = "SGB";
					}
					break;
				case "MAME":
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
					game.System = "SNES";
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
					DoLoadErrorCallback(ex.ToString(), "DGB", LoadErrorType.Xml);
					return false;
				}
			}
		}

		public static void SaveActiveRomStates()
		{
			string textToSave = "";
			foreach(string _key in romActiveStates.Keys)
			{
				if (textToSave.Length > 0)
				{
					textToSave += "\n";
				}
				textToSave += _key + ":" + romActiveStates[_key];
			}

			File.WriteAllText(".\\_magicbox\\_romActiveStates.txt", textToSave);
		}

		public static void ParseRomActiveStates()
		{
			if (File.Exists(".\\_magicbox\\_romActiveStates.txt"))
			{
				Dictionary<string, bool> parsedStates = new Dictionary<string, bool>();
				string[] sourceLines = File.ReadAllLines(".\\_magicbox\\_romActiveStates.txt");
				foreach(string line in sourceLines)
				{
					string[] components = line.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
					if (components.Length > 1)
					{
						if (!parsedStates.ContainsKey(components[0]))
						{
							parsedStates.Add(components[0], components[1].ToUpper() == "TRUE");
						}
					}
				}

				List<string> keys = new List<string>();
				foreach(string _key in romActiveStates.Keys)
				{
					keys.Add(_key);
				}
				foreach (string _key in keys)
				{
					if (parsedStates.ContainsKey(_key))
					{
						romActiveStates[_key] = parsedStates[_key];
					}
				}
			}
		}

		public static bool ShouldToggleOutOfActiveGame()
		{
			if (romActiveStates.ContainsKey(activeRom))
			{
				if (!romActiveStates[activeRom])
				{
					return true;
				}
			}
			return false;
		}

		public static void InitialiseShuffler(bool forceRefresh = false)
		{
			if (knownRoms.Count <= 0 || forceRefresh)
			{
				knownRoms.Clear();

				string[] romFiles = Directory.GetFiles("_magicbox");
				foreach (string _fileName in romFiles)
				{
					if (!_fileName.EndsWith(".txt") && !_fileName.EndsWith(".bin") && !_fileName.EndsWith(".ccd") && !_fileName.EndsWith(".img") && !_fileName.EndsWith(".sub"))
					{
						System.Diagnostics.Debug.WriteLine("!!!!!!!!     found rom: " + _fileName);
						knownRoms.Add(_fileName);
					}
				}

				foreach(string romId in knownRoms)
				{
					if (!romActiveStates.ContainsKey(romId))
					{
						romActiveStates.Add(romId, true);
					}
				}
				ParseRomActiveStates();
			}

			if (gameTriggers.Count <= 0 || forceRefresh)
			{
				gameTriggers.Clear();

				string[] triggerFiles = Directory.GetFiles("_shuffletriggers");
				foreach (string _fileName in triggerFiles)
				{
					if (_fileName.EndsWith(".txt"))
					{
						System.Diagnostics.Debug.WriteLine("!!!!!!!!     found trigger: " + _fileName);

						string[] pathComponents = _fileName.Split(new string[] { "\\" }, StringSplitOptions.None);
						string _trimmedFileName = pathComponents[pathComponents.Length - 1];
						string _key = _trimmedFileName.Split(new string[] { ".txt", "(" }, StringSplitOptions.None)[0];
						System.Diagnostics.Debug.WriteLine("!!!!!!!!         adding trigger for key: " + _key);

						GameTriggerDefinition definition = GameTriggerDefinition.FromFile(_fileName, _key);
						gameTriggers.Add(_key, definition);
					}
				}
				RefreshActiveTriggers();
			}
		}

		public string SelectShuffledGame()
		{
			string path = "";
			if (knownRoms.Count > 1)
			{
				int attempts = 0;
				Random random = new Random();

				List<string> allowedRoms = new List<string>();
				foreach(string romId in knownRoms)
				{
					if (romActiveStates.ContainsKey(romId))
					{
						if (romActiveStates[romId])
						{
							allowedRoms.Add(romId);
						}
					}
				}
				RefreshRomHistory();

				path = activeRom;
				if (allowedRoms.Count > 1)
				{
					while (attempts < 1000 && (path == activeRom || romHistory.Contains(path)) && allowedRoms.Count > 0)
					{
						System.Diagnostics.Debug.WriteLine("!!!!!!!! reshuffling " + path);
						path = allowedRoms[random.Next(0, allowedRoms.Count)];
						attempts++;
					}
				} else if (allowedRoms.Count == 1)
				{
					path = allowedRoms[0];
				}

				activeRom = path;
				romHistory.Add(activeRom);
				if (romHistory.Count > allowedRoms.Count * 0.5f)
				{
					romHistory.RemoveAt(0);
				}
				System.Diagnostics.Debug.WriteLine("!!!!!!!! activeRom: " + activeRom + ", romHistory: " + romHistory.Count.ToString() + ", attempts: " + attempts.ToString());
			}
			else if (knownRoms.Count == 1)
			{
				path = knownRoms[0];
			}

			return path;
		}

		public static void RefreshRomHistory()
		{
			List<string> newHistory = new List<string>();
			foreach(string romId in romHistory)
			{
				if (!romActiveStates.ContainsKey(romId))
				{
					newHistory.Add(romId);
				} else
				{
					if (!romActiveStates[romId])
					{
						newHistory.Add(romId);
					}
				}
			}
			romHistory = newHistory;
		}

		public bool LoadGameFromShuffler(string path)
		{
			System.Diagnostics.Debug.WriteLine("!!!!!!!! I want to load " + path);

			bool allowArchives = true;
			if (OpenAdvanced is OpenAdvanced_MAME) allowArchives = false;
			using var file = new HawkFile(path, false, allowArchives);
			if (!file.Exists) return false; // if the provided file doesn't even exist, give up!

			CanonicalFullPath = file.CanonicalFullPath;

			IEmulator nextEmulator;
			RomGame rom = null;
			GameInfo game = null;

			CoreComm nextComm = lastCoreComm;
			string launchLibretroCore = lastLaunchLibretroCore;
			string forcedCoreName = null;

			if (loadedEmulators.ContainsKey(path))
			{
				nextEmulator = loadedEmulators[path].emulator;
				rom = loadedEmulators[path].rom;
				game = loadedEmulators[path].game;

				activeEmulator = loadedEmulators[path];
			}
			else
			{
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

					DispatchErrorMessage(ex, system);
					return false;
				}

				EmulatorInstance newInstance = new EmulatorInstance();
				newInstance.emulator = nextEmulator;
				newInstance.rom = rom;
				newInstance.game = game;
				loadedEmulators.Add(path, newInstance);

				

				activeEmulator = newInstance;
			}

			Rom = rom;
			LoadedEmulator = nextEmulator;
			Game = game;

			RefreshActiveTriggers();

			//activeMemoryDomains = activeEmulator.emulator.AsMemoryDomains().ToList<MemoryDomain>();

			// now make a thread to check for switches!

			/*
			if (checkFrameWorker != null)
			{
				checkFrameWorker.CancelAsync();
			}
			checkFrameWorker = new BackgroundWorker();
			checkFrameWorker.DoWork += AsyncCheckFrame;
			checkFrameWorker.RunWorkerAsync();
			*/

			return true;
		}

		public static void RefreshActiveTriggers()
		{
			if (activeEmulator != null)
			{
				string _triggerKey = activeEmulator.GetTriggerKey();

				if (gameTriggers.ContainsKey(_triggerKey))
				{
					activeGameTriggerDef = gameTriggers[_triggerKey];
				}
				else
				{
					activeGameTriggerDef = null;
					System.Diagnostics.Debug.WriteLine("!!!!!!!! no trigger for game (" + activeEmulator.game.Name + ") key: " + _triggerKey);
				}
			}
		}

		public static string GetActiveGameFileHandle()
		{
			if (activeEmulator != null)
			{
				return activeEmulator.GetTriggerKey();
			}
			return "NONE";
		}

		public static CoreComm lastCoreComm;
		public static string lastLaunchLibretroCore;

		public bool LoadRom(string tempPath, CoreComm nextComm, string launchLibretroCore, string forcedCoreName = null, int recursiveCount = 0)
		{
			System.Diagnostics.Debug.WriteLine("!!!!!!!! LOADING ROM " + tempPath + ", " + nextComm.ToString() + ", " + launchLibretroCore);

			lastCoreComm = nextComm;
			lastLaunchLibretroCore = launchLibretroCore;

			InitialiseShuffler();

			string path = SelectShuffledGame();
			if (tempPath.StartsWith("!"))
			{
				path = tempPath.Substring(1, tempPath.Length - 1);
			}

			if (path.Length > 0)
			{
				return LoadGameFromShuffler(path);
			} else
			{
				return false;
			}
		}

		public static bool CanAllowSwitch()
		{
			List<string> allowedRoms = new List<string>();
			foreach (string romId in knownRoms)
			{
				if (romActiveStates.ContainsKey(romId))
				{
					if (romActiveStates[romId])
					{
						allowedRoms.Add(romId);
					}
				}
			}
			return allowedRoms.Count > 1;
		}

		private void AsyncCheckFrame(object sender, DoWorkEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine("!!!!!!! AsyncCheckFrame starts");

			int loopCount = 0;
			int frameIndex = 0;
			while(true)
			{
				//System.Diagnostics.Debug.WriteLine("!!!!!!!     AsyncCheckFrame frame " + loopCount.ToString());
				if (shouldCheckForSwitches && !shouldSwitchGames && activeGameTriggerDef != null && frameIndex != RomLoader.frameIndex)
				{
					if (activeEmulator != null)
					{
						MemoryDomain[] domains = activeEmulator.emulator.AsMemoryDomains().ToArray<MemoryDomain>();
						//System.Diagnostics.Debug.WriteLine("@@@@@@@@@        AsyncCheckFrame got " + domains.Length.ToString() + " domains");
						foreach(MemoryDomain domain in domains)
						{
							//System.Diagnostics.Debug.WriteLine("@@@@@@@@@            " + domain.Name);

						}
						shouldSwitchGames = activeGameTriggerDef.CheckFrame();
					}

					//System.Diagnostics.Debug.WriteLine("!!!!!!!        AsyncCheckFrame will check frame " + RomLoader.frameIndex);
					//shouldSwitchGames = activeGameTriggerDef.CheckFrame();
					frameIndex = RomLoader.frameIndex;

					//RomLoader.frameIndex++;
				}
				loopCount++;
			}
		}

		private void DispatchErrorMessage(Exception ex, string system)
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
					DispatchErrorMessage(e, system);
				}

				return;
			}

			// all of the specific exceptions we're trying to catch here aren't expected to have inner exceptions,
			// so drill down in case we got a TargetInvocationException or something like that
			while (ex.InnerException != null)
				ex = ex.InnerException;

			if (ex is MissingFirmwareException)
			{
				DoLoadErrorCallback(ex.Message, system, LoadErrorType.MissingFirmware);
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

		/// <remarks>TODO add and handle <see cref="FilesystemFilter.LuaScripts"/> (you can drag-and-drop scripts and there are already non-rom things in this list, so why not?)</remarks>
		private static readonly FilesystemFilterSet RomFSFilterSet = new FilesystemFilterSet(
			new FilesystemFilter("Music Files", new string[0], devBuildExtraExts: new[] { "psf", "minipsf", "sid", "nsf" }),
			new FilesystemFilter("Disc Images", new[] { "cue", "ccd", "mds", "m3u" }),
			new FilesystemFilter("NES", new[] { "nes", "fds", "unf", "nsf" }, addArchiveExts: true),
			new FilesystemFilter("Super NES", new[] { "smc", "sfc", "xml" }, addArchiveExts: true),
			new FilesystemFilter("PlayStation", new[] { "cue", "ccd", "mds", "m3u" }),
			new FilesystemFilter("PSX Executables (experimental)", new string[0], devBuildExtraExts: new[] { "exe" }),
			new FilesystemFilter("PSF Playstation Sound File", new[] { "psf", "minipsf" }),
			new FilesystemFilter("Nintendo 64", new[] { "z64", "v64", "n64" }),
			new FilesystemFilter("Gameboy", new[] { "gb", "gbc", "sgb" }, addArchiveExts: true),
			new FilesystemFilter("Gameboy Advance", new[] { "gba" }, addArchiveExts: true),
			new FilesystemFilter("Nintendo DS", new[] { "nds" }),
			new FilesystemFilter("Master System", new[] { "sms", "gg", "sg" }, addArchiveExts: true),
			new FilesystemFilter("PC Engine", new[] { "pce", "sgx", "cue", "ccd", "mds" }, addArchiveExts: true),
			new FilesystemFilter("Atari 2600", new[] { "a26" }, devBuildExtraExts: new[] { "bin" }, addArchiveExts: true),
			new FilesystemFilter("Atari 7800", new[] { "a78" }, devBuildExtraExts: new[] { "bin" }, addArchiveExts: true),
			new FilesystemFilter("Atari Lynx", new[] { "lnx" }, addArchiveExts: true),
			new FilesystemFilter("ColecoVision", new[] { "col" }, addArchiveExts: true),
			new FilesystemFilter("IntelliVision", new[] { "int", "bin", "rom" }, addArchiveExts: true),
			new FilesystemFilter("TI-83", new[] { "rom" }, addArchiveExts: true),
			FilesystemFilter.Archives,
			new FilesystemFilter("Genesis", new[] { "gen", "md", "smd", "32x", "bin", "cue", "ccd" }, addArchiveExts: true),
			new FilesystemFilter("SID Commodore 64 Music File", new string[0], devBuildExtraExts: new[] { "sid" }, devBuildAddArchiveExts: true),
			new FilesystemFilter("WonderSwan", new[] { "ws", "wsc" }, addArchiveExts: true),
			new FilesystemFilter("Apple II", new[] { "dsk", "do", "po" }, addArchiveExts: true),
			new FilesystemFilter("Virtual Boy", new[] { "vb" }, addArchiveExts: true),
			new FilesystemFilter("Neo Geo Pocket", new[] { "ngp", "ngc" }, addArchiveExts: true),
			new FilesystemFilter("Commodore 64", new[] { "prg", "d64", "g64", "crt", "tap" }, addArchiveExts: true),
			new FilesystemFilter("Amstrad CPC", new string[0], devBuildExtraExts: new[] { "cdt", "dsk" }, devBuildAddArchiveExts: true),
			new FilesystemFilter("Sinclair ZX Spectrum", new[] { "tzx", "tap", "dsk", "pzx", "csw", "wav" }, addArchiveExts: true),
			new FilesystemFilter("Odyssey 2", new[] { "o2" }),
			new FilesystemFilter("Uzebox", new[] { "uze" }),
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

	public class EmulatorInstance
	{
		public IEmulator emulator = null;
		public RomGame rom = null;
		public GameInfo game = null;

		public string GetTriggerKey()
		{
			string _triggerKeyStart = game.Name.Replace(":",",").Replace(".", "").Split(new string[] { "(" }, StringSplitOptions.None)[0];
			if (!_triggerKeyStart.EndsWith(" "))
			{
				_triggerKeyStart += " ";
			}
			string _triggerKey = _triggerKeyStart.ToUpper() + game.System.ToString();
			return _triggerKey;
		}
	}

	public class GameTriggerDefinition {
		public Dictionary<string, ShuffleTriggerDefinition> triggers = new Dictionary<string, ShuffleTriggerDefinition>();
		public LifeCountDefinition lifeCountDefinition;

		public int liveFrames = 0;
		public static GameTriggerDefinition FromFile(string _triggerFilePath, string _triggerKey) {
			GameTriggerDefinition newDef = new GameTriggerDefinition();

			if (_triggerFilePath != null)
			{
				string[] lines = File.ReadAllLines(_triggerFilePath);
				foreach (string line in lines)
				{
					string[] components = line.Split(new string[] { ">" }, StringSplitOptions.None);
					if (components.Length > 1)
					{
						newDef.triggers.Add(components[0], ShuffleTriggerDefinition.FromLine(components[1]));
					}
				}
			}

			newDef.lifeCountDefinition = LifeCountDefinition.FromFile(".\\_lifesettings\\" + _triggerKey + ".txt");

			return newDef;
		}

		public bool CheckFrame()
		{
			liveFrames++;

			if (lifeCountDefinition != null)
			{
				if (RomLoader.infiniteLivesOn && liveFrames % Math.Max(1, lifeCountDefinition.period) == 0)
				{
					lifeCountDefinition.Apply();
				}
			}

			// Saturn is super slow so I have to implement something...
			if (RomLoader.activeEmulator.game.System == "SAT")
			{
				if (RomLoader.frameIndex % 10 != 0)
				{
					return false;
				}
			}

			//System.Diagnostics.Debug.WriteLine("!!!!!!!        CheckFrame called");

			bool shouldSwitch = false;
			//if (LuaLibraryBase.publicApiContainer != null)
			if (MemoryApi.instance != null && RomLoader.CanAllowSwitch())
			{
				//System.Diagnostics.Debug.WriteLine("!!!!!!!!!              checking MemoryApi.instance");


				foreach (string _key in triggers.Keys)
				{
					//System.Diagnostics.Debug.WriteLine("!!!!!!!!!              checking _key " + _key);

					bool triggerFired = triggers[_key].CheckFrame();
					if (triggerFired)
					{
						shouldSwitch = true;
					}
				}
			} else
			{
				//System.Diagnostics.Debug.WriteLine("!!!!!!!!!                MemoryApi.instance is null");
			}

			return shouldSwitch;
		}
	}

	public class LifeCountDefinition
	{
		public List<int> bytes = new List<int>();
		public List<int> values = new List<int>();
		public List<string> domains = new List<string>();
		public int period = 60;

		public void Apply()
		{
			for (int i = 0; i < bytes.Count; i++)
			{
				int location = bytes[i];
				int value = values.Count > i ? values[i] : 0;
				string domain = domains.Count > i ? domains[i] : "DEFAULT";
				string domainToCheck = domain;
				if (domain.ToUpper() == "DEFAULT")
				{
					domainToCheck = ShuffleTriggerDefinition.DefaultMemoryDomian();
				}

				//System.Diagnostics.Debug.WriteLine("Writing " + ((uint)(value) % 0x100).ToString("X2") + " to " + location.ToString("X2") + " in " + domainToCheck);
				MemoryApi.instance.WriteByte(location, (uint)(value) % 0x100, domainToCheck);
			}
		}

		public string GetStringValue()
		{
			string runningString = "period>" + period.ToString();
			for(int i = 0; i < bytes.Count || i < values.Count || i < domains.Count; i++)
			{
				runningString += "\nstate>";
				if (i < bytes.Count)
				{
					if (!runningString.EndsWith(">"))
					{
						runningString += "/";
					}
					runningString += "byte:" + bytes[i].ToString("X4");
				}

				if (i < values.Count)
				{
					if (!runningString.EndsWith(">"))
					{
						runningString += "/";
					}
					runningString += "value:" + values[i].ToString("X2");
				}

				if (i < domains.Count)
				{
					if (!runningString.EndsWith(">"))
					{
						runningString += "/";
					}
					runningString += "domain:" + domains[i];
				}
			}

			return runningString;
		}

		public static LifeCountDefinition FromFile(string _filePath)
		{
			LifeCountDefinition newDef = new LifeCountDefinition();

			if (File.Exists(_filePath))
			{
				System.Diagnostics.Debug.WriteLine("!!!!!!!!         found LifeCountDefinition at " + _filePath);

				string[] lines = File.ReadAllLines(_filePath);
				foreach (string line in lines)
				{
					if (line.StartsWith("period>"))
					{
						newDef.period = int.Parse(line.Substring("period>".Length));
					}

					if (line.StartsWith("state>"))
					{
						newDef.bytes.Add(0);
						newDef.values.Add(0);
						newDef.domains.Add("DEFAULT");

						string[] parameters = line.Substring("state>".Length).Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
						foreach (string parameter in parameters)
						{
							string[] components = parameter.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
							if (components.Length > 1)
							{
								if (components[0] == "byte")
								{
									newDef.bytes[newDef.bytes.Count - 1] = int.Parse(components[1], System.Globalization.NumberStyles.HexNumber);
								}
								if (components[0] == "value")
								{
									newDef.values[newDef.values.Count - 1] = int.Parse(components[1], System.Globalization.NumberStyles.HexNumber);
								}
								if (components[0] == "domain")
								{
									newDef.domains[newDef.domains.Count - 1] = components[1];
								}
							}
						}
					}
					
				}
			} else
			{
				System.Diagnostics.Debug.WriteLine("!!!!!!!!         did not find LifeCountDefinition at " + _filePath);

			}

			return newDef;
		}
	}

	public class ShuffleTriggerDefinition {
		public List<int> bytes = new List<int>();
		public int baseType = 256;
		public int minChange = 0;
		public int maxChange = 2147483647;
		public int delay = 0;
		public string domain = "DEFAULT";
		public bool enabled = true;
		public bool blockJumpFromZero = false;

		public long lastValue = 0;
		public int countdownToActivate = 0;

		public static MemoryDomain DomainWithName(string name)
		{
			foreach(MemoryDomain _domain in RomLoader.activeMemoryDomains)
			{
				if (_domain.Name == name)
				{
					return _domain;
				}
			}

			return RomLoader.activeMemoryDomains[0];
		}

		public static string DefaultMemoryDomian()
		{
			switch (RomLoader.activeEmulator.game.System)
			{
				case "GEN":
					return "68K RAM";
				case "GB":
					return "CartRAM";
				case "NES":
					return "WRAM";
				case "SMS":
					return "Main RAM";
				case "GG":
					return "Main RAM";
				case "SAT":
					return "Work RAM High";
				case "GBA":
					return "IWRAM";
				case "NGP":
					return "RAM";
			}

			return MemoryApi.instance.MainMemoryName;
		}

		public bool CheckFrame()
		{
			if (!enabled /*|| RomLoader.activeMemoryDomains.Count <= 0*/)
			{
				return false;
			}

			long currentValue = 0;
			int multiplicand = 1;
			//MemoryDomain domainToCheck = DomainWithName(domain);
			string domainToCheck = domain;
			if (domain.ToUpper() == "DEFAULT")
			{
				domainToCheck = DefaultMemoryDomian();
			}

			string locationString = "";
			foreach(int location in bytes)
			{
				//uint valueHere = domainToCheck.PeekByte(location); 
				uint valueHere = MemoryApi.instance.ReadByte(location, domainToCheck);

				locationString += location.ToString("X4") + " ";

				if (baseType == 100)
				{
					uint lowerVal = valueHere % 0x10;
					uint upperVal = (valueHere - lowerVal) / 16;
					valueHere = lowerVal + (upperVal * 10);
				}
				currentValue += valueHere * multiplicand;
				multiplicand *= baseType;
			}

			if (lastValue != currentValue)
			{
				long oldValue = lastValue;

				long difference = currentValue - lastValue;
				lastValue = currentValue;

				bool shouldBlockJump = blockJumpFromZero && oldValue == 0;

				if (difference > minChange && difference < maxChange && !shouldBlockJump)
				{
					System.Diagnostics.Debug.WriteLine("!!!!!!!!! Trigger at " + locationString + " " + domainToCheck);
					System.Diagnostics.Debug.WriteLine("!!!!!!!!!              CheckFrame goes " + oldValue.ToString() + " -> " + currentValue.ToString());
					if (delay > 0)
					{
						countdownToActivate = delay + 1;
					} else
					{
						return true;
					}
				}
			}

			// account for the delay
			if (countdownToActivate > 0)
			{
				countdownToActivate--;
				if (countdownToActivate <= 0)
				{
					countdownToActivate = 0;
					return true;
				}
			}

			//System.Diagnostics.Debug.WriteLine("!!!!!!!!!              CheckFrame returns false");
			return false;
		}

		public static ShuffleTriggerDefinition FromLine(string _line) {
			ShuffleTriggerDefinition newDef = new ShuffleTriggerDefinition();

			string[] fields = _line.Split(new string[] { "/" }, StringSplitOptions.None);
			foreach (string field in fields)
			{
				string[] components = field.Split(new string[] { ":" }, StringSplitOptions.None);
				if (components.Length > 1)
				{
					if (components[0] == "bytes")
					{
						string[] byteStrings = components[1].Split(new string[] { "," }, StringSplitOptions.None);
						foreach (string byteString in byteStrings)
						{
							if (byteString.Length > 0)
							{
								newDef.bytes.Add(int.Parse(byteString, System.Globalization.NumberStyles.HexNumber));
							}
						}
					}

					if (components[0] == "base")
					{
						newDef.baseType = int.Parse(components[1]);
					}
					if (components[0] == "minChange")
					{
						newDef.minChange = int.Parse(components[1]);
					}
					if (components[0] == "maxChange")
					{
						newDef.maxChange = int.Parse(components[1]);
					}
					if (components[0] == "delay")
					{
						newDef.delay = int.Parse(components[1]);
					}
					if (components[0] == "domain")
					{
						newDef.domain = components[1];
					}
					if (components[0] == "enabled")
					{
						newDef.enabled = components[1].ToUpper() == "TRUE";
					}
					if (components[0] == "blockJumpFromZero")
					{
						newDef.blockJumpFromZero = components[1].ToUpper() == "TRUE";
					}
				}
			}

			return newDef;
		}
	}
}

