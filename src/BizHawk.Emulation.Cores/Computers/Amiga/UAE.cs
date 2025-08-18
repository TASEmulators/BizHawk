﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Computers.Amiga
{
	[PortedCore(
		name: CoreNames.UAE,
		author: "UAE Team",
		portedVersion: "5.0.0",
		portedUrl: "https://github.com/libretro/libretro-uae",
		isReleased: true)]
	public partial class UAE : WaterboxCore
	{
		private static readonly Configuration ConfigPAL = new()
		{
			SystemId = VSystemID.Raw.Amiga,
			MaxSamples = 8 * 1024,
			DefaultWidth = LibUAE.PAL_WIDTH,
			DefaultHeight = LibUAE.PAL_HEIGHT,
			MaxWidth = LibUAE.PAL_WIDTH,
			MaxHeight = LibUAE.PAL_HEIGHT,
			DefaultFpsNumerator = LibUAE.VIDEO_NUMERATOR_PAL,
			DefaultFpsDenominator = LibUAE.VIDEO_DENOMINATOR_PAL
		};

		private static readonly Configuration ConfigNTSC = new()
		{
			SystemId = VSystemID.Raw.Amiga,
			MaxSamples = 8 * 1024,
			DefaultWidth = LibUAE.NTSC_WIDTH,
			DefaultHeight = LibUAE.NTSC_HEIGHT,
			// games never switch region, and video dumping won't be happy, but amiga can still do it
			MaxWidth = LibUAE.PAL_WIDTH,
			MaxHeight = LibUAE.PAL_HEIGHT,
			DefaultFpsNumerator = LibUAE.VIDEO_NUMERATOR_NTSC,
			DefaultFpsDenominator = LibUAE.VIDEO_DENOMINATOR_NTSC
		};

		private readonly LibWaterboxCore.EmptyCallback _ledCallback;
		private readonly List<IRomAsset> _roms;
		private const int _messageDuration = 4;
		private const int _driveNullOrEmpty = -1;
		private int[] _driveSlots;
		private List<string> _args;
		private int _currentDrive;
		private int _currentSlot;
		private bool _ejectPressed;
		private bool _insertPressed;
		private bool _nextSlotPressed;
		private bool _nextDrivePressed;
		private int _correctedWidth;
		private string _chipsetCompatible = "";
		private string GetFullName(IRomAsset rom) => Path.GetFileName(rom.RomPath.SubstringAfter('|'));

		public override int VirtualWidth => _correctedWidth;

		private void LEDCallback()
		{
			DriveLightOn = true;
		}

		[CoreConstructor(VSystemID.Raw.Amiga)]
		public UAE(CoreLoadParameters<object, UAESyncSettings> lp)
			: base(lp.Comm, lp.SyncSettings?.Region is VideoStandard.NTSC ? ConfigNTSC : ConfigPAL)
		{
			_roms = lp.Roms;
			_syncSettings = lp.SyncSettings ?? new();
			_syncSettings.FloppyDrives = Math.Min(LibUAE.MAX_FLOPPIES, _syncSettings.FloppyDrives);
			DeterministicEmulation = lp.DeterministicEmulationRequested || _syncSettings.FloppySpeed is FloppySpeed._100;
			var filesToRemove = new List<string>();

			Ports = [
				_syncSettings.ControllerPort1,
				_syncSettings.ControllerPort2
			];
			_driveSlots = Enumerable.Repeat(_driveNullOrEmpty, LibUAE.MAX_FLOPPIES).ToArray();
			DriveLightEnabled = _syncSettings.FloppyDrives > 0;

			UpdateVideoStandard(true);
			CreateArguments(_syncSettings);
			ControllerDefinition = CreateControllerDefinition(_syncSettings);
			_ledCallback = LEDCallback;

			var uae = PreInit<LibUAE>(new WaterboxOptions
			{
				Filename = "uae.wbx",
				SbrkHeapSizeKB = 1024,
				SealedHeapSizeKB = 512,
				InvisibleHeapSizeKB = 512,
				PlainHeapSizeKB = 512,
				MmapHeapSizeKB = 20 * 1024,
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, [ _ledCallback ]);

			for (var index = 0; index < lp.Roms.Count; index++)
			{
				var rom = lp.Roms[index];
				_exe.AddReadonlyFile(rom.FileData, FileNames.FD + index);
				if (index < _syncSettings.FloppyDrives)
				{
					_driveSlots[index] = index;
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

			if (!uae.Init(_args.Count, _args.ToArray()))
				throw new InvalidOperationException("Core rejected the rom!");

			foreach (var f in filesToRemove)
			{
				_exe.RemoveReadonlyFile(f);
			}

			PostInit();

			uae.SetLEDCallback(_syncSettings.FloppyDrives > 0 ? _ledCallback : null);
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			DriveLightOn = false;
			var fi = new LibUAE.FrameInfo
			{
				Port1 = new LibUAE.ControllerState
				{
					Type = Ports[0],
					Buttons = LibUAE.AllButtons.None
				},
				Port2 = new LibUAE.ControllerState
				{
					Type = Ports[1],
					Buttons = LibUAE.AllButtons.None
				},
				Action = LibUAE.DriveAction.None
			};

			for (int port = 1; port <= 2; port++)
			{
				ref var currentPort = ref (port is 1 ? ref fi.Port1 : ref fi.Port2);

				switch (Ports[port - 1])
				{
					case LibUAE.ControllerType.DJoy:
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
					case LibUAE.ControllerType.CD32Joy:
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
					case LibUAE.ControllerType.Mouse:
						{
							if (controller.IsPressed($"P{port} {Inputs.MouseLeftButton}"))
							{
								currentPort.Buttons |= LibUAE.AllButtons.Button_1;
							}

							if (controller.IsPressed($"P{port} {Inputs.MouseRightButton}"))
							{
								currentPort.Buttons |= LibUAE.AllButtons.Button_2;
							}

							if (controller.IsPressed($"P{port} {Inputs.MouseMiddleButton}"))
							{
								currentPort.Buttons |= LibUAE.AllButtons.Button_3;
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
					fi.Action = LibUAE.DriveAction.EjectDisk;
					if (_driveSlots[_currentDrive] == _driveNullOrEmpty)
					{
						CoreComm.Notify($"Drive FD{_currentDrive} is already empty!", _messageDuration);
					}
					else
					{
						CoreComm.Notify($"Ejected drive FD{_currentDrive}: {GetFullName(_roms[_driveSlots[_currentDrive]])}", _messageDuration);
						_driveSlots[_currentDrive] = _driveNullOrEmpty;
					}
				}
			}
			else if (controller.IsPressed(Inputs.InsertDisk))
			{
				if (!_insertPressed)
				{
					fi.Action = LibUAE.DriveAction.InsertDisk;
					unsafe
					{
						var str = FileNames.FD + _currentSlot;
						fixed (char* filename = str)
						{
							fixed (byte* buffer = fi.Name.Buffer)
							{
								Encoding.ASCII.GetBytes(filename, str.Length, buffer, LibUAE.FILENAME_MAXLENGTH);
							}
						}
					}
					_driveSlots[_currentDrive] = _currentSlot;
					CoreComm.Notify($"Insterted drive FD{_currentDrive}: {GetFullName(_roms[_driveSlots[_currentDrive]])}", _messageDuration);
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
					string name = "";
					if (_driveSlots[_currentDrive] == _driveNullOrEmpty)
					{
						name = "empty";
					}
					else
					{
						name = GetFullName(_roms[_driveSlots[_currentDrive]]);
					}
					CoreComm.Notify($"Selected drive FD{_currentDrive}: {name}", _messageDuration);
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
			writer.Write(_driveSlots[0]);
			writer.Write(_driveSlots[1]);
			writer.Write(_driveSlots[2]);
			writer.Write(_driveSlots[3]);
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_ejectPressed = reader.ReadBoolean();
			_insertPressed = reader.ReadBoolean();
			_nextSlotPressed = reader.ReadBoolean();
			_nextDrivePressed = reader.ReadBoolean();
			_currentDrive = reader.ReadInt32();
			_currentSlot = reader.ReadInt32();
			_driveSlots[0] = reader.ReadInt32();
			_driveSlots[1] = reader.ReadInt32();
			_driveSlots[2] = reader.ReadInt32();
			_driveSlots[3] = reader.ReadInt32();
		}

		private void UpdateVideoStandard(bool initial)
		{
			var ntsc = initial
				? _syncSettings.Region is VideoStandard.NTSC
				: BufferHeight == LibUAE.NTSC_HEIGHT;

			if (ntsc)
			{
				_correctedWidth = LibUAE.PAL_WIDTH * 6 / 7;
				VsyncNumerator = LibUAE.VIDEO_NUMERATOR_NTSC;
				VsyncDenominator = LibUAE.VIDEO_DENOMINATOR_NTSC;
			}
			else
			{
				_correctedWidth = LibUAE.PAL_WIDTH;
				VsyncNumerator = LibUAE.VIDEO_NUMERATOR_PAL;
				VsyncDenominator = LibUAE.VIDEO_DENOMINATOR_PAL;
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