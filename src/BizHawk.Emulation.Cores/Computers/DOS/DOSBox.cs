using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	[PortedCore(
		name: CoreNames.DOSBox,
		author: "Jonathan Campbell et al.",
		portedVersion: "2025.02.01 (324193b)",
		portedUrl: "https://github.com/joncampbell123/dosbox-x",
		isReleased: false)]
	public sealed partial class DOSBox : WaterboxCore
	{
		private static readonly Configuration DefaultConfig = new Configuration
		{
			SystemId = VSystemID.Raw.DOS,
			MaxSamples = 8 * 1024,
			DefaultWidth = LibDOSBox.VGA_MAX_WIDTH,
			DefaultHeight = LibDOSBox.VGA_MAX_HEIGHT,
			MaxWidth = LibDOSBox.SVGA_MAX_WIDTH,
			MaxHeight = LibDOSBox.SVGA_MAX_HEIGHT,
			DefaultFpsNumerator = LibDOSBox.DEFAULT_FRAMERATE_NUMERATOR_DOS,
			DefaultFpsDenominator = LibDOSBox.DEFAULT_FRAMERATE_DENOMINATOR_DOS
		};

		private LibDOSBox _libDOSBox;
		private readonly List<IRomAsset> _romAssets;
		private readonly List<IDiscAsset> _discAssets;
		private const int _messageDuration = 4;

		// Drive management variables
		private List<IRomAsset> _floppyDiskImageFiles = new List<IRomAsset>();
		private IRomAsset _hardDiskImageFile = null;
		private uint _hardDiskImageFileSize = 0;
		private int _floppyDiskCount = 0;
		private int _currentFloppyDisk = 0;
		private int _currentCDROM = 0;
		private bool _disposed;

		private string GetFullName(IRomAsset rom) => Path.GetFileName(rom.RomPath.SubstringAfter('|'));
		private string GetFullName(IDiscAsset disk) => Path.GetFileName(disk.DiscData.Name.SubstringAfter('|'));

		// CD Handling logic
		private List<string> _cdRomFileNames = new List<string>();
		private Dictionary<string, DiscSectorReader> _cdRomFileToReaderMap = new Dictionary<string, DiscSectorReader>();
		private readonly LibDOSBox.CDReadCallback _CDReadCallback;

		public override int VirtualWidth => BufferHeight * 4 / 3;

		// Image selection / swapping variables

		[CoreConstructor(VSystemID.Raw.DOS)]
		public DOSBox(CoreLoadParameters<object, SyncSettings> lp)
			: base(lp.Comm, DefaultConfig)
		{
			_romAssets = lp.Roms;
			_discAssets = lp.Discs;
			_syncSettings = lp.SyncSettings ?? new();
			_serviceProvider.Unregister<ISaveRam>();

			DriveLightEnabled = false;
			ControllerDefinition = CreateControllerDefinition(_syncSettings, _romAssets.Count, _discAssets.Count);

			// Parsing input files
			var ConfigFiles = new List<IRomAsset>();

			// Parsing rom files
			foreach (var file in _romAssets)
			{
				switch (Path.GetExtension(file.RomPath))
				{
					case ".ima" or ".img" or ".xdf" or ".dmf" or ".fdd" or ".fdi" or ".nfd" or ".d88":
						_floppyDiskImageFiles.Add(file);
						break;

					case ".hdd":
						_hardDiskImageFile = file;
						break;

					case ".conf":
						ConfigFiles.Add(file);
						break;

					default:
						throw new Exception($"Unrecognized input file provided: '{file.RomPath}'");
				}
			}

			// Getting size of hard disk to mount. Using the one provided or, if not, the one specified in the pre-formatted setting
			_hardDiskImageFileSize = _hardDiskImageFile != null ? (uint) _hardDiskImageFile.FileData.Length : (uint) _syncSettings.FormattedHardDisk;

			// Calculating hard disk size in kb
			uint hardDiskImageFileSizeKb = _hardDiskImageFileSize / 1024u;

			Console.WriteLine(_hardDiskImageFileSize is 0
				? "Running without a hard disk drive mounted."
				: $"Using {hardDiskImageFileSizeKb} kb of memory to host the writable hard disk drive.");

			_CDReadCallback = CDRead;
			_libDOSBox = PreInit<LibDOSBox>(new WaterboxOptions
			{
				Filename = "dosbox.wbx",
				SbrkHeapSizeKB = 4 * 1024,
				SealedHeapSizeKB = 1024,
				InvisibleHeapSizeKB = 1024,
				PlainHeapSizeKB = 1024,
				MmapHeapSizeKB = (384 * 1024) + hardDiskImageFileSizeKb,
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[] { _CDReadCallback });


			////// CD Loading Logic Start
			_libDOSBox.SetCdCallbacks(_CDReadCallback);

			// Processing each disc
			int curDiscIndex = 0;
			for (var discIdx = 0; discIdx < _discAssets.Count; discIdx++)
			{
				// Creating file name to pass to dosbox
				var cdRomFileName = $"{FileNames.CD}{discIdx}.cdrom";

				// Getting disc data structure
				var CDDataStruct = GetCDDataStruct(_discAssets[discIdx].DiscData);
				Console.WriteLine($"[CD] Adding Disc {discIdx}: '{GetFullName(_discAssets[discIdx])}' as '{cdRomFileName}' with sector count: {CDDataStruct.End}, track count: {CDDataStruct.Last}.");

				// Adding file name to list
				_cdRomFileNames.Add(cdRomFileName);

				// Creating reader
				var discSectorReader = new DiscSectorReader(_discAssets[discIdx].DiscData);

				// Adding reader to map
				_cdRomFileToReaderMap[cdRomFileName] = discSectorReader;

				// Passing CD Data to the core
				_libDOSBox.PushCDData(curDiscIndex, CDDataStruct.End, CDDataStruct.Last);

				// Passing track data to the core
				for (var trackIdx = 0; trackIdx < CDDataStruct.Last; trackIdx++) _libDOSBox.PushTrackData(curDiscIndex, trackIdx, CDDataStruct.Tracks[trackIdx]);
			}
			////// CD Loading Logic End

			// Getting base config file
			var configString = Encoding.UTF8.GetString(Resources.DOSBOX_CONF_BASE.Value);
			configString += "\n";

			// Getting selected machine preset config file
			configString += Encoding.UTF8.GetString(_syncSettings.ConfigurationPreset switch
			{
				ConfigurationPreset._1981_IBM_5150 => Resources.DOSBOX_CONF_1981_IBM_5150.Value,
				ConfigurationPreset._1983_IBM_5160 => Resources.DOSBOX_CONF_1983_IBM_5160.Value,
				ConfigurationPreset._1986_IBM_5162 => Resources.DOSBOX_CONF_1986_IBM_5162.Value,
				ConfigurationPreset._1987_IBM_PS2_25 => Resources.DOSBOX_CONF_1987_IBM_PS2_25.Value,
				ConfigurationPreset._1990_IBM_PS2_25_286 => Resources.DOSBOX_CONF_1990_IBM_PS2_25_286.Value,
				ConfigurationPreset._1991_IBM_PS2_25_386 => Resources.DOSBOX_CONF_1991_IBM_PS2_25_386.Value,
				ConfigurationPreset._1993_IBM_PS2_53_SLC2_486 => Resources.DOSBOX_CONF_1993_IBM_PS2_53_SLC2_486.Value,
				ConfigurationPreset._1994_IBM_PS2_76i_SLC2_486 => Resources.DOSBOX_CONF_1994_IBM_PS2_76i_SLC2_486.Value,
				ConfigurationPreset._1997_IBM_APTIVA_2140 => Resources.DOSBOX_CONF_1997_IBM_APTIVA_2140.Value,
				ConfigurationPreset._1999_IBM_THINKPAD_240 => Resources.DOSBOX_CONF_1999_IBM_THINKPAD_240.Value,
				_ => [ ]
			});
			configString += "\n";

			// Adding joystick configuration
			configString += "[joystick]\n";
			if (_syncSettings.EnableJoystick1 || _syncSettings.EnableJoystick2) configString += "joysticktype = 2axis\n";
			else configString += "joysticktype = none\n";

			// Adding PC Speaker
			configString += "[speaker]\n";
			if (_syncSettings.PCSpeaker != PCSpeaker.Auto) configString += $"pcspeaker = {_syncSettings.PCSpeaker}\n";
			configString += "\n";

			// Adding sound blaser configuration
			configString += "[sblaster]\n";
			if (_syncSettings.SoundBlasterModel != SoundBlasterModel.Auto) configString += $"sbtype = {_syncSettings.SoundBlasterModel}\n";
			if (_syncSettings.SoundBlasterIRQ != -1) configString += $"irq = {_syncSettings.SoundBlasterIRQ}\n";
			configString += "\n";

			// Adding memory size configuration
			configString += "[dosbox]\n";
			if (_syncSettings.RAMSize != -1)
			{
				if (_syncSettings.RAMSize > MAX_MEMORY_SIZE_MB) throw new Exception($"Requested memory size ({_syncSettings.RAMSize}mb) exceeds maximum allowed ({MAX_MEMORY_SIZE_MB}mb) by this core.");
				configString += $"memsize = {_syncSettings.RAMSize}\n";
			}
			configString += "\n";

			// Adding autoexec line
			configString += "[autoexec]\n";
			configString += "@echo off\n";

			////// Floppy disks: Mounting and appending mounting lines
			string floppyMountLine = "imgmount a ";
			foreach (var file in _floppyDiskImageFiles)
			{
				string floppyNewName = $"{FileNames.FD}{_floppyDiskCount}{Path.GetExtension(file.RomPath)}";
				_exe.AddReadonlyFile(file.FileData, floppyNewName);
				floppyMountLine += floppyNewName + " ";
				_floppyDiskCount++;
			}
			if (_floppyDiskCount > 0) configString += floppyMountLine + "\n";

			////// CD-ROMs: Mounting and appending mounting lines
			string cdromMountLine = "imgmount d ";
			foreach (var file in _cdRomFileNames) cdromMountLine += file + " ";
			if (_cdRomFileNames.Count > 0) configString += cdromMountLine + "\n";

			//// Hard Disk mounting
			if (_hardDiskImageFileSize > 0)
			{
				byte[] HDDImageData = [ ];

				// If formatted disk selected
				if (_syncSettings.FormattedHardDisk != HardDiskOptions.None)
				{
					var HDDImageFile = _syncSettings.FormattedHardDisk switch
					{
						HardDiskOptions.FAT16_21MB => Resources.DOSBOX_HDD_IMAGE_FAT16_21MB.Value,
						HardDiskOptions.FAT16_41MB => Resources.DOSBOX_HDD_IMAGE_FAT16_41MB.Value,
						HardDiskOptions.FAT16_241MB => Resources.DOSBOX_HDD_IMAGE_FAT16_241MB.Value,
						HardDiskOptions.FAT16_504MB => Resources.DOSBOX_HDD_IMAGE_FAT16_504MB.Value,
						HardDiskOptions.FAT16_2014MB => Resources.DOSBOX_HDD_IMAGE_FAT16_2014MB.Value,
						_ => Resources.DOSBOX_HDD_IMAGE_FAT16_21MB.Value
					};

					HDDImageData = Zstd.DecompressZstdStream(new MemoryStream(HDDImageFile)).ToArray();
				}

				// If provided as ROM file
				if (_hardDiskImageFile != null) HDDImageData = _hardDiskImageFile.FileData;

				// Adding HDD to the core
				_exe.AddReadonlyFile(HDDImageData, FileNames.HDD);
				configString += "imgmount c " + FileNames.HDD + ".img\n";
			}

			//// CPU (core) configuration
			configString += "[cpu]\n";
			if (_syncSettings.CPUCycles != -1) configString += $"cycles = {_syncSettings.CPUCycles}";
			if (_syncSettings.CPUType != CPUType.Auto)
			{
				var cputype = Enum.GetName(typeof(CPUType), _syncSettings.CPUType)!.Replace("C", "");
				configString += $"cputype = {cputype}\n";
			}

			//// DOSBox-x configuration
			configString += "[dosbox]\n";
			if (_syncSettings.VideoCardType != VideoCardType.Auto)
			{
				var vgatype = Enum.GetName(typeof(VideoCardType), _syncSettings.VideoCardType)!.Replace('P', '+');
				configString += $"machine = {vgatype}\n";
			}

			/////////////// Configuration End: Adding single config file to the wbx

			// Reconverting config to byte array
			IEnumerable<byte> configData = Encoding.UTF8.GetBytes(configString);

			// Adding EOL
			configString += "@echo on\n";
			configString += "\n";

			/////// Appending any additional user-provided config files
			foreach (var file in ConfigFiles)
			{
				// Forcing a new line
				configData = configData.Concat("\n"u8.ToArray());
				configData = configData.Concat(file.FileData);
			}

			_exe.AddReadonlyFile(configData.ToArray(), FileNames.Config);
			Console.WriteLine($"Configuration: {System.Text.Encoding.Default.GetString(configData.ToArray())}");

			////////////// Initializing Core
			if (!_libDOSBox.Init(new LibDOSBox.InitSettings()
			{
				Joystick1Enabled = _syncSettings.EnableJoystick1 ? 1 : 0,
				Joystick2Enabled = _syncSettings.EnableJoystick2 ? 1 : 0,
				HardDiskDriveSize = _hardDiskImageFileSize,
			}))
			{
				throw new InvalidOperationException("Core rejected the rom!");
			}

			// Setting framerate, if forced; otherwise, use the default.
			// The default is necessary because DOSBox does not populate framerate value on init. Only after the first frame run
			if (_syncSettings.forceFPSNumerator > 0 && _syncSettings.forceFPSDenominator > 0)
				UpdateFramerate(_syncSettings.forceFPSNumerator, _syncSettings.forceFPSDenominator);
			else
				UpdateFramerate(LibDOSBox.DEFAULT_FRAMERATE_NUMERATOR_DOS, LibDOSBox.DEFAULT_FRAMERATE_DENOMINATOR_DOS);


			PostInit();

			DriveLightEnabled = false;
			if (_syncSettings.FormattedHardDisk != HardDiskOptions.None) DriveLightEnabled = true;
			if (_floppyDiskCount > 0) DriveLightEnabled = true;
			if (_cdRomFileNames.Count > 0) DriveLightEnabled = true;
		}

		public static LibDOSBox.CDData GetCDDataStruct(Disc cd)
		{
			var ret = new LibDOSBox.CDData();
			var ses = cd.Session1;
			var ntrack = ses.InformationTrackCount;

			for (var i = 0; i < LibDOSBox.CD_MAX_TRACKS; i++)
			{
				ret.Tracks[i] = new();
				ret.Tracks[i].Offset = 0;
				ret.Tracks[i].LoopEnabled = 0;
				ret.Tracks[i].LoopOffset = 0;

				if (i < ntrack)
				{
					ret.Tracks[i].Start = ses.Tracks[i + 1].LBA;
					ret.Tracks[i].End = ses.Tracks[i + 2].LBA;
					ret.Tracks[i].Mode = ses.Tracks[i + 1].Mode;
					if (i == ntrack - 1)
					{
						ret.End = ret.Tracks[i].End;
						ret.Last = ntrack;
					}
				}
				else
				{
					ret.Tracks[i].Start = 0;
					ret.Tracks[i].End = 0;
					ret.Tracks[i].Mode = 0;
				}
			}

			return ret;
		}

		private void CDRead(string cdRomName, int lba, IntPtr dest, int sectorSize)
		{
			// Console.WriteLine($"Reading from {cdRomName} : {lba} : {sectorSize}");

			if (!_cdRomFileToReaderMap.TryGetValue(cdRomName, out var cdRomReader)) throw new InvalidOperationException($"Unrecognized CD File with name: {cdRomName}");

			byte[] sectorBuffer = new byte[4096];
			switch (sectorSize)
			{
				case 2048:
					cdRomReader.ReadLBA_2048(lba, sectorBuffer, 0);
					Marshal.Copy(sectorBuffer, 0, dest, 2048);
					break;
				case 2352:
					cdRomReader.ReadLBA_2352(lba, sectorBuffer, 0);
					Marshal.Copy(sectorBuffer, 0, dest, 2352);
					break;
				case 2448:
					cdRomReader.ReadLBA_2448(lba, sectorBuffer, 0);
					Marshal.Copy(sectorBuffer, 0, dest, 2448);
					break;
				default:
					throw new InvalidOperationException($"Unsupported CD sector size: {sectorSize}");

			}
			DriveLightOn = true;
		}

		// These variables prevent buttons from acting too fast on consecutive frames
		private bool _isPrevFloppyDiskPressed = false;
		private bool _isNextFloppyDiskPressed = false;
		private bool _isSwapFloppyDiskPressed = false;

		private bool _isPrevCDROMPressed = false;
		private bool _isNextCDROMPressed = false;
		private bool _isSwapCDROMPressed = false;

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			DriveLightOn = false;
			var fi = new LibDOSBox.FrameInfo();

			// Setting joystick inputs
			if (_syncSettings.EnableJoystick1)
			{
				fi.Joystick1.Up = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Up}") ? 1 : 0;
				fi.Joystick1.Down = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Down}") ? 1 : 0;
				fi.Joystick1.Left = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Left}") ? 1 : 0;
				fi.Joystick1.Right = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Right}") ? 1 : 0;
				fi.Joystick1.Button1 = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Button1}") ? 1 : 0;
				fi.Joystick1.Button2 = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Button2}") ? 1 : 0;
			}

			if (_syncSettings.EnableJoystick2)
			{
				fi.Joystick2.Up = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Up}") ? 1 : 0;
				fi.Joystick2.Down = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Down}") ? 1 : 0;
				fi.Joystick2.Left = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Left}") ? 1 : 0;
				fi.Joystick2.Right = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Right}") ? 1 : 0;
				fi.Joystick2.Button1 = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Button1}") ? 1 : 0;
				fi.Joystick2.Button2 = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Button2}") ? 1 : 0;
			}

			// Setting mouse inputs
			if (_syncSettings.EnableMouse)
			{
				// Getting new mouse state values
				DOSBox.MouseState mouseState = new()
				{
					PosX = controller.AxisValue($"{Inputs.Mouse} {MouseInputs.PosX}"),
					PosY = controller.AxisValue($"{Inputs.Mouse} {MouseInputs.PosY}"),
					LeftButtonHeld = controller.IsPressed($"{Inputs.Mouse} {MouseInputs.LeftButton}"),
					MiddleButtonHeld = controller.IsPressed($"{Inputs.Mouse} {MouseInputs.MiddleButton}"),
					RightButtonHeld = controller.IsPressed($"{Inputs.Mouse} {MouseInputs.RightButton}"),
				};

				var deltaX = controller.AxisValue($"{Inputs.Mouse} {MouseInputs.SpeedX}");
				var deltaY = controller.AxisValue($"{Inputs.Mouse} {MouseInputs.SpeedY}");
				fi.Mouse.PosX = mouseState.PosX;
				fi.Mouse.PosY = mouseState.PosY;
				fi.Mouse.DeltaX = deltaX != 0 ? deltaX : fi.Mouse.PosX - _lastMouseState.PosX;
				fi.Mouse.DeltaY = deltaY != 0 ? deltaY : fi.Mouse.PosY - _lastMouseState.PosY;

				// Button pressed criteria:
				// If the input is made in this frame and the button is not held from before
				fi.Mouse.LeftButtonPressed = mouseState.LeftButtonHeld && !_lastMouseState.LeftButtonHeld ? 1 : 0;
				fi.Mouse.MiddleButtonPressed = mouseState.MiddleButtonHeld && !_lastMouseState.MiddleButtonHeld ? 1 : 0;
				fi.Mouse.RightButtonPressed = mouseState.RightButtonHeld && !_lastMouseState.RightButtonHeld ? 1 : 0;

				// Button released criteria:
				// If the input is not pressed in this frame and the button is held from before
				fi.Mouse.LeftButtonReleased = !mouseState.LeftButtonHeld && _lastMouseState.LeftButtonHeld ? 1 : 0;
				fi.Mouse.MiddleButtonReleased = !mouseState.MiddleButtonHeld && _lastMouseState.MiddleButtonHeld ? 1 : 0;
				fi.Mouse.RightButtonReleased = !mouseState.RightButtonHeld && _lastMouseState.RightButtonHeld ? 1 : 0;
				fi.Mouse.Sensitivity = _syncSettings.MouseSensitivity;

				// Updating mouse state
				_lastMouseState = mouseState;
			}

			// Only manage multiple floppy disks if more than 1 were provided
			fi.DriveActions.InsertFloppyDisk = -1; // -1 indicates no disk change this frame
			if (_floppyDiskCount >= 2)
			{
				if (!_isPrevFloppyDiskPressed && controller.IsPressed(Inputs.PrevFloppyDisk))
				{
					_currentFloppyDisk = _currentFloppyDisk == 0 ? _floppyDiskCount - 1 : _currentFloppyDisk - 1;
					CoreComm.Notify($"Selected {FileNames.FD}{_currentFloppyDisk}: {GetFullName(_floppyDiskImageFiles[_currentFloppyDisk])}", _messageDuration);
				}

				if (!_isNextFloppyDiskPressed && controller.IsPressed(Inputs.NextFloppyDisk))
				{
					_currentFloppyDisk = (_currentFloppyDisk + 1) % _floppyDiskCount;
					CoreComm.Notify($"Selected {FileNames.FD}{_currentFloppyDisk}: {GetFullName(_floppyDiskImageFiles[_currentFloppyDisk])}", _messageDuration);
				}

				// Processing floppy disk swapping
				if (!_isSwapFloppyDiskPressed && controller.IsPressed(Inputs.SwapFloppyDisk))
				{
					fi.DriveActions.InsertFloppyDisk = _currentFloppyDisk;
					CoreComm.Notify($"Insterted {FileNames.FD}{_currentFloppyDisk}: {GetFullName(_floppyDiskImageFiles[_currentFloppyDisk])} into drive A:", _messageDuration);
				}
			}

			// Only manage multiple CDROMs if more than 1 were provided
			fi.DriveActions.InsertCDROM = -1; // -1 indicates no disk change this frame
			if (_cdRomFileNames.Count >= 2)
			{
				if (!_isPrevCDROMPressed && controller.IsPressed(Inputs.PrevCDROM))
				{
					_currentCDROM = _currentCDROM == 0 ? _cdRomFileNames.Count - 1 : _currentCDROM - 1;
					CoreComm.Notify($"Selected {FileNames.CD}{_currentCDROM}: {GetFullName(_discAssets[_currentCDROM])}", _messageDuration);
				}

				if (!_isNextCDROMPressed && controller.IsPressed(Inputs.NextCDROM))
				{
					_currentCDROM = (_currentCDROM + 1) % _cdRomFileNames.Count;
					CoreComm.Notify($"Selected {FileNames.CD}{_currentCDROM}: {GetFullName(_discAssets[_currentCDROM])}", _messageDuration);
				}

				// Processing CDROM disk swapping
				if (!_isSwapCDROMPressed && controller.IsPressed(Inputs.SwapCDROM))
				{
					fi.DriveActions.InsertCDROM = _currentCDROM;
					CoreComm.Notify($"Insterted {FileNames.CD}{_currentCDROM}: {GetFullName(_discAssets[_currentCDROM])} into drive D:", _messageDuration);
				}
			}

			// These variables prevent buttons from acting too fast on consecutive frames
			_isPrevFloppyDiskPressed = controller.IsPressed(Inputs.PrevFloppyDisk);
			_isNextFloppyDiskPressed = controller.IsPressed(Inputs.NextFloppyDisk);
			_isSwapFloppyDiskPressed = controller.IsPressed(Inputs.SwapFloppyDisk);

			_isPrevCDROMPressed = controller.IsPressed(Inputs.PrevCDROM);
			_isNextCDROMPressed = controller.IsPressed(Inputs.NextCDROM);
			_isSwapCDROMPressed = controller.IsPressed(Inputs.SwapCDROM);

			// Processing keyboard inputs
			foreach (var (name, key) in _keyboardMap)
			{
				if (controller.IsPressed(name))
				{
					unsafe
					{
						fi.Keys.Buffer[(int) key] = 1;
					}
				}
			}

			// Specifying frame rate
			fi.framerateNumerator = VsyncNumerator;
			fi.framerateDenominator = VsyncDenominator;

			return fi;
		}

		private void UpdateFramerate(int numerator, int denominator)
		{
			VsyncNumerator = numerator;
			VsyncDenominator = denominator;

			var newRefreshRate = (double) VsyncNumerator / VsyncDenominator;
			Console.WriteLine($"[Frame {Frame}] Refresh Rate set to: " +
				$"{VsyncNumerator} / " +
				$"{VsyncDenominator} = " +
				$"{newRefreshRate.ToString(CultureInfo.InvariantCulture)} Hz");
		}

		protected override void FrameAdvancePost()
		{
			DriveLightOn = _libDOSBox.GetDriveActivityFlag();

			// Checking refresh rate base on the reported refresh rate updates
			var currentRefreshRateNumerator = VsyncNumerator;
			var currentRefreshRateDenominator = VsyncDenominator;

			var newRefreshRateNumerator = _libDOSBox.GetRefreshRateNumerator();
			var newRefreshRateDenominator = _libDOSBox.GetRefreshRateDenominator();

			// Change BK's own framerate if the values changed. Only if not forced. And only if the provided values are valid (they might be zero initially until the core sets it).
			if (currentRefreshRateNumerator != newRefreshRateNumerator || currentRefreshRateDenominator != newRefreshRateDenominator)
				if (_syncSettings.forceFPSNumerator == 0 || _syncSettings.forceFPSDenominator == 0)
					if (newRefreshRateNumerator > 0 && newRefreshRateDenominator > 0)
						UpdateFramerate(newRefreshRateNumerator, newRefreshRateDenominator);
		}

		protected override void SaveStateBinaryInternal(BinaryWriter writer)
		{
			writer.Write(_currentFloppyDisk);
			writer.Write(_currentCDROM);

			writer.Write(_lastMouseState.PosX);
			writer.Write(_lastMouseState.PosY);
			writer.Write(_lastMouseState.LeftButtonHeld);
			writer.Write(_lastMouseState.MiddleButtonHeld);
			writer.Write(_lastMouseState.RightButtonHeld);

			// Storing current refresh rate
			writer.Write(VsyncNumerator);
			writer.Write(VsyncDenominator);
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_currentFloppyDisk = reader.ReadInt32();
			_currentCDROM = reader.ReadInt32();

			_lastMouseState.PosX = reader.ReadInt32();
			_lastMouseState.PosY = reader.ReadInt32();
			_lastMouseState.LeftButtonHeld = reader.ReadBoolean();
			_lastMouseState.MiddleButtonHeld = reader.ReadBoolean();
			_lastMouseState.RightButtonHeld = reader.ReadBoolean();

			// Restoring refresh rate
			var newVsyncNumerator = reader.ReadInt32();
			var newVsyncDenominator = reader.ReadInt32();

			// Updating it now, if different
			if (newVsyncNumerator != VsyncNumerator || newVsyncDenominator != VsyncDenominator)
				UpdateFramerate(newVsyncNumerator, newVsyncDenominator);
		}

		private static class FileNames
		{
			public const string Config = "dosbox-x.conf";
			public const string FD = "FloppyDisk";
			public const string CD = "CompactDisk";
			public const string HDD = "HardDiskDrive";
		}

		public bool HasValidHDD()
		{
			return _hardDiskImageFileSize > 0;
		}

		public byte[] GetHDDContents()
		{
			if (_hardDiskImageFileSize == 0) throw new Exception($"Trying to export HDD contents but no HDD was defined");

			var hddSize = _libDOSBox.GetHDDSize();
			byte[] hddArray = new byte[hddSize];
			_libDOSBox.GetHDDData(hddArray);
			return hddArray;
		}

		public override void Dispose()
		{
			if (_disposed) return;
			_disposed = true;

			if (_discAssets is not null)
			{
				foreach (var discAsset in _discAssets)
				{
					discAsset.DiscData.Dispose();
				}
			}

			base.Dispose();
		}
	}
}
