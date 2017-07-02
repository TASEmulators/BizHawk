using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Sega.PicoDrive
{
	[CoreAttributes("PicoDrive", "notaz", true, false,
		"0e352905c7aa80b166933970abbcecfce96ad64e", "https://github.com/notaz/picodrive", false)]
	public class PicoDrive : WaterboxCore
	{
		private LibPicoDrive _core;

		[CoreConstructor("GEN")]
		public PicoDrive(CoreComm comm, byte[] rom)
			: base(comm, new Configuration
			{
				MaxSamples = 2048,
				DefaultWidth = 320,
				DefaultHeight = 224,
				MaxWidth = 320,
				MaxHeight = 480,
				SystemId = "GEN"
			})
		{
			_core = PreInit<LibPicoDrive>(new PeRunnerOptions
			{
				Filename = "picodrive.wbx",
				SbrkHeapSizeKB = 4096,
				SealedHeapSizeKB = 4096,
				InvisibleHeapSizeKB = 4096,
				MmapHeapSizeKB = 65536,
				PlainHeapSizeKB = 4096,
			});

			_exe.AddReadonlyFile(rom, "romfile.md");
			if (!_core.Init())
				throw new InvalidOperationException("Core rejected the rom!");
			_exe.RemoveReadonlyFile("romfile.md");
			PostInit();
			ControllerDefinition = PicoDriveController;
		}

		public static readonly ControllerDefinition PicoDriveController = new ControllerDefinition
		{
			Name = "PicoDrive Genesis Controller",
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 A", "P1 B", "P1 C", "P1 Start", "P1 X", "P1 Y", "P1 Z", "P1 Mode",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 A", "P2 B", "P2 C", "P2 Start", "P2 X", "P2 Y", "P2 Z", "P2 Mode",
				"Power", "Reset"
			}
		};

		private static readonly string[] ButtonOrders =
		{
			"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 B", "P1 C", "P1 A", "P1 Start", "P1 Z", "P1 Y", "P1 X", "P1 Mode",
			"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 B", "P2 C", "P2 A", "P2 Start", "P2 Z", "P2 Y", "P2 X", "P2 Mode",
			"Power", "Reset"
		};

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var b = 0;
			var v = 1;
			foreach (var s in ButtonOrders)
			{
				if (controller.IsPressed(s))
					b |= v;
				v <<= 1;
			}
			return new LibPicoDrive.FrameInfo { Buttons = b };
		}
	}
}
