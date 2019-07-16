using System;
using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// CPCHawk: Core Class
    /// * IDebugggable *
    /// </summary>
    public partial class AmstradCPC : IDebuggable
    {
        public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
        {
            return new Dictionary<string, RegisterValue>
            {
                ["A"] = _cpu.Regs[_cpu.A],
                ["AF"] = _cpu.Regs[_cpu.F] + (_cpu.Regs[_cpu.A] << 8),
                ["B"] = _cpu.Regs[_cpu.B],
                ["BC"] = _cpu.Regs[_cpu.C] + (_cpu.Regs[_cpu.B] << 8),
                ["C"] = _cpu.Regs[_cpu.C],
                ["D"] = _cpu.Regs[_cpu.D],
                ["DE"] = _cpu.Regs[_cpu.E] + (_cpu.Regs[_cpu.D] << 8),
                ["E"] = _cpu.Regs[_cpu.E],
                ["F"] = _cpu.Regs[_cpu.F],
                ["H"] = _cpu.Regs[_cpu.H],
                ["HL"] = _cpu.Regs[_cpu.L] + (_cpu.Regs[_cpu.H] << 8),
                ["I"] = _cpu.Regs[_cpu.I],
                ["IX"] = _cpu.Regs[_cpu.Ixl] + (_cpu.Regs[_cpu.Ixh] << 8),
                ["IY"] = _cpu.Regs[_cpu.Iyl] + (_cpu.Regs[_cpu.Iyh] << 8),
                ["L"] = _cpu.Regs[_cpu.L],
                ["PC"] = _cpu.Regs[_cpu.PCl] + (_cpu.Regs[_cpu.PCh] << 8),
                ["R"] = _cpu.Regs[_cpu.R],
                ["Shadow AF"] = _cpu.Regs[_cpu.F_s] + (_cpu.Regs[_cpu.A_s] << 8),
                ["Shadow BC"] = _cpu.Regs[_cpu.C_s] + (_cpu.Regs[_cpu.B_s] << 8),
                ["Shadow DE"] = _cpu.Regs[_cpu.E_s] + (_cpu.Regs[_cpu.D_s] << 8),
                ["Shadow HL"] = _cpu.Regs[_cpu.L_s] + (_cpu.Regs[_cpu.H_s] << 8),
                ["SP"] = _cpu.Regs[_cpu.Iyl] + (_cpu.Regs[_cpu.Iyh] << 8),
                ["Flag C"] = _cpu.FlagC,
                ["Flag N"] = _cpu.FlagN,
                ["Flag P/V"] = _cpu.FlagP,
                ["Flag 3rd"] = _cpu.Flag3,
                ["Flag H"] = _cpu.FlagH,
                ["Flag 5th"] = _cpu.Flag5,
                ["Flag Z"] = _cpu.FlagZ,
                ["Flag S"] = _cpu.FlagS
            };
        }

        public void SetCpuRegister(string register, int value)
        {
            switch (register)
            {
                default:
                    throw new InvalidOperationException();
                case "A":
                    _cpu.Regs[_cpu.A] = (ushort)value;
                    break;
                case "AF":
                    _cpu.Regs[_cpu.F] = (ushort)(value & 0xFF);
                    _cpu.Regs[_cpu.A] = (ushort)(value & 0xFF00);
                    break;
                case "B":
                    _cpu.Regs[_cpu.B] = (ushort)value;
                    break;
                case "BC":
                    _cpu.Regs[_cpu.C] = (ushort)(value & 0xFF);
                    _cpu.Regs[_cpu.B] = (ushort)(value & 0xFF00);
                    break;
                case "C":
                    _cpu.Regs[_cpu.C] = (ushort)value;
                    break;
                case "D":
                    _cpu.Regs[_cpu.D] = (ushort)value;
                    break;
                case "DE":
                    _cpu.Regs[_cpu.E] = (ushort)(value & 0xFF);
                    _cpu.Regs[_cpu.D] = (ushort)(value & 0xFF00);
                    break;
                case "E":
                    _cpu.Regs[_cpu.E] = (ushort)value;
                    break;
                case "F":
                    _cpu.Regs[_cpu.F] = (ushort)value;
                    break;
                case "H":
                    _cpu.Regs[_cpu.H] = (ushort)value;
                    break;
                case "HL":
                    _cpu.Regs[_cpu.L] = (ushort)(value & 0xFF);
                    _cpu.Regs[_cpu.H] = (ushort)(value & 0xFF00);
                    break;
                case "I":
                    _cpu.Regs[_cpu.I] = (ushort)value;
                    break;
                case "IX":
                    _cpu.Regs[_cpu.Ixl] = (ushort)(value & 0xFF);
                    _cpu.Regs[_cpu.Ixh] = (ushort)(value & 0xFF00);
                    break;
                case "IY":
                    _cpu.Regs[_cpu.Iyl] = (ushort)(value & 0xFF);
                    _cpu.Regs[_cpu.Iyh] = (ushort)(value & 0xFF00);
                    break;
                case "L":
                    _cpu.Regs[_cpu.L] = (ushort)value;
                    break;
                case "PC":
                    _cpu.Regs[_cpu.PCl] = (ushort)(value & 0xFF);
                    _cpu.Regs[_cpu.PCh] = (ushort)(value & 0xFF00);
                    break;
                case "R":
                    _cpu.Regs[_cpu.R] = (ushort)value;
                    break;
                case "Shadow AF":
                    _cpu.Regs[_cpu.F_s] = (ushort)(value & 0xFF);
                    _cpu.Regs[_cpu.A_s] = (ushort)(value & 0xFF00);
                    break;
                case "Shadow BC":
                    _cpu.Regs[_cpu.C_s] = (ushort)(value & 0xFF);
                    _cpu.Regs[_cpu.B_s] = (ushort)(value & 0xFF00);
                    break;
                case "Shadow DE":
                    _cpu.Regs[_cpu.E_s] = (ushort)(value & 0xFF);
                    _cpu.Regs[_cpu.D_s] = (ushort)(value & 0xFF00);
                    break;
                case "Shadow HL":
                    _cpu.Regs[_cpu.L_s] = (ushort)(value & 0xFF);
                    _cpu.Regs[_cpu.H_s] = (ushort)(value & 0xFF00);
                    break;
                case "SP":
                    _cpu.Regs[_cpu.SPl] = (ushort)(value & 0xFF);
                    _cpu.Regs[_cpu.SPh] = (ushort)(value & 0xFF00);
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

        public long TotalExecutedCycles => _cpu.TotalExecutedCycles;
    }
}
