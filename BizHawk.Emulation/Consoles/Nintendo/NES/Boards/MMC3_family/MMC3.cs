//this file contains the MMC3 family of boards

//fceux contains a comment in mmc3.cpp:
//Code for emulating iNES mappers 4,12,44,45,47,49,52,74,114,115,116,118,119,165,205,214,215,245,249,250,254

using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	public class MMC3 : IDisposable
	{
		//state
		int reg_addr;
		bool chr_mode, prg_mode;
		public ByteBuffer regs = new ByteBuffer(8);

		public byte mirror;
		int a12_old;
		byte irq_reload, irq_counter;
		public bool irq_pending, irq_enable, irq_reload_flag;
		public bool wram_enable, wram_write_protect;

		//it really seems like these should be the same but i cant seem to unify them.
		//theres no sense in delaying the IRQ, so its logic must be tied to the separator.
		//the hint, of course, is that the countdown value is the same.
		//will someone else try to unify them?
		int separator_counter;
		int irq_countdown;

		//volatile state
		public ByteBuffer chr_regs_1k = new ByteBuffer(8);
		ByteBuffer prg_regs_8k = new ByteBuffer(4);

		//configuration
		public enum EMMC3Type
		{
			None, MMC3A, MMC3BSharp, MMC3BNonSharp, MMC3C, MMC6
		}
		EMMC3Type _mmc3type = EMMC3Type.None;
		public EMMC3Type MMC3Type
		{
			get { return _mmc3type; }
			set
			{
				_mmc3type = value;
				oldIrqType = (_mmc3type == EMMC3Type.MMC3A || _mmc3type == EMMC3Type.MMC3BNonSharp || _mmc3type == EMMC3Type.MMC6);
			}
		}
		bool oldIrqType;

		public void Dispose()
		{
			regs.Dispose();
			chr_regs_1k.Dispose();
			prg_regs_8k.Dispose();
		}

		public NES.NESBoardBase.EMirrorType MirrorType { get { return mirror == 0 ? NES.NESBoardBase.EMirrorType.Vertical : NES.NESBoardBase.EMirrorType.Horizontal; } }

		protected NES.NESBoardBase board;
		public MMC3(NES.NESBoardBase board, int num_prg_banks)
		{
			this.board = board;
			if (board.Cart.chips.Contains("MMC3A")) MMC3Type = EMMC3Type.MMC3A;
			else if (board.Cart.chips.Contains("MMC3B")) MMC3Type = EMMC3Type.MMC3BSharp;
			else if (board.Cart.chips.Contains("MMC3BNONSHARP")) MMC3Type = EMMC3Type.MMC3BNonSharp;
			else if (board.Cart.chips.Contains("MMC3C")) MMC3Type = EMMC3Type.MMC3C;
			else MMC3Type = EMMC3Type.MMC3C; //arbitrary choice. is it the best choice?

			Sync();
		}

		public void Sync()
		{
			SyncIRQ();
			if (prg_mode)
			{
				prg_regs_8k[0] = 0xFE;
				prg_regs_8k[1] = regs[7];
				prg_regs_8k[2] = regs[6];
				prg_regs_8k[3] = 0xFF;
			}
			else
			{
				prg_regs_8k[0] = regs[6];
				prg_regs_8k[1] = regs[7];
				prg_regs_8k[2] = 0xFE;
				prg_regs_8k[3] = 0xFF;
			}

			byte r0_0 = (byte)(regs[0] & ~1);
			byte r0_1 = (byte)(regs[0] | 1);
			byte r1_0 = (byte)(regs[1] & ~1);
			byte r1_1 = (byte)(regs[1] | 1);

			if (chr_mode)
			{
				chr_regs_1k[0] = regs[2];
				chr_regs_1k[1] = regs[3];
				chr_regs_1k[2] = regs[4];
				chr_regs_1k[3] = regs[5];
				chr_regs_1k[4] = r0_0;
				chr_regs_1k[5] = r0_1;
				chr_regs_1k[6] = r1_0;
				chr_regs_1k[7] = r1_1;
			}
			else
			{
				chr_regs_1k[0] = r0_0;
				chr_regs_1k[1] = r0_1;
				chr_regs_1k[2] = r1_0;
				chr_regs_1k[3] = r1_1;
				chr_regs_1k[4] = regs[2];
				chr_regs_1k[5] = regs[3];
				chr_regs_1k[6] = regs[4];
				chr_regs_1k[7] = regs[5];
			}
		}

		public virtual void SyncState(Serializer ser)
		{
			ser.Sync("reg_addr", ref reg_addr);
			ser.Sync("chr_mode", ref chr_mode);
			ser.Sync("prg_mode", ref prg_mode);
			ser.Sync("regs", ref regs);
			ser.Sync("mirror", ref mirror);
			ser.Sync("a12_old", ref a12_old);
			ser.Sync("irq_reload", ref irq_reload);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("irq_pending", ref irq_pending);
			ser.Sync("irq_enable", ref irq_enable);
			ser.Sync("separator_counter", ref separator_counter);
			ser.Sync("irq_countdown", ref irq_countdown);
			ser.Sync("irq_reload_flag", ref irq_reload_flag);
			ser.Sync("wram_enable", ref wram_enable);
			ser.Sync("wram_write_protect", ref wram_write_protect);
			Sync();
		}

		//some MMC3 variants pass along the irq signal differently (primarily different delay)
		//this is overrideable so that those boards can get signals whenever this mmc3 base class code manipulates the irq line
		public virtual void SyncIRQ()
		{
			board.SyncIRQ(irq_pending);
		}

		public void WritePRG(int addr, byte value)
		{
			switch (addr & 0x6001)
			{
				case 0x0000: //$8000
					chr_mode = value.Bit(7);
					prg_mode = value.Bit(6);
					reg_addr = (value & 7);
					Sync();
					break;
				case 0x0001: //$8001
					regs[reg_addr] = value;
					Sync();
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

		public virtual int Get_PRGBank_8K(int addr)
		{
			int bank_8k = addr >> 13;
			bank_8k = prg_regs_8k[bank_8k];
			return bank_8k;
		}

		public virtual int Get_CHRBank_1K(int addr)
		{
			int bank_1k = addr >> 10;
			bank_1k = chr_regs_1k[bank_1k];
			return bank_1k;
		}


		public virtual void AddressPPU(int addr)
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
					irq_countdown = 6;
				}
			}

			a12_old = a12;
		}
	}

	public abstract class MMC3Board_Base : NES.NESBoardBase
	{
		//state
		public MMC3 mmc3;
		public int extra_vrom;


		public override void AddressPPU(int addr)
		{
			mmc3.AddressPPU(addr);
		}

		public override void ClockPPU()
		{
			mmc3.ClockPPU();
		}

		//configuration
		protected int prg_mask, chr_mask;

		public override void Dispose()
		{
			if(mmc3 != null) mmc3.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			mmc3.SyncState(ser);
			ser.Sync("extra_vrom", ref extra_vrom);
		}

		protected virtual int Get_CHRBank_1K(int addr)
		{
			return mmc3.Get_CHRBank_1K(addr);
		}

		protected virtual int Get_PRGBank_8K(int addr)
		{
			return mmc3.Get_PRGBank_8K(addr);
		}

		protected virtual int MapCHR(int addr)
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
					return VROM[addr + extra_vrom];
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
			mmc3.WritePRG(addr, value);
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
			int num_prg_banks = Cart.prg_size / 8;
			prg_mask = num_prg_banks - 1;

			int num_chr_banks = (Cart.chr_size);
			chr_mask = num_chr_banks - 1;

			mmc3 = new MMC3(this, num_prg_banks);
			SetMirrorType(EMirrorType.Vertical);
		}

		//used by a couple of boards for controlling nametable wiring with the mapper
		protected int RewireNametable_TLSROM(int addr, int bitsel)
		{
			int bank_1k = mmc3.Get_CHRBank_1K(addr & 0x1FFF);
			int nt = (bank_1k >> bitsel) & 1;
			int ofs = addr & 0x3FF;
			addr = 0x2000 + (nt << 10);
			addr |= (ofs);
			return addr;
		}

	}


}