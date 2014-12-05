using System;
using System.Collections.Generic;

using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators
{
	public partial class TI83 : IDebuggable
	{
		public IDictionary<string, int> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, int>
			{
				{ "A", cpu.RegisterA },
				{ "AF", cpu.RegisterAF },
				{ "B", cpu.RegisterB },
				{ "BC", cpu.RegisterBC },
				{ "C", cpu.RegisterC },
				{ "D", cpu.RegisterD },
				{ "DE", cpu.RegisterDE },
				{ "E", cpu.RegisterE },
				{ "F", cpu.RegisterF },
				{ "H", cpu.RegisterH },
				{ "HL", cpu.RegisterHL },
				{ "I", cpu.RegisterI },
				{ "IX", cpu.RegisterIX },
				{ "IY", cpu.RegisterIY },
				{ "L", cpu.RegisterL },
				{ "PC", cpu.RegisterPC },
				{ "R", cpu.RegisterR },
				{ "Shadow AF", cpu.RegisterShadowAF },
				{ "Shadow BC", cpu.RegisterShadowBC },
				{ "Shadow DE", cpu.RegisterShadowDE },
				{ "Shadow HL", cpu.RegisterShadowHL },
				{ "SP", cpu.RegisterSP },
				{ "Flag C", cpu.RegisterF.Bit(0) ? 1 : 0 },
				{ "Flag N", cpu.RegisterF.Bit(1) ? 1 : 0 },
				{ "Flag P/V", cpu.RegisterF.Bit(2) ? 1 : 0 },
				{ "Flag 3rd", cpu.RegisterF.Bit(3) ? 1 : 0 },
				{ "Flag H", cpu.RegisterF.Bit(4) ? 1 : 0 },
				{ "Flag 5th", cpu.RegisterF.Bit(5) ? 1 : 0 },
				{ "Flag Z", cpu.RegisterF.Bit(6) ? 1 : 0 },
				{ "Flag S", cpu.RegisterF.Bit(7) ? 1 : 0 }
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					cpu.RegisterA = (byte)value;
					break;
				case "AF":
					cpu.RegisterAF = (byte)value;
					break;
				case "B":
					cpu.RegisterB = (byte)value;
					break;
				case "BC":
					cpu.RegisterBC = (byte)value;
					break;
				case "C":
					cpu.RegisterC = (byte)value;
					break;
				case "D":
					cpu.RegisterD = (byte)value;
					break;
				case "DE":
					cpu.RegisterDE = (byte)value;
					break;
				case "E":
					cpu.RegisterE = (byte)value;
					break;
				case "F":
					cpu.RegisterF = (byte)value;
					break;
				case "H":
					cpu.RegisterH = (byte)value;
					break;
				case "HL":
					cpu.RegisterHL = (byte)value;
					break;
				case "I":
					cpu.RegisterI = (byte)value;
					break;
				case "IX":
					cpu.RegisterIX = (byte)value;
					break;
				case "IY":
					cpu.RegisterIY = (byte)value;
					break;
				case "L":
					cpu.RegisterL = (byte)value;
					break;
				case "PC":
					cpu.RegisterPC = (ushort)value;
					break;
				case "R":
					cpu.RegisterR = (byte)value;
					break;
				case "Shadow AF":
					cpu.RegisterShadowAF = (byte)value;
					break;
				case "Shadow BC":
					cpu.RegisterShadowBC = (byte)value;
					break;
				case "Shadow DE":
					cpu.RegisterShadowDE = (byte)value;
					break;
				case "Shadow HL":
					cpu.RegisterShadowHL = (byte)value;
					break;
				case "SP":
					cpu.RegisterSP = (byte)value;
					break;
			}
		}

		public ITracer Tracer
		{
			[FeatureNotImplemented]
			get { throw new NotImplementedException(); }
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; private set; }
	}
}
