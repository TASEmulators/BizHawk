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

	internal sealed class Sunsoft1_Alt : NesBoardBase
	{
		private int prg;

		public override bool Configure(EDetectionOrigin origin)
		{
			if (Cart.BoardType != "SUNSOFT-1" || Cart.Pcb != "SUNSOFT-4")
				return false;

			AssertChr(0); AssertVram(8); AssertWram(0); AssertPrg(128);
			SetMirrorType(Cart.PadH, Cart.PadV);
			return true;
		}

		public override void WriteWram(int addr, byte value)
		{
			prg = value & 7;
		}

		public override byte ReadPrg(int addr)
		{
			if (addr >= 0x4000)
				return Rom[addr & 0x3fff | 7 << 14];
			else
				return Rom[addr & 0x3fff | prg << 14];
		}

		public override void SyncState(BizHawk.Common.Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg), ref prg);
		}

	}
}
