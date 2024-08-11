using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M68000;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : IDisassemblable
	{
		public string Cpu
		{
			get => "M68000";
			set
			{
			}
		}

		public string PCRegisterName => "M68K PC";

		public IEnumerable<string> AvailableCpus { get; } = [ "M68000" ];

		public string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			_disassemblerInstance.ReadWord = a => (short)m.PeekUshort(a, m.EndianType == MemoryDomain.Endian.Big);
			_disassemblerInstance.ReadByte = a => (sbyte)m.PeekByte(a);
			_disassemblerInstance.ReadLong = a => (int)m.PeekUint(a, m.EndianType == MemoryDomain.Endian.Big);
			var info = _disassemblerInstance.Disassemble((int)addr);

			length = info.Length;

			return $"{info.RawBytes[..4]}  {info.Mnemonic,-7} {info.Args}";
		}

		// TODO: refactor MC6800's disassembler to be a static call
		private readonly MC68000 _disassemblerInstance = new();
	}
}
