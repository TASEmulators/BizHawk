using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64 : IDisassemblable
	{
		public string Cpu
		{
			get => "R4300";
			set { }
		}

		public IEnumerable<string> AvailableCpus { get; } = [ "R4300" ];

		public string PCRegisterName => "PC";

		public string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			length = 4; // TODO: is this right?
			var instruction = m.PeekUint(addr, true);

			//TODO - reserve buffer here for disassembling into. allocating repeatedly will be slower
			var result = api.m64p_decode_op(instruction, addr);
			string strResult = Marshal.PtrToStringAnsi(result);

			return strResult;
		}
	}
}
