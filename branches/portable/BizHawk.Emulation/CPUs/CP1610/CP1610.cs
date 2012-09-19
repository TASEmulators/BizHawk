using System;
using System.IO;

namespace BizHawk.Emulation.CPUs.CP1610
{
	public sealed partial class CP1610
	{
		private const ushort RESET = 0x1000;
		private const ushort INTERRUPT = 0x1004;

		private bool FlagS, FlagC, FlagZ, FlagO, FlagI, FlagD, IntRM, BusRq, BusAk, MSync, Interruptible, Interrupted;
		private ushort[] Register = new ushort[8];
		private ushort RegisterSP { get { return Register[6]; } set { Register[6] = value; } }
		private ushort RegisterPC { get { return Register[7]; } set { Register[7] = value; } }

		public int TotalExecutedCycles;
		public int PendingCycles;

		public Func<ushort, ushort> ReadMemory;
		public Func<ushort, ushort, bool> WriteMemory;

		private static bool Logging = false;
		private static StreamWriter Log;

		static CP1610()
		{
			if (Logging)
				Log = new StreamWriter("log_CP1610.txt");
		}

		public void Reset()
		{
			BusAk = true;
			Interruptible = false;
			FlagS = FlagC = FlagZ = FlagO = FlagI = FlagD = false;
			for (int register = 0; register <= 6; register++)
				Register[register] = 0;
			RegisterPC = RESET;
		}

		public bool GetBusAk()
		{
			return BusAk;
		}

		public void SetIntRM(bool value)
		{
			IntRM = value;
		}

		public void SetBusRq(bool value)
		{
			BusRq = value;
		}

		public int GetPendingCycles()
		{
			return PendingCycles;
		}

		public void AddPendingCycles(int cycles)
		{
			PendingCycles += cycles;
		}

		public void LogData()
		{
			if (!Logging)
				return;
			for (int register = 0; register <= 5; register++)
				Log.WriteLine("R{0:d} = {1:X4}", register, Register[register]);
			Log.WriteLine("SP = {0:X4}", RegisterSP);
			Log.WriteLine("PC = {0:X4}", RegisterPC);
			Log.WriteLine("S = {0}", FlagS);
			Log.WriteLine("C = {0}", FlagC);
			Log.WriteLine("Z = {0}", FlagZ);
			Log.WriteLine("O = {0}", FlagO);
			Log.WriteLine("I = {0}", FlagI);
			Log.WriteLine("D = {0}", FlagD);
			Log.WriteLine("INTRM = {0}", IntRM);
			Log.WriteLine("BUSRQ = {0}", BusRq);
			Log.WriteLine("BUSAK = {0}", BusAk);
			Log.WriteLine("MSYNC = {0}", MSync);
			Log.Flush();
		}
	}
}
