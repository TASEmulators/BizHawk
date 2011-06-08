//this file contains the MMC3 family of chips
//which includes:
//NAMCOT 109
//MMC3 (which was apparently based on NAMCOT 109 and shares enough functionality to be derived from it in this codebase)

//see http://nesdev.parodius.com/bbs/viewtopic.php?t=5426&sid=e7472c15a758ebf05c588c8330c2187f
//and http://nesdev.parodius.com/bbs/viewtopic.php?t=311
//for some info on NAMCOT 109

//mappers handled by this:
//004,095,118,119,206

//fceux contains a comment in mmc3.cpp:
//Code for emulating iNES mappers 4,12,44,45,47,49,52,74,114,115,116,118,119,165,205,214,215,245,249,250,254


using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	public class Namcot109 : IDisposable
	{
		protected NES.NESBoardBase board;
		public Namcot109(NES.NESBoardBase board)
		{
			this.board = board;

			prg_regs_8k[0] = 0;
			prg_regs_8k[1] = 1;
			prg_regs_8k[2] = 0xFE; //constant
			prg_regs_8k[3] = 0xFF; //constant
			prg_regs_8k[4+0] = 0xFE; //constant
			prg_regs_8k[4+1] = 1;
			prg_regs_8k[4+2] = 0;
			prg_regs_8k[4+3] = 0xFF; //constant

			chr_regs_1k[0] = 0;
			chr_regs_1k[1] = 1;
			chr_regs_1k[2] = 2;
			chr_regs_1k[3] = 3;
			chr_regs_1k[4] = 4;
			chr_regs_1k[5] = 5;
			chr_regs_1k[6] = 6;
			chr_regs_1k[7] = 7;
		}

		public void Dispose()
		{
			chr_regs_1k.Dispose();
			prg_regs_8k.Dispose();
		}

		//state
		public int chr_mode, prg_mode, reg_addr;

		ByteBuffer chr_regs_1k = new ByteBuffer(8);
		ByteBuffer prg_regs_8k = new ByteBuffer(8);

		public virtual void WritePRG(int addr, byte value)
		{
			switch (addr & 0x6001)
			{
				case 0x0000: //$8000
					chr_mode = (value >> 7) & 1;
					chr_mode <<= 2;
					prg_mode = (value >> 6) & 1;
					prg_mode <<= 2;
					reg_addr = (value & 7);
					break;
				case 0x0001: //$8001
					switch (reg_addr)
					{
						case 0: chr_regs_1k[0] = (byte)(value & ~1); chr_regs_1k[1] = (byte)(value | 1); break;
						case 1: chr_regs_1k[2] = (byte)(value & ~1); chr_regs_1k[3] = (byte)(value | 1); break;
						case 2: chr_regs_1k[4] = value; break;
						case 3: chr_regs_1k[5] = value; break;
						case 4: chr_regs_1k[6] = value; break;
						case 5: chr_regs_1k[7] = value; break;
						case 6: prg_regs_8k[0] = value; prg_regs_8k[4 + 2] = value; break;
						case 7: prg_regs_8k[1] = value; prg_regs_8k[4 + 1] = value; break;
					}
					break;
			}
		}

		public int Get_PRGBank_8K(int addr)
		{
			int bank_8k = addr >> 13;
			bank_8k += prg_mode;
			bank_8k = prg_regs_8k[bank_8k];
			return bank_8k;
		}

		public int Get_CHRBank_1K(int addr)
		{
			int bank_1k = addr >> 10;
			bank_1k ^= chr_mode;
			bank_1k = chr_regs_1k[bank_1k];
			return bank_1k;
		}


	}

	public class MMC3 : Namcot109
	{
		public MMC3(NES.NESBoardBase board, int num_prg_banks)
			: base(board)
		{
		}

		//state
		public NES.NESBoardBase.EMirrorType mirror;
		int a12_old;
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
					//board.NES.LogLine("irq en");
					irq_enable = true;
					SyncIRQ();
					break;
			}
		}

		void IRQ_EQ_Pass()
		{
			if (irq_enable)
			{
				//board.NES.LogLine("mmc3 IRQ");
				irq_pending = true;
			}
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
				if (irq_counter == 0)
				{
					IRQ_EQ_Pass();
				}
			}
		}

		//it really seems like these should be the same but i cant seem to unify them.
		//theres no sense in delaying the IRQ, so its logic must be tied to the separator.
		//the hint, of course, is that the countdown value is the same.
		//will someone else try to unify them?
		int separator_counter;
		int irq_countdown;

		public void ClockPPU()
		{
			if (separator_counter > 0)
				separator_counter--;

			if (irq_countdown > 0)
			{
				irq_countdown--;
				if (irq_countdown == 0)
				{
					//board.NES.LogLine("ClockIRQ");
					ClockIRQ();
				}
			}
		}


		public void AddressPPU(int addr)
		{
			int a12 = (addr >> 12) & 1;
			bool rising_edge = (a12 == 1 && a12_old == 0);
			if (rising_edge)
			{
				if (separator_counter > 0)
				{
					separator_counter = 15;
				}
				else
				{
					separator_counter = 15;
					irq_countdown = 15;
				}
			}

			a12_old = a12;
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

		int MapCHR(int addr)
		{
			int bank_1k = mapper.Get_CHRBank_1K(addr);
			bank_1k &= chr_mask;
			addr = (bank_1k << 10) | (addr & 0x3FF);
			return addr;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				addr = MapCHR(addr);
				if (VROM != null)
					return VROM[addr];
				else return VRAM[addr];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (VRAM == null) return;
				addr = MapCHR(addr);
				VRAM[addr] = value;
			}
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
			mmc3.AddressPPU(addr);
		}

		public override void ClockPPU()
		{
			mmc3.ClockPPU();
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
			mapper = new Namcot109(this);
			base.BaseSetup();
		}
	}

}