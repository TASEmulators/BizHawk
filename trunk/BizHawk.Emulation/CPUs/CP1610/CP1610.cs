using System;
using System.IO;

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

		private bool logging = true;
		private string log = "";

		~CP1610()
		{
			using (StreamWriter write = new StreamWriter("log.txt"))
			{
				if (logging)
					write.Write(log);
			}
		}

		public void LogData()
		{
			if (!logging)
				return;
			for (int register = 0; register <= 5; register++)
				log += string.Format("R{0:d} = {1:X4}\n", register, Register[register]);
			log += string.Format("SP = {0:X4}\n", RegisterSP);
			log += string.Format("PC = {0:X4}\n", RegisterPC);
			log += string.Format("S = {0:X4}\n", FlagS);
			log += string.Format("C = {0:X4}\n", FlagC);
			log += string.Format("Z = {0:X4}\n", FlagZ);
			log += string.Format("O = {0:X4}\n", FlagO);
			log += string.Format("I = {0:X4}\n", FlagI);
			log += string.Format("D = {0:X4}\n", FlagD);
			log += "------\n";
		}
	}
}
