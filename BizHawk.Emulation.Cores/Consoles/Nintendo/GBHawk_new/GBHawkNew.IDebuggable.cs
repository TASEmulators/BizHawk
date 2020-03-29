using System;
using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkNew
{
	public partial class GBHawkNew : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["PCl"] = LibGBHawk.GB_cpu_get_regs(GB_Pntr, 0),
				["PCh"] = LibGBHawk.GB_cpu_get_regs(GB_Pntr, 1),
				["SPl"] = LibGBHawk.GB_cpu_get_regs(GB_Pntr, 2),
				["SPh"] = LibGBHawk.GB_cpu_get_regs(GB_Pntr, 3),
				["A"] = LibGBHawk.GB_cpu_get_regs(GB_Pntr, 4),
				["F"] = LibGBHawk.GB_cpu_get_regs(GB_Pntr, 5),
				["B"] = LibGBHawk.GB_cpu_get_regs(GB_Pntr, 6),
				["C"] = LibGBHawk.GB_cpu_get_regs(GB_Pntr, 7),
				["D"] = LibGBHawk.GB_cpu_get_regs(GB_Pntr, 8),
				["E"] = LibGBHawk.GB_cpu_get_regs(GB_Pntr, 9),
				["H"] = LibGBHawk.GB_cpu_get_regs(GB_Pntr, 10),
				["L"] = LibGBHawk.GB_cpu_get_regs(GB_Pntr, 11),
				["Flag I"] = LibGBHawk.GB_cpu_get_flags(GB_Pntr, 0),
				["Flag C"] = LibGBHawk.GB_cpu_get_flags(GB_Pntr, 1),
				["Flag H"] = LibGBHawk.GB_cpu_get_flags(GB_Pntr, 2),
				["Flag N"] = LibGBHawk.GB_cpu_get_flags(GB_Pntr, 3),
				["Flag Z"] = LibGBHawk.GB_cpu_get_flags(GB_Pntr, 4)
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				case ("PCl"): LibGBHawk.GB_cpu_set_regs(GB_Pntr, 0, (byte)value); break;
				case ("PCh"): LibGBHawk.GB_cpu_set_regs(GB_Pntr, 1, (byte)value); break;
				case ("SPl"): LibGBHawk.GB_cpu_set_regs(GB_Pntr, 2, (byte)value); break;
				case ("SPh"): LibGBHawk.GB_cpu_set_regs(GB_Pntr, 3, (byte)value); break;
				case ("A"): LibGBHawk.GB_cpu_set_regs(GB_Pntr, 4, (byte)value); break;
				case ("F"): LibGBHawk.GB_cpu_set_regs(GB_Pntr, 5, (byte)value); break;
				case ("B"): LibGBHawk.GB_cpu_set_regs(GB_Pntr, 6, (byte)value); break;
				case ("C"): LibGBHawk.GB_cpu_set_regs(GB_Pntr, 7, (byte)value); break;
				case ("D"): LibGBHawk.GB_cpu_set_regs(GB_Pntr, 8, (byte)value); break;
				case ("E"): LibGBHawk.GB_cpu_set_regs(GB_Pntr, 9, (byte)value); break;
				case ("H"): LibGBHawk.GB_cpu_set_regs(GB_Pntr, 10, (byte)value); break;
				case ("L"): LibGBHawk.GB_cpu_set_regs(GB_Pntr, 11, (byte)value); break;

				case ("Flag I"): LibGBHawk.GB_cpu_set_flags(GB_Pntr, 0, value > 0); break;
				case ("Flag C"): LibGBHawk.GB_cpu_set_flags(GB_Pntr, 1, value > 0); break;
				case ("Flag H"): LibGBHawk.GB_cpu_set_flags(GB_Pntr, 2, value > 0); break;
				case ("Flag N"): LibGBHawk.GB_cpu_set_flags(GB_Pntr, 3, value > 0); break;
				case ("Flag Z"): LibGBHawk.GB_cpu_set_flags(GB_Pntr, 4, value > 0); break;
			}
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem(new[] { "System Bus" });

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => (long)LibGBHawk.GB_cpu_cycles(GB_Pntr);
	}
}
