using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var regs = _machine.GetCpuFlagsAndRegisters();

			var dic = new Dictionary<string, RegisterValue>();

			foreach (var reg in regs)
			{
				dic.Add(
					reg.Key,
					reg.Key.Contains("Flag")
						? reg.Value > 0
						: (RegisterValue)reg.Value);
			}

			return dic;
		}

		public void SetCpuRegister(string register, int value)
		{
            switch (register)
            {
                default:
                    throw new InvalidOperationException();
                case "A":
                    _machine.Cpu.RA = (byte)value;
                    break;
                case "X":
                    _machine.Cpu.RX = (byte)value;
                    break;
                case "Y":
                    _machine.Cpu.RY = (byte)value;
                    break;
                case "S":
                    _machine.Cpu.RS = (byte)value;
                    break;
                case "PC":
                    _machine.Cpu.RPC = (ushort)value;
                    break;
                case "Flag C":
                    _machine.Cpu.FlagC = value > 0;
                    break;
                case "Flag Z":
                    _machine.Cpu.FlagZ = value > 0;
                    break;
                case "Flag I":
                    _machine.Cpu.FlagI = value > 0;
                    break;
                case "Flag D":
                    _machine.Cpu.FlagD = value > 0;
                    break;
                case "Flag B":
                    _machine.Cpu.FlagB = value > 0;
                    break;
                case "Flag T":
                    _machine.Cpu.FlagT = value > 0;
                    break;
                case "Flag V":
                    _machine.Cpu.FlagV = value > 0;
                    break;
                case "Flag N":
                    _machine.Cpu.FlagV = value > 0;
                    break;
            }
		}

		public bool CanStep(StepType type) { return false; }

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		public IMemoryCallbackSystem MemoryCallbacks
		{
			[FeatureNotImplemented]
			get { throw new NotImplementedException(); }
		}
	}
}
