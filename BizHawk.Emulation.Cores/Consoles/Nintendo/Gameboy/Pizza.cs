using BizHawk.Common;
using BizHawk.Common.BizInvoke;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy
{
	[CoreAttributes("Pizza Boy", "Davide Berra", true, true, "c7bc6ee376028b3766de8d7a02e60ab794841f45",
		"https://github.com/davideberra/emu-pizza/", false)]
	public class Pizza : WaterboxCore, IGameboyCommon
	{
		private LibPizza _pizza;
		private readonly bool _sgb;

		[CoreConstructor("SGB")]
		public Pizza(byte[] rom, CoreComm comm)
			: this(rom, comm, true)
		{ }

		[CoreConstructor("GB")]
		public Pizza(CoreComm comm, byte[] rom)
			: this(rom, comm, false)
		{ }

		public Pizza(byte[] rom, CoreComm comm, bool sgb)
			: base(comm, new Configuration
			{
				DefaultWidth = 160,
				DefaultHeight = 144,
				MaxWidth = 256,
				MaxHeight = 224,
				MaxSamples = 1024,
				DefaultFpsNumerator = TICKSPERSECOND,
				DefaultFpsDenominator = TICKSPERFRAME
			})
		{
			if (sgb && (rom[0x143] & 0xc0) == 0xc0)
			{
				throw new CGBNotSupportedException();
			}

			_pizza = PreInit<LibPizza>(new PeRunnerOptions
			{
				Filename = "pizza.wbx",
				SbrkHeapSizeKB = 128,
				InvisibleHeapSizeKB = 16,
				SealedHeapSizeKB = 5 * 1024,
				PlainHeapSizeKB = 16,
				MmapHeapSizeKB = 0
			});

			var spc = sgb
				? Util.DecompressGzipFile(new MemoryStream(Properties.Resources.SgbCartPresent_SPC))
				: new byte[0];
			_sgb = sgb;
			if (!_pizza.Init(rom, rom.Length, _sgb, spc, spc.Length))
			{
				throw new InvalidOperationException("Core rejected the rom!");
			}

			PostInit();

			if (_sgb)
			{
				VsyncNumerator = TICKSPERSECOND_SGB;
				BufferWidth = 256;
				BufferHeight = 224;
			}
			InitializeRtc(new DateTime(2010, 1, 1)); // TODO: connect to syncsettings
			Console.WriteLine("Pizza Initialized: CGB {0} SGB {1}", IsCGBMode(), IsSGBMode());
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

		#region Controller

		private static readonly ControllerDefinition _gbDefinition;
		private static readonly ControllerDefinition _sgbDefinition;
		public override ControllerDefinition ControllerDefinition => _sgb ? _sgbDefinition : _gbDefinition;

		private static ControllerDefinition CreateControllerDefinition(int p)
		{
			var ret = new ControllerDefinition { Name = "Gameboy Controller" };
			for (int i = 0; i < p; i++)
			{
				ret.BoolButtons.AddRange(
					new[] { "Up", "Down", "Left", "Right", "A", "B", "Select", "Start" }
						.Select(s => $"P{i + 1} {s}"));
			}
			return ret;
		}

		static Pizza()
		{
			_gbDefinition = CreateControllerDefinition(1);
			_sgbDefinition = CreateControllerDefinition(4);
		}

		private LibPizza.Buttons GetButtons(IController c)
		{
			LibPizza.Buttons b = 0;
			for (int i = _sgb ? 4 : 1; i > 0; i--)
			{
				if (c.IsPressed($"P{i} Up"))
					b |= LibPizza.Buttons.UP;
				if (c.IsPressed($"P{i} Down"))
					b |= LibPizza.Buttons.DOWN;
				if (c.IsPressed($"P{i} Left"))
					b |= LibPizza.Buttons.LEFT;
				if (c.IsPressed($"P{i} Right"))
					b |= LibPizza.Buttons.RIGHT;
				if (c.IsPressed($"P{i} A"))
					b |= LibPizza.Buttons.A;
				if (c.IsPressed($"P{i} B"))
					b |= LibPizza.Buttons.B;
				if (c.IsPressed($"P{i} Select"))
					b |= LibPizza.Buttons.SELECT;
				if (c.IsPressed($"P{i} Start"))
					b |= LibPizza.Buttons.START;
				if (i != 1)
					b = (LibPizza.Buttons)((uint)b << 8);
			}
			return b;
		}

		#endregion

		LibPizza.FrameInfo _tmp; // TODO: clean this up so it's not so hacky

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			return _tmp = new LibPizza.FrameInfo
			{
				Time = GetRtcTime(false),
				Keys = GetButtons(controller)
			};
		}

		protected override void FrameAdvancePost()
		{
			//Console.WriteLine(_tmp.Cycles);
			_tmp = null;
		}

		public bool IsCGBMode() => _pizza.IsCGB();
		public bool IsSGBMode() => _sgb;

		public override string SystemId => _sgb ? "SGB" : "GB";
	}
}
