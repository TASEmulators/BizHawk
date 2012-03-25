using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.CPUs.Z80;
using BizHawk.Emulation.Sound;

namespace BizHawk.Emulation.Consoles.Coleco
{
	public partial class ColecoVision : IEmulator
	{
		public byte[] rom;
		public Z80A cpu;

		public byte ReadMemory(ushort addr)
		{
			return 0xFF;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			return;
		}

		public void HardReset()
		{

		}

		public void FrameAdvance(bool render)
		{

		}
	}
}
