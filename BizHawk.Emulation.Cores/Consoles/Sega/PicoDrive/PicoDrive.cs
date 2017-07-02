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
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			return new LibWaterboxCore.FrameInfo();
		}
	}
}
