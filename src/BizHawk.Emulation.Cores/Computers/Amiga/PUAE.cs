using System.Collections.Generic;
using System.IO;
using System.Text;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Computers.Amiga
{
	[PortedCore(
		name: CoreNames.PUAE,
		author: "UAE Team",
		portedVersion: "5.0.0",
		portedUrl: "https://github.com/libretro/libretro-uae",
		isReleased: false)]
	public partial class PUAE : WaterboxCore
	{
		private readonly List<IRomAsset> _roms;
		//private readonly List<IDiscAsset> _discs;
		private List<string> _args;
		private string _chipsetCompatible = "";

		private int _currentDrive;
		private int _currentSlot;

		private bool _ejectPressed;
		private bool _insertPressed;
		private bool _nextSlotPressed;
		private bool _nextDrivePressed;

		[CoreConstructor(VSystemID.Raw.Amiga)]
		public PUAE(CoreLoadParameters<object, PUAESyncSettings> lp)
			: base(lp.Comm, new Configuration
			{
				DefaultWidth          = LibPUAE.PAL_WIDTH,
				DefaultHeight         = LibPUAE.PAL_HEIGHT,
				MaxWidth              = LibPUAE.PAL_WIDTH,
				MaxHeight             = LibPUAE.PAL_HEIGHT,
				MaxSamples            = 2 * 1024,
				SystemId              = VSystemID.Raw.Amiga,
				DefaultFpsNumerator   = 50,
				DefaultFpsDenominator = 1
			})
		{
			_roms = lp.Roms;
			//_discs = lp.Discs;
			_syncSettings = lp.SyncSettings ?? new();
			_syncSettings.FloppyDrives = Math.Min(LibPUAE.MAX_FLOPPIES, _syncSettings.FloppyDrives);
			DeterministicEmulation = lp.DeterministicEmulationRequested || _syncSettings.FloppySpeed is FloppySpeed._100;
			var filesToRemove = new List<string>();

			_ports = [
				_syncSettings.ControllerPort1,
				_syncSettings.ControllerPort2
			];

			CreateArguments(_syncSettings);
			ControllerDefinition = CreateControllerDefinition(_syncSettings);

			var paue = PreInit<LibPUAE>(new WaterboxOptions
			{
				Filename                   = "puae.wbx",
				SbrkHeapSizeKB             = 1024,
				SealedHeapSizeKB           = 512,
				InvisibleHeapSizeKB        = 512,
				PlainHeapSizeKB            = 512,
				MmapHeapSizeKB             = 20 * 1024,
				SkipCoreConsistencyCheck   = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			for (var index = 0; index < lp.Roms.Count; index++)
			{
				if (lp.Roms[index].Extension.ToLowerInvariant() == ".hdf") // doesn't work yet
				{
					var access = "ro";
					var device_name = "DH0";
					var volume_name = FileNames.HD + index;
					var blocks_per_track = 32;
					var surfaces = 1;
					var reserved = 2;
					var block_size = 512;
					var boot_priority = 0;
					var filesys_path = "";
					var controller_unit = "uae0";

					if (Encoding.ASCII.GetString(lp.Roms[index].FileData, 0, 3) == "RDS")
					{
						blocks_per_track = 0;
						surfaces = 0;
						reserved = 0;
					}

					_exe.AddReadonlyFile(lp.Roms[index].FileData, volume_name);
						AppendSetting($"hardfile2=" +
							$"{access}," +
							$"{device_name}:" +
							$"\"{volume_name}\"," +
							$"{blocks_per_track}," +
							$"{surfaces}," +
							$"{reserved}," +
							$"{block_size}," +
							$"{boot_priority}," +
							$"{filesys_path}," +
							$"{controller_unit}");
				}
				else
				{
					_exe.AddReadonlyFile(lp.Roms[index].FileData, FileNames.FD + index);
					if (index < _syncSettings.FloppyDrives)
					{
						AppendSetting($"floppy{index}={FileNames.FD}{index}");
						AppendSetting($"floppy{index}type={(int)DriveType.DRV_35_DD}");
						AppendSetting("floppy_write_protect=true");
					}
				}
			}

			//AppendSetting("filesystem2=ro,DH0:data:Floppy/,0");

			var (kickstartData, kickstartInfo) = CoreComm.CoreFileProvider.GetFirmwareWithGameInfoOrThrow(
				new(VSystemID.Raw.Amiga, _chipsetCompatible),
				"Firmware files are required!");
			_exe.AddReadonlyFile(kickstartData, kickstartInfo.Name);
			filesToRemove.Add(kickstartInfo.Name);
			_args.AddRange(
			[
				"-r", kickstartInfo.Name
			]);

			var s = string.Join(" ", _args);
			Console.WriteLine();
			Console.WriteLine(s);
			Console.WriteLine();

			if (!paue.Init(_args.Count, _args.ToArray()))
				throw new InvalidOperationException("Core rejected the rom!");

			foreach (var f in filesToRemove)
			{
				_exe.RemoveReadonlyFile(f);
			}

			PostInit();
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var fi = new LibPUAE.FrameInfo
			{
				Port1 = new LibPUAE.ControllerState
				{
					Type = _ports[0],
					Buttons = 0
				},
				Port2 = new LibPUAE.ControllerState
				{
					Type = _ports[1],
					Buttons = 0
				},
				Action = LibPUAE.DriveAction.None
			};

			for (int port = 1; port <= 2; port++)
			{
				ref var currentPort = ref (port is 1 ? ref fi.Port1 : ref fi.Port2);

				switch (_ports[port - 1])
				{
					case LibPUAE.ControllerType.Joystick:
						{
							foreach (var (name, button) in _joystickMap)
							{
								if (controller.IsPressed($"P{port} {Inputs.Joystick} {name}"))
								{
									currentPort.Buttons |= button;
								}
							}
							break;
						}
					case LibPUAE.ControllerType.CD32_pad:
						{
							foreach (var (name, button) in _cd32padMap)
							{
								if (controller.IsPressed($"P{port} {Inputs.Cd32Pad} {name}"))
								{
									currentPort.Buttons |= button;
								}
							}
							break;
						}
					case LibPUAE.ControllerType.Mouse:
						{
							if (controller.IsPressed($"P{port} {Inputs.MouseLeftButton}"))
							{
								currentPort.Buttons |= LibPUAE.AllButtons.Button1;
							}

							if (controller.IsPressed($"P{port} {Inputs.MouseRightButton}"))
							{
								currentPort.Buttons |= LibPUAE.AllButtons.Button2;
							}

							if (controller.IsPressed($"P{port} {Inputs.MouseMiddleButton}"))
							{
								currentPort.Buttons |= LibPUAE.AllButtons.Button3;
							}

							currentPort.MouseX = controller.AxisValue($"P{port} {Inputs.MouseX}");
							currentPort.MouseY = controller.AxisValue($"P{port} {Inputs.MouseY}");
							break;
						}
				}
			}

			if (controller.IsPressed(Inputs.Eject))
			{
				if (!_ejectPressed)
				{
					fi.Action = LibPUAE.DriveAction.Eject;
				}
			}
			else if (controller.IsPressed(Inputs.Insert))
			{
				if (!_insertPressed)
				{
					fi.Action = LibPUAE.DriveAction.Insert;
					unsafe
					{
						var str = FileNames.FD + _currentSlot;
						fixed (char* filename = str)
						{
							fixed (byte* buffer = fi.Name.Buffer)
							{
								Encoding.ASCII.GetBytes(filename, str.Length, buffer, LibPUAE.FILENAME_MAXLENGTH);
							}
						}
					}
				}
			}

			if (controller.IsPressed(Inputs.NextSlot))
			{
				if (!_nextSlotPressed)
				{
					_currentSlot++;
					_currentSlot %= _roms.Count;
					var selectedFile = _roms[_currentSlot];
					CoreComm.Notify(selectedFile.Game.Name, null);
				}
			}

			if (controller.IsPressed(Inputs.NextDrive))
			{
				if (!_nextDrivePressed)
				{
					_currentDrive++;
					_currentDrive %= _syncSettings.FloppyDrives;
					CoreComm.Notify($"Selected FD{ _currentDrive }: Drive", null);
				}
			}

			_ejectPressed     = controller.IsPressed(Inputs.Eject);
			_insertPressed    = controller.IsPressed(Inputs.Insert);
			_nextSlotPressed  = controller.IsPressed(Inputs.NextSlot);
			_nextDrivePressed = controller.IsPressed(Inputs.NextDrive);			
			fi.CurrentDrive = _currentDrive;

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
			VsyncNumerator = BufferHeight == LibPUAE.NTSC_HEIGHT ? 60 : 50;
		}

		protected override void SaveStateBinaryInternal(BinaryWriter writer)
		{
			writer.Write(_ejectPressed);
			writer.Write(_insertPressed);
			writer.Write(_nextSlotPressed);
			writer.Write(_nextDrivePressed);
			writer.Write(_currentDrive);
			writer.Write(_currentSlot);
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_ejectPressed = reader.ReadBoolean();
			_insertPressed = reader.ReadBoolean();
			_nextSlotPressed = reader.ReadBoolean();
			_nextDrivePressed = reader.ReadBoolean();
			_currentDrive = reader.ReadInt32();
			_currentSlot = reader.ReadInt32();
		}

		private static class FileNames
		{
			public const string FD = "FloppyDisk";
			public const string CD = "CompactDisk";
			public const string HD = "HardDrive";
		}
	}
}