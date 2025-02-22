using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;
using static BizHawk.Emulation.Cores.Computers.DOS.LibDOSBox;

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
		
		private readonly LibWaterboxCore.EmptyCallback _ledCallback;
		private readonly List<IRomAsset> _roms;
		private const int _messageDuration = 4;
		private const int _driveNullOrEmpty = -1;

		// Drive management variables
		private bool _nextFloppyDiskPressed = false;
		private bool _nextCDROMPressed = false;
		private bool _nextHardDiskDrivePressed = false;
		private List<IRomAsset> _floppyDiskImageFiles = new List<IRomAsset>();
		private List<IRomAsset> _CDROMDiskImageFiles = new List<IRomAsset>();
		private List<IRomAsset> _hardDiskDriveImageFiles = new List<IRomAsset>();
		private int _floppyDiskCount = 0;
		private int _CDROMCount = 0;
		private int _hardDiskDriveCount = 0;
		private int _currentFloppyDisk = 0;
		private int _currentCDROM = 0;
		private int _currentHardDiskDrive = 0;

		private int _correctedWidth;
		private string _chipsetCompatible = "";
		private string GetFullName(IRomAsset rom) => rom.Game.Name + rom.Extension;

		public override int VirtualWidth => _correctedWidth;

		// Image selection / swapping variables


		private void LEDCallback()
		{
			DriveLightOn = true;
		}

		[CoreConstructor(VSystemID.Raw.DOS)]
		public DOSBox(CoreLoadParameters<object, SyncSettings> lp)
			: base(lp.Comm, DefaultConfig)
		{
			_roms = lp.Roms;
			_syncSettings = lp.SyncSettings ?? new();
			_syncSettings.FloppyDrives = Math.Min(LibDOSBox.MAX_FLOPPIES, _syncSettings.FloppyDrives);
			DeterministicEmulation = lp.DeterministicEmulationRequested || _syncSettings.FloppySpeed is FloppySpeed._100;
			var filesToRemove = new List<string>();

			DriveLightEnabled = false;

			UpdateVideoStandard(true);
			ControllerDefinition = CreateControllerDefinition(_syncSettings);
			_ledCallback = LEDCallback;

			// Parsing input files
			var ConfigFiles = new List<IRomAsset>();

			// Parsing rom files
			foreach (var file in _roms)
			{
				bool recognized = false;

				// Checking for supported floppy disk extensions
				if (file.RomPath.EndsWith(".ima", StringComparison.OrdinalIgnoreCase) ||
					// file.RomPath.EndsWith(".img", StringComparison.OrdinalIgnoreCase) || // Reserving img for hard disk drive images
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
				if (file.RomPath.EndsWith(".iso", StringComparison.OrdinalIgnoreCase) ||
					file.RomPath.EndsWith(".dosbox-cdrom", StringComparison.OrdinalIgnoreCase) || // Temporary to circumvent BK's detection of isos as discs (not roms)
					file.RomPath.EndsWith(".cue", StringComparison.OrdinalIgnoreCase) || // Must be accompanied by a bin file
					file.RomPath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
					file.RomPath.EndsWith(".mdf", StringComparison.OrdinalIgnoreCase) ||
					file.RomPath.EndsWith(".chf", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("Added CDROM Image");
					_CDROMDiskImageFiles.Add(file);
					recognized = true;
				}

				// Checking for supported Hard Disk Drive Image extensions
				if (file.RomPath.EndsWith(".img", StringComparison.OrdinalIgnoreCase) || // Only one with R/W capabilities
				    file.RomPath.EndsWith(".qcow2", StringComparison.OrdinalIgnoreCase) ||
					file.RomPath.EndsWith(".vhd", StringComparison.OrdinalIgnoreCase) ||
					file.RomPath.EndsWith(".nhd", StringComparison.OrdinalIgnoreCase) ||
					file.RomPath.EndsWith(".hdi", StringComparison.OrdinalIgnoreCase))
				{
					_hardDiskDriveImageFiles.Add(file);
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

			var dosbox = PreInit<LibDOSBox>(new WaterboxOptions
			{
				Filename = "dosbox.wbx",
				SbrkHeapSizeKB = 4 * 1024 * 32,
				SealedHeapSizeKB = 32 * 512,
				InvisibleHeapSizeKB = 32 * 512,
				PlainHeapSizeKB = 4 * 1024 * 32,
				MmapHeapSizeKB = 4 * 1024 * 32,
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[] { _ledCallback });

			// Getting base config file
			IEnumerable<byte> configData = Zstd.DecompressZstdStream(new MemoryStream(Resources.DOSBOX_BASE_CONF.Value)).ToArray();

			// Converting to string
			var configString = Encoding.UTF8.GetString(configData.ToArray());

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
				string cdromNewName = FileNames.CD + _CDROMCount.ToString() + (typeExtension == ".dosbox-cdrom" ? ".iso" : typeExtension);
				_exe.AddReadonlyFile(file.FileData, cdromNewName);
				cdromMountLine += cdromNewName + " ";
				_CDROMCount++;
			}
			if (_CDROMCount > 0) configString += cdromMountLine + "\n";

			// Reconverting config to byte array
			configData = Encoding.UTF8.GetBytes(configString);

			// Adding EOL
			configString += "@echo on\n";
			configString += "\n";

			// Appending any additionaluser-provided config files
			foreach (var file in ConfigFiles)
			{
				// Forcing a new line
				configData = configData.Concat("\n"u8.ToArray());
				configData = configData.Concat(file.FileData);
			}

			// Adding single config file to the wbx
			_exe.AddReadonlyFile(configData.ToArray(), FileNames.DOSBOX_CONF);
			 Console.WriteLine("Configuration: {0}", System.Text.Encoding.Default.GetString(configData.ToArray()));

			// Adding default HDD file
			var hddImageFile = Zstd.DecompressZstdStream(new MemoryStream(Resources.DOSBOX_HDD_IMAGE_FAT16_21MB.Value)).ToArray();
			_exe.AddReadonlyFile(hddImageFile, FileNames.HD);


			if (!dosbox.Init())
				throw new InvalidOperationException("Core rejected the rom!");

			foreach (var f in filesToRemove)
			{
				_exe.RemoveReadonlyFile(f);
			}

			PostInit();

			//dosbox.SetLEDCallback(_syncSettings.FloppyDrives > 0 ? _ledCallback : null);
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			DriveLightOn = false;
			var fi = new LibDOSBox.FrameInfo();

			fi.driveActions.insertFloppyDisk = -1;
			fi.driveActions.insertCDROM = -1;
			fi.driveActions.insertHardDiskDrive = -1;

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

			if (_hardDiskDriveCount > 0)
			{
				if (controller.IsPressed(Inputs.NextHardDiskDrive))
				{
					if (!_nextHardDiskDrivePressed)
					{
						_currentHardDiskDrive = (_currentHardDiskDrive + 1) % _hardDiskDriveCount;
						fi.driveActions.insertHardDiskDrive = _currentHardDiskDrive;
						CoreComm.Notify($"Insterted Hard Disk Drive {_currentHardDiskDrive}: {GetFullName(_hardDiskDriveImageFiles[_currentHardDiskDrive])} into drive C:", _messageDuration);
					}
				}
			}

			_nextFloppyDiskPressed = controller.IsPressed(Inputs.NextFloppyDisk);
			_nextCDROMPressed = controller.IsPressed(Inputs.NextCDROM);
			_nextHardDiskDrivePressed = controller.IsPressed(Inputs.NextHardDiskDrive);

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
			UpdateVideoStandard(false);
		}

		protected override void SaveStateBinaryInternal(BinaryWriter writer)
		{
			writer.Write(_nextFloppyDiskPressed);
			writer.Write(_nextCDROMPressed);
			writer.Write(_nextHardDiskDrivePressed);
			writer.Write(_currentFloppyDisk);
			writer.Write(_currentCDROM);
			writer.Write(_currentHardDiskDrive);
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_nextFloppyDiskPressed = reader.ReadBoolean();
			_nextCDROMPressed = reader.ReadBoolean();
			_nextHardDiskDrivePressed = reader.ReadBoolean();
			_currentFloppyDisk = reader.ReadInt32();
			_currentCDROM = reader.ReadInt32();
			_currentHardDiskDrive = reader.ReadInt32();
		}

		private void UpdateVideoStandard(bool initial)
		{
				_correctedWidth = LibDOSBox.SVGA_MAX_WIDTH;
				VsyncNumerator = LibDOSBox.VIDEO_NUMERATOR_PAL;
				VsyncDenominator = LibDOSBox.VIDEO_DENOMINATOR_PAL;
		}

		private static class FileNames
		{
			public const string DOSBOX_CONF = "dosbox-x.conf";
			public const string FD = "FloppyDisk";
			public const string CD = "CompactDisk";
			public const string HD = "HardDiskDrive";
		}
	}
}