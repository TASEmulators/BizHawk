//this file contains the MMC3 family of boards

//fceux contains a comment in mmc3.cpp:
//Code for emulating iNES mappers 4,12,44,45,47,49,52,74,114,115,116,118,119,165,205,214,215,245,249,250,254

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal class MMC3
	{
		//state
		public int reg_addr;
		public bool get_chr_mode => chr_mode; // one of the pirate mappers needs this
		public bool chr_mode;
		public bool prg_mode;
		public byte[] regs = new byte[8];

		public byte mirror;
		private int a12_old;
		private byte irq_reload, irq_counter;
		public bool irq_pending, irq_enable, irq_reload_flag;
		public bool wram_enable, wram_write_protect;

		public bool just_cleared_pending, just_cleared;

		//it really seems like these should be the same but i cant seem to unify them.
		//theres no sense in delaying the IRQ, so its logic must be tied to the separator.
		//the hint, of course, is that the countdown value is the same.
		//will someone else try to unify them?
		private int separator_counter;
		private int irq_countdown;

		//volatile state
		public byte[] chr_regs_1k = new byte[8];
		public byte[] prg_regs_8k = new byte[4];

		//configuration
		public enum EMMC3Type
		{
			None, MMC3A, MMC3BSharp, MMC3BNonSharp, MMC3C, MMC6
		}

		private EMMC3Type _mmc3type = EMMC3Type.None;
		public EMMC3Type MMC3Type
		{
			get => _mmc3type;
			set
			{
				_mmc3type = value;
				oldIrqType = value is EMMC3Type.MMC3A or EMMC3Type.MMC3BNonSharp or EMMC3Type.MMC6;
			}
		}

		private bool oldIrqType;

		public EMirrorType MirrorType => mirror switch
		{
			1 => EMirrorType.Horizontal,
			2 => EMirrorType.OneScreenA,
			3 => EMirrorType.OneScreenB,
			_ => EMirrorType.Vertical
		};

		protected NesBoardBase board;
		public MMC3(NesBoardBase board, int num_prg_banks)
		{
			just_cleared = just_cleared_pending = false;

			MirrorMask = 1;
			this.board = board;
			if (board.Cart.Chips.Contains("MMC3A")) MMC3Type = EMMC3Type.MMC3A;
			else if (board.Cart.Chips.Contains("MMC3B")) MMC3Type = EMMC3Type.MMC3BSharp;
			else if (board.Cart.Chips.Contains("MMC3BNONSHARP")) MMC3Type = EMMC3Type.MMC3BNonSharp;
			else if (board.Cart.Chips.Contains("MMC3C")) MMC3Type = EMMC3Type.MMC3C;
			else MMC3Type = EMMC3Type.MMC3C; //arbitrary choice. is it the best choice?

			//initial values seem necessary
			regs[0] = 0;
			regs[1] = 2;
			regs[2] = 4;
			regs[3] = 5;
			regs[4] = 6;
			regs[5] = 7;
			regs[6] = 0;
			regs[7] = 1;

#pragma warning disable CA2214 // calling override before subclass ctor executes
			Sync();
#pragma warning restore CA2214
		}

		public virtual void Sync()
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
			ser.Sync(nameof(reg_addr), ref reg_addr);
			ser.Sync(nameof(chr_mode), ref chr_mode);
			ser.Sync(nameof(prg_mode), ref prg_mode);
			ser.Sync(nameof(regs), ref regs, false);
			ser.Sync(nameof(mirror), ref mirror);
			ser.Sync(nameof(a12_old), ref a12_old);
			ser.Sync(nameof(irq_reload), ref irq_reload);
			ser.Sync(nameof(irq_counter), ref irq_counter);
			ser.Sync(nameof(irq_pending), ref irq_pending);
			ser.Sync(nameof(irq_enable), ref irq_enable);
			ser.Sync(nameof(separator_counter), ref separator_counter);
			ser.Sync(nameof(irq_countdown), ref irq_countdown);
			ser.Sync(nameof(irq_reload_flag), ref irq_reload_flag);
			ser.Sync(nameof(wram_enable), ref wram_enable);
			ser.Sync(nameof(wram_write_protect), ref wram_write_protect);
			ser.Sync(nameof(cmd), ref cmd);
			ser.Sync(nameof(just_cleared), ref just_cleared);
			ser.Sync(nameof(just_cleared_pending), ref just_cleared_pending);
			Sync();
		}

		//some MMC3 variants pass along the irq signal differently (primarily different delay)
		//this is overrideable so that those boards can get signals whenever this mmc3 base class code manipulates the irq line
		public virtual void SyncIRQ()
		{
			board.SyncIRQ(irq_pending);
		}

		public byte cmd;

		public int MirrorMask { get; set; }

		public void WritePRG(int addr, byte value)
		{
			switch (addr & 0x6001)
			{
				case 0x0000: //$8000
					cmd = value;
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
					mirror = (byte)(value & MirrorMask);
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
					// does not take immediate effect (fixes Klax)
					just_cleared_pending = true;
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

		private void IRQ_EQ_Pass()
		{
			if (irq_enable)
			{
				//board.NES.LogLine("mmc3 IRQ");
				irq_pending = true;
			}
			SyncIRQ();
		}

		private void ClockIRQ()
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

			if (just_cleared)
			{
				irq_counter = 0;
				if (oldIrqType)
					irq_reload_flag = true;
			}

			just_cleared = just_cleared_pending;
			just_cleared_pending = false;
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
			// see https://forums.nesdev.com/viewtopic.php?f=3&t=11361&start=60
			// apparently mmc3 can't see internal pattern tables
			// fixes Recca
			if (addr<0x3F00)
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
						irq_countdown = 5;
					}
				}

				a12_old = a12;
			}
		}
	}

	internal abstract class MMC3Board_Base : NesBoardBase
	{
		//state
		public MMC3 mmc3;
		public int extra_vrom;

		public override void AddressPpu(int addr)
		{
			mmc3.AddressPPU(addr);
		}

		public override void ClockPpu()
		{
			mmc3.ClockPPU();
		}

		//configuration
		protected int prg_mask, chr_mask;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			mmc3.SyncState(ser);
			ser.Sync(nameof(extra_vrom), ref extra_vrom);
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
			// allow NPOT chr sizes
			bank_1k %= chr_mask + 1;
			addr = (bank_1k << 10) | (addr & 0x3FF);
			return addr;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				addr = MapCHR(addr);
				if (Vrom != null)
					return Vrom[addr + extra_vrom];
				else return Vram[addr];
			}

			return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (Vram == null) return;
				addr = MapCHR(addr);
				Vram[addr] = value;
			}
			base.WritePpu(addr, value);
		}


		public override void WritePrg(int addr, byte value)
		{
			mmc3.WritePRG(addr, value);
		}

		public override byte ReadPrg(int addr)
		{
			int bank_8k = Get_PRGBank_8K(addr);
			bank_8k &= prg_mask;
			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return Rom[addr];
		}

		protected virtual void BaseSetup()
		{
			int num_prg_banks = Cart.PrgSize / 8;
			prg_mask = num_prg_banks - 1;

			int num_chr_banks = (Cart.ChrSize);
			if (num_chr_banks == 0) // vram only board
				num_chr_banks = 8;
			chr_mask = num_chr_banks - 1;

			mmc3 = new MMC3(this, num_prg_banks);
			SetMirrorType(EMirrorType.Vertical);
		}
	}
}
