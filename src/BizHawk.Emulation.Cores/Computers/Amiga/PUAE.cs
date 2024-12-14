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
		private static readonly Configuration ConfigPAL = new Configuration
		{
			SystemId = VSystemID.Raw.Amiga,
			MaxSamples = 2 * 1024,
			DefaultWidth = LibPUAE.PAL_WIDTH,
			DefaultHeight = LibPUAE.PAL_HEIGHT,
			MaxWidth = LibPUAE.PAL_WIDTH,
			MaxHeight = LibPUAE.PAL_HEIGHT,
			DefaultFpsNumerator = LibPUAE.PUAE_VIDEO_NUMERATOR_PAL,
			DefaultFpsDenominator = LibPUAE.PUAE_VIDEO_DENOMINATOR_PAL
		};

		private static readonly Configuration ConfigNTSC = new Configuration
		{
			SystemId = VSystemID.Raw.Amiga,
			MaxSamples = 2 * 1024,
			DefaultWidth = LibPUAE.NTSC_WIDTH,
			DefaultHeight = LibPUAE.NTSC_HEIGHT,
			// games never switch region, and video dumping won't be happy, but amiga can still do it
			MaxWidth = LibPUAE.PAL_WIDTH,
			MaxHeight = LibPUAE.PAL_HEIGHT,
			DefaultFpsNumerator = LibPUAE.PUAE_VIDEO_NUMERATOR_NTSC,
			DefaultFpsDenominator = LibPUAE.PUAE_VIDEO_DENOMINATOR_NTSC
		};
		
		private readonly LibWaterboxCore.EmptyCallback _ledCallback;
		private readonly List<IRomAsset> _roms;
		private const int _messageDuration = 4;
		private List<string> _args;
		private List<string> _drives;
		private int _currentDrive;
		private int _currentSlot;
		private bool _ejectPressed;
		private bool _insertPressed;
		private bool _nextSlotPressed;
		private bool _nextDrivePressed;
		private int _correctedWidth;
		private string _chipsetCompatible = "";
		public override int VirtualWidth => _correctedWidth;
		private string GetFullName(IRomAsset rom) => rom.Game.Name + rom.Extension;

		private void LEDCallback()
		{
			DriveLightOn = true;
		}

		[CoreConstructor(VSystemID.Raw.Amiga)]
		public PUAE(CoreLoadParameters<object, PUAESyncSettings> lp)
			: base(lp.Comm, lp.SyncSettings?.Region is VideoStandard.NTSC ? ConfigNTSC : ConfigPAL)
		{
			_roms = lp.Roms;
			_syncSettings = lp.SyncSettings ?? new();
			_syncSettings.FloppyDrives = Math.Min(LibPUAE.MAX_FLOPPIES, _syncSettings.FloppyDrives);
			DeterministicEmulation = lp.DeterministicEmulationRequested || _syncSettings.FloppySpeed is FloppySpeed._100;
			var filesToRemove = new List<string>();

			_ports = [
				_syncSettings.ControllerPort1,
				_syncSettings.ControllerPort2
			];
			_drives = new(_syncSettings.FloppyDrives);
			DriveLightEnabled = _syncSettings.FloppyDrives > 0;

			UpdateVideoStandard(true);
			CreateArguments(_syncSettings);
			ControllerDefinition = CreateControllerDefinition(_syncSettings);
			_ledCallback = LEDCallback;

			var puae = PreInit<LibPUAE>(new WaterboxOptions
			{
				Filename = "puae.wbx",
				SbrkHeapSizeKB = 1024,
				SealedHeapSizeKB = 512,
				InvisibleHeapSizeKB = 512,
				PlainHeapSizeKB = 512,
				MmapHeapSizeKB = 20 * 1024,
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[] { _ledCallback });

			for (var index = 0; index < lp.Roms.Count; index++)
			{
				var rom = lp.Roms[index];
				_exe.AddReadonlyFile(rom.FileData, FileNames.FD + index);
				if (index < _syncSettings.FloppyDrives)
				{
					_drives.Add(GetFullName(rom));
					AppendSetting($"floppy{index}={FileNames.FD}{index}");
					AppendSetting($"floppy{index}type={(int) DriveType.DRV_35_DD}");
					AppendSetting("floppy_write_protect=true");
				}
			}

			var (kickstartData, kickstartInfo) = CoreComm.CoreFileProvider.GetFirmwareWithGameInfoOrThrow(
				new(VSystemID.Raw.Amiga, _chipsetCompatible),
				"Firmware files are required!");
			_exe.AddReadonlyFile(kickstartData, kickstartInfo.Name);
			filesToRemove.Add(kickstartInfo.Name);
			_args.AddRange(
			[
				"-r", kickstartInfo.Name
			]);

			Console.WriteLine();
			Console.WriteLine(string.Join(" ", _args));
			Console.WriteLine();

			if (!puae.Init(_args.Count, _args.ToArray()))
				throw new InvalidOperationException("Core rejected the rom!");

			foreach (var f in filesToRemove)
			{
				_exe.RemoveReadonlyFile(f);
			}

			PostInit();

			puae.SetLEDCallback(_syncSettings.FloppyDrives > 0 ? _ledCallback : null);
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			DriveLightOn = false;
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
								currentPort.Buttons |= LibPUAE.AllButtons.Button_1;
							}

							if (controller.IsPressed($"P{port} {Inputs.MouseRightButton}"))
							{
								currentPort.Buttons |= LibPUAE.AllButtons.Button_2;
							}

							if (controller.IsPressed($"P{port} {Inputs.MouseMiddleButton}"))
							{
								currentPort.Buttons |= LibPUAE.AllButtons.Button_3;
							}

							currentPort.MouseX = controller.AxisValue($"P{port} {Inputs.MouseX}");
							currentPort.MouseY = controller.AxisValue($"P{port} {Inputs.MouseY}");
							break;
						}
				}
			}

			if (controller.IsPressed(Inputs.EjectDisk))
			{
				if (!_ejectPressed)
				{
					fi.Action = LibPUAE.DriveAction.EjectDisk;
					CoreComm.Notify($"Ejected drive FD{_currentDrive}: {_drives[_currentDrive]}", _messageDuration);
					_drives[_currentDrive] = "empty";
				}
			}
			else if (controller.IsPressed(Inputs.InsertDisk))
			{
				if (!_insertPressed)
				{
					fi.Action = LibPUAE.DriveAction.InsertDisk;
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
					_drives[_currentDrive] = GetFullName(_roms[_currentSlot]);
					CoreComm.Notify($"Insterted drive FD{_currentDrive}: {_drives[_currentDrive]}", _messageDuration);
				}
			}

			if (controller.IsPressed(Inputs.NextSlot))
			{
				if (!_nextSlotPressed)
				{
					_currentSlot++;
					_currentSlot %= _roms.Count;
					var selectedFile = _roms[_currentSlot];
					CoreComm.Notify($"Selected slot {_currentSlot}: {GetFullName(selectedFile)}", _messageDuration);
				}
			}

			if (controller.IsPressed(Inputs.NextDrive))
			{
				if (!_nextDrivePressed)
				{
					_currentDrive++;
					_currentDrive %= _syncSettings.FloppyDrives;
					if (_drives.Count <= _currentDrive)
					{
						_drives.Add("empty");
					}
					CoreComm.Notify($"Selected drive FD{_currentDrive}: {_drives[_currentDrive]}", _messageDuration);
				}
			}

			_ejectPressed = controller.IsPressed(Inputs.EjectDisk);
			_insertPressed = controller.IsPressed(Inputs.InsertDisk);
			_nextSlotPressed = controller.IsPressed(Inputs.NextSlot);
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
			UpdateVideoStandard(false);
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

		private void UpdateVideoStandard(bool initial)
		{
			var ntsc = initial
				? _syncSettings.Region is VideoStandard.NTSC
				: BufferHeight == LibPUAE.NTSC_HEIGHT;

			if (ntsc)
			{
				_correctedWidth = LibPUAE.PAL_WIDTH * 6 / 7;
				VsyncNumerator = LibPUAE.PUAE_VIDEO_NUMERATOR_NTSC;
				VsyncDenominator = LibPUAE.PUAE_VIDEO_DENOMINATOR_NTSC;
			}
			else
			{
				_correctedWidth = LibPUAE.PAL_WIDTH;
				VsyncNumerator = LibPUAE.PUAE_VIDEO_NUMERATOR_PAL;
				VsyncDenominator = LibPUAE.PUAE_VIDEO_DENOMINATOR_PAL;
			}
		}

		private static class FileNames
		{
			public const string FD = "FloppyDisk";
			public const string CD = "CompactDisk";
			public const string HD = "HardDrive";
		}
	}
}