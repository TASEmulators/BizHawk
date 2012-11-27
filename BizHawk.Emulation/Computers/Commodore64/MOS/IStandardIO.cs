using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public interface IStandardIO
	{
		byte Peek(int addr);
		void Poke(int addr, byte val);
		byte Read(ushort addr);
		void Write(ushort addr, byte val);
	}
}
