using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	[PortedCore(
		name: CoreNames.DOSBox,
		author: "Jonathan Campbell et al.",
		portedVersion: "2025.02.01 (324193b)",
		portedUrl: "https://github.com/TASEmulators/dosbox-x",
		isReleased: true)]
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
			DefaultFpsNumerator = LibDOSBox.VIDEO_NUMERATOR_NTSC,
			DefaultFpsDenominator = LibDOSBox.VIDEO_DENOMINATOR_NTSC
		};

		private readonly List<IRomAsset> _roms;
		private const int _messageDuration = 4;

		// Drive management variables
		private bool _nextFloppyDiskPressed = false;
		private bool _nextCDROMPressed = false;
		private bool _nextHardDiskDrivePressed = false;
		private List<IRomAsset> _floppyDiskImageFiles = new List<IRomAsset>();
		private List<IRomAsset> _CDROMDiskImageFiles = new List<IRomAsset>();
		private int _floppyDiskCount = 0;
		private int _CDROMCount = 0;
		private int _currentFloppyDisk = 0;
		private int _currentCDROM = 0;
		private string GetFullName(IRomAsset rom) => rom.Game.Name + rom.Extension;

		public override int VirtualWidth => LibDOSBox.SVGA_MAX_WIDTH;
		private LibDOSBox _libDOSBox;

		// Image selection / swapping variables

		[CoreConstructor(VSystemID.Raw.DOS)]
		public DOSBox(CoreLoadParameters<object, SyncSettings> lp)
			: base(lp.Comm, DefaultConfig)
		{
			_roms = lp.Roms;
			_syncSettings = lp.SyncSettings ?? new();

			VsyncNumerator = LibDOSBox.VIDEO_NUMERATOR_PAL;
			VsyncDenominator = LibDOSBox.VIDEO_DENOMINATOR_PAL;
			DriveLightEnabled = false;
			ControllerDefinition = CreateControllerDefinition(_syncSettings);

			// Parsing input files
			var ConfigFiles = new List<IRomAsset>();

			// Parsing rom files
			foreach (var file in _roms)
			{
				bool recognized = false;

				// Checking for supported floppy disk extensions
				if (file.RomPath.EndsWith(".ima", StringComparison.OrdinalIgnoreCase) ||
					file.RomPath.EndsWith(".img", StringComparison.OrdinalIgnoreCase) || 
					file.RomPath.EndsWith(".xdf", StringComparison.OrdinalIgnoreCase) ||
					file.RomPath.EndsWith(".dmf", StringComparison.OrdinalIgnoreCase) ||
					file.RomPath.EndsWith(".fdd", StringComparison.OrdinalIgnoreCase) ||
					file.RomPath.EndsWith(".fdi", StringComparison.OrdinalIgnoreCase) ||
					file.RomPath.EndsWith(".nfd", StringComparison.OrdinalIgnoreCase) ||
					file.RomPath.EndsWith(".d88", StringComparison.OrdinalIgnoreCase))
				{
					_floppyDiskImageFiles.Add(file);
					recognized = true;
				}

				// Checking for supported CD-ROM extensions
				if (file.RomPath.EndsWith(".dosbox-iso", StringComparison.OrdinalIgnoreCase) || // Temporary to circumvent BK's detection of isos as discs (not roms)
					file.RomPath.EndsWith(".dosbox-cue", StringComparison.OrdinalIgnoreCase) || // Must be accompanied by a bin file
					file.RomPath.EndsWith(".dosbox-bin", StringComparison.OrdinalIgnoreCase) ||
					file.RomPath.EndsWith(".dosbox-mdf", StringComparison.OrdinalIgnoreCase) ||
					file.RomPath.EndsWith(".dosbox-chf", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("Added CDROM Image");
					_CDROMDiskImageFiles.Add(file);
					recognized = true;
				}

				// Checking for DOSBox-x config files
				if (file.RomPath.EndsWith(".conf", StringComparison.OrdinalIgnoreCase))
				{
					ConfigFiles.Add(file);
					recognized = true;
				}

				if (!recognized) throw new Exception($"Unrecognized input file provided: '{file.RomPath}'");
			}

			// These are the actual size in bytes for each hdd selection
			ulong writableHDDImageFileSize = _syncSettings.WriteableHardDisk switch
			{
				WriteableHardDiskOptions.FAT16_21Mb => 21411840,
				WriteableHardDiskOptions.FAT16_41Mb => 42823680,
				WriteableHardDiskOptions.FAT16_241Mb => 252370944,
				WriteableHardDiskOptions.FAT16_504Mb => 527966208,
				WriteableHardDiskOptions.FAT16_2014Mb => 2111864832,
				WriteableHardDiskOptions.FAT32_4091Mb => 4289725440,
				_ => 0
			};

			_libDOSBox = PreInit<LibDOSBox>(new WaterboxOptions
			{
				Filename = "dosbox.wbx",
				SbrkHeapSizeKB = 4 * 1024 * 32,
				SealedHeapSizeKB = 32 * 512,
				InvisibleHeapSizeKB = 32 * 512,
				PlainHeapSizeKB = 4 * 1024 * 32,
				MmapHeapSizeKB = 4 * 1024 * 32 + (uint) (writableHDDImageFileSize / 1024ul),
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[] { });

			// Getting base config file
			IEnumerable<byte> configData = [ ];
			switch (_syncSettings.ConfigurationPreset)
			{
				case ConfigurationPreset.Early80s: configData = new MemoryStream(Resources.DOSBOX_CONF_EARLY80S.Value).ToArray(); break;
				case ConfigurationPreset.Late80s: configData = new MemoryStream(Resources.DOSBOX_CONF_LATE80S.Value).ToArray(); break;
				case ConfigurationPreset.Early90s: configData = new MemoryStream(Resources.DOSBOX_CONF_EARLY90S.Value).ToArray(); break;
				case ConfigurationPreset.Mid90s: configData = new MemoryStream(Resources.DOSBOX_CONF_MID90S.Value).ToArray(); break;
				case ConfigurationPreset.Late90s: configData = new MemoryStream(Resources.DOSBOX_CONF_LATE90S.Value).ToArray(); break;
			}

			// Converting to string
			var configString = Encoding.UTF8.GetString(configData.ToArray());
			configString += "\n";

			// Adding joystick configuration
			configString += "[joystick]\n";
			if (_syncSettings.EnableJoystick1 || _syncSettings.EnableJoystick2) configString += "joysticktype = 2axis\n";
			else configString += "joysticktype = none\n";

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
			foreach (var file in _CDROMDiskImageFiles)
			{
				string typeExtension = Path.GetExtension(file.RomPath);

				// Hack to avoid these CD-ROM images from being considered IDiscAssets
				if (typeExtension == ".dosbox-iso") typeExtension = ".iso";
				if (typeExtension == ".dosbox-bin") typeExtension = ".bin";
				if (typeExtension == ".dosbox-cue") typeExtension = ".cue";
				if (typeExtension == ".dosbox-mdf") typeExtension = ".mdf";
				if (typeExtension == ".dosbox-chf") typeExtension = ".chf";
				string cdromNewName = FileNames.CD + _CDROMCount.ToString() + typeExtension;
				_exe.AddReadonlyFile(file.FileData, cdromNewName);
				cdromMountLine += cdromNewName + " ";
				_CDROMCount++;
			}
			if (_CDROMCount > 0) configString += cdromMountLine + "\n";

			//// Hard Disk mounting

			// Config file


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
			configData = Encoding.UTF8.GetBytes(configString);

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
			if (!_libDOSBox.Init(_syncSettings.EnableJoystick1, _syncSettings.EnableJoystick2, _syncSettings.EnableMouse, writableHDDImageFileSize))
				throw new InvalidOperationException("Core rejected the rom!");

			PostInit();

			DriveLightEnabled = false;
			if (_syncSettings.WriteableHardDisk != WriteableHardDiskOptions.None) DriveLightEnabled = true;
			if (_floppyDiskCount > 0) DriveLightEnabled = true;
			if (_CDROMCount > 0) DriveLightEnabled = true;
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			DriveLightOn = false;
			var fi = new LibDOSBox.FrameInfo();

			fi.driveActions.insertFloppyDisk = -1;
			fi.driveActions.insertCDROM = -1;

			// Setting joystick inputs
			fi.joystick1.up      = _syncSettings.EnableJoystick1 && controller.IsPressed("P1 " + Inputs.Joystick + " " + JoystickButtons.Up) ? 1 : 0;
			fi.joystick1.down    = _syncSettings.EnableJoystick1 && controller.IsPressed("P1 " + Inputs.Joystick + " " + JoystickButtons.Down) ? 1 : 0;
			fi.joystick1.left    = _syncSettings.EnableJoystick1 && controller.IsPressed("P1 " + Inputs.Joystick + " " + JoystickButtons.Left) ? 1 : 0;
			fi.joystick1.right   = _syncSettings.EnableJoystick1 && controller.IsPressed("P1 " + Inputs.Joystick + " " + JoystickButtons.Right) ? 1 : 0;
			fi.joystick1.button1 = _syncSettings.EnableJoystick1 && controller.IsPressed("P1 " + Inputs.Joystick + " " + JoystickButtons.Button1) ? 1 : 0;
			fi.joystick1.button2 = _syncSettings.EnableJoystick1 && controller.IsPressed("P1 " + Inputs.Joystick + " " + JoystickButtons.Button2) ? 1 : 0;

			fi.joystick2.up      = _syncSettings.EnableJoystick2 && controller.IsPressed("P2 " + Inputs.Joystick + " " + JoystickButtons.Up) ? 1 : 0;
			fi.joystick2.down    = _syncSettings.EnableJoystick2 && controller.IsPressed("P2 " + Inputs.Joystick + " " + JoystickButtons.Down) ? 1 : 0;
			fi.joystick2.left    = _syncSettings.EnableJoystick2 && controller.IsPressed("P2 " + Inputs.Joystick + " " + JoystickButtons.Left) ? 1 : 0;
			fi.joystick2.right   = _syncSettings.EnableJoystick2 && controller.IsPressed("P2 " + Inputs.Joystick + " " + JoystickButtons.Right) ? 1 : 0;
			fi.joystick2.button1 = _syncSettings.EnableJoystick2 && controller.IsPressed("P2 " + Inputs.Joystick + " " + JoystickButtons.Button1) ? 1 : 0;
			fi.joystick2.button2 = _syncSettings.EnableJoystick2 && controller.IsPressed("P2 " + Inputs.Joystick + " " + JoystickButtons.Button2) ? 1 : 0;

			// Setting mouse inputs
			fi.mouse.posX = _syncSettings.EnableMouse ? controller.AxisValue(Inputs.Mouse + " " + MouseInputs.XAxis) : 0;
			fi.mouse.posY = _syncSettings.EnableMouse ? controller.AxisValue(Inputs.Mouse + " " + MouseInputs.YAxis) : 0;
			fi.mouse.leftButton = _syncSettings.EnableMouse && controller.IsPressed(Inputs.Mouse + " " + MouseInputs.LeftButton) ? 1 : 0;
			fi.mouse.middleButton = _syncSettings.EnableMouse && controller.IsPressed(Inputs.Mouse + " " + MouseInputs.MiddleButton) ? 1 : 0;
			fi.mouse.rightButton = _syncSettings.EnableMouse && controller.IsPressed(Inputs.Mouse + " " + MouseInputs.RightButton) ? 1 : 0;

			if (_floppyDiskCount > 0)
			{
				if (controller.IsPressed(Inputs.NextFloppyDisk))
				{
					if (!_nextFloppyDiskPressed)
					{
						_currentFloppyDisk = (_currentFloppyDisk + 1) % _floppyDiskCount;
						fi.driveActions.insertFloppyDisk = _currentFloppyDisk;
						CoreComm.Notify($"Insterted FloppyDisk {_currentFloppyDisk}: {GetFullName(_floppyDiskImageFiles[_currentFloppyDisk])}  into drive A:", _messageDuration);
					}
				}
			}

			if (_CDROMCount > 0)
			{
				if (controller.IsPressed(Inputs.NextCDROM))
				{
					if (!_nextCDROMPressed)
					{
						_currentCDROM = (_currentCDROM + 1) % _CDROMCount;
						fi.driveActions.insertCDROM = _currentCDROM;
						CoreComm.Notify($"Insterted CDROM {_currentCDROM}: {GetFullName(_CDROMDiskImageFiles[_currentCDROM])}  into drive D:", _messageDuration);
					}
				}
			}

			_nextFloppyDiskPressed = controller.IsPressed(Inputs.NextFloppyDisk);
			_nextCDROMPressed = controller.IsPressed(Inputs.NextCDROM);

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
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_nextFloppyDiskPressed = reader.ReadBoolean();
			_nextCDROMPressed = reader.ReadBoolean();
			_currentFloppyDisk = reader.ReadInt32();
			_currentCDROM = reader.ReadInt32();
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