using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
    public sealed partial class Chip6510 : IDisassemblable
    {
        public IEnumerable<string> AvailableCpus
        {
            get { yield return "6510"; }
        }

        public string Cpu
        {
            get { return "6510"; }
            set
            {
            }
        }

        public string PCRegisterName
        {
            get { return "PC"; }
        }

        public string Disassemble(MemoryDomain m, uint addr, out int length)
        {
            return Components.M6502.MOS6502X.Disassemble((ushort)addr, out length, CpuPeek);
        }
    }
}
