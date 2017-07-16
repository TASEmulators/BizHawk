using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Magnavox
{
	[Core("o2em", "", true, false, "", "", false)]
	public class O2Em : WaterboxCore
	{
		private LibO2Em _core;

		[CoreConstructor("O2")]
		public O2Em(CoreComm comm, byte[] rom)
			:base(comm, new Configuration
			{
				DefaultFpsNumerator = 60,
				DefaultFpsDenominator = 1,
				DefaultWidth = 320,
				DefaultHeight = 240,
				MaxSamples = 2048,
				MaxWidth = 320,
				MaxHeight = 240,
				SystemId = "O2"
			})
		{
			var bios = comm.CoreFileProvider.GetFirmware("O2", "BIOS", true);
			_core = PreInit<LibO2Em>(new PeRunnerOptions
			{
				Filename = "o2em.wbx",
				SbrkHeapSizeKB = 4 * 1024,
				SealedHeapSizeKB = 4 * 1024,
				InvisibleHeapSizeKB = 4 * 1024,
				PlainHeapSizeKB = 4 * 1024,
			});


			if (!_core.Init(rom, rom.Length, bios, bios.Length))
				throw new InvalidOperationException("Init() failed");

			PostInit();
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			return new LibWaterboxCore.FrameInfo();
		}
	}
}
