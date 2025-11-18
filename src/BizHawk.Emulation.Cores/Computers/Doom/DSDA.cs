using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	[PortedCore(
		name: CoreNames.DSDA,
		author: "The DSDA-Doom Team",
		portedVersion: "0.28.2 (fe0dfa0)",
		portedUrl: "https://github.com/kraflab/dsda-doom")]
	[ServiceNotApplicable(typeof(ISaveRam))]
	public partial class DSDA : IRomInfo
	{
		[CoreConstructor(VSystemID.Raw.Doom)]
		public DSDA(CoreLoadParameters<DoomSettings, DoomSyncSettings> lp)
		{
			ServiceProvider = new BasicServiceProvider(this);
			Comm = lp.Comm;
			_finalSyncSettings = _syncSettings = lp.SyncSettings ?? new DoomSyncSettings();
			_settings = lp.Settings ?? new DoomSettings();
			_loadCallback = LoadCallback;
			_errorCallback = ErrorCallback;

			// Gathering information for the rest of the wads
			_wadFiles = lp.Roms;

			// Checking for correct IWAD configuration
			var foundIWAD = false;
			_pwadFiles = [ ];

			foreach (var wadFile in _wadFiles)
			{
				bool recognized = false;

				if (wadFile.RomData is [ (byte) 'I', (byte) 'W', (byte) 'A', (byte) 'D', .. ])
				{
					// Check not more than one IWAD is provided
					if (foundIWAD)
					{
						throw new ArgumentException(
							$"More than one IWAD provided. Trying to load '{wadFile.RomPath}', but IWAD '{_iwadName}' was already provided",
							paramName: nameof(lp));
					}

					_iwadName = wadFile.RomPath;
					_iwadData = wadFile.RomData;
					foundIWAD = true;
					recognized = true;
				}
				else if (wadFile.RomData is [ (byte) 'P', (byte) 'W', (byte) 'A', (byte) 'D', .. ])
				{
					if (_fakePwads.Contains(GetFullName(wadFile).ToLowerInvariant()))
					{
						_iwadName = wadFile.RomPath;
						_iwadData = wadFile.RomData;
						foundIWAD = true;
					}
					else
					{
						_pwadFiles.Add(wadFile);
					}

					recognized = true;
				}

				if (!recognized)
				{
					throw new ArgumentException(
						$"Unrecognized WAD provided: '{wadFile.RomPath}' has non-standard header.",
						paramName: nameof(lp));
				}
			}

			// Check at least one IWAD was provided
			if (!foundIWAD)
			{
				_iwadData = lp.Comm.CoreFileProvider.GetFirmware(
					new(VSystemID.Raw.Doom, "Doom2_IWAD"));

				if (_iwadData == null)
				{
					throw new ArgumentException(
						"No IWAD file provided",
						paramName: nameof(lp));
				}

				_ = FirmwareDatabase.FirmwareFilesByHash.TryGetValue(SHA1Checksum.ComputeDigestHex(_iwadData), out var ff);
				_iwadName = ff.RecommendedName;
			}

			// Getting dsda-doom.wad -- required by DSDA
			_dsdaWadFileData = Zstd.DecompressZstdStream(new MemoryStream(Resources.DSDA_DOOM_WAD.Value)).ToArray();

			// Getting sum of wad sizes for the accurate calculation of the invisible heap
			int totalWadSize = _dsdaWadFileData.Length;
			foreach (var wadFile in _wadFiles)
			{
				totalWadSize += wadFile.FileData.Length;
			}
			int totalWadSizeKb = (totalWadSize / 1024) + 1;
			if (!string.IsNullOrEmpty(_iwadName))
			{
				totalWadSizeKb += _iwadData.Length / 1024;
			}
			Console.WriteLine($"Reserving {totalWadSizeKb}kb for WAD file memory");

			Point resolution;
			int multiplier = 1;
			var aspectIndex = (int)_settings.InternalAspect;
			var resolutionIndex = _settings.ScaleFactor - 1;
			var resolutions = _resolutions[aspectIndex];

			if (resolutionIndex < resolutions.Length)
			{
				resolution = resolutions[resolutionIndex];
			}
			else
			{
				multiplier = _settings.ScaleFactor - resolutions.Length + 1;
				resolution = resolutions[^1];
			}

			BufferWidth = resolution.X * multiplier;
			BufferHeight = resolution.Y * multiplier;
			VirtualHeight = _settings.InternalAspect == AspectRatio.Native
				? BufferWidth * 3 / 4
				: BufferHeight;

			_configFile = Encoding.ASCII.GetBytes(
				$"screen_resolution \"{BufferWidth}x{BufferHeight}\"\n"
				// we need the core to treat native resolution as 4:3 aspect,
				// that ensures FOV is correct on higher resolutions
				+ $"render_aspect {(int)(_settings.InternalAspect == AspectRatio.Native
					? AspectRatio._4by3
					: _settings.InternalAspect)}\n"
				+ $"render_wipescreen {(_syncSettings.RenderWipescreen ? 1 : 0)}\n"
				+ "render_stretch_hud 1\n" // patch_stretch_doom_format
				+ "uncapped_framerate 0\n"
				+ "dsda_show_level_splits 0\n" // TODO
			);

			if (!PlayerPresent(_syncSettings, _settings.DisplayPlayer))
			{
				_settings.DisplayPlayer = 0;
				for (var player = 0; player <= 4 && _settings.DisplayPlayer == 0; player++)
				{
					if (PlayerPresent(_syncSettings, player))
					{
						_settings.DisplayPlayer = player;
					}
				}
			}

			_randomCallback = pr_class =>
			{
				foreach (var cb in RandomCallbacks) cb(pr_class);
			};

			_elf = new WaterboxHost(new WaterboxOptions
			{
				Path = PathUtils.DllDirectoryPath,
				Filename = "dsda.wbx",
				SbrkHeapSizeKB = 64 * 1024, // This core loads quite a bunch of things on global mem -- reserve enough memory
				SealedHeapSizeKB = 4 * 1024,
				InvisibleHeapSizeKB = (uint)totalWadSizeKb + 4 * 1024, // Make sure there's enough space for the wads
				PlainHeapSizeKB = 4 * 1024,
				MmapHeapSizeKB = 2 * 1024 * 1024, // Allow the game to malloc quite a lot of objects to support those big wads
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			try
			{
				var callingConventionAdapter = CallingConventionAdapters.MakeWaterbox(
				[
					_loadCallback, _randomCallback, _errorCallback
				], _elf);

				using (_elf.EnterExit())
				{
					_core = BizInvoker.GetInvoker<LibDSDA>(_elf, _elf, callingConventionAdapter);

					// Adding dsda-doom wad file
					_core.dsda_add_wad_file(_dsdaWadFileName, _dsdaWadFileData.Length, _loadCallback);

					// Adding IWAD file
					_gameMode = _core.dsda_add_wad_file(_iwadName, _iwadData.Length, _loadCallback);
					if (_gameMode is LibDSDA.GameMode.Fail)
					{
						throw new ArgumentException(
							$"Could not load IWAD file: '{_iwadName}'",
							paramName: nameof(lp));
					}

					// Adding PWAD file(s)
					foreach (var wadFile in _pwadFiles)
					{
						_gameMode = _core.dsda_add_wad_file(wadFile.RomPath, wadFile.RomData.Length, _loadCallback);
						if (_gameMode is LibDSDA.GameMode.Fail)
						{
							throw new ArgumentException(
								$"Could not load PWAD file: '{wadFile.RomPath}'",
								paramName: nameof(lp));
						}
					}

					_elf.AddReadonlyFile(_configFile, "dsda-doom.cfg");

					var initSettings = _syncSettings.GetNativeSettings();
					initSettings.DisplayPlayer = _settings.DisplayPlayer - 1;
					CreateArguments(initSettings);

					_core.dsda_set_error_callback(_errorCallback);

					var initResult = _core.dsda_init(ref initSettings, _args.Count, _args.ToArray());
					if (!initResult)
					{
						throw new ArgumentException(
							$"{nameof(_core.dsda_init)}() failed",
							paramName: nameof(lp));
					}

					VsyncNumerator = 35;
					VsyncDenominator = 1;

					// db stores md5 for detection but it's nice to show both to user
					RomDetails = lp.Game.Name +
						$"\r\n\r\nIWAD: {Path.GetFileName(_iwadName.SubstringAfter('|'))}" +
						$"\r\n{SHA1Checksum.ComputePrefixedHex(_iwadData)}" +
						$"\r\n{MD5Checksum.ComputePrefixedHex(_iwadData)}";

					if (_pwadFiles.Count > 0)
					{
						SortedList<string> hashes = [ ];

						foreach (var file in _pwadFiles)
						{
							var md5Hash = MD5Checksum.ComputePrefixedHex(file.RomData);
							hashes.Add(md5Hash);
							RomDetails += $"\r\n\r\nPWAD: {GetFullName(file)}" +
								$"\r\n{SHA1Checksum.ComputePrefixedHex(file.RomData)}" +
								$"\r\n{md5Hash}";
						}

						lp.Game.Hash = MD5Checksum.ComputeDigestHex(Encoding.ASCII.GetBytes(string.Concat(hashes)));
					}

					_elf.Seal();
				}

				// we have to set render info after the core is sealed to ensure
				// states don't depend on its initial value
				InitVideo();

				// Registering memory domains
				SetupMemoryDomains();

				if (lp.Game[nameof(ControllerType.Doom)])
				{
					_syncSettings.InputFormat = ControllerType.Doom;
				}
				else if (lp.Game[nameof(ControllerType.Heretic)])
				{
					_syncSettings.InputFormat = ControllerType.Heretic;
				}
				else if (lp.Game[nameof(ControllerType.Hexen)])
				{
					_syncSettings.InputFormat = ControllerType.Hexen;
				}

				ControllerDefinition = CreateControllerDefinition(_syncSettings);

				_core.dsda_set_random_callback(RandomCallbacks.Count > 0 ? _randomCallback : null);
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		private void CreateArguments(LibDSDA.InitSettings initSettings)
		{
			_args =
			[
				"dsda", "-warp",
			];
			ConditionalArg(_syncSettings.InitialEpisode is not 0
				&& _gameMode != LibDSDA.GameMode.Commercial,
				$"{_syncSettings.InitialEpisode}");
			_args.Add($"{_syncSettings.InitialMap}");
			_args.AddRange([ "-skill",     $"{(int)_syncSettings.SkillLevel}" ]);
			_args.AddRange([ "-complevel", $"{(int)_syncSettings.CompatibilityLevel}" ]);
			_args.AddRange([ "-config", "dsda-doom.cfg" ]);
			ConditionalArg(_syncSettings.FastMonsters,    "-fast");
			ConditionalArg(_syncSettings.MonstersRespawn, "-respawn");
			ConditionalArg(_syncSettings.NoMonsters,      "-nomonsters");
			ConditionalArg(_syncSettings.PistolStart,     "-pistolstart");
			ConditionalArg(_syncSettings.CoopSpawns,      "-coop_spawns");
			ConditionalArg(_syncSettings.ChainEpisodes,   "-chain_episodes");
			ConditionalArg(_syncSettings.TurningResolution == TurningResolution.Longtics, "-longtics");
			ConditionalArg(_syncSettings.MultiplayerMode   == MultiplayerMode.Deathmatch, "-deathmatch");
			ConditionalArg(_syncSettings.MultiplayerMode   == MultiplayerMode.Altdeath,   "-altdeath");
			ConditionalArg(_syncSettings.Turbo > 0, $"-turbo {_syncSettings.Turbo}");
			ConditionalArg((initSettings.Player1Present
				+ initSettings.Player2Present
				+ initSettings.Player3Present
				+ initSettings.Player4Present) > 1, "-solo-net");

			if (_syncSettings.CompatibilityLevel >= CompatibilityLevel.Boom_202)
			{
				_args.AddRange([ "-rngseed", $"{_syncSettings.RNGSeed}" ]);
			}
		}

		private void ConditionalArg(bool condition, string setting)
		{
			if (condition)
			{
				_args.Add(setting);
			}
		}

		private string GetFullName(IRomAsset rom) => Path.GetFileName(rom.RomPath.SubstringAfter('|'));

		private static bool PlayerPresent(DoomSyncSettings syncSettings, int port) =>
			port switch
			{
				1 => syncSettings.Player1Present,
				2 => syncSettings.Player2Present,
				3 => syncSettings.Player3Present,
				4 => syncSettings.Player4Present,
				_ => false
			};

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		internal CoreComm Comm { get; }
		private readonly WaterboxHost _elf;
		private readonly LibDSDA _core;
		private readonly LibDSDA.load_archive_cb _loadCallback;
		private readonly byte[] _dsdaWadFileData;
		private readonly byte[] _configFile;
		private readonly int[] _runSpeeds = [ 25, 50 ];
		private readonly int[] _strafeSpeeds = [ 24, 40 ];
		private readonly int[] _turnSpeeds = [ 640, 1280, 320 ];
		private readonly string _dsdaWadFileName = "dsda-doom.wad";

		// these are IWADs in terms of contents but they have "PWAD" in header
		// source ports including upstream have special hacks to load them like IWADs
		private readonly List<string> _fakePwads = [ "chex.wad", "rekkrsa.wad" ];

		// order must match AspectRatio values since they're used as index
		private readonly Point[][] _resolutions =
		[
			// we want to support 1x widescreen so that internal scale is universal,
			// but lowest widescreen multiple of native height (corrected or not) is 1280x720.
			// it doesn't divide nicely so we have to use
			// artificial lowres replacements that aren't exactly 16:9 or 16:10.
			// but for 426x240 the core will also stretch the status bar
			// because it's just below some threshold in its aspect heuristics.
			// so we add 2 pixels to it (to keep it even), while 426x256 already works fine
			[ new(320, 200) ],
			[ new(428, 240), new(854, 480), new(1280, 720) ],
			[ new(426, 256), new(854, 512), new(1280, 768) ],
			[ new(320, 240) ],
		];

		private string _iwadName = "";
		private byte[] _iwadData = Array.Empty<byte>();
		private int[] _turnHeld = [ 0, 0, 0, 0 ];
		private int _turnCarry; // Chocolate Doom mouse behaviour (enabled in upstream by default)
		private bool _lastGammaInput;
		private List<string> _args;
		private List<IRomAsset> _wadFiles;
		private List<IRomAsset> _pwadFiles;
		private LibDSDA.GameMode _gameMode;
		private LibDSDA.random_cb _randomCallback;
		private LibDSDA.error_cb _errorCallback;

		public List<Action<int>> RandomCallbacks = [ ];
		public string RomDetails { get; } // IRomInfo

		private void ErrorCallback(string error)
		{
			throw new InternalCoreException($"\n\n{error}\n");
		}

		/// <summary>
		/// core callback for file loading
		/// </summary>
		/// <param name="filename">string identifying file to be loaded</param>
		/// <param name="buffer">buffer to load file to</param>
		/// <param name="maxsize">maximum length buffer can hold</param>
		/// <returns>actual size loaded, or 0 on failure</returns>
		private int LoadCallback(string filename, IntPtr buffer, int maxsize)
		{
			byte[] srcdata = null;

			if (buffer == IntPtr.Zero)
			{
				Console.WriteLine($"Couldn't satisfy firmware request {filename} because buffer == NULL");
				return 0;
			}

			if (filename == _dsdaWadFileName)
			{
				if (_dsdaWadFileData == null)
				{
					Console.WriteLine("Could not read from 'dsda-doom.wad'. File must be missing from the Resources folder.");
					return 0;
				}
				srcdata = _dsdaWadFileData;
			}

			if (filename == _iwadName)
			{
				if (_iwadData == null)
				{
					Console.WriteLine($"Could not read from WAD file '{_iwadName}'");
					return 0;
				}
				srcdata = _iwadData;
			}

			foreach (var wadFile in _wadFiles)
			{
				if (filename == wadFile.RomPath)
				{
					if (wadFile.FileData == null)
					{
						Console.WriteLine($"Could not read from WAD file '{filename}'");
						return 0;
					}
					srcdata = wadFile.FileData;
				}
			}

			if (srcdata != null)
			{
				if (srcdata.Length > maxsize)
				{
					Console.WriteLine($"Couldn't satisfy firmware request {filename} because {srcdata.Length} > {maxsize}");
					return 0;
				}
				else
				{
					Console.WriteLine($"Copying Data from {srcdata} to {buffer}. Size: {srcdata.Length}");
					Marshal.Copy(srcdata, 0, buffer, srcdata.Length);
					Console.WriteLine($"Firmware request {filename} satisfied at size {srcdata.Length}");
					return srcdata.Length;
				}
			}
			else
			{
				throw new InvalidOperationException($"Unknown error processing file '{filename}'");
			}
		}
	}
}
