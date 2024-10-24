using System.Collections.Generic;
using System.IO;
using System.Text;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
//using BizHawk.Emulation.DiscSystem;

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
			var filesToRemove = new List<string>();
			CreateArguments(_syncSettings);
			ControllerDefinition = _controllerDefinition;

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
				if (lp.Roms[index].Extension.ToLowerInvariant() == ".hdf")
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

		private static readonly (string Name, LibPUAE.PUAEJoystick Button)[] _joystickMap = CreateJoystickMap();
		private static readonly (string Name, LibPUAE.PUAEKeyboard Key)[] _keyboardMap = CreateKeyboardMap();
		private static readonly ControllerDefinition _controllerDefinition = CreateControllerDefinition();

		private static (string Name, LibPUAE.PUAEJoystick Value)[] CreateJoystickMap()
		{
			var joystickMap = new List<(string, LibPUAE.PUAEJoystick)>();
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var b in Enum.GetValues(typeof(LibPUAE.PUAEJoystick)))
			{
				var name = Enum.GetName(typeof(LibPUAE.PUAEJoystick), b)!.Replace('_', ' ');
				joystickMap.Add((name, (LibPUAE.PUAEJoystick)b));
			}

			return joystickMap.ToArray();
		}

		private static (string Name, LibPUAE.PUAEKeyboard Value)[] CreateKeyboardMap()
		{
			var keyboardMap = new List<(string, LibPUAE.PUAEKeyboard)>();
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var k in Enum.GetValues(typeof(LibPUAE.PUAEKeyboard)))
			{
				var name = Enum.GetName(typeof(LibPUAE.PUAEKeyboard), k)!.Replace('_', ' ');
				keyboardMap.Add((name, (LibPUAE.PUAEKeyboard)k));
			}

			return keyboardMap.ToArray();
		}

		private static ControllerDefinition CreateControllerDefinition()
		{
			var controller = new ControllerDefinition("Amiga Controller");

			foreach (var (name, _) in _joystickMap)
			{
				controller.BoolButtons.Add(name);
				controller.CategoryLabels[name] = "Joystick";
			}

			controller.BoolButtons.AddRange(
			[
				Inputs.MouseLeftButton, Inputs.MouseMIddleButton, Inputs.MouseRightButton
			]);

			controller
				.AddAxis(Inputs.MouseX, 0.RangeTo(LibPUAE.PAL_WIDTH),  LibPUAE.PAL_WIDTH  / 2)
				.AddAxis(Inputs.MouseY, 0.RangeTo(LibPUAE.PAL_HEIGHT), LibPUAE.PAL_HEIGHT / 2);

			foreach (var b in controller.BoolButtons)
			{
				if (b.StartsWithOrdinal("Mouse"))
				{
					controller.CategoryLabels[b] = "Mouse";
				}
			}

			controller.BoolButtons.AddRange(
			[
				Inputs.NextDrive, Inputs.NextSlot, Inputs.Insert, Inputs.Eject
			]);

			foreach (var (name, _) in _keyboardMap)
			{
				controller.BoolButtons.Add(name);
				controller.CategoryLabels[name] = "Keyboard";
			}

			return controller.MakeImmutable();
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var fi = new LibPUAE.FrameInfo
			{
				MouseButtons = 0,
				Action = LibPUAE.DriveAction.None
			};

			foreach (var (name, button) in _joystickMap)
			{
				if (controller.IsPressed(name))
				{
					fi.JoystickState |= button;
				}
			}

			if (controller.IsPressed(Inputs.MouseLeftButton))
			{
				fi.MouseButtons |= 0b00000001;
			}

			if (controller.IsPressed(Inputs.MouseRightButton))
			{
				fi.MouseButtons |= 0b00000010;
			}

			if (controller.IsPressed(Inputs.MouseMIddleButton))
			{
				fi.MouseButtons |= 0b00000100;
			}

			fi.MouseX = controller.AxisValue(Inputs.MouseX);
			fi.MouseY = controller.AxisValue(Inputs.MouseY);

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

		private static class Inputs
		{
			public const string MouseLeftButton = "Mouse Left Button";
			public const string MouseRightButton = "Mouse Right Button";
			public const string MouseMIddleButton = "Mouse Middle Button";
			public const string MouseX = "Mouse X";
			public const string MouseY = "Mouse Y";
			public const string Eject = "Eject";
			public const string Insert = "Insert";
			public const string NextDrive = "Next Drive";
			public const string NextSlot = "Next Slot";
		}
	}
}