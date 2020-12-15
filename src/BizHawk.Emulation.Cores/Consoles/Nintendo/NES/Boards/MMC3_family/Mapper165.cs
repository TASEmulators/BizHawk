using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Fire Emblem (Ch)
	// mmc3 with mmc2-style chr swapping
	// seem to be some graphical glitches...
	internal sealed class Mapper165 : MMC3Board_Base
	{
		private bool latch0 = false;
		private bool latch1 = false;
		private int real_chr_mask;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER165":
					break;
				default:
					return false;
			}
			Cart.VramSize = 4;

			real_chr_mask = Cart.ChrSize / 4 - 1;

			BaseSetup();

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(latch0), ref latch0);
			ser.Sync(nameof(latch1), ref latch1);
		}

		// same as ReadPPU, but doesn't process latches
		public override byte PeekPPU(int addr)
		{
			byte ret;
			if (addr < 0x2000)
			{

				int bank = mmc3.regs[addr < 0x1000 ? latch0 ? 1 : 0 : latch1 ? 4 : 2];
				if (bank == 0)
					ret = Vram[addr & 0xfff];
				else
					ret = Vrom[(addr & 0xfff) + (((bank >> 2) & real_chr_mask) << 12)];
			}
			else
				ret = base.ReadPpu(addr);
			return ret;
		}

		public override byte ReadPpu(int addr)
		{
			byte ret;
			if (addr < 0x2000)
			{

				int bank = mmc3.regs[addr < 0x1000 ? latch0 ? 1 : 0 : latch1 ? 4 : 2];
				if (bank == 0)
					ret = Vram[addr & 0xfff];
				else
					ret = Vrom[(addr & 0xfff) + (((bank >> 2) & real_chr_mask) << 12)];
			}
			else
				ret = base.ReadPpu(addr);

			// latch processes for the next read
			switch (addr & 0x3ff8)
			{
				case 0x0fd0: latch0 = false; break;
				case 0x0fe8: latch0 = true; break;
				case 0x1fd0: latch1 = false; break;
				case 0x1fe8: latch1 = true; break;
			}
			return ret;
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				int bank = mmc3.regs[addr < 0x1000 ? latch0 ? 1 : 0 : latch1 ? 4 : 2];
				if (bank == 0)
					Vram[addr & 0xfff] = value;
			}
			else
				base.WritePpu(addr, value);
		}
	}
}
