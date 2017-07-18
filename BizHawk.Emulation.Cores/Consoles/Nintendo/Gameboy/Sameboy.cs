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
		private bool _sgb;

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
			var bios = Util.DecompressGzipFile(new MemoryStream(_cgb ? Resources.SameboyCgbBoot : Resources.SameboyDmgBoot));
			// var bios = comm.CoreFileProvider.GetFirmware(_cgb ? "GBC" : "GB", "World", true);

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

		static Sameboy()
		{
			_gbDefinition = CreateControllerDefinition(1);
			_sgbDefinition = CreateControllerDefinition(4);
		}

		private LibSameboy.Buttons GetButtons(IController c)
		{
			LibSameboy.Buttons b = 0;
			for (int i = _sgb ? 4 : 1; i > 0; i--)
			{
				if (c.IsPressed($"P{i} Up"))
					b |= LibSameboy.Buttons.UP;
				if (c.IsPressed($"P{i} Down"))
					b |= LibSameboy.Buttons.DOWN;
				if (c.IsPressed($"P{i} Left"))
					b |= LibSameboy.Buttons.LEFT;
				if (c.IsPressed($"P{i} Right"))
					b |= LibSameboy.Buttons.RIGHT;
				if (c.IsPressed($"P{i} A"))
					b |= LibSameboy.Buttons.A;
				if (c.IsPressed($"P{i} B"))
					b |= LibSameboy.Buttons.B;
				if (c.IsPressed($"P{i} Select"))
					b |= LibSameboy.Buttons.SELECT;
				if (c.IsPressed($"P{i} Start"))
					b |= LibSameboy.Buttons.START;
				if (i != 1)
					b = (LibSameboy.Buttons)((uint)b << 8);
			}
			return b;
		}

		#endregion

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			return new LibSameboy.FrameInfo
			{
				Time = 0,
				Keys = GetButtons(controller)
			};
		}

		public bool IsCGBMode() => _cgb;
	}
}
