using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	public class TxROM : NES.NESBoardBase
	{
		class MMC3 : IDisposable
		{
			public MMC3(NES.NESBoardBase board, int num_prg_banks)
			{
				bank_regs[8] = (byte)(num_prg_banks - 1);
				bank_regs[9] = (byte)(num_prg_banks - 2);
			}

			public void Dispose()
			{
				bank_regs.Dispose();
				prg_lookup.Dispose();
			}

			//state
			int chr_mode, prg_mode, reg_addr;
			public NES.NESBoardBase.EMirrorType mirror;

			//this contains the 8 programmable regs and 2 more at the end to represent PRG banks -2 and -1; and 4 more at the end to break down chr regs 0 and 1
			ByteBuffer bank_regs = new ByteBuffer(14);
			ByteBuffer prg_lookup = new ByteBuffer(new byte[] { 6, 7, 9, 8, 9, 7, 6, 8 });
			ByteBuffer chr_lookup = new ByteBuffer(new byte[] { 10, 11, 12, 13, 2, 3, 4, 5 });

			public void WritePRG(int addr, byte value)
			{
				switch (addr & 0x6001)
				{
					case 0x0000: //$8000
						chr_mode = (value >> 7) & 1;
						prg_mode = (value >> 6) & 1;
						reg_addr = (value & 7);
						break;
					case 0x0001: //$8001
						bank_regs[reg_addr] = value;
						//setup the 2K chr regs
						bank_regs[10] = (byte)((bank_regs[0] & ~1) + 0);
						bank_regs[11] = (byte)((bank_regs[0] & ~1) + 1);
						bank_regs[12] = (byte)((bank_regs[1] & ~1) + 0);
						bank_regs[13] = (byte)((bank_regs[1] & ~1) + 1);
						break;
					case 0x2000: //$A000
						//mirroring
						if ((value & 1) == 0) mirror = EMirrorType.Vertical;
						else mirror = EMirrorType.Horizontal;
						break;
					case 0x2001: //$A001
						//wram enable/protect
						break;
					case 0x4000: //$C000
						//IRQ reload
						break;
					case 0x4001: //$C001
						//IRQ clear
						break;
					case 0x6000: //$E000
						//IRQ ack/disable
						break;
					case 0x6001: //$E001
						//IRQ enable
						break;
				}
			}

			public int Get_PRGBank_8K(int addr)
			{
				int bank_8k = addr >> 13;
				bank_8k = bank_regs[prg_lookup[prg_mode * 4 + bank_8k]];
				return bank_8k;
			}

			public int Get_CHRBank_1K(int addr)
			{
				int bank_1k = addr >> 10;
				if (chr_mode == 1)
					bank_1k ^= 4;
				bank_1k = bank_regs[chr_lookup[bank_1k]];
				return bank_1k;
			}

		}

		//configuration
		int prg_mask, chr_mask;
		int wram_mask;

		//state
		MMC3 mmc3;

		public override void Dispose()
		{
			mmc3.Dispose();
		}

		public override void WritePRG(int addr, byte value)
		{
			mmc3.WritePRG(addr, value);
			SetMirrorType(mmc3.mirror);  //often redundant, but gets the job done
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = mmc3.Get_PRGBank_8K(addr);
			bank_8k &= prg_mask;
			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return ROM[addr];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = mmc3.Get_CHRBank_1K(addr);
				bank_1k &= chr_mask;
				addr = (bank_1k << 10) | (addr & 0x3FF);
				if (VROM != null)
					return VROM[addr];
				else return VRAM[addr];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			base.WritePPU(addr, value);
		}

		public override byte ReadWRAM(int addr)
		{
			if (Cart.wram_size != 0)
				return WRAM[addr & wram_mask];
			else return 0xFF;
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (Cart.wram_size != 0)
				WRAM[addr & wram_mask] = value;
		}

		public override byte[] SaveRam
		{
			get
			{
				if (!Cart.wram_battery) return null;
				return WRAM;
				//some boards have a pram that is backed-up or not backed-up. need to handle that somehow
				//(nestopia splits it into NVWRAM and WRAM but i didnt like that at first.. but it may player better with this architecture)
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "NES-TSROM": //super mario bros. 3 USA
					AssertPrg(128,256,512); AssertChr(128,256); AssertVram(0); AssertWram(8);
					AssertBattery(false);
				    break;
				case "NES-TGROM": //mega man 4
					AssertPrg(128, 256, 512); AssertChr(0); AssertVram(8); AssertWram(0);
					break;
				case "NES-TKROM": //kirby's adventure
					AssertPrg(128, 256, 512); AssertChr(128, 256); AssertVram(0); AssertWram(8);
					break;
				case "NES-TLROM": //mega man 3
					AssertPrg(128, 256, 512); AssertChr(128, 256); AssertVram(0); AssertWram(0);
					break;
				default:
					return false;
			}

			//remember to setup the PRG banks -1 and -2
			int num_prg_banks = Cart.prg_size / 8;
			prg_mask = num_prg_banks - 1;

			int num_chr_banks = (Cart.chr_size);
			chr_mask = num_chr_banks - 1;

			wram_mask = (Cart.wram_size * 1024) - 1;

			mmc3 = new MMC3(this, num_prg_banks);
			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

	}
}
