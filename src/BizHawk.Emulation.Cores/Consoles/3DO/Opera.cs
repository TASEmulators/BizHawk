using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.Amiga;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;
using static BizHawk.Emulation.Cores.Computers.Amiga.UAE;

namespace BizHawk.Emulation.Consoles._3DO
{
	[PortedCore(
		name: CoreNames.Opera,
		author: "Libretro Team",
		portedVersion: "2025.03.08 (67a29e6)",
		portedUrl: "https://github.com/libretro/opera-libretro",
		isReleased: false)]
	public partial class Opera : WaterboxCore
	{
		private static readonly Configuration DefaultConfig = new Configuration
		{
			SystemId = VSystemID.Raw._3DO,
			MaxSamples = 8 * 1024,
			DefaultWidth = LibOpera.NTSC_WIDTH,
			DefaultHeight = LibOpera.NTSC_HEIGHT,
			MaxWidth = LibOpera.PAL_WIDTH,
			MaxHeight = LibOpera.PAL_HEIGHT,
			DefaultFpsNumerator = LibOpera.VIDEO_NUMERATOR_NTSC,
			DefaultFpsDenominator = LibOpera.VIDEO_DENOMINATOR_NTSC
		};

		private readonly List<IRomAsset> _roms;
		private const int _messageDuration = 4;

		private string GetFullName(IRomAsset rom) => rom.Game.Name + rom.Extension;

		public override int VirtualWidth => BufferHeight * 4 / 3;
		private LibOpera _libOpera;

		// Image selection / swapping variables

		[CoreConstructor(VSystemID.Raw._3DO)]
		public Opera(CoreLoadParameters<object, SyncSettings> lp)
			: base(lp.Comm, DefaultConfig)
		{
			_roms = lp.Roms;
			_syncSettings = lp.SyncSettings ?? new();
			ControllerDefinition = CreateControllerDefinition(_syncSettings);

			_libOpera = PreInit<LibOpera>(new WaterboxOptions
			{
				Filename = "opera.wbx",
				SbrkHeapSizeKB = 256 * 1024,
				SealedHeapSizeKB = 1024,
				InvisibleHeapSizeKB = 1024,
				PlainHeapSizeKB = 1024,
				MmapHeapSizeKB = 256 * 1024,
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[] { });

			// Adding Game file
			var gameFile = _roms[0];
			string gameFileName = Path.GetFileNameWithoutExtension(gameFile.RomPath) + ".iso";
			_exe.AddReadonlyFile(gameFile.RomData, gameFileName);

			// Adding BIOS file
			string biosType = _syncSettings.SystemType switch
			{
				SystemType.Panasonic_FZ1_U => "Panasonic_FZ1_U",
				SystemType.Panasonic_FZ1_E => "Panasonic_FZ1_E",
				SystemType.Panasonic_FZ1_J => "Panasonic_FZ1_J",
				SystemType.Panasonic_FZ10_U => "Panasonic_FZ10_U",
				SystemType.Panasonic_FZ10_E => "Panasonic_FZ10_E",
				SystemType.Panasonic_FZ10_J => "Panasonic_FZ10_J",
				SystemType.Goldstar_GDO101P => "Goldstar_GDO101P",
				SystemType.Goldstar_FC1 => "Goldstar_FC1",
				SystemType.Sanyo_IMP21J_Try => "Sanyo_IMP21J_Try",
				SystemType.Sanyo_HC21 => "Sanyo_HC21",
				SystemType.Shootout_At_Old_Tucson => "Shootout_At_Old_Tucson",
				SystemType._3DO_NTSC_1fc2 => "3DO_NTSC_1fc2",
				_ => ""
			};

			var (biosData, biosInfo) = CoreComm.CoreFileProvider.GetFirmwareWithGameInfoOrThrow(new(VSystemID.Raw._3DO, biosType), "BIOS ROM files are required!");
			string biosFileName = biosInfo.Name + ".bin";
			_exe.AddReadonlyFile(biosData, biosFileName);

			// Adding Font ROM file, if required
			string fontROMType = _syncSettings.FontROM switch
			{
				FontROM.Kanji_ROM_Panasonic_FZ1 => "Kanji_ROM_Panasonic_FZ1",
				FontROM.Kanji_ROM_Panasonic_FZ10 => "Kanji_ROM_Panasonic_FZ10",
				_ => "None"
			};

			string fontROMFileName = "None";
			if (fontROMType != "None")
			{
				var (fontROMData, fontROMInfo) = CoreComm.CoreFileProvider.GetFirmwareWithGameInfoOrThrow(new(VSystemID.Raw._3DO, fontROMType), "Font ROM files are required!");
				_exe.AddReadonlyFile(fontROMData, fontROMInfo.Name);
				fontROMFileName = fontROMInfo.Name;
			}

			////////////// Initializing Core
			Console.WriteLine($"Launching Core with Game ROM: '{gameFileName}', BIOS ROM: '{biosFileName}', Font ROM: '{fontROMFileName}'");
			if (!_libOpera.Init(gameFileName, biosFileName, fontROMFileName, (int)_syncSettings.Controller1Type, (int)_syncSettings.Controller2Type))
				throw new InvalidOperationException("Core rejected the rom!");

			PostInit();
		}

		private LibOpera.GameInput _port1PrevGameInput = new LibOpera.GameInput();
		private LibOpera.GameInput _port2PrevGameInput = new LibOpera.GameInput();

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var fi = new LibOpera.FrameInfo();

			fi.port1 = ProcessController(1, _syncSettings.Controller1Type, _port1PrevGameInput, controller);
			fi.port2 = ProcessController(2, _syncSettings.Controller2Type, _port2PrevGameInput, controller);

			_port1PrevGameInput = fi.port1;
			_port2PrevGameInput = fi.port2;

			return fi;
		}

		private static LibOpera.GameInput ProcessController(int port, ControllerType type, LibOpera.GameInput prevInputs, IController controller)
		{
			LibOpera.GameInput gameInput = new LibOpera.GameInput();

			switch (type)
			{
				case ControllerType.Gamepad:
					gameInput.gamepad.up       = controller.IsPressed($"P{port} {JoystickButtons.Up}") ? 1 : 0;
					gameInput.gamepad.down     = controller.IsPressed($"P{port} {JoystickButtons.Down}") ? 1 : 0;
					gameInput.gamepad.left     = controller.IsPressed($"P{port} {JoystickButtons.Left}") ? 1 : 0;
					gameInput.gamepad.right    = controller.IsPressed($"P{port} {JoystickButtons.Right}") ? 1 : 0;
					gameInput.gamepad.select   = controller.IsPressed($"P{port} {JoystickButtons.Select}") ? 1 : 0;
					gameInput.gamepad.start    = controller.IsPressed($"P{port} {JoystickButtons.Start}") ? 1 : 0;
					gameInput.gamepad.buttonA  = controller.IsPressed($"P{port} {JoystickButtons.ButtonA}") ? 1 : 0;
					gameInput.gamepad.buttonB  = controller.IsPressed($"P{port} {JoystickButtons.ButtonB}") ? 1 : 0;
					gameInput.gamepad.buttonX  = controller.IsPressed($"P{port} {JoystickButtons.ButtonX}") ? 1 : 0;
					gameInput.gamepad.buttonY  = controller.IsPressed($"P{port} {JoystickButtons.ButtonY}") ? 1 : 0;
					gameInput.gamepad.buttonL  = controller.IsPressed($"P{port} {JoystickButtons.ButtonL}") ? 1 : 0;
					gameInput.gamepad.buttonR  = controller.IsPressed($"P{port} {JoystickButtons.ButtonR}") ? 1 : 0;
					break;

				case ControllerType.Mouse:
					gameInput.mouse.posX = controller.AxisValue($"P{port} {Inputs.MouseX}");
					gameInput.mouse.posY = controller.AxisValue($"P{port} {Inputs.MouseY}");
					gameInput.mouse.dX = gameInput.mouse.posX - prevInputs.mouse.posX;
					gameInput.mouse.dY = gameInput.mouse.posY - prevInputs.mouse.posY;
					gameInput.mouse.leftButton   = controller.IsPressed($"P{port} {Inputs.MouseLeftButton}") ? 1 : 0;
					gameInput.mouse.middleButton = controller.IsPressed($"P{port} {Inputs.MouseMiddleButton}") ? 1 : 0;
					gameInput.mouse.rightButton  = controller.IsPressed($"P{port} {Inputs.MouseRightButton}") ? 1 : 0;
					gameInput.mouse.fourthButton = controller.IsPressed($"P{port} {Inputs.MouseFourthButton}") ? 1 : 0;
					break;

				case ControllerType.FlightStick:
					gameInput.flightStick.up      = controller.IsPressed($"P{port} {FlightStickButtons.Up}") ? 1 : 0;
					gameInput.flightStick.down    = controller.IsPressed($"P{port} {FlightStickButtons.Down}") ? 1 : 0;
					gameInput.flightStick.left    = controller.IsPressed($"P{port} {FlightStickButtons.Left}") ? 1 : 0;
					gameInput.flightStick.right   = controller.IsPressed($"P{port} {FlightStickButtons.Right}") ? 1 : 0;
					gameInput.flightStick.fire    = controller.IsPressed($"P{port} {FlightStickButtons.Fire}") ? 1 : 0;
					gameInput.flightStick.buttonA = controller.IsPressed($"P{port} {FlightStickButtons.ButtonA}") ? 1 : 0;
					gameInput.flightStick.buttonB = controller.IsPressed($"P{port} {FlightStickButtons.ButtonB}") ? 1 : 0;
					gameInput.flightStick.buttonC = controller.IsPressed($"P{port} {FlightStickButtons.ButtonC}") ? 1 : 0;
					gameInput.flightStick.buttonX = controller.IsPressed($"P{port} {FlightStickButtons.ButtonX}") ? 1 : 0;
					gameInput.flightStick.buttonP = controller.IsPressed($"P{port} {FlightStickButtons.ButtonP}") ? 1 : 0;
					gameInput.flightStick.leftTrigger = controller.IsPressed($"P{port} {FlightStickButtons.LeftTrigger}") ? 1 : 0;
					gameInput.flightStick.rightTrigger = controller.IsPressed($"P{port} {FlightStickButtons.RightTrigger}") ? 1 : 0;
					gameInput.flightStick.horizontalAxis = controller.AxisValue($"P{port} {Inputs.FlighStickHorizontalAxis}");
					gameInput.flightStick.verticalAxis = controller.AxisValue($"P{port} {Inputs.FlighStickVerticalAxis}");
					gameInput.flightStick.altitudeAxis = controller.AxisValue($"P{port} {Inputs.FlighStickAltitudeAxis}");
					break;

				case ControllerType.LightGun:
			        gameInput.lightGun.trigger = controller.IsPressed($"P{port} {LightGunButtons.Trigger}") ? 1 : 0;
					gameInput.lightGun.select = controller.IsPressed($"P{port} {LightGunButtons.Select}") ? 1 : 0;
					gameInput.lightGun.reload = controller.IsPressed($"P{port} {LightGunButtons.Reload}") ? 1 : 0;
					gameInput.lightGun.isOffScreen = controller.IsPressed($"P{port} {LightGunButtons.IsOffScreen}") ? 1 : 0;
					gameInput.lightGun.screenX = controller.AxisValue($"P{port} {Inputs.LightGunScreenX}");
					gameInput.lightGun.screenY = controller.AxisValue($"P{port} {Inputs.LightGunScreenY}");
					break;

				case ControllerType.ArcadeLightGun:
					gameInput.arcadeLightGun.trigger = controller.IsPressed($"P{port} {ArcadeLightGunButtons.Trigger}") ? 1 : 0;
					gameInput.arcadeLightGun.select = controller.IsPressed($"P{port} {ArcadeLightGunButtons.Select}") ? 1 : 0;
					gameInput.arcadeLightGun.start = controller.IsPressed($"P{port} {ArcadeLightGunButtons.Start}") ? 1 : 0;
					gameInput.arcadeLightGun.reload = controller.IsPressed($"P{port} {ArcadeLightGunButtons.Reload}") ? 1 : 0;
					gameInput.arcadeLightGun.auxA = controller.IsPressed($"P{port} {ArcadeLightGunButtons.AuxA}") ? 1 : 0;
					gameInput.arcadeLightGun.isOffScreen = controller.IsPressed($"P{port} {ArcadeLightGunButtons.IsOffScreen}") ? 1 : 0;
					gameInput.arcadeLightGun.screenX = controller.AxisValue($"P{port} {Inputs.LightGunScreenX}");
					gameInput.arcadeLightGun.screenY = controller.AxisValue($"P{port} {Inputs.LightGunScreenY}");
					break;
			}

			return gameInput;
		}

		protected override void FrameAdvancePost()
		{
		}

		protected override void SaveStateBinaryInternal(BinaryWriter writer)
		{
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
		}

	}
}