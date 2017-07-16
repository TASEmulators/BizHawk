using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy
{
	[Core("SameBoy", "LIJI32", true, false, "efc11783c7fb6da66e1dd084e41ba6a85c0bd17e",
		"https://sameboy.github.io/", false)]
	public class Sameboy : WaterboxCore, IGameboyCommon
	{
		/// <summary>
		/// the nominal length of one frame
		/// </summary>
		private const int TICKSPERFRAME = 35112;

		/// <summary>
		/// number of ticks per second (GB, CGB)
		/// </summary>
		private const int TICKSPERSECOND = 2097152;

		/// <summary>
		/// number of ticks per second (SGB)
		/// </summary>
		private const int TICKSPERSECOND_SGB = 2147727;

		private LibSameboy _core;
		private bool _cgb;

		[CoreConstructor("GB")]
		public Sameboy(CoreComm comm, byte[] rom)
			: base(comm, new Configuration
			{
				DefaultWidth = 160,
				DefaultHeight = 144,
				MaxWidth = 256,
				MaxHeight = 224,
				MaxSamples = 1024,
				DefaultFpsNumerator = TICKSPERSECOND,
				DefaultFpsDenominator = TICKSPERFRAME,
				SystemId = "GB"
			})
		{
			_core = PreInit<LibSameboy>(new PeRunnerOptions
			{
				Filename = "sameboy.wbx",
				SbrkHeapSizeKB = 128,
				InvisibleHeapSizeKB = 16 * 1024,
				SealedHeapSizeKB = 5 * 1024,
				PlainHeapSizeKB = 4096,
				MmapHeapSizeKB = 34 * 1024
			});

			_cgb = (rom[0x143] & 0xc0) == 0xc0;
			Console.WriteLine("Automaticly detected CGB to " + _cgb);
			var bios = Util.DecompressGzipFile(new MemoryStream(
				_cgb ? Resources.SameboyCgbBoot : Resources.SameboyDmgBoot));

			_exe.AddReadonlyFile(rom, "game.rom");
			_exe.AddReadonlyFile(bios, "boot.rom");

			if (!_core.Init(_cgb))
			{
				throw new InvalidOperationException("Core rejected the rom!");
			}

			_exe.RemoveReadonlyFile("game.rom");
			_exe.RemoveReadonlyFile("boot.rom");

			PostInit();
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			return new LibSameboy.FrameInfo
			{
			};
		}

		public bool IsCGBMode() => _cgb;
	}
}
