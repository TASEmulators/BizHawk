using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Disassembler
	/// </summary>
	public sealed partial class F3850 : IDisassemblable
	{


		#region IDisassemblable

		public string Cpu
		{
			get { return "F3850"; }
			set { }
		}

		public string PCRegisterName
		{
			get { return "PC"; }
		}

		public IEnumerable<string> AvailableCpus
		{
			get { yield return "F3850"; }
		}

		public string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			length = 0;
			string ret = "";// Disassemble((ushort)addr, a => m.PeekByte(a), out length);
			return ret;
		}

		#endregion
	}
}
