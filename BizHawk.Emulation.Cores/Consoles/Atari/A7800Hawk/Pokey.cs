using System;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// emualtes pokey sound chip
	// note: A7800 implementation is used only for sound
	// potentiometers, keyboard, and IRQs are not used in this context
	/*
	 * Regs 0,2,4,6: Frequency control (divider = value + 1)
	 * Regs 1,3,5,7: Channel control (Bits 0-3 = volume) (bits 4 - 7 control clocking)
	 * Reg 8: Control register
	 * 
	 * Reg A: Random number generator
	 * 
	 * The registers are write only, except for the RNG none of the things that would return reads are connected
	 * for now return FF
	 */



	public class Pokey
	{
		public A7800Hawk Core { get; set; }

		public byte[] Regs = new byte[16];

		public int random_poly; // 17 (or 9) bit random number generator (polynomial counter)

		public Pokey()
		{

		}

		public byte ReadReg(int reg)
		{
			byte ret = 0xFF;

			if (reg==0xA) { ret = Regs[0xA]; }

			return ret;
		}

		public void WriteReg(int reg, byte value)
		{
			Regs[reg] = value;


		}

		public void Tick()
		{

		}

		public void Reset()
		{
			Regs = new byte[16];
			random_poly = 0x1FF;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Pokey");

			ser.Sync("Regs", ref Regs, false);
			ser.Sync("ranom_poly", ref random_poly);

			ser.EndSection();
		}

	}
}
