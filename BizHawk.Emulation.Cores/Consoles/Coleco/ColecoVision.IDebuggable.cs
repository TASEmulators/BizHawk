using System;
using System.Collections.Generic;

using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["A"] = _cpu.RegisterA,
				["AF"] = _cpu.RegisterAF,
				["B"] = _cpu.RegisterB,
				["BC"] = _cpu.RegisterBC,
				["C"] = _cpu.RegisterC,
				["D"] = _cpu.RegisterD,
				["DE"] = _cpu.RegisterDE,
				["E"] = _cpu.RegisterE,
				["F"] = _cpu.RegisterF,
				["H"] = _cpu.RegisterH,
				["HL"] = _cpu.RegisterHL,
				["I"] = _cpu.RegisterI,
				["IX"] = _cpu.RegisterIX,
				["IY"] = _cpu.RegisterIY,
				["L"] = _cpu.RegisterL,
				["PC"] = _cpu.RegisterPC,
				["R"] = _cpu.RegisterR,
				["Shadow AF"] = _cpu.RegisterShadowAF,
				["Shadow BC"] = _cpu.RegisterShadowBC,
				["Shadow DE"] = _cpu.RegisterShadowDE,
				["Shadow HL"] = _cpu.RegisterShadowHL,
				["SP"] = _cpu.RegisterSP,
				["Flag C"] = _cpu.RegisterF.Bit(0),
				["Flag N"] = _cpu.RegisterF.Bit(1),
				["Flag P/V"] = _cpu.RegisterF.Bit(2),
				["Flag 3rd"] = _cpu.RegisterF.Bit(3),
				["Flag H"] = _cpu.RegisterF.Bit(4),
				["Flag 5th"] = _cpu.RegisterF.Bit(5),
				["Flag Z"] = _cpu.RegisterF.Bit(6),
				["Flag S"] = _cpu.RegisterF.Bit(7)
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					_cpu.RegisterA = (byte)value;
					break;
				case "AF":
					_cpu.RegisterAF = (byte)value;
					break;
				case "B":
					_cpu.RegisterB = (byte)value;
					break;
				case "BC":
					_cpu.RegisterBC = (byte)value;
					break;
				case "C":
					_cpu.RegisterC = (byte)value;
					break;
				case "D":
					_cpu.RegisterD = (byte)value;
					break;
				case "DE":
					_cpu.RegisterDE = (byte)value;
					break;
				case "E":
					_cpu.RegisterE = (byte)value;
					break;
				case "F":
					_cpu.RegisterF = (byte)value;
					break;
				case "H":
					_cpu.RegisterH = (byte)value;
					break;
				case "HL":
					_cpu.RegisterHL = (byte)value;
					break;
				case "I":
					_cpu.RegisterI = (byte)value;
					break;
				case "IX":
					_cpu.RegisterIX = (byte)value;
					break;
				case "IY":
					_cpu.RegisterIY = (byte)value;
					break;
				case "L":
					_cpu.RegisterL = (byte)value;
					break;
				case "PC":
					_cpu.RegisterPC = (ushort)value;
					break;
				case "R":
					_cpu.RegisterR = (byte)value;
					break;
				case "Shadow AF":
					_cpu.RegisterShadowAF = (byte)value;
					break;
				case "Shadow BC":
					_cpu.RegisterShadowBC = (byte)value;
					break;
				case "Shadow DE":
					_cpu.RegisterShadowDE = (byte)value;
					break;
				case "Shadow HL":
					_cpu.RegisterShadowHL = (byte)value;
					break;
				case "SP":
					_cpu.RegisterSP = (byte)value;
					break;
			}
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; }

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type)
		{
			throw new NotImplementedException();
		}

		public int TotalExecutedCycles => _cpu.TotalExecutedCycles;
	}
}
