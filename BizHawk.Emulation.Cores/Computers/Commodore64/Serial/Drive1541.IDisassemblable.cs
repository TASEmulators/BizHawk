using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Serial
{
    public sealed partial class Drive1541 : IDisassemblable
    {
        [SaveState.DoNotSave]
        IEnumerable<string> IDisassemblable.AvailableCpus
        {
            get { yield return "Disk Drive 6502"; }
        }

        [SaveState.DoNotSave]
        string IDisassemblable.Cpu
        {
            get { return "Disk Drive 6502"; }
            set
            {
            }
        }

        [SaveState.DoNotSave]
        string IDisassemblable.PCRegisterName
        {
            get { return "PC"; }
        }

        string IDisassemblable.Disassemble(MemoryDomain m, uint addr, out int length)
        {
            return Components.M6502.MOS6502X.Disassemble((ushort)addr, out length, CpuPeek);
        }
    }
}
