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

		private static bool logging = true;
		private static StreamWriter log;

		static CP1610()
		{
			if (logging)
				log = new StreamWriter("log_CP1610.txt");
		}

		public void LogData()
		{
			if (!logging)
				return;
			for (int register = 0; register <= 5; register++)
				log.WriteLine("R{0:d} = {1:X4}", register, Register[register]);
			log.WriteLine("SP = {0:X4}", RegisterSP);
			log.WriteLine("PC = {0:X4}", RegisterPC);
			log.WriteLine("S = {0:X4}", FlagS);
			log.WriteLine("C = {0:X4}", FlagC);
			log.WriteLine("Z = {0:X4}", FlagZ);
			log.WriteLine("O = {0:X4}", FlagO);
			log.WriteLine("I = {0:X4}", FlagI);
			log.WriteLine("D = {0:X4}", FlagD);
			log.WriteLine("------");
			log.WriteLine();
			log.Flush();
		}
	}
}
