using BizHawk.Common;
using BizHawk.Common.BizInvoke;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy
{
	[CoreAttributes("Pizza Boy", "Davide Berra", true, false, "c7bc6ee376028b3766de8d7a02e60ab794841f45",
		"https://github.com/davideberra/emu-pizza/", false)]
	public class Pizza : WaterboxCore, IGameboyCommon
	{
		private LibPizza _pizza;

		[CoreConstructor("GB")]
		public Pizza(byte[] rom, CoreComm comm)
			:base(comm, new Configuration
			{
				DefaultWidth = 160,
				DefaultHeight = 144,
				MaxWidth = 160,
				MaxHeight = 144,
				MaxSamples = 1024,
				SystemId = "GB",
				DefaultFpsNumerator = TICKSPERSECOND,
				DefaultFpsDenominator = TICKSPERFRAME
			})
		{
			ControllerDefinition = BizHawk.Emulation.Cores.Nintendo.Gameboy.Gameboy.GbController;

			_pizza = PreInit<LibPizza>(new PeRunnerOptions
			{
				Filename = "pizza.wbx",
				SbrkHeapSizeKB = 2 * 1024,
				InvisibleHeapSizeKB = 16 * 1024,
				SealedHeapSizeKB = 16 * 1024,
				PlainHeapSizeKB = 16 * 1024,
				MmapHeapSizeKB = 32 * 1024
			});

			if (!_pizza.Init(rom, rom.Length))
			{
				throw new InvalidOperationException("Core rejected the rom!");
			}

			PostInit();
		}

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

		private static LibPizza.Buttons GetButtons(IController c)
		{
			LibPizza.Buttons b = 0;
			if (c.IsPressed("Up"))
				b |= LibPizza.Buttons.UP;
			if (c.IsPressed("Down"))
				b |= LibPizza.Buttons.DOWN;
			if (c.IsPressed("Left"))
				b |= LibPizza.Buttons.LEFT;
			if (c.IsPressed("Right"))
				b |= LibPizza.Buttons.RIGHT;
			if (c.IsPressed("A"))
				b |= LibPizza.Buttons.A;
			if (c.IsPressed("B"))
				b |= LibPizza.Buttons.B;
			if (c.IsPressed("Select"))
				b |= LibPizza.Buttons.SELECT;
			if (c.IsPressed("Start"))
				b |= LibPizza.Buttons.START;
			return b;
		}

		LibPizza.FrameInfo _tmp; // TODO: clean this up so it's not so hacky

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			return _tmp = new LibPizza.FrameInfo
			{
				Keys = GetButtons(controller)
			};
		}

		protected override void FrameAdvancePost()
		{
			Console.WriteLine(_tmp.Cycles);
			_tmp = null;
		}

		public bool IsCGBMode() => _pizza.IsCGB();

	}
}
