using System;
using System.Collections.Generic;

using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public sealed partial class SMS : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				{ "A", Cpu.RegisterA },
				{ "AF", Cpu.RegisterAF },
				{ "B", Cpu.RegisterB },
				{ "BC", Cpu.RegisterBC },
				{ "C", Cpu.RegisterC },
				{ "D", Cpu.RegisterD },
				{ "DE", Cpu.RegisterDE },
				{ "E", Cpu.RegisterE },
				{ "F", Cpu.RegisterF },
				{ "H", Cpu.RegisterH },
				{ "HL", Cpu.RegisterHL },
				{ "I", Cpu.RegisterI },
				{ "IX", Cpu.RegisterIX },
				{ "IY", Cpu.RegisterIY },
				{ "L", Cpu.RegisterL },
				{ "PC", Cpu.RegisterPC },
				{ "R", Cpu.RegisterR },
				{ "Shadow AF", Cpu.RegisterShadowAF },
				{ "Shadow BC", Cpu.RegisterShadowBC },
				{ "Shadow DE", Cpu.RegisterShadowDE },
				{ "Shadow HL", Cpu.RegisterShadowHL },
				{ "SP", Cpu.RegisterSP },
				{ "Flag C", Cpu.RegisterF.Bit(0) },
				{ "Flag N", Cpu.RegisterF.Bit(1) },
				{ "Flag P/V", Cpu.RegisterF.Bit(2) },
				{ "Flag 3rd", Cpu.RegisterF.Bit(3) },
				{ "Flag H", Cpu.RegisterF.Bit(4) },
				{ "Flag 5th", Cpu.RegisterF.Bit(5) },
				{ "Flag Z", Cpu.RegisterF.Bit(6) },
				{ "Flag S", Cpu.RegisterF.Bit(7) },
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					Cpu.RegisterA = (byte)value;
					break;
				case "AF":
					Cpu.RegisterAF = (byte)value;
					break;
				case "B":
					Cpu.RegisterB = (byte)value;
					break;
				case "BC":
					Cpu.RegisterBC = (byte)value;
					break;
				case "C":
					Cpu.RegisterC = (byte)value;
					break;
				case "D":
					Cpu.RegisterD = (byte)value;
					break;
				case "DE":
					Cpu.RegisterDE = (byte)value;
					break;
				case "E":
					Cpu.RegisterE = (byte)value;
					break;
				case "F":
					Cpu.RegisterF = (byte)value;
					break;
				case "H":
					Cpu.RegisterH = (byte)value;
					break;
				case "HL":
					Cpu.RegisterHL = (byte)value;
					break;
				case "I":
					Cpu.RegisterI = (byte)value;
					break;
				case "IX":
					Cpu.RegisterIX = (byte)value;
					break;
				case "IY":
					Cpu.RegisterIY = (byte)value;
					break;
				case "L":
					Cpu.RegisterL = (byte)value;
					break;
				case "PC":
					Cpu.RegisterPC = (ushort)value;
					break;
				case "R":
					Cpu.RegisterR = (byte)value;
					break;
				case "Shadow AF":
					Cpu.RegisterShadowAF = (byte)value;
					break;
				case "Shadow BC":
					Cpu.RegisterShadowBC = (byte)value;
					break;
				case "Shadow DE":
					Cpu.RegisterShadowDE = (byte)value;
					break;
				case "Shadow HL":
					Cpu.RegisterShadowHL = (byte)value;
					break;
				case "SP":
					Cpu.RegisterSP = (byte)value;
					break;
			}
		}

		public bool CanStep(StepType type) { return false; }

		public IMemoryCallbackSystem MemoryCallbacks { get; private set; }

		[FeatureNotImplemented]
		public void Step(StepType type)
		{
			throw new NotImplementedException();
		}
	}
}
