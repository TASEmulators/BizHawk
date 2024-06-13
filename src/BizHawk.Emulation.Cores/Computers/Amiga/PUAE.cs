using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.DiscSystem;

using System;
using System.Collections.Generic;
using System.IO;

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
		private readonly List<IDiscAsset> _discs;
		private LibPUAE _puae;
		private List<string> _args;
		private static string _chipsetCompatible = "";
		private static int _currentDrive = 0;
		private static int _currentSlot = 0;
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
			_discs = lp.Discs;
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

			for (var index = 0; index < Math.Min(Math.Min(
				lp.Roms.Count, LibPUAE.MAX_FLOPPIES), _syncSettings.FloppyDrives); index++)
			{
				_exe.AddReadonlyFile(lp.Roms[index].FileData, FileNames.FD + index);
				AppendSetting($"floppy{ index }={ FileNames.FD }{ index }");
				AppendSetting($"floppy{ index }type={ (int)DriveType.DRV_35_DD }");
			}

			var (kickstartData, kickstartInfo) = CoreComm.CoreFileProvider.GetFirmwareWithGameInfoOrThrow(
				new(VSystemID.Raw.Amiga, _chipsetCompatible),
				"Firmware files are required!");
			_exe.AddReadonlyFile(kickstartData, kickstartInfo.Name);
			filesToRemove.Add(kickstartInfo.Name);
			_args.AddRange(new List<string>
			{
				"-r", kickstartInfo.Name
			});

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

			controller.BoolButtons.AddRange(new List<string>
			{
				Inputs.MLB, Inputs.MMB, Inputs.MRB
			});

			controller
				.AddAxis(Inputs.X, (0).RangeTo(LibPUAE.PAL_WIDTH),  LibPUAE.PAL_WIDTH  / 2)
				.AddAxis(Inputs.Y, (0).RangeTo(LibPUAE.PAL_HEIGHT), LibPUAE.PAL_HEIGHT / 2);

			foreach (var b in controller.BoolButtons)
			{
				if (b.StartsWithOrdinal("Mouse"))
				{
					controller.CategoryLabels[b] = "Mouse";
				}
			}

			controller.BoolButtons.AddRange(new List<string>
			{
				Inputs.EJ, Inputs.INS, Inputs.ND, Inputs.NS
			});

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
				MouseButtons = 0
			};

			foreach (var b in Enum.GetValues(typeof(LibPUAE.PUAEJoystick)))
			{
				if (controller.IsPressed(Enum.GetName(typeof(LibPUAE.PUAEJoystick), b).Replace('_', ' ')))
				{
					fi.JoystickState |= (LibPUAE.PUAEJoystick)b;
				}
			}
			
			if (controller.IsPressed(Inputs.MLB))
			{
				fi.MouseButtons |= 1 << 0;
			}
			if (controller.IsPressed(Inputs.MRB))
			{
				fi.MouseButtons |= 1 << 1;
			}
			if (controller.IsPressed(Inputs.MMB))
			{
				fi.MouseButtons |= 1 << 2;
			}

			if (controller.IsPressed(Inputs.NS))
			{
				if (!_nextSlotPressed)
				{
					_currentSlot++;
					_currentSlot %= _roms.Count + _discs.Count;

					string selectedFile;
					if (_currentSlot < _roms.Count)
					{
						selectedFile = _roms[_currentSlot].Game.Name;
					}
					else
					{
						selectedFile = _discs[_currentSlot - _roms.Count].DiscName;
					}
					_comm.Notify(selectedFile, null);
				}
			}
			_nextSlotPressed = controller.IsPressed(Inputs.NS);

			if (controller.IsPressed(Inputs.ND))
			{
				if (!_nextDrivePressed)
				{
					_currentDrive++;
					_currentDrive %= _syncSettings.FloppyDrives + (_discs.Count > 0 ? 1 : 0);

					string selectedDrive;
					if (_currentDrive < _syncSettings.FloppyDrives)
					{
						selectedDrive = "FD" + _currentDrive;
					}
					else
					{
						selectedDrive = "CD";
					}
					_comm.Notify(selectedDrive, null);
				}
			}
			_nextDrivePressed = controller.IsPressed(Inputs.ND);

			fi.MouseX = controller.AxisValue(Inputs.X);
			fi.MouseY = controller.AxisValue(Inputs.Y);
			
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

		public void SaveStateBinary(BinaryWriter writer)
		{
			using (_exe.EnterExit())
			{
				_exe.SaveStateBinary(writer);
			}

			writer.Write(_nextSlotPressed);
			writer.Write(_nextDrivePressed);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			using (_exe.EnterExit())
			{
				_exe.LoadStateBinary(reader);
			}

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
			public const string MLB = "Mouse Left Button";
			public const string MRB = "Mouse Right Button";
			public const string MMB = "Mouse Middle Button";
			public const string X   = "Mouse X";
			public const string Y   = "Mouse Y";
			public const string EJ  = "Eject";
			public const string INS = "Insert";
			public const string ND  = "Next Drive";
			public const string NS  = "Next Slot";
		}
	}
}