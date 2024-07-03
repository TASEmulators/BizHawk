using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
//using BizHawk.Emulation.DiscSystem;

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Amiga
{
	[PortedCore(
		name: CoreNames.PUAE,
		author: "UAE Team",
		portedVersion: "5.0.0",
		portedUrl: "https://github.com/libretro/libretro-uae")]
	public partial class PUAE : WaterboxCore
	{
		internal CoreComm _comm { get; }
		private readonly List<IRomAsset> _roms;
		//private readonly List<IDiscAsset> _discs;
		private LibPUAE _puae;
		private List<string> _args;
		private string _chipsetCompatible = "";
		private int _currentDrive = 0;
		private int _currentSlot = 0;
		private byte[] _currentRom;
		private bool _ejectPressed = false;
		private bool _insertPressed = false;
		private bool _nextSlotPressed = false;
		private bool _nextDrivePressed = false;

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
			_comm = lp.Comm;
			_roms = lp.Roms;
			//_discs = lp.Discs;
			_syncSettings = lp.SyncSettings ?? new();
			var filesToRemove = new List<string>();
			CreateArguments(_syncSettings);
			ControllerDefinition = InitInput();

			_puae = PreInit<LibPUAE>(new WaterboxOptions
			{
				Filename                   = "puae.wbx",
				SbrkHeapSizeKB             = 5 * 512,
				SealedHeapSizeKB           = 10 * 1024,
				InvisibleHeapSizeKB        = 10 * 1024,
				PlainHeapSizeKB            = 10 * 1024,
				MmapHeapSizeKB             = 40 * 1024,
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

					if (Encoding.ASCII.GetString(lp.Roms[index].RomData, 0, 3) == "RDS")
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
					_exe.AddTransientFile(lp.Roms[index].FileData, FileNames.FD + index);
					if (index < Math.Min(LibPUAE.MAX_FLOPPIES, _syncSettings.FloppyDrives))
					{
						AppendSetting($"floppy{index}={FileNames.FD}{index}");
						AppendSetting($"floppy{index}type={(int)DriveType.DRV_35_DD}");
						AppendSetting($"floppy_write_protect=no");
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

			if (!_puae.Init(_args.Count, _args.ToArray()))
				throw new InvalidOperationException("Core rejected the rom!");

			foreach (var f in filesToRemove)
			{
				_exe.RemoveReadonlyFile(f);
			}

			PostInit();
		}

		private static ControllerDefinition InitInput()
		{
			var controller = new ControllerDefinition("Amiga Controller");

			foreach (var b in Enum.GetValues(typeof(LibPUAE.PUAEJoystick)))
			{
				var name = Enum.GetName(typeof(LibPUAE.PUAEJoystick), b).Replace('_', ' ');
				controller.BoolButtons.Add(name);
				controller.CategoryLabels[name] = "Joystick";
			}

			controller.BoolButtons.AddRange(
			[
				Inputs.MouseLeftButton, Inputs.MouseMIddleButton, Inputs.MouseRightButton
			]);

			controller
				.AddAxis(Inputs.MouseX, (0).RangeTo(LibPUAE.PAL_WIDTH),  LibPUAE.PAL_WIDTH  / 2)
				.AddAxis(Inputs.MouseY, (0).RangeTo(LibPUAE.PAL_HEIGHT), LibPUAE.PAL_HEIGHT / 2);

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

			foreach (var b in Enum.GetValues(typeof(LibPUAE.PUAEKeyboard)))
			{
				var name = Enum.GetName(typeof(LibPUAE.PUAEKeyboard), b).Replace('_', ' ');
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

			foreach (var b in Enum.GetValues(typeof(LibPUAE.PUAEJoystick)))
			{
				if (controller.IsPressed(Enum.GetName(typeof(LibPUAE.PUAEJoystick), b).Replace('_', ' ')))
				{
					fi.JoystickState |= (LibPUAE.PUAEJoystick)b;
				}
			}
			
			if (controller.IsPressed(Inputs.MouseLeftButton))
			{
				fi.MouseButtons |= LibPUAE.b00000001;
			}
			if (controller.IsPressed(Inputs.MouseRightButton))
			{
				fi.MouseButtons |= LibPUAE.b00000010;
			}
			if (controller.IsPressed(Inputs.MouseMIddleButton))
			{
				fi.MouseButtons |= LibPUAE.b00000100;
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
						string str = FileNames.FD + _currentSlot;
						fixed(char* filename = str)
						fixed (byte* buffer = fi.Name.Buffer)
						{
							Encoding.ASCII.GetBytes(filename, str.Length, buffer, LibPUAE.FILENAME_MAXLENGTH);
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
					_currentRom = selectedFile.FileData;
					_comm.Notify(selectedFile.Game.Name, null);
				}
			}
			if (controller.IsPressed(Inputs.NextDrive))
			{
				if (!_nextDrivePressed)
				{
					_currentDrive++;
					_currentDrive %= _syncSettings.FloppyDrives;
					_comm.Notify($"Selected FD{ _currentDrive }: Drive", null);
				}
			}
			_ejectPressed     = controller.IsPressed(Inputs.Eject);
			_insertPressed    = controller.IsPressed(Inputs.Insert);
			_nextSlotPressed  = controller.IsPressed(Inputs.NextSlot);
			_nextDrivePressed = controller.IsPressed(Inputs.NextDrive);			
			fi.CurrentDrive = _currentDrive;
			
			foreach (var b in Enum.GetValues(typeof(LibPUAE.PUAEKeyboard)))
			{
				var name = Enum.GetName(typeof(LibPUAE.PUAEKeyboard), b);
				var value = (int)Enum.Parse(typeof(LibPUAE.PUAEKeyboard), name);
				if (controller.IsPressed(name.Replace('_', ' ')))
				{
					unsafe
					{
						fi.Keys.Buffer[value] = 1;
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
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_ejectPressed = reader.ReadBoolean();
			_insertPressed = reader.ReadBoolean();
			_nextSlotPressed = reader.ReadBoolean();
			_nextDrivePressed = reader.ReadBoolean();
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