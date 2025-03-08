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
		private const int _messageDuration = 4;

		// Drive management variables
		private bool _nextFloppyDiskPressed = false;
		private bool _nextCDROMPressed = false;
		private List<IRomAsset> _floppyDiskImageFiles = new List<IRomAsset>();
		private List<IRomAsset> _CDROMDiskImageFiles = new List<IRomAsset>();
		private int _floppyDiskCount = 0;
		private int _CDROMCount = 0;
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
				// Checking for supported CD-ROM extensions
				else if (ext is ".dosbox-iso" // Temporary to circumvent BK's detection of isos as discs (not roms)
					or ".dosbox-cue" // Must be accompanied by a bin file immediately after in the rom list
					or ".dosbox-bin" // Must be accompanied by a cue file immediately before in the rom list
					or ".dosbox-mdf"
					or ".dosbox-chf")
				{
					Console.WriteLine("Added CDROM Image");
					_CDROMDiskImageFiles.Add(file);
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

			_libDOSBox = PreInit<LibDOSBox>(new WaterboxOptions
			{
				Filename = "dosbox.wbx",
				SbrkHeapSizeKB = 4 * 1024,
				SealedHeapSizeKB = 1024,
				InvisibleHeapSizeKB = 1024,
				PlainHeapSizeKB = 1024,
				MmapHeapSizeKB = 256 * 1024 + (uint) (writableHDDImageFileSize / 1024ul),
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[] { });

			// Getting base config file
			var configString = Encoding.UTF8.GetString(Resources.DOSBOX_CONF_BASE.Value);
			configString += "\n";

			// Getting selected machine preset config file
			configString += Encoding.UTF8.GetString(_syncSettings.ConfigurationPreset switch
			{
				ConfigurationPreset._1981_IBM_5150                    => Resources.DOSBOX_CONF_1981_IBM_5150.Value,
				ConfigurationPreset._1983_IBM_5160                    => Resources.DOSBOX_CONF_1983_IBM_5160.Value,
				ConfigurationPreset._1986_IBM_5162                    => Resources.DOSBOX_CONF_1986_IBM_5162.Value,
				ConfigurationPreset._1987_IBM_PS2_25                  => Resources.DOSBOX_CONF_1987_IBM_PS2_25.Value,
				ConfigurationPreset._1990_IBM_PS2_25_286              => Resources.DOSBOX_CONF_1990_IBM_PS2_25_286.Value,
				ConfigurationPreset._1991_IBM_PS2_25_386              => Resources.DOSBOX_CONF_1991_IBM_PS2_25_386.Value,
				ConfigurationPreset._1993_IBM_PS2_53_SLC2_486         => Resources.DOSBOX_CONF_1993_IBM_PS2_53_SLC2_486.Value,
				ConfigurationPreset._1994_IBM_PS2_76i_SLC2_486        => Resources.DOSBOX_CONF_1994_IBM_PS2_76i_SLC2_486.Value,
				ConfigurationPreset._1997_IBM_APTIVA_2140             => Resources.DOSBOX_CONF_1997_IBM_APTIVA_2140.Value,
				ConfigurationPreset._1999_IBM_THINKPAD_240            => Resources.DOSBOX_CONF_1999_IBM_THINKPAD_240.Value,
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
			foreach (var file in _CDROMDiskImageFiles)
			{
				string typeExtension = Path.GetExtension(file.RomPath);
				string newTypeExtension = "";

				// Hack to avoid these CD-ROM images from being considered IDiscAssets
				if (typeExtension == ".dosbox-iso") newTypeExtension = ".iso";
				if (typeExtension == ".dosbox-bin") newTypeExtension = ".bin";
				if (typeExtension == ".dosbox-cue") newTypeExtension = ".cue";
				if (typeExtension == ".dosbox-mdf") newTypeExtension = ".mdf";
				if (typeExtension == ".dosbox-chf") newTypeExtension = ".chf";

				string cdromFileName = Path.GetFileName(file.RomPath);
				string cdromNewFileName = cdromFileName.Replace(typeExtension, newTypeExtension);

				Console.WriteLine($"Adding {cdromNewFileName} as read only file into the core");
				_exe.AddReadonlyFile(file.FileData, cdromNewFileName);

				// Important: .cue and .bin CDROM images must be placed together in the .xml. This saves us from developing a matching engine here
				if (newTypeExtension != ".cue") _CDROMCount++; // .cue CDROM extensions only work with an accompanying .bin, so don't increment CDROM count until that happens
				if (newTypeExtension != ".bin") cdromMountLine += cdromNewFileName + " "; // .bin CDROM extensions only work with an accompanying .cue, so don't add it to DOSBOX as a new CDROM to mount
			}
			if (_CDROMCount > 0) configString += cdromMountLine + "\n";

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
			if (!_libDOSBox.Init(_syncSettings.EnableJoystick1, _syncSettings.EnableJoystick2, _syncSettings.EnableMouse, writableHDDImageFileSize, _syncSettings.FPSNumerator, _syncSettings.FPSDenominator))
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

			// Setting joystick inputs
			fi.joystick1.up      = _syncSettings.EnableJoystick1 && controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Up}") ? 1 : 0;
			fi.joystick1.down    = _syncSettings.EnableJoystick1 && controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Down}") ? 1 : 0;
			fi.joystick1.left    = _syncSettings.EnableJoystick1 && controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Left}") ? 1 : 0;
			fi.joystick1.right   = _syncSettings.EnableJoystick1 && controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Right}") ? 1 : 0;
			fi.joystick1.button1 = _syncSettings.EnableJoystick1 && controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Button1}") ? 1 : 0;
			fi.joystick1.button2 = _syncSettings.EnableJoystick1 && controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Button2}") ? 1 : 0;
			fi.joystick2.up      = _syncSettings.EnableJoystick2 && controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Up}") ? 1 : 0;
			fi.joystick2.down    = _syncSettings.EnableJoystick2 && controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Down}") ? 1 : 0;
			fi.joystick2.left    = _syncSettings.EnableJoystick2 && controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Left}") ? 1 : 0;
			fi.joystick2.right   = _syncSettings.EnableJoystick2 && controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Right}") ? 1 : 0;
			fi.joystick2.button1 = _syncSettings.EnableJoystick2 && controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Button1}") ? 1 : 0;
			fi.joystick2.button2 = _syncSettings.EnableJoystick2 && controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Button2}") ? 1 : 0;

			// Setting mouse inputs
			if (_syncSettings.EnableMouse)
			{
				// 272 is minimal delta for RMouse on my machine, this will be obsolete when global sensitivity for RMouse is added and when it's bindable from GUI
				var deltaX = controller.AxisValue($"{Inputs.Mouse} {MouseInputs.XDelta}") / 272;
				var deltaY = controller.AxisValue($"{Inputs.Mouse} {MouseInputs.YDelta}") / 272;
				fi.mouse.posX = controller.AxisValue($"{Inputs.Mouse} {MouseInputs.XAxis}");
				fi.mouse.posY = controller.AxisValue($"{Inputs.Mouse} { MouseInputs.YAxis}");
				fi.mouse.dX = deltaX != 0 ? deltaX : fi.mouse.posX - _mouseState.posX;
				fi.mouse.dY = deltaY != 0 ? deltaY : fi.mouse.posY - _mouseState.posY;

				// Button pressed criteria:
				// If the input is made in this frame and the button is not held from before
				fi.mouse.leftButtonPressed    = controller.IsPressed($"{Inputs.Mouse} {MouseInputs.LeftButton}") && !_mouseState.leftButtonHeld ? 1 : 0;
				fi.mouse.middleButtonPressed  = controller.IsPressed($"{Inputs.Mouse} {MouseInputs.MiddleButton}") && !_mouseState.middleButtonHeld ? 1 : 0;
				fi.mouse.rightButtonPressed   = controller.IsPressed($"{Inputs.Mouse} {MouseInputs.RightButton}") && !_mouseState.rightButtonHeld ? 1 : 0;

				// Button released criteria:
				// If the input is not pressed in this frame and the button is held from before
				fi.mouse.leftButtonReleased   = !controller.IsPressed($"{Inputs.Mouse} {MouseInputs.LeftButton}") && _mouseState.leftButtonHeld ? 1 : 0;
				fi.mouse.middleButtonReleased = !controller.IsPressed($"{Inputs.Mouse} {MouseInputs.MiddleButton}") && _mouseState.middleButtonHeld ? 1 : 0;
				fi.mouse.rightButtonReleased  = !controller.IsPressed($"{Inputs.Mouse} {MouseInputs.RightButton}") && _mouseState.rightButtonHeld ? 1 : 0;
				fi.mouse.sensitivity = _syncSettings.MouseSensitivity;
			}

			// Updating mouse state
			_mouseState.posX = fi.mouse.posX;
			_mouseState.posY = fi.mouse.posY;
			if (fi.mouse.leftButtonPressed    > 0) _mouseState.leftButtonHeld = true;
			if (fi.mouse.middleButtonPressed  > 0) _mouseState.middleButtonHeld = true;
			if (fi.mouse.rightButtonPressed   > 0) _mouseState.rightButtonHeld = true;
			if (fi.mouse.leftButtonReleased   > 0) _mouseState.leftButtonHeld = false;
			if (fi.mouse.middleButtonReleased > 0) _mouseState.middleButtonHeld = false;
			if (fi.mouse.rightButtonReleased  > 0) _mouseState.rightButtonHeld = false;

			// Processing floppy disks swaps
			fi.driveActions.insertFloppyDisk = -1;
			if (_floppyDiskCount > 1)
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
			_nextFloppyDiskPressed = controller.IsPressed(Inputs.NextFloppyDisk);

			// Processing CDROM swaps
			fi.driveActions.insertCDROM = -1;
			if (_CDROMCount > 1)
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