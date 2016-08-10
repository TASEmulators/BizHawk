using System.Collections.Generic;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M68000;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx64
{
	public partial class GPGX : IDisassemblable
	{
		public string Cpu
		{
			get
			{
				return "M68000";
			}
			set
			{
			}
		}

		public string PCRegisterName
		{
			get { return "M68K PC"; }
		}

		public IEnumerable<string> AvailableCpus
		{
			get { yield return "M68000"; }
		}

		public string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			_disassemblerInstance.ReadWord = (a) => (short)m.PeekUshort(a, m.EndianType == MemoryDomain.Endian.Big);
			_disassemblerInstance.ReadByte = (a) => (sbyte)m.PeekByte(a);
			_disassemblerInstance.ReadLong = (a) => (int)m.PeekUint(a, m.EndianType == MemoryDomain.Endian.Big);
			var info = _disassemblerInstance.Disassemble((int)addr);

			length = info.Length;

			return string.Format("{0:X4}  {1,-7} {2}", info.RawBytes.Substring(0, 4), info.Mnemonic, info.Args);
		}

		// TODO: refactor MC6800's disassembler to be a static call
		private MC68000 _disassemblerInstance = new MC68000();
	}
}
