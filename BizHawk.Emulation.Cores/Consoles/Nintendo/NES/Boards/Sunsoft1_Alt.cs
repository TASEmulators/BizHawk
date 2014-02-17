using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	/*
	 * Fantasy Zone (J, NOT TENGEN)
	 * It uses its own one-off PCB that rewires the SUNSOFT-1 chip to provide
	 * PRG control instead of CHR control.  To confuse matters, the game makes
	 * a second set of compatibility writes to a different set of registers to
	 * make it run on "Mapper 93" (they were perhaps anticipating putting the
	 * mask roms on a different board??
	 * 
	 * In any event, here is how it's actually emulated.
	 */

	public sealed class Sunsoft1_Alt : NES.NESBoardBase
	{
		int prg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			if (Cart.board_type != "SUNSOFT-1" || Cart.pcb != "SUNSOFT-4")
				return false;

			AssertChr(0); AssertVram(8); AssertWram(0); AssertPrg(128);
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void WriteWRAM(int addr, byte value)
		{
			prg = value & 7;
		}

		public override byte ReadPRG(int addr)
		{
			if (addr >= 0x4000)
				return ROM[addr & 0x3fff | 7 << 14];
			else
				return ROM[addr & 0x3fff | prg << 14];
		}

		public override void SyncState(BizHawk.Common.Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg);
		}

	}
}
