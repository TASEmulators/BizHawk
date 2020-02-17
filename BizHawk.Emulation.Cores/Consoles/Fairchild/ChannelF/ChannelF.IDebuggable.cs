using System;
using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var res = new Dictionary<string, RegisterValue>
			{
				["A"] = CPU.Regs[CPU.A],
				["W"] = CPU.Regs[CPU.W],
				["ISAR"] = CPU.Regs[CPU.ISAR],
				["PC0"] = CPU.RegPC0,
				["PC1"] = CPU.RegPC1,
				["DC0"] = CPU.RegDC0,
				["DB"] = CPU.Regs[CPU.DB],
				["IO"] = CPU.Regs[CPU.IO],
				["J"] = CPU.Regs[CPU.J],
				["H"] = CPU.Regs[CPU.Hl] + (CPU.Regs[CPU.Hh] << 8),
				["K"] = CPU.Regs[CPU.Kl] + (CPU.Regs[CPU.Kh] << 8),
				["Q"] = CPU.Regs[CPU.Ql] + (CPU.Regs[CPU.Qh] << 8),
				["Flag C"] = CPU.FlagC,
				["Flag O"] = CPU.FlagO,
				["Flag Z"] = CPU.FlagZ,
				["Flag S"] = CPU.FlagS,
				["Flag I"] = CPU.FlagICB
			};

			for (int i = 0; i < 64; i++)
			{
				res.Add("SPR" + i, CPU.Regs[i]);
			}

			return res;
		}

		public void SetCpuRegister(string register, int value)
		{
			if (register.StartsWith("SPR"))
			{
				var reg = Convert.ToInt32(register.Replace("SPR", ""));

				if (reg > 63)
					throw new InvalidOperationException();

				CPU.Regs[reg] = (byte) value;
			}
			else
			{
				switch (register)
				{
					default:
						throw new InvalidOperationException();
					case "A":
						CPU.Regs[CPU.A] = (byte)value;
						break;
					case "W":
						CPU.Regs[CPU.W] = (byte)value;
						break;
					case "ISAR":
						CPU.Regs[CPU.ISAR] = (byte)(value & 0x3F);
						break;
					case "PC0":
						CPU.RegPC0 = (ushort)value;
						break;
					case "PC1":
						CPU.RegPC1 = (ushort)value;
						break;
					case "DC0":
						CPU.RegDC0 = (ushort)value;
						break;
					case "DB":
						CPU.Regs[CPU.DB] = (byte)value;
						break;
					case "IO":
						CPU.Regs[CPU.IO] = (byte)value;
						break;
					case "J":
						CPU.Regs[CPU.J] = (byte)value;
						break;
					case "H":
						CPU.Regs[CPU.Hl] = (byte)(value & 0xFF);
						CPU.Regs[CPU.Hh] = (byte)(value & 0xFF00);
						break;
					case "K":
						CPU.Regs[CPU.Kl] = (byte)(value & 0xFF);
						CPU.Regs[CPU.Kh] = (byte)(value & 0xFF00);
						break;
					case "Q":
						CPU.Regs[CPU.Ql] = (byte)(value & 0xFF);
						CPU.Regs[CPU.Qh] = (byte)(value & 0xFF00);
						break;
				}
			}
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; }

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type)
		{
			throw new NotImplementedException();
		}

		public long TotalExecutedCycles => CPU.TotalExecutedCycles;
	}
}
