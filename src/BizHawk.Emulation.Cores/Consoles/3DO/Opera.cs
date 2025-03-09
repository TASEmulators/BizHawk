using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;

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

			// Parsing input files
			var ConfigFiles = new List<IRomAsset>();

			_libOpera = PreInit<LibOpera>(new WaterboxOptions
			{
				Filename = "opera.wbx",
				SbrkHeapSizeKB = 4 * 1024,
				SealedHeapSizeKB = 1024,
				InvisibleHeapSizeKB = 1024,
				PlainHeapSizeKB = 1024,
				MmapHeapSizeKB = 256 * 1024,
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[] { });

			////////////// Initializing Core
			if (!_libOpera.Init((int)_syncSettings.Controller1Type, (int)_syncSettings.Controller2Type))
				throw new InvalidOperationException("Core rejected the rom!");

			PostInit();
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var fi = new LibOpera.FrameInfo();

			// Setting joystick inputs
			if (_syncSettings.Controller1Type == ControllerType.Gamepad)
			{
				fi.joystick1.up      = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Up}") ? 1 : 0;
				fi.joystick1.down    = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Down}") ? 1 : 0;
				fi.joystick1.left    = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Left}") ? 1 : 0;
				fi.joystick1.right   = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Right}") ? 1 : 0;
				fi.joystick1.button1 = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Button1}") ? 1 : 0;
				fi.joystick1.button2 = controller.IsPressed($"P1 {Inputs.Joystick} {JoystickButtons.Button2}") ? 1 : 0;
			}

			if (_syncSettings.Controller2Type == ControllerType.Gamepad)
			{
				fi.joystick2.up      = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Up}") ? 1 : 0;
				fi.joystick2.down    = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Down}") ? 1 : 0;
				fi.joystick2.left    = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Left}") ? 1 : 0;
				fi.joystick2.right   = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Right}") ? 1 : 0;
				fi.joystick2.button1 = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Button1}") ? 1 : 0;
				fi.joystick2.button2 = controller.IsPressed($"P2 {Inputs.Joystick} {JoystickButtons.Button2}") ? 1 : 0;
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