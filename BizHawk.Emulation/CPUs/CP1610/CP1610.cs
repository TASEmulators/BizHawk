using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.CPUs.CP1610
{
	public sealed partial class CP1610
	{
		private bool FlagS, FlagC, FlagZ, FlagO, FlagI, FlagD;
		private ushort[] Register = new ushort[8];
		public ushort RegisterSP { get { return Register[6]; } set { Register[6] = value; } }
		public ushort RegisterPC { get { return Register[7]; } set { Register[7] = value; } }

		public int TotalExecutedCycles;
		public int PendingCycles;

		public Func<ushort, ushort> ReadMemory;
		public Action<ushort, ushort> WriteMemory;
	}
}
