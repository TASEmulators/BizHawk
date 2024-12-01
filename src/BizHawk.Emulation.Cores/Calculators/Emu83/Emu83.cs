using System.Linq;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Calculators.TI83;

namespace BizHawk.Emulation.Cores.Calculators.Emu83
{
	[PortedCore(CoreNames.Emu83, "CasualPokePlayer", "d2e6e1d", "https://github.com/CasualPokePlayer/Emu83")]
	[ServiceNotApplicable(typeof(IBoardInfo), typeof(IRegionable), typeof(ISaveRam), typeof(ISoundProvider))]
	public partial class Emu83 : TI83Common
	{
		private static readonly LibEmu83 LibEmu83;

		static Emu83()
		{
			var resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "libemu83.so" : "libemu83.dll", hasLimitedLifetime: false);
			LibEmu83 = BizInvoker.GetInvoker<LibEmu83>(resolver, CallingConventionAdapters.Native);
		}

		private IntPtr Context = IntPtr.Zero;

		private readonly BasicServiceProvider _serviceProvider;

		private readonly TI83Disassembler _disassembler = new();

		[CoreConstructor(VSystemID.Raw.TI83)]
		public Emu83(CoreLoadParameters<TI83CommonSettings, object> lp)
		{
			try
			{
				_serviceProvider = new BasicServiceProvider(this);
				PutSettings(lp.Settings ?? new TI83CommonSettings());
				var rom = lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new("TI83", "Rom"));
				Context = LibEmu83.TI83_CreateContext(rom, rom.Length);
				if (Context == IntPtr.Zero)
				{
					throw new Exception("Core returned null! Bad ROM?");
				}
				var linkFiles = lp.Roms.Select(r => r.RomData).ToList();
				foreach (var linkFile in linkFiles)
				{
					if (!LibEmu83.TI83_LoadLinkFile(Context, linkFile, linkFile.Length))
					{
						throw new Exception("Core rejected the link files!");
					}
				}
				LibEmu83.TI83_SetLinkFilesAreLoaded(Context);
				_inputCallback = ReadKeyboard;
				LibEmu83.TI83_SetInputCallback(Context, _inputCallback);
				_serviceProvider.Register<IDisassemblable>(_disassembler);
				_traceCallback = MakeTrace;
				LibEmu83.TI83_SetTraceCallback(Context, null);
				const string TRACE_HEADER = "Z80A: PC, machine code, mnemonic, operands, registers (AF, BC, DE, HL, IX, IY, SP, Cy)";
				Tracer = new TraceBuffer(TRACE_HEADER);
				_serviceProvider.Register(Tracer);
				InitMemoryDomains();
				InitMemoryCallbacks();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		private static readonly ControllerDefinition TI83Controller = new ControllerDefinition("TI83 Controller")
		{
			BoolButtons =
			{
				"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "DOT",
				"ON", "ENTER",
				"DOWN", "LEFT", "UP", "RIGHT",
				"PLUS", "MINUS", "MULTIPLY", "DIVIDE",
				"CLEAR", "EXP", "DASH", "PARACLOSE", "TAN", "VARS", "PARAOPEN",
				"COS", "PRGM", "STAT", "COMMA", "SIN", "MATRIX", "X",
				"STO", "LN", "LOG", "SQUARED", "NEG1", "MATH", "ALPHA",
				"GRAPH", "TRACE", "ZOOM", "WINDOW", "Y", "2ND", "MODE", "DEL",
				"SEND",
			},
		}.MakeImmutable();

		private IController _controller = NullController.Instance;

		private byte ReadKeyboard(byte _keyboardMask)
		{
			InputCallbacks.Call();

			// ref TI-9X
			int ret = 0xFF;
			////Console.WriteLine("keyboardMask: {0:X2}",keyboardMask);
			if ((_keyboardMask & 1) == 0)
			{
				if (_controller.IsPressed("DOWN")) ret ^= 1;
				if (_controller.IsPressed("LEFT")) ret ^= 2;
				if (_controller.IsPressed("RIGHT")) ret ^= 4;
				if (_controller.IsPressed("UP")) ret ^= 8;
			}

			if ((_keyboardMask & 2) == 0)
			{
				if (_controller.IsPressed("ENTER")) ret ^= 1;
				if (_controller.IsPressed("PLUS")) ret ^= 2;
				if (_controller.IsPressed("MINUS")) ret ^= 4;
				if (_controller.IsPressed("MULTIPLY")) ret ^= 8;
				if (_controller.IsPressed("DIVIDE")) ret ^= 16;
				if (_controller.IsPressed("EXP")) ret ^= 32;
				if (_controller.IsPressed("CLEAR")) ret ^= 64;
			}

			if ((_keyboardMask & 4) == 0)
			{
				if (_controller.IsPressed("DASH")) ret ^= 1;
				if (_controller.IsPressed("3")) ret ^= 2;
				if (_controller.IsPressed("6")) ret ^= 4;
				if (_controller.IsPressed("9")) ret ^= 8;
				if (_controller.IsPressed("PARACLOSE")) ret ^= 16;
				if (_controller.IsPressed("TAN")) ret ^= 32;
				if (_controller.IsPressed("VARS")) ret ^= 64;
			}

			if ((_keyboardMask & 8) == 0)
			{
				if (_controller.IsPressed("DOT")) ret ^= 1;
				if (_controller.IsPressed("2")) ret ^= 2;
				if (_controller.IsPressed("5")) ret ^= 4;
				if (_controller.IsPressed("8")) ret ^= 8;
				if (_controller.IsPressed("PARAOPEN")) ret ^= 16;
				if (_controller.IsPressed("COS")) ret ^= 32;
				if (_controller.IsPressed("PRGM")) ret ^= 64;
				if (_controller.IsPressed("STAT")) ret ^= 128;
			}

			if ((_keyboardMask & 16) == 0)
			{
				if (_controller.IsPressed("0")) ret ^= 1;
				if (_controller.IsPressed("1")) ret ^= 2;
				if (_controller.IsPressed("4")) ret ^= 4;
				if (_controller.IsPressed("7")) ret ^= 8;
				if (_controller.IsPressed("COMMA")) ret ^= 16;
				if (_controller.IsPressed("SIN")) ret ^= 32;
				if (_controller.IsPressed("MATRIX")) ret ^= 64;
				if (_controller.IsPressed("X")) ret ^= 128;
			}

			if ((_keyboardMask & 32) == 0)
			{
				if (_controller.IsPressed("STO")) ret ^= 2;
				if (_controller.IsPressed("LN")) ret ^= 4;
				if (_controller.IsPressed("LOG")) ret ^= 8;
				if (_controller.IsPressed("SQUARED")) ret ^= 16;
				if (_controller.IsPressed("NEG1")) ret ^= 32;
				if (_controller.IsPressed("MATH")) ret ^= 64;
				if (_controller.IsPressed("ALPHA")) ret ^= 128;
			}

			if ((_keyboardMask & 64) == 0)
			{
				if (_controller.IsPressed("GRAPH")) ret ^= 1;
				if (_controller.IsPressed("TRACE")) ret ^= 2;
				if (_controller.IsPressed("ZOOM")) ret ^= 4;
				if (_controller.IsPressed("WINDOW")) ret ^= 8;
				if (_controller.IsPressed("Y")) ret ^= 16;
				if (_controller.IsPressed("2ND")) ret ^= 32;
				if (_controller.IsPressed("MODE")) ret ^= 64;
				if (_controller.IsPressed("DEL")) ret ^= 128;
			}

			return (byte)ret;
		}
	}
}
