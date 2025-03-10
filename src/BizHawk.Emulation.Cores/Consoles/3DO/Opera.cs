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

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var fi = new LibOpera.FrameInfo();

			// Setting joystick inputs
			fi.port1 = 0;
			if (_syncSettings.Controller1Type == ControllerType.Gamepad)
			{
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Up}")       ? JoystickButtonCodes.Up      : 0);
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Down}")     ? JoystickButtonCodes.Down    : 0);
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Left}")     ? JoystickButtonCodes.Left    : 0);
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Right}")    ? JoystickButtonCodes.Right   : 0);
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Select}")   ? JoystickButtonCodes.Select  : 0);
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Start}")    ? JoystickButtonCodes.Start   : 0);
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.ButtonA}")  ? JoystickButtonCodes.ButtonA : 0);
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.ButtonB}")  ? JoystickButtonCodes.ButtonB : 0);
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.ButtonX}")  ? JoystickButtonCodes.ButtonX : 0);
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.ButtonY}")  ? JoystickButtonCodes.ButtonY : 0);
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.ButtonL}")  ? JoystickButtonCodes.ButtonL : 0);
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.ButtonR}")  ? JoystickButtonCodes.ButtonR : 0);
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.ButtonL2}") ? JoystickButtonCodes.ButtonL : 0);
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.ButtonR2}") ? JoystickButtonCodes.ButtonR : 0);
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.ButtonL3}") ? JoystickButtonCodes.ButtonL : 0);
				fi.port1 += (UInt16)(controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.ButtonR3}") ? JoystickButtonCodes.ButtonR : 0);
			}

			fi.port2 = 0;
			if (_syncSettings.Controller1Type == ControllerType.Gamepad)
			{
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Up}") ? (uint) JoystickButtonCodes.Up : 0);
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Down}") ? (uint) JoystickButtonCodes.Down : 0);
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Left}") ? (uint) JoystickButtonCodes.Left : 0);
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Right}") ? (uint) JoystickButtonCodes.Right : 0);
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Select}") ? (uint) JoystickButtonCodes.Select : 0);
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Start}") ? (uint) JoystickButtonCodes.Start : 0);
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.ButtonA}") ? (uint) JoystickButtonCodes.ButtonA : 0);
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.ButtonB}") ? (uint) JoystickButtonCodes.ButtonB : 0);
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.ButtonX}") ? (uint) JoystickButtonCodes.ButtonX : 0);
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.ButtonY}") ? (uint) JoystickButtonCodes.ButtonY : 0);
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.ButtonL}") ? (uint) JoystickButtonCodes.ButtonL : 0);
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.ButtonR}") ? (uint) JoystickButtonCodes.ButtonR : 0);
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.ButtonL2}") ? (uint) JoystickButtonCodes.ButtonL : 0);
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.ButtonR2}") ? (uint) JoystickButtonCodes.ButtonR : 0);
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.ButtonL3}") ? (uint) JoystickButtonCodes.ButtonL : 0);
				fi.port2 += (UInt16)(controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.ButtonR3}") ? (uint) JoystickButtonCodes.ButtonR : 0);
			}

			return fi;
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