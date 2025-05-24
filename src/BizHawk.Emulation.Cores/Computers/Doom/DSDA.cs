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
		author: "The DSDA Team",
		portedVersion: "0.28.2 (fe0dfa0)",
		portedUrl: "https://github.com/kraflab/dsda-doom")]
	[ServiceNotApplicable(typeof(ISaveRam))]
	public partial class DSDA : IRomInfo
	{
		[CoreConstructor(VSystemID.Raw.Doom)]
		public DSDA(CoreLoadParameters<DoomSettings, DoomSyncSettings> lp)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			_finalSyncSettings = _syncSettings = lp.SyncSettings ?? new DoomSyncSettings();
			_settings = lp.Settings ?? new DoomSettings();
			_loadCallback = LoadCallback;
			ControllerDefinition = CreateControllerDefinition(_syncSettings);

			// Gathering information for the rest of the wads
			_wadFiles = lp.Roms;

			// Checking for correct IWAD configuration
			_pwadFiles = new();
			bool foundIWAD = false;
			string IWADName = "";
			foreach (var wadFile in _wadFiles)
			{
				bool recognized = false;
				if (wadFile.RomData is [ (byte) 'I', (byte) 'W', (byte) 'A', (byte) 'D', .. ])
				{
					// Check not more than one IWAD is provided
					if (foundIWAD) throw new Exception($"More than one IWAD provided. Trying to load '{wadFile.RomPath}', but IWAD '{IWADName}' was already provided");
					IWADName = wadFile.RomPath;
					_iwadFile = wadFile;
					foundIWAD = true;
					recognized = true;
				}
				else if (wadFile.RomData is [ (byte) 'P', (byte) 'W', (byte) 'A', (byte) 'D', .. ])
				{
					_pwadFiles.Add(wadFile);
					recognized = true;
				}
				if (!recognized) throw new Exception($"Unrecognized WAD provided: '{wadFile.RomPath}' has non-standard header.");
			}

			// Check at least one IWAD was provided
			if (!foundIWAD) throw new Exception($"No IWAD was provided");

			// Getting dsda-doom.wad -- required by DSDA
			_dsdaWadFileData = Zstd.DecompressZstdStream(new MemoryStream(Resources.DSDA_DOOM_WAD.Value)).ToArray();

			// Getting sum of wad sizes for the accurate calculation of the invisible heap
			uint totalWadSize = (uint)_dsdaWadFileData.Length;
			foreach (var wadFile in _wadFiles) totalWadSize += (uint) wadFile.FileData.Length;
			uint totalWadSizeKb = (totalWadSize / 1024) + 1;
			Console.WriteLine($"Reserving {totalWadSizeKb}kb for WAD file memory");

			_configFile = Encoding.ASCII.GetBytes(
				$"screen_resolution \"{
					_nativeResolution.X *     _settings.ScaleFactor}x{
					_nativeResolution.Y *     _settings.ScaleFactor}\"\n"
				+ $"render_wipescreen {      (_syncSettings.RenderWipescreen ? 1 : 0)}\n"
				+ "boom_translucent_sprites 0\n"
				+ "render_aspect 3\n" // 4:3, controls FOV on higher resolutions (see SetRatio() in the core)
				+ "render_stretch_hud 0\n"
				+ "uncapped_framerate 0\n"
				+ "dsda_show_level_splits 0\n"
			);

			_elf = new WaterboxHost(new WaterboxOptions
			{
				Path = PathUtils.DllDirectoryPath,
				Filename = "dsda.wbx",
				SbrkHeapSizeKB = 64 * 1024, // This core loads quite a bunch of things on global mem -- reserve enough memory
				SealedHeapSizeKB = 4 * 1024,
				InvisibleHeapSizeKB = totalWadSizeKb + 4 * 1024, // Make sure there's enough space for the wads
				PlainHeapSizeKB = 4 * 1024,
				MmapHeapSizeKB = 1024 * 1024, // Allow the game to malloc quite a lot of objects to support those big wads
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			try
			{
				var callingConventionAdapter = CallingConventionAdapters.MakeWaterbox(
				[
					_loadCallback
				], _elf);

				using (_elf.EnterExit())
				{
					_core = BizInvoker.GetInvoker<LibDSDA>(_elf, _elf, callingConventionAdapter);

					// Adding dsda-doom wad file
					_core.dsda_add_wad_file(_dsdaWadFileName, _dsdaWadFileData.Length, _loadCallback);

					// Adding IWAD file
					var loadWadResult = _core.dsda_add_wad_file(_iwadFile.RomPath, _iwadFile.RomData.Length, _loadCallback);
					if (loadWadResult is 0) throw new Exception($"Could not load WAD file: '{_iwadFile.RomPath}'");
					_gameMode = (LibDSDA.GameMode)loadWadResult;

					// Adding PWAD file(s)
					foreach (var wadFile in _pwadFiles)
					{
						loadWadResult = _core.dsda_add_wad_file(wadFile.RomPath, wadFile.RomData.Length, _loadCallback);
						if (loadWadResult is 0) throw new Exception($"Could not load WAD file: '{wadFile.RomPath}'");
						_gameMode = (LibDSDA.GameMode)loadWadResult;
					}

					_elf.AddReadonlyFile(_configFile, "dsda-doom.cfg");

					var initSettings = _syncSettings.GetNativeSettings(lp.Game);
					CreateArguments(initSettings);
					var initResult = _core.dsda_init(ref initSettings, _args.Count, _args.ToArray());
					if (!initResult) throw new Exception($"{nameof(_core.dsda_init)}() failed");

					VsyncNumerator = 35;
					VsyncDenominator = 1;

					RomDetails += $"IWAD: {GetFullName(_iwadFile)}" +
						$"\r\n{SHA1Checksum.ComputePrefixedHex(_iwadFile.RomData)}" +
						$"\r\n{MD5Checksum.ComputePrefixedHex(_iwadFile.RomData)}";

					foreach (var file in _pwadFiles)
					{
						RomDetails += $"\r\n\r\nPWAD: {GetFullName(file)}" +
							$"\r\n{SHA1Checksum.ComputePrefixedHex(file.RomData)}" +
							$"\r\n{MD5Checksum.ComputePrefixedHex(file.RomData)}";
					}

					_elf.Seal();
				}

				// pull the default video size from the core
				UpdateVideo();

				// Registering memory domains
				SetupMemoryDomains();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		private void CreateArguments(LibDSDA.InitSettings initSettings)
		{
			_args = new List<string>
			{
				"dsda",
			};

			_args.Add("-warp");
			ConditionalArg(_syncSettings.InitialEpisode is not 0
				&& _gameMode != LibDSDA.GameMode.Commercial,
				$"{_syncSettings.InitialEpisode}");
			_args.Add($"{_syncSettings.InitialMap}");
			_args.AddRange([ "-skill",     $"{(int)_syncSettings.SkillLevel}" ]);
			_args.AddRange([ "-complevel", $"{(int)_syncSettings.CompatibilityLevel}" ]);
			_args.AddRange([ "-config", "dsda-doom.cfg" ]);
			ConditionalArg(!_syncSettings.StrictMode,     "-tas");
			ConditionalArg(_syncSettings.FastMonsters,    "-fast");
			ConditionalArg(_syncSettings.MonstersRespawn, "-respawn");
			ConditionalArg(_syncSettings.NoMonsters,      "-nomonsters");
			ConditionalArg(_syncSettings.PistolStart,     "-pistolstart");
			ConditionalArg(_syncSettings.ChainEpisodes,   "-chain_episodes");
			ConditionalArg(_syncSettings.TurningResolution == TurningResolution.Longtics, "-longtics");
			ConditionalArg(_syncSettings.MultiplayerMode   == MultiplayerMode.Deathmatch, "-deathmatch");
			ConditionalArg(_syncSettings.MultiplayerMode   == MultiplayerMode.Altdeath,   "-altdeath");
			ConditionalArg(_syncSettings.Turbo > 0, $"-turbo {_syncSettings.Turbo}");
			ConditionalArg((initSettings.Player1Present + initSettings.Player2Present + initSettings.Player3Present + initSettings.Player4Present) > 1, "-solo-net");
		}

		private void ConditionalArg(bool condition, string setting)
		{
			if (condition)
			{
				_args.Add(setting);
			}
		}

		private string GetFullName(IRomAsset rom) => Path.GetFileName(rom.RomPath.SubstringAfter('|'));

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly WaterboxHost _elf;
		private readonly LibDSDA _core;
		private readonly LibDSDA.load_archive_cb _loadCallback;
		private readonly Point _nativeResolution = new(320, 200);
		private readonly int[] _runSpeeds = [ 25, 50 ];
		private readonly int[] _strafeSpeeds = [ 24, 40 ];
		private readonly int[] _turnSpeeds = [ 640, 1280, 320 ];
		private readonly string _dsdaWadFileName = "dsda-doom.wad";
		private readonly byte[] _dsdaWadFileData;
		private readonly byte[] _configFile;
		private int[] _turnHeld = [ 0, 0, 0, 0 ];
		private int _turnCarry = 0; // Chocolate Doom mouse behaviour (enabled in upstream by default)
		private bool _lastGammaInput = false;
		private List<string> _args;
		private IRomAsset _iwadFile;
		private List<IRomAsset> _wadFiles;
		private List<IRomAsset> _pwadFiles;
		private LibDSDA.GameMode _gameMode;
		public string RomDetails { get; } // IRomInfo

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
