using System.Collections.Generic;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public class NDSDisassembler : VerifiedDisassembler
	{
		private readonly LibMelonDS _core;

		public NDSDisassembler(LibMelonDS core)
			=> _core = core;

		public override IEnumerable<string> AvailableCpus => new[]
		{
			"ARM v5",
			"ARM v5 (Thumb)",
			"ARM v4",
			"ARM v4 (Thumb)",
		};

		public override string PCRegisterName => int.Parse(Cpu.Substring(5, 1)) == 5 ? "ARM9 r15" : "ARM7 r15";

		public override string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			if (m is NDS.NDSSystemBus NdsSysBus)
			{
				NdsSysBus.UseArm9 = int.Parse(Cpu.Substring(5, 1)) == 5;
			}

			var ret = new byte[80];
			var type = Cpu switch
			{
				"ARM v5" => LibMelonDS.TraceMask.ARM9_ARM,
				"ARM v5 (Thumb)" => LibMelonDS.TraceMask.ARM9_THUMB,
				"ARM v4" => LibMelonDS.TraceMask.ARM7_ARM,
				"ARM v4 (Thumb)" => LibMelonDS.TraceMask.ARM7_THUMB,
				_ => throw new InvalidOperationException("Invalid CPU mode?")
			};

			if (Cpu.Length == 14)
			{
				addr &= ~1u;
				var op = m.PeekByte(addr) | (uint)m.PeekByte(addr + 1) << 8;
				_core.GetDisassembly(type, op, ret);
				length = 2;
			}
			else
			{
				addr &= ~3u;
				var op = m.PeekByte(addr)
					| (uint)m.PeekByte(addr + 1) << 8
					| (uint)m.PeekByte(addr + 2) << 16
					| (uint)m.PeekByte(addr + 3) << 24;
				_core.GetDisassembly(type, op, ret);
				length = 4;
			}

			return Encoding.ASCII.GetString(ret);
		}
	}
}
