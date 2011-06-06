//this file contains the MMC3 family of chips
//which includes:
//NAMCOT 109
//MMC3 (which was apparently based on NAMCOT 109 and shares enough functionality to be derived from it in this codebase)

//see http://nesdev.parodius.com/bbs/viewtopic.php?t=5426&sid=e7472c15a758ebf05c588c8330c2187f
//and http://nesdev.parodius.com/bbs/viewtopic.php?t=311
//for some info on NAMCOT 109

//mappers handled by this:
//004,095,118,119,206

using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	public class Namcot109 : IDisposable
	{
		public Namcot109(int num_prg_banks)
		{
			bank_regs[8] = (byte)(num_prg_banks - 1);
			bank_regs[9] = (byte)(num_prg_banks - 2);
			bank_regs[0] = 0;
			bank_regs[1] = 1;
			bank_regs[2] = 2;
			bank_regs[3] = 3;
			bank_regs[4] = 4;
			bank_regs[5] = 5;
			sync_2k_chr();
		}

		public void Dispose()
		{
			bank_regs.Dispose();
			prg_lookup.Dispose();
		}

		//state
		public int chr_mode, prg_mode, reg_addr;

		//this contains the 8 programmable regs and 2 more at the end to represent PRG banks -2 and -1; and 4 more at the end to break down chr regs 0 and 1
		//in other words:
		//0: chr reg 0 (not used directly)
		//1: chr reg 1 (not used directly)
		//2,3,4,5: chr reg 2,3,4,5
		//6,7: prg reg 6,7
		//8,9: prg reg -2,-1
		//10: chr reg 0A
		//11: chr reg 0B
		//12: chr reg 1A
		//13: chr reg 1B
		ByteBuffer bank_regs = new ByteBuffer(14);
		ByteBuffer prg_lookup = new ByteBuffer(new byte[] { 6, 7, 9, 8, 9, 7, 6, 8 });
		ByteBuffer chr_lookup = new ByteBuffer(new byte[] { 10, 11, 12, 13, 2, 3, 4, 5 });

		void sync_2k_chr()
		{
			bank_regs[10] = (byte)((bank_regs[0] & ~1) + 0);
			bank_regs[11] = (byte)((bank_regs[0] & ~1) + 1);
			bank_regs[12] = (byte)((bank_regs[1] & ~1) + 0);
			bank_regs[13] = (byte)((bank_regs[1] & ~1) + 1);
		}

		public virtual void WritePRG(int addr, byte value)
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
					sync_2k_chr();
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

	public class MMC3 : Namcot109
	{
		NES.NESBoardBase board;
		public MMC3(NES.NESBoardBase board, int num_prg_banks)
			: base(num_prg_banks)
		{
			this.board = board;
		}

		//state
		public NES.NESBoardBase.EMirrorType mirror;
		int ppubus_state, ppubus_statecounter;
		byte irq_reload, irq_counter;
		bool irq_pending, irq_enable;

		void SyncIRQ()
		{
			board.NES.irq_cart = irq_pending;
		}

		public override void WritePRG(int addr, byte value)
		{
			switch (addr & 0x6001)
			{
				case 0x0000: //$8000
				case 0x0001: //$8001
					base.WritePRG(addr, value);
					break;
				case 0x2000: //$A000
					//mirroring
					if ((value & 1) == 0) mirror = NES.NESBoardBase.EMirrorType.Vertical;
					else mirror = NES.NESBoardBase.EMirrorType.Horizontal;
					board.SetMirrorType(mirror);
					break;
				case 0x2001: //$A001
					//wram enable/protect
					break;
				case 0x4000: //$C000 - IRQ Reload value
					irq_reload = value;
					break;
				case 0x4001: //$C001 - IRQ Clear
					irq_counter = 0;
					break;
				case 0x6000: //$E000 - IRQ Acknowledge / Disable
					irq_enable = false;
					irq_pending = false;
					SyncIRQ();
					break;
				case 0x6001: //$E001 - IRQ Enable
					irq_enable = true;
					SyncIRQ();
					break;
			}
		}

		void IRQ_EQ_Pass()
		{
			if (irq_enable)
				irq_pending = true;
			SyncIRQ();
		}

		void ClockIRQ()
		{
			if (irq_counter == 0)
			{
				irq_counter = irq_reload;

				//TODO - MMC3 variant behaviour??? not sure
				//was needed to pass 2-details.nes
				if (irq_counter == 0)
					IRQ_EQ_Pass();
			}
			else
			{
				irq_counter--;
				//Console.WriteLine(irq_counter);
				if (irq_counter == 0)
				{
					IRQ_EQ_Pass();
				}
			}
		}

		//TODO - this should be determined from NES timestamps to correctly emulate ppu writes interfering
		public void Tick_PPU(int addr)
		{
			ppubus_statecounter++;
			int state = (addr >> 12) & 1;
			if (ppubus_state == 0 && ppubus_statecounter > 1 && state == 1)
			{
				ppubus_statecounter = 0;
				ClockIRQ();
			}
			if (ppubus_state != state)
			{
				ppubus_state = state;
			}
		}

	}

	public abstract class MMC3_Family_Board_Base : NES.NESBoardBase
	{
		//configuration
		protected int prg_mask, chr_mask;

		public override void Dispose()
		{
			mapper.Dispose();
		}

		protected Namcot109 mapper;
		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = mapper.Get_CHRBank_1K(addr);
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


		public override void WritePRG(int addr, byte value)
		{
			mapper.WritePRG(addr, value);
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = mapper.Get_PRGBank_8K(addr);
			bank_8k &= prg_mask;
			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return ROM[addr];
		}

		protected virtual void BaseSetup()
		{
			//remember to setup the PRG banks -1 and -2
			int num_prg_banks = Cart.prg_size / 8;
			prg_mask = num_prg_banks - 1;

			int num_chr_banks = (Cart.chr_size);
			chr_mask = num_chr_banks - 1;
		}

		//used by a couple of boards for controlling nametable wiring with the mapper
		protected int RewireNametable_Mapper095_and_TLSROM(int addr, int bitsel)
		{
			int bank_1k = mapper.Get_CHRBank_1K(addr & 0x1FFF);
			int nt = (bank_1k >> bitsel) & 1;
			int ofs = addr & 0x3FF;
			addr = 0x2000 + (nt << 10);
			addr |= (ofs);
			return addr;
		}
	}

	public abstract class MMC3Board_Base : MMC3_Family_Board_Base
	{
		//configuration
		protected int wram_mask;

		//state
		protected MMC3 mmc3;

		public override void AddressPPU(int addr)
		{
			mmc3.Tick_PPU(addr);
		}
		
		protected override void BaseSetup()
		{
			wram_mask = (Cart.wram_size * 1024) - 1;

			int num_prg_banks = Cart.prg_size / 8;
			mapper = mmc3 = new MMC3(this,num_prg_banks);

			base.BaseSetup();
			SetMirrorType(EMirrorType.Vertical);
		}
	}

	public abstract class Namcot109Board_Base : MMC3_Family_Board_Base
	{
		protected override void BaseSetup()
		{
			int num_prg_banks = Cart.prg_size / 8;
			mapper = new Namcot109(num_prg_banks);

			base.BaseSetup();
		}
	}

}