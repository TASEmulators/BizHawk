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
	// this is the base class for the MMC3 mapper
	public class Namcot109 : IDisposable
	{
		//state
		public int chr_mode, prg_mode, reg_addr;
		ByteBuffer chr_regs_1k = new ByteBuffer(8);
		ByteBuffer prg_regs_8k = new ByteBuffer(8);

		protected NES.NESBoardBase board;
		public Namcot109(NES.NESBoardBase board)
		{
			this.board = board;

			prg_regs_8k[0] = 0;
			prg_regs_8k[1] = 1;
			prg_regs_8k[2] = 0xFE; //constant
			prg_regs_8k[3] = 0xFF; //constant
			prg_regs_8k[4 + 0] = 0xFE; //constant
			prg_regs_8k[4 + 1] = 1;
			prg_regs_8k[4 + 2] = 0;
			prg_regs_8k[4 + 3] = 0xFF; //constant

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

		public virtual void SyncState(Serializer ser)
		{
			ser.Sync("chr_mode", ref chr_mode);
			ser.Sync("prg_mode", ref prg_mode);
			ser.Sync("reg_addr", ref reg_addr);
			ser.Sync("chr_regs_1k", ref chr_regs_1k);
			ser.Sync("prg_regs_8k", ref prg_regs_8k);
		}

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
		//state
		public byte mirror;
		int a12_old;
		byte irq_reload, irq_counter;
		protected bool irq_pending, irq_enable, irq_reload_flag;
		public bool wram_enable, wram_write_protect;

		//it really seems like these should be the same but i cant seem to unify them.
		//theres no sense in delaying the IRQ, so its logic must be tied to the separator.
		//the hint, of course, is that the countdown value is the same.
		//will someone else try to unify them?
		int separator_counter;
		int irq_countdown;


		//configuration
		public enum EMMC3Type
		{
			None, MMC3A, MMC3BSharp, MMC3BNonSharp, MMC3C
		}
		EMMC3Type _mmc3type = EMMC3Type.None;
		public EMMC3Type MMC3Type
		{
			get { return _mmc3type; }
			set
			{
				_mmc3type = value;
				oldIrqType = (_mmc3type == EMMC3Type.MMC3A || _mmc3type == EMMC3Type.MMC3BNonSharp);
			}
		}
		bool oldIrqType;


		public NES.NESBoardBase.EMirrorType MirrorType { get { return mirror == 0 ? NES.NESBoardBase.EMirrorType.Vertical : NES.NESBoardBase.EMirrorType.Horizontal; } }

		public MMC3(NES.NESBoardBase board, int num_prg_banks)
			: base(board)
		{
			if (board.Cart.chips.Contains("MMC3A")) MMC3Type = EMMC3Type.MMC3A;
			else if (board.Cart.chips.Contains("MMC3B")) MMC3Type = EMMC3Type.MMC3BSharp;
			else if (board.Cart.chips.Contains("MMC3BNONSHARP")) MMC3Type = EMMC3Type.MMC3BNonSharp;
			else if (board.Cart.chips.Contains("MMC3C")) MMC3Type = EMMC3Type.MMC3C;
			else MMC3Type = EMMC3Type.MMC3C; //arbitrary choice. is it the best choice?
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("mirror", ref mirror);
			ser.Sync("mirror", ref a12_old);
			ser.Sync("irq_reload", ref irq_reload);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("irq_pending", ref irq_pending);
			ser.Sync("irq_enable", ref irq_enable);
			ser.Sync("separator_counter", ref separator_counter);
			ser.Sync("irq_countdown", ref irq_countdown);
			ser.Sync("irq_reload_flag", ref irq_reload_flag);
			ser.Sync("wram_enable", ref wram_enable);
			ser.Sync("wram_write_protect", ref wram_write_protect);
		}

		protected virtual void SyncIRQ()
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
					mirror = (byte)(value & 1);
					board.SetMirrorType(MirrorType);
					break;
				case 0x2001: //$A001
					//wram enable/protect
					wram_write_protect = value.Bit(6);
					wram_enable = value.Bit(7);
					//Console.WriteLine("wram_write_protect={0},wram_enable={1}", wram_write_protect, wram_enable);
					break;
				case 0x4000: //$C000 - IRQ Reload value
					irq_reload = value;
					break;
				case 0x4001: //$C001 - IRQ Clear
					irq_counter = 0;
					if (oldIrqType)
						irq_reload_flag = true;
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
			int last_irq_counter = irq_counter;
			if (irq_reload_flag || irq_counter == 0)
			{
				irq_counter = irq_reload;
			}
			else
			{
				irq_counter--;
			}
			if (irq_counter == 0)
			{
				if (oldIrqType)
				{
					if (last_irq_counter != 0 || irq_reload_flag)
						IRQ_EQ_Pass();
				}
				else
					IRQ_EQ_Pass();
			}
			
			irq_reload_flag = false;
		}

		public virtual void ClockPPU()
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


		public virtual void AddressPPU(int addr)
		{
			int a12 = (addr >> 12) & 1;
			bool rising_edge = (a12 == 1 && a12_old == 0);
			if (rising_edge)
			{
				if (separator_counter > 0)
				{
					separator_counter = 12;
				}
				else
				{
					separator_counter = 12;
					irq_countdown = 12;
				}
			}

			a12_old = a12;
		}

	}

	public abstract class MMC3_Family_Board_Base : NES.NESBoardBase
	{
		protected Namcot109 mapper;

		//configuration
		protected int prg_mask, chr_mask;

		public override void Dispose()
		{
			mapper.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			mapper.SyncState(ser);
		}

		protected virtual int Get_CHRBank_1K(int addr)
		{
			return mapper.Get_CHRBank_1K(addr);
		}

		int MapCHR(int addr)
		{
			int bank_1k = Get_CHRBank_1K(addr);
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

		protected virtual int Get_PRGBank_8K(int addr)
		{
			return mapper.Get_PRGBank_8K(addr);
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = Get_PRGBank_8K(addr);
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
			mapper = mmc3 = new MMC3(this, num_prg_banks);

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