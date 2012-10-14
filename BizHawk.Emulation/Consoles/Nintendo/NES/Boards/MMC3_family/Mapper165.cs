using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	// Fire Emblem (Ch)
	// mmc3 with mmc2-style chr swapping
	// seem to be some graphical glitches...
	public class Mapper165 : MMC3Board_Base
	{
		bool latch0 = false;
		bool latch1 = false;
		int real_chr_mask;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER165":
					break;
				default:
					return false;
			}
			Cart.vram_size = 4;

			real_chr_mask = Cart.chr_size / 4 - 1;

			BaseSetup();

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("latch0", ref latch0);
			ser.Sync("latch1", ref latch1);
		}

		public override byte ReadPPU(int addr)
		{
			byte ret;
			if (addr < 0x2000)
			{

				int bank = mmc3.regs[addr < 0x1000 ? latch0 ? 1 : 0 : latch1 ? 4 : 2];
				if (bank == 0)
					ret = VRAM[addr & 0xfff];
				else
					ret = VROM[(addr & 0xfff) + (((bank >> 2) & real_chr_mask) << 12)];
			}
			else
				ret = base.ReadPPU(addr);

			// latch processes for the next read
			switch (addr & 0x3ff0)
			{
				case 0x0fd0: latch0 = false; break;
				case 0x0fe0: latch0 = true; break;
				case 0x1fd0: latch1 = false; break;
				case 0x1fe0: latch1 = true; break;
			}
			return ret;
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				int bank = mmc3.regs[addr < 0x1000 ? latch0 ? 1 : 0 : latch1 ? 4 : 2];
				if (bank == 0)
					VRAM[addr & 0xfff] = value;
			}
			else
				base.WritePPU(addr, value);
		}
	}
}
