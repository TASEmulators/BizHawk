using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Belogic
{
	[PortedCore(CoreNames.Uzem, "David Etherton")]
	public class Uzem : WaterboxCore
	{
		private LibUzem _uze;
		private readonly bool _mouseEnabled;

		[CoreConstructor(VSystemID.Raw.UZE)]
		public Uzem(CoreComm comm, byte[] rom)
			: base(comm, new Configuration
			{
				DefaultWidth = 720,
				DefaultHeight = 224,
				MaxWidth = 720,
				MaxHeight = 224,
				MaxSamples = 4096,
				SystemId = VSystemID.Raw.UZE,
				DefaultFpsNumerator = 1125000,
				DefaultFpsDenominator = 18733,
			})
		{
			_uze = PreInit<LibUzem>(new WaterboxOptions
			{
				Filename = "uzem.wbx",
				SbrkHeapSizeKB = 20,
				SealedHeapSizeKB = 4,
				InvisibleHeapSizeKB = 4,
				MmapHeapSizeKB = 4,
				PlainHeapSizeKB = 4,
				SkipCoreConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			_exe.AddReadonlyFile(rom, "romfile");
			if (!_uze.Init())
				throw new InvalidOperationException("Core rejected the rom!");
			_mouseEnabled = _uze.MouseEnabled();
			_exe.RemoveReadonlyFile("romfile");

			PostInit();
		}

		private static readonly ControllerDefinition TwoPads = new ControllerDefinition("SNES Controller")
		{
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Select", "P1 Start", "P1 X", "P1 A", "P1 B", "P1 Y", "P1 L", "P1 R",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Select", "P2 Start", "P2 X", "P2 A", "P2 B", "P2 Y", "P2 L", "P2 R",
				"Power"
			}
		}.MakeImmutable();

		private static readonly ControllerDefinition Mouse = new ControllerDefinition("SNES Controller")
		{
			BoolButtons = { "P1 Mouse Left", "P1 Mouse Right", "Power" }
		}.AddXYPair("P1 Mouse {0}", AxisPairOrientation.RightAndUp, (-127).RangeTo(127), 0) //TODO verify direction against hardware
			.MakeImmutable();

		private static readonly string[] PadBits =
		{
			"B", "Y", "Select", "Start", "Up", "Down", "Left", "Right", "A", "X", "L", "R"
		};

		private static int EncodePad(IController c, int p)
		{
			int ret = unchecked((int)0xffff0000);
			int val = 1;
			int idx = 0;
			foreach (var s in PadBits)
			{
				if (c.IsPressed("P" + p + " " + PadBits[idx++]))
					ret |= val;
				val <<= 1;
			}
			return ret;
		}

		private static int EncodeDelta(float value)
		{
			int v = (int)value;
			if (v > 127)
				v = 127;
			if (v < -127)
				v = -127;

			int ret = 0;
			if (v < 0)
			{
				ret |= 1;
				v = -v;
			}

			int mask = 64;
			int bit = 2;
			while (mask != 0)
			{
				if ((v & mask) != 0)
					ret |= bit;
				mask >>= 1;
				bit <<= 1;
			}
			return ret;
		}

		public override ControllerDefinition ControllerDefinition => _mouseEnabled ? Mouse : TwoPads;

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var ret = new LibUzem.FrameInfo();
			if (_mouseEnabled)
			{
				ret.ButtonsP1 = EncodeDelta(controller.AxisValue("P1 Mouse X")) << 24
					| EncodeDelta(controller.AxisValue("P1 Mouse Y")) << 16
					| 0x8000;
				if (controller.IsPressed("P1 Mouse Left"))
					ret.ButtonsP1 |= 0x200;
				if (controller.IsPressed("P1 Mouse Right"))
					ret.ButtonsP1 |= 0x100;
			}
			else
			{
				ret.ButtonsP1 = EncodePad(controller, 1);
				ret.ButtonsP2 = EncodePad(controller, 2);
			}
			if (controller.IsPressed("Power"))
				ret.ButtonsConsole = 1;

			return ret;
		}

		// with the high clockrate and full software signal generation, horizontal resolution is
		// very variable.  in practice, 240 wide, 360 wide, and 180 wide are what you'll see...
		public override int VirtualWidth => 619;
		public override int VirtualHeight => 448;
	}
}
