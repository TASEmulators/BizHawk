using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public partial class ZXSpectrum
    {
        public ushort Get16BitPC()
        {
            return Convert.ToUInt16(_cpu.PCh << 8 | _cpu.PCl);
        }
    }
}
