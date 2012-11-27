using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public abstract partial class Sid : IStandardIO
	{
		// ------------------------------------

		private class Envelope
		{
		}

		private class Voice
		{
		}

		// ------------------------------------

		public void HardReset()
		{
		}

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
			return 0;
		}

		public void Poke(int addr, byte val)
		{
		}

		public byte Read(ushort addr)
		{
			return 0;
		}

		public void Write(ushort addr, byte val)
		{
		}
	}
}
