using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// via
	public class MOS6522 : Timer
	{
		// ------------------------------------

		public void ExecutePhase1()
		{
		}

		public void ExecutePhase2()
		{
		}

		// ------------------------------------

		public byte Peek(int addr)
		{
			return ReadRegister((ushort)(addr & 0xF));
		}

		public void Poke(int addr, byte val)
		{
			WriteRegister((ushort)(addr & 0xF), val);
		}

		public byte Read(ushort addr)
		{
			addr &= 0xF;
			switch (addr)
			{
				default:
					return ReadRegister(addr);
			}
		}

		private byte ReadRegister(ushort addr)
		{
			switch (addr)
			{
				default:
					return 0;
			}
		}

		public void Write(ushort addr, byte val)
		{
			addr &= 0xF;
			switch (addr)
			{
				default:
					WriteRegister(addr, val);
					break;
			}
		}

		private void WriteRegister(ushort addr, byte val)
		{
			switch (addr)
			{
				default:
					break;
			}
		}
	
		// ------------------------------------
	}
}
