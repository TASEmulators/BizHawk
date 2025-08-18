﻿using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//AKA mapper 64
	internal sealed class TENGEN_800032 : NesBoardBase
	{
		// configuration
		private int prg_bank_mask_8k;
		private int chr_bank_mask_1k;

		// regenerable state
		private readonly int[] prg_banks_8k = new int[4];
		private readonly int[] chr_banks_1k = new int[8];

		// state
		private int[] regs = new int[16];
		private int address;
		private bool chr_1k, chr_mode, prg_mode;

		// irq
		private int irq_countdown;
		private int a12_old;
		private int irq_reload, irq_counter;
		private bool irq_pending, irq_enable;
		private bool irq_mode;
		private bool irq_reload_pending;
		private int separator_counter;
		private int irq_countdown_2 = 0;
		private bool clock_scanline_irq;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(regs), ref regs, false);
			ser.Sync(nameof(address), ref address);
			ser.Sync(nameof(chr_1k), ref chr_1k);
			ser.Sync(nameof(chr_mode), ref chr_mode);
			ser.Sync(nameof(prg_mode), ref prg_mode);
			ser.Sync(nameof(irq_countdown), ref irq_countdown);
			ser.Sync(nameof(a12_old), ref a12_old);
			ser.Sync(nameof(irq_reload), ref irq_reload);
			ser.Sync(nameof(irq_counter), ref irq_counter);
			ser.Sync(nameof(irq_pending), ref irq_pending);
			ser.Sync(nameof(irq_enable), ref irq_enable);
			ser.Sync(nameof(irq_mode), ref irq_mode);
			ser.Sync(nameof(irq_reload_pending), ref irq_reload_pending);
			ser.Sync(nameof(separator_counter), ref separator_counter);

			Sync();
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER064":
					break;
				case "TENGEN-800032":
					AssertPrg(64, 128); AssertChr(64, 128); AssertVram(0); AssertWram(0);
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = Cart.PrgSize / 8 - 1;
			chr_bank_mask_1k = Cart.ChrSize / 1 - 1;

			SetMirrorType(EMirrorType.Vertical);

			Sync();

			return true;
		}

		private void Sync()
		{
			SyncIRQ();

			if (prg_mode)
			{
				prg_banks_8k[0] = regs[0xF] & prg_bank_mask_8k;
				prg_banks_8k[1] = regs[0x6] & prg_bank_mask_8k;
				prg_banks_8k[2] = regs[0x7] & prg_bank_mask_8k;
				prg_banks_8k[3] = 0xFF & prg_bank_mask_8k;
			}
			else
			{
				prg_banks_8k[0] = regs[0x6] & prg_bank_mask_8k;
				prg_banks_8k[1] = regs[0x7] & prg_bank_mask_8k;
				prg_banks_8k[2] = regs[0xF] & prg_bank_mask_8k;
				prg_banks_8k[3] = 0xFF & prg_bank_mask_8k;
			}

			if (chr_mode)
			{
				chr_banks_1k[0] = regs[0x2] & chr_bank_mask_1k;
				chr_banks_1k[1] = regs[0x3] & chr_bank_mask_1k;
				chr_banks_1k[2] = regs[0x4] & chr_bank_mask_1k;
				chr_banks_1k[3] = regs[0x5] & chr_bank_mask_1k;
				if (chr_1k)
				{
					chr_banks_1k[4] = regs[0x0] & chr_bank_mask_1k;
					chr_banks_1k[5] = regs[0x8] & chr_bank_mask_1k;
					chr_banks_1k[6] = regs[0x1] & chr_bank_mask_1k;
					chr_banks_1k[7] = regs[0x9] & chr_bank_mask_1k;
				}
				else
				{
					chr_banks_1k[4] = ((regs[0x0] & 0xFE) + 0) & chr_bank_mask_1k;
					chr_banks_1k[5] = ((regs[0x0] & 0xFE) + 1) & chr_bank_mask_1k;
					chr_banks_1k[6] = ((regs[0x1] & 0xFE) + 0) & chr_bank_mask_1k;
					chr_banks_1k[7] = ((regs[0x1] & 0xFE) + 1) & chr_bank_mask_1k;
				}
			}
			else
			{
				chr_banks_1k[4] = regs[0x2] & chr_bank_mask_1k;
				chr_banks_1k[5] = regs[0x3] & chr_bank_mask_1k;
				chr_banks_1k[6] = regs[0x4] & chr_bank_mask_1k;
				chr_banks_1k[7] = regs[0x5] & chr_bank_mask_1k;
				if (chr_1k)
				{
					chr_banks_1k[0] = regs[0x0] & chr_bank_mask_1k;
					chr_banks_1k[1] = regs[0x8] & chr_bank_mask_1k;
					chr_banks_1k[2] = regs[0x1] & chr_bank_mask_1k;
					chr_banks_1k[3] = regs[0x9] & chr_bank_mask_1k;
				}
				else
				{
					chr_banks_1k[0] = ((regs[0x0] & 0xFE) + 0) & chr_bank_mask_1k;
					chr_banks_1k[1] = ((regs[0x0] & 0xFE) + 1) & chr_bank_mask_1k;
					chr_banks_1k[2] = ((regs[0x1] & 0xFE) + 0) & chr_bank_mask_1k;
					chr_banks_1k[3] = ((regs[0x1] & 0xFE) + 1) & chr_bank_mask_1k;
				}
			}
		}

		public override void WritePrg(int addr, byte value)
		{
			//Console.WriteLine("mapping {0:X4} = {1:X2}", addr, value);
			addr &= 0xE001;
			switch (addr)
			{
				case 0x0000:
					address = value & 0xF;
					chr_1k = value.Bit(5);
					prg_mode = value.Bit(6);
					chr_mode = value.Bit(7);
					Sync();
					break;

				case 0x0001: //data port
					regs[address] = value;
					Sync();
					break;

				case 0x2000:
					if (value.Bit(0)) SetMirrorType(EMirrorType.Horizontal);
					else SetMirrorType(EMirrorType.Vertical);
					break;

				case 0x4000:
					irq_reload = value;
					break;

				case 0x4001:
					irq_mode = value.Bit(0);
					if (irq_mode) irq_countdown = 4;
					irq_reload_pending = true;
					break;

				case 0x6000:
					irq_enable = false;
					irq_pending = false;
					irq_counter = 0;
					SyncIRQ();
					break;
				case 0x6001:
					irq_enable = true;
					SyncIRQ();
					break;
			}
		}

		public override byte ReadPrg(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_banks_8k[bank_8k];
			addr = (bank_8k << 13) | ofs;
			return Rom[addr];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = addr >> 10;
				int ofs = addr & ((1 << 10) - 1);
				bank_1k = chr_banks_1k[bank_1k];
				addr = (bank_1k << 10) | ofs;
				return Vrom[addr];
			}
			else
				return base.ReadPpu(addr);
		}

		private void SyncIRQ()
		{
			IrqSignal = irq_pending;
		}

		private void ClockIRQ()
		{
			if (irq_reload_pending)
			{
				irq_counter = irq_reload + 1;
				irq_reload_pending = false;

				if (irq_counter == 0)
				{
					if (irq_enable)
					{
						irq_countdown_2 = 9;
					}
				}
			}

			irq_counter--;
			if (irq_counter==0)
			{
				if (irq_enable)
				{
					irq_countdown_2 = 9;
				}

				irq_counter = irq_reload + 1;
			}

			if (irq_counter < 0)
			{
				irq_counter = irq_reload;
			}
			/*
			else if (irq_counter == 0)
			{

				irq_counter = irq_reload;
				if (irq_counter == 0)
				{
					if (irq_enable)
					{
						irq_countdown_2 = 9;
					}
				}
			}
			else
			{
				irq_counter--;
				if (irq_enable)
				{

					if (irq_counter==0)
						irq_countdown_2 = 9;
				}
			}
			*/
		}

		public override void ClockCpu()
		{

			if (irq_mode)
			{
				irq_countdown--;
				if (irq_countdown == 0)
				{
					ClockIRQ();
					irq_countdown = 4;
				}
			}
			else
			{
				if (clock_scanline_irq)
				{
					clock_scanline_irq = false;
					ClockIRQ();
				}
			}
		}

		public override void ClockPpu()
		{
			if (separator_counter > 0)
				separator_counter--;

			if (irq_countdown_2 > 0)
			{
				irq_countdown_2--;
				if (irq_countdown_2==0)
				{
					irq_pending = true;
					SyncIRQ();
				}
			}
		}

		public override void AddressPpu(int addr)
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
					clock_scanline_irq = true;
				}
			}

			a12_old = a12;
		}
	}
}