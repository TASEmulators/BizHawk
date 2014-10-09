using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	sealed public partial class MOS6526_2
	{
		public Func<bool> ReadCNT;
		public Func<bool> ReadFlag;
		public bool ReadIRQBuffer() { 
			return (idr & 0x80) == 0; 
		}
		public Func<byte> ReadPortA = (() => { return 0xFF; });
		public Func<byte> ReadPortB = (() => { return 0xFF; });
		public Func<bool> ReadSP;
	}
}
