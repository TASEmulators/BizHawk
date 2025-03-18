using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BizHawk.Common;
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
	public partial class DOSBox : WaterboxCore
	{
		private static readonly Configuration DefaultConfig = new Configuration
		{
			SystemId = VSystemID.Raw.DOS,
			MaxSamples = 8 * 1024,
			DefaultWidth = LibDOSBox.VGA_MAX_WIDTH,
			DefaultHeight = LibDOSBox.VGA_MAX_HEIGHT,
			MaxWidth = LibDOSBox.SVGA_MAX_WIDTH,
			MaxHeight = LibDOSBox.SVGA_MAX_HEIGHT,
			DefaultFpsNumerator = LibDOSBox.VIDEO_NUMERATOR_DOS,
			DefaultFpsDenominator = LibDOSBox.VIDEO_DENOMINATOR_DOS
		};

		private readonly List<IRomAsset> _roms;
		private readonly List<IDiscAsset> _discAssets;

		// Drive management variables
		private bool _nextFloppyDiskPressed = false;
		private bool _nextCDROMPressed = false;
		private List<IRomAsset> _floppyDiskImageFiles = new List<IRomAsset>();
		private int _floppyDiskCount = 0;
		private int _currentFloppyDisk = 0;
		private int _currentCDROM = 0;
		private string GetFullName(IRomAsset rom) => rom.Game.Name + rom.Extension;

		public override int VirtualWidth => BufferHeight * 4 / 3;
		private LibDOSBox _libDOSBox;

		// Image selection / swapping variables

		[CoreConstructor(VSystemID.Raw.DOS)]
		public DOSBox(CoreLoadParameters<object, SyncSettings> lp)
			: base(lp.Comm, DefaultConfig)
		{
			_roms = lp.Roms;
			_discAssets = lp.Discs;
			_syncSettings = lp.SyncSettings ?? new();

			VsyncNumerator = (int) _syncSettings.FPSNumerator;
			VsyncDenominator = (int) _syncSettings.FPSDenominator;
			DriveLightEnabled = false;
			ControllerDefinition = CreateControllerDefinition(_syncSettings);

			// Parsing input files
			var ConfigFiles = new List<IRomAsset>();

			// Parsing rom files
			foreach (var file in _roms)
			{
				var ext = Path.GetExtension(file.RomPath);
				bool recognized = false;

				// Checking for supported floppy disk extensions
				if (ext is ".ima" or ".img" or ".xdf" or ".dmf" or ".fdd" or ".fdi" or ".nfd" or ".d88")
				{
					_floppyDiskImageFiles.Add(file);
					recognized = true;
				}
				// Checking for DOSBox-x config files
				else if (ext is ".conf")
				{
					ConfigFiles.Add(file);
					recognized = true;
				}

				if (!recognized) throw new Exception($"Unrecognized input file provided: '{file.RomPath}'");
			}

			var writableHDDImageFileSize = (ulong) _syncSettings.WriteableHardDisk;

			_CDReadCallback = CDRead;
			_libDOSBox = PreInit<LibDOSBox>(new WaterboxOptions
			{
				Filename = "dosbox.wbx",
				SbrkHeapSizeKB = 32 * 1024,
				SealedHeapSizeKB = 32 * 1024,
				InvisibleHeapSizeKB = 1024,
				PlainHeapSizeKB = 32 * 1024,
				MmapHeapSizeKB = 256 * 1024 + (uint) (writableHDDImageFileSize / 1024ul),
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
				var cdRomFileName = _discAssets[discIdx].DiscName + ".cdrom";

				// Getting disc data structure
				var CDDataStruct = GetCDDataStruct(_discAssets[discIdx].DiscData);
				Console.WriteLine($"[CD] Adding Disc {discIdx}: '{_discAssets[discIdx].DiscName}' as '{cdRomFileName}' with sector count: {CDDataStruct.end}, track count: {CDDataStruct.last}.");

				// Adding file name to list
				_cdRomFileNames.Add(cdRomFileName);

				// Creating reader
				var discSectorReader = new DiscSectorReader(_discAssets[discIdx].DiscData);

				// Adding reader to map
				_cdRomFileToReaderMap[cdRomFileName] = discSectorReader;

				// Passing CD Data to the core
				_libDOSBox.pushCDData(curDiscIndex, CDDataStruct.end, CDDataStruct.last);

				// Passing track data to the core
				for (var trackIdx = 0; trackIdx < CDDataStruct.last; trackIdx++) _libDOSBox.pushTrackData(curDiscIndex, trackIdx, CDDataStruct.tracks[trackIdx]);
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
			if (_syncSettings.RAMSize != -1) configString += $"memsize = {_syncSettings.RAMSize}\n";
			configString += "\n";

			// Adding autoexec line
			configString += "[autoexec]\n";
			configString += "@echo off\n";

			////// Floppy disks: Mounting and appending mounting lines
			string floppyMountLine = "imgmount a ";
			foreach (var file in _floppyDiskImageFiles)
			{
				string floppyNewName = FileNames.FD + _floppyDiskCount.ToString() + Path.GetExtension(file.RomPath);
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
			if (_syncSettings.WriteableHardDisk != WriteableHardDiskOptions.None)
			{
				var writableHDDImageFile = _syncSettings.WriteableHardDisk switch
				{
					WriteableHardDiskOptions.FAT16_21Mb => Resources.DOSBOX_HDD_IMAGE_FAT16_21MB.Value,
					WriteableHardDiskOptions.FAT16_41Mb => Resources.DOSBOX_HDD_IMAGE_FAT16_41MB.Value,
					WriteableHardDiskOptions.FAT16_241Mb => Resources.DOSBOX_HDD_IMAGE_FAT16_241MB.Value,
					WriteableHardDiskOptions.FAT16_504Mb => Resources.DOSBOX_HDD_IMAGE_FAT16_504MB.Value,
					WriteableHardDiskOptions.FAT16_2014Mb => Resources.DOSBOX_HDD_IMAGE_FAT16_2014MB.Value,
					WriteableHardDiskOptions.FAT32_4091Mb => Resources.DOSBOX_HDD_IMAGE_FAT32_4091MB.Value,
					_ => Resources.DOSBOX_HDD_IMAGE_FAT16_21MB.Value
				};

				var writableHDDImageData = Zstd.DecompressZstdStream(new MemoryStream(writableHDDImageFile)).ToArray();
				_exe.AddReadonlyFile(writableHDDImageData, FileNames.WHD);
				configString += "imgmount c " + FileNames.WHD + ".img\n";
			}

			//// CPU (core) configuration
			configString += "[cpu]\n";
			if (_syncSettings.CPUCycles != -1) configString += $"cycles = {_syncSettings.CPUCycles}";


			//// DOSBox-x configuration
			configString += "[dosbox]\n";
			if (_syncSettings.MachineType != MachineType.Auto)
			{
				var machineName = Enum.GetName(typeof(MachineType), _syncSettings.MachineType)!.Replace('P', '+');
				configString += $"machine = {machineName}\n";
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

			_exe.AddReadonlyFile(configData.ToArray(), FileNames.DOSBOX_CONF);
			Console.WriteLine("Configuration: {0}", System.Text.Encoding.Default.GetString(configData.ToArray()));

			////////////// Initializing Core
			if (!_libDOSBox.Init(
				joystick1Enabled: _syncSettings.EnableJoystick1,
				joystick2Enabled: _syncSettings.EnableJoystick2,
				mouseEnabled: _syncSettings.EnableMouse,
				hardDiskDriveSize: writableHDDImageFileSize,
				fpsNumerator: _syncSettings.FPSNumerator,
				fpsDenominator: _syncSettings.FPSDenominator))
			{
				throw new InvalidOperationException("Core rejected the rom!");
			}

			PostInit();

			DriveLightEnabled = false;
			if (_syncSettings.WriteableHardDisk != WriteableHardDiskOptions.None) DriveLightEnabled = true;
			if (_floppyDiskCount > 0) DriveLightEnabled = true;
			if (_cdRomFileNames.Count > 0) DriveLightEnabled = true;
		}

		// CD Handling logic
		private List<string> _cdRomFileNames = new List<string>();
		private Dictionary<string, DiscSectorReader> _cdRomFileToReaderMap = new Dictionary<string, DiscSectorReader>();
		private readonly LibDOSBox.CDReadCallback _CDReadCallback;

		public static LibDOSBox.CDData GetCDDataStruct(Disc cd)
		{
			var ret = new LibDOSBox.CDData();
			var ses = cd.Session1;
			var ntrack = ses.InformationTrackCount;

			for (var i = 0; i < LibDOSBox.CD_MAX_TRACKS; i++)
			{
				ret.tracks[i] = new();
				ret.tracks[i].offset = 0;
				ret.tracks[i].loopEnabled = 0;
				ret.tracks[i].loopOffset = 0;

				if (i < ntrack)
				{
					ret.tracks[i].start = ses.Tracks[i + 1].LBA;
					ret.tracks[i].end = ses.Tracks[i + 2].LBA;
					ret.tracks[i].mode = ses.Tracks[i + 1].Mode;
					if (i == ntrack - 1)
					{
						ret.end = ret.tracks[i].end;
						ret.last = ntrack;
					}
				}
				else
				{
					ret.tracks[i].start = 0;
					ret.tracks[i].end = 0;
					ret.tracks[i].mode = 0;
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

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			DriveLightOn = false;
			var fi = new LibDOSBox.FrameInfo();

			// Setting joystick inputs
			if (_syncSettings.EnableJoystick1)
			{
				fi.joystick1.up      = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Up}") ? 1 : 0;
				fi.joystick1.down    = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Down}") ? 1 : 0;
				fi.joystick1.left    = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Left}") ? 1 : 0;
				fi.joystick1.right   = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Right}") ? 1 : 0;
				fi.joystick1.button1 = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Button1}") ? 1 : 0;
				fi.joystick1.button2 = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Button2}") ? 1 : 0;
			}

			if (_syncSettings.EnableJoystick2)
			{
                fi.joystick2.up      = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Up}") ? 1 : 0;
				fi.joystick2.down    = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Down}") ? 1 : 0;
				fi.joystick2.left    = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Left}") ? 1 : 0;
				fi.joystick2.right   = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Right}") ? 1 : 0;
				fi.joystick2.button1 = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Button1}") ? 1 : 0;
				fi.joystick2.button2 = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Button2}") ? 1 : 0;
			}

			// Setting mouse inputs
			if (_syncSettings.EnableMouse)
			{
				// 272 is minimal delta for RMouse on my machine, this will be obsolete when global sensitivity for RMouse is added and when it's bindable from GUI
				var deltaX = controller.AxisValue($"{Inputs.Mouse} {MouseInputs.SpeedX}") / 272;
				var deltaY = controller.AxisValue($"{Inputs.Mouse} {MouseInputs.SpeedY}") / 272;
				fi.mouse.posX = controller.AxisValue($"{Inputs.Mouse} {MouseInputs.PosX}");
				fi.mouse.posY = controller.AxisValue($"{Inputs.Mouse} { MouseInputs.PosY}");
				fi.mouse.dX = deltaX != 0 ? deltaX : fi.mouse.posX - _mouseState.posX;
				fi.mouse.dY = deltaY != 0 ? deltaY : fi.mouse.posY - _mouseState.posY;

				// Button pressed criteria:
				// If the input is made in this frame and the button is not held from before
				fi.mouse.leftButtonPressed = controller.IsPressed($"{Inputs.Mouse} {MouseInputs.LeftButton}") && !_mouseState.leftButtonHeld ? 1 : 0;
				fi.mouse.middleButtonPressed = controller.IsPressed($"{Inputs.Mouse} {MouseInputs.MiddleButton}") && !_mouseState.middleButtonHeld ? 1 : 0;
				fi.mouse.rightButtonPressed = controller.IsPressed($"{Inputs.Mouse} {MouseInputs.RightButton}") && !_mouseState.rightButtonHeld ? 1 : 0;

				// Button released criteria:
				// If the input is not pressed in this frame and the button is held from before
				fi.mouse.leftButtonReleased = !controller.IsPressed($"{Inputs.Mouse} {MouseInputs.LeftButton}") && _mouseState.leftButtonHeld ? 1 : 0;
				fi.mouse.middleButtonReleased = !controller.IsPressed($"{Inputs.Mouse} {MouseInputs.MiddleButton}") && _mouseState.middleButtonHeld ? 1 : 0;
				fi.mouse.rightButtonReleased = !controller.IsPressed($"{Inputs.Mouse} {MouseInputs.RightButton}") && _mouseState.rightButtonHeld ? 1 : 0;
				fi.mouse.sensitivity = _syncSettings.MouseSensitivity;

				// Getting new mouse state values
				var nextState = new DOSBox.MouseState();
				nextState.posX = fi.mouse.posX;
				nextState.posY = fi.mouse.posY;
				if (fi.mouse.leftButtonPressed > 0) nextState.leftButtonHeld = true;
				if (fi.mouse.middleButtonPressed > 0) nextState.middleButtonHeld = true;
				if (fi.mouse.rightButtonPressed > 0) nextState.rightButtonHeld = true;
				if (fi.mouse.leftButtonReleased > 0) nextState.leftButtonHeld = false;
				if (fi.mouse.middleButtonReleased > 0) nextState.middleButtonHeld = false;
				if (fi.mouse.rightButtonReleased > 0) nextState.rightButtonHeld = false;

				// Updating mouse state
				_mouseState = nextState;
            }

            // Processing floppy disks swaps
            fi.driveActions.insertFloppyDisk = -1;
			if (_floppyDiskCount > 1 && controller.IsPressed(Inputs.NextFloppyDisk) && !_nextFloppyDiskPressed)
			{
				_currentFloppyDisk = (_currentFloppyDisk + 1) % _floppyDiskCount;
				fi.driveActions.insertFloppyDisk = _currentFloppyDisk;
				CoreComm.Notify($"Insterted FloppyDisk {_currentFloppyDisk}: {GetFullName(_floppyDiskImageFiles[_currentFloppyDisk])} into drive A:", null);
			}
			_nextFloppyDiskPressed = controller.IsPressed(Inputs.NextFloppyDisk);

			// Processing CDROM swaps
			fi.driveActions.insertCDROM = -1;
			if (_cdRomFileNames.Count > 1 && controller.IsPressed(Inputs.NextCDROM) && !_nextCDROMPressed)
			{
				_currentCDROM = (_currentCDROM + 1) % _cdRomFileNames.Count;
				fi.driveActions.insertCDROM = _currentCDROM;
				CoreComm.Notify($"Insterted CDROM {_currentCDROM}: {_cdRomFileNames[_currentCDROM]} into drive D:", null);
			}
			_nextCDROMPressed = controller.IsPressed(Inputs.NextCDROM);

			// Processing keyboard inputs
			foreach (var (name, key) in _keyboardMap)
			{
				if (controller.IsPressed(name))
				{
					unsafe
					{
						fi.Keys.Buffer[(int)key] = 1;
					}
				}
			}

			return fi;
		}

		protected override void FrameAdvancePost()
		{
			DriveLightOn = _libDOSBox.getDriveActivityFlag();
		}

		protected override void SaveStateBinaryInternal(BinaryWriter writer)
		{
			writer.Write(_nextFloppyDiskPressed);
			writer.Write(_nextCDROMPressed);
			writer.Write(_currentFloppyDisk);
			writer.Write(_currentCDROM);

			writer.Write(_mouseState.posX);
			writer.Write(_mouseState.posY);
			writer.Write(_mouseState.leftButtonHeld);
			writer.Write(_mouseState.middleButtonHeld);
			writer.Write(_mouseState.rightButtonHeld);
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_nextFloppyDiskPressed = reader.ReadBoolean();
			_nextCDROMPressed = reader.ReadBoolean();
			_currentFloppyDisk = reader.ReadInt32();
			_currentCDROM = reader.ReadInt32();

			_mouseState.posX = reader.ReadInt32();
			_mouseState.posY = reader.ReadInt32();
			_mouseState.leftButtonHeld = reader.ReadBoolean();
			_mouseState.middleButtonHeld = reader.ReadBoolean();
			_mouseState.rightButtonHeld = reader.ReadBoolean();
		}

		private static class FileNames
		{
			public const string DOSBOX_CONF = "dosbox-x.conf";
			public const string FD = "FloppyDisk";
			public const string CD = "CompactDisk";
			public const string WHD = "__WritableHardDiskDrive";
		}
	}
}