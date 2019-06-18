using System;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M6502;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	partial class QuickNES
	{
		public TraceBuffer Tracer { get; private set; }

		private LibQuickNES.TraceCallback _tracecb;

		private const string TraceHeader = "6502: PC, mnemonic, operands, registers (A, X, Y, P, SP)";

		private void MakeTrace(IntPtr data)
		{
			int[] s = new int[7];
			Marshal.Copy(data, s, 0, 7);

			byte a = (byte)s[0];
			byte x = (byte)s[1];
			byte y = (byte)s[2];
			ushort sp = (ushort)s[3];
			ushort pc = (ushort)s[4];
			byte p = (byte)s[5];

			byte opcode = (byte)s[6];

			int notused = 0;
			string opcodeStr = MOS6502X.Disassemble(pc, out notused, (address) => _memoryDomains.SystemBus.PeekByte(address));

			Tracer.Put(new TraceInfo
			{
				Disassembly = $"{pc:X4}:  {opcodeStr}".PadRight(26),
				RegisterInfo = string.Join(" ",
					$"A:{a:X2}",
					$"X:{x:X2}",
					$"Y:{y:X2}",
					$"P:{p:X2}",
					$"SP:{sp:X2}")
			});
		}

		private void ConnectTracer()
		{
			Tracer = new TraceBuffer { Header = TraceHeader };
			(ServiceProvider as BasicServiceProvider).Register<ITraceable>(Tracer);
			_tracecb = new LibQuickNES.TraceCallback(MakeTrace);
		}
	}
}
