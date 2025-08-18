﻿using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Cores.Components;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//Mapper 069 is FME7
	//or, Sunsoft-5, which is FME7 with additional sound hardware

	internal sealed class Sunsoft_5 : Sunsoft_FME7
	{
		private Sunsoft5BAudio audio;

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "SUNSOFT-5B": //Gimmick! (J)
					AssertPrg(256); AssertChr(128); AssertWram(0); AssertVram(0); AssertBattery(false);
					break;
				default:
					return false;
			}

			BaseConfigure();
			if (NES.apu != null)
				audio = new Sunsoft5BAudio(NES.apu.ExternalQueue);

			return true;
		}

		public override void WritePrg(int addr, byte value)
		{
			int a = addr & 0xe000;
			if (a == 0x4000)
				audio.RegSelect(value);
			else if (a == 0x6000)
				audio.RegWrite(value);
			else
				base.WritePrg(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			audio.SyncState(ser);
		}

		public override void ClockCpu()
		{
			audio.Clock();
			base.ClockCpu();
		}
	}

	internal class Sunsoft_FME7 : NesBoardBase
	{
		//configuration
		private int prg_bank_mask_8k, chr_bank_mask_1k, wram_bank_mask_8k;

		//state
		private int addr_reg;
		private byte[] regs = new byte[12];
		private byte[] prg_banks_8k = new byte[4];
		private int wram_bank;
		private bool wram_ram_selected, wram_ram_enabled;
		private ushort irq_counter;
		private bool irq_countdown, irq_enabled, irq_asserted;
		private int clock_counter;


		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(addr_reg), ref addr_reg);
			ser.Sync(nameof(regs), ref regs, false);
			ser.Sync(nameof(prg_banks_8k), ref prg_banks_8k, false);
			ser.Sync(nameof(wram_bank), ref wram_bank);
			ser.Sync(nameof(wram_ram_selected), ref wram_ram_selected);
			ser.Sync(nameof(wram_ram_enabled), ref wram_ram_enabled);
			ser.Sync(nameof(irq_counter), ref irq_counter);
			ser.Sync(nameof(irq_countdown), ref irq_countdown);
			ser.Sync(nameof(irq_enabled), ref irq_enabled);
			ser.Sync(nameof(irq_asserted), ref irq_asserted);
			ser.Sync(nameof(clock_counter), ref clock_counter);
			SyncIrq();
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "NES-JLROM": // Mr Gimmick
					AssertPrg(256); AssertChr(128); AssertWram(0); AssertVram(0); AssertBattery(false);
					break;
				case "NES-JSROM": // batman(E)
					AssertPrg(128); AssertChr(256); AssertWram(8); AssertVram(0); AssertBattery(false);
					break;
				case "MAPPER069":
					break;
				case "SUNSOFT-5A": //Batman (J)
					AssertPrg(128); AssertChr(128,256); AssertWram(0,8); AssertVram(0); AssertBattery(false);
					break;
				case "SUNSOFT-FME-7": //Barcode World (J)
					AssertPrg(128, 256); AssertChr(128, 256); AssertWram(0, 8); AssertVram(0);
					break;
				case "NES-BTR": //Batman - Return of the Joker (U), Mr Gimmick (Proto)
					AssertPrg(128, 256); AssertChr(128, 256); AssertWram(0, 8); AssertVram(0); AssertBattery(false);
					break;
				default:
					return false;
			}

			BaseConfigure();

			return true;
		}

		protected void BaseConfigure()
		{
			prg_bank_mask_8k = (Cart.PrgSize / 8) - 1;
			wram_bank_mask_8k = (Cart.WramSize / 8) - 1;
			chr_bank_mask_1k = Cart.ChrSize - 1;
			prg_banks_8k[3] = 0xFF;
			SetMirrorType(EMirrorType.Vertical);
		}

		private void SyncPRG()
		{
			wram_ram_enabled = (regs[8] & 0x80) != 0;
			wram_ram_selected = (regs[8]&0x40)!=0;
			wram_bank = (byte)(regs[8] & 0x7F);
			for(int i=0;i<3;i++)
			{
				prg_banks_8k[i] = regs[8 + i + 1];
			}
		}

		public override void WritePrg(int addr, byte value)
		{
			addr &= 0xE000;
			switch (addr)
			{
				case 0x0000: //$8000:  [.... AAAA]   Address for use with $A000
					addr_reg = value & 0xF;
					break;
				case 0x2000: //$A000:  [DDDD DDDD]   Data port
					switch(addr_reg)
					{
						case 0: case 1: case 2: case 3:
						case 4: case 5: case 6: case 7:
							regs[addr_reg] = value;
							//NES.LogLine("cr set to {0},{1},{2},{3},{4},{5},{6},{7}", regs[0], regs[1], regs[2], regs[3], regs[4], regs[5], regs[6], regs[7]);
							break;
						case 8: case 9: case 0xA: case 0xB:
							regs[addr_reg] = value;
							//NES.LogLine("pr/wr set to {0},{1},{2},{3},~0xFF~", regs[8], regs[9], regs[10], regs[11]);
							SyncPRG();
							break;
						case 0xC:
							switch (value & 3)
							{
								case 0: SetMirrorType(EMirrorType.Vertical); break;
								case 1: SetMirrorType(EMirrorType.Horizontal); break;
								case 2: SetMirrorType(EMirrorType.OneScreenA); break;
								case 3: SetMirrorType(EMirrorType.OneScreenB); break;
							}
							break;
						case 0xD:
							irq_countdown = value.Bit(7);
							irq_enabled = value.Bit(0);
							//if (value != 0) NES.LogLine("irq set to {0},{1} with value {2:x2}", irq_countdown, irq_enabled, value);

							//always ACK for reg 0xD and no other reg
							//http://forums.nesdev.com/viewtopic.php?f=2&t=12436&start=15
							irq_asserted = false;
							SyncIrq();

							break;
						case 0xE:
							irq_counter &= 0xFF00;
							irq_counter |= value;
							//NES.LogLine("irq_counter set to {0:x4}", irq_counter);
							break;
						case 0xF:
							irq_counter &= 0x00FF;
							irq_counter |= (ushort)(value << 8);
							//NES.LogLine("irq_counter set to {0:x4}", irq_counter);
							break;
						}
					break;
			}
		}

		private void SyncIrq()
		{
			IrqSignal = irq_asserted;
		}

		public override void ClockCpu()
		{
			if (!irq_countdown) return;
			irq_counter--;
			if (irq_counter == 0xFFFF)
			{
				irq_asserted = true;
				SyncIrq();
			}
		}

		/*
		public override void ClockPPU()
		{
			clock_counter++;
			if (clock_counter == 3)
			{
				ClockCPU();
				clock_counter = 0;
			}
		}*/

		public override byte ReadPrg(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1<<13)-1);
			bank_8k = prg_banks_8k[bank_8k];
			bank_8k &= prg_bank_mask_8k;
			addr = (bank_8k << 13) | ofs;
			return Rom[addr];
		}

		private int CalcWRAMAddress(int addr, int bank_mask_8k)
		{
			int ofs = addr & ((1 << 13) - 1);
			int bank_8k = wram_bank;
			bank_8k &= bank_mask_8k;
			addr = (bank_8k << 13) | ofs;
			return addr;
		}

		private int CalcPPUAddress(int addr)
		{
			int bank_1k = addr >> 10;
			int ofs = addr & ((1 << 10) - 1);
			bank_1k = regs[bank_1k];
			bank_1k &= chr_bank_mask_1k;
			return (bank_1k<<10) | ofs;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
				return Vrom[CalcPPUAddress(addr)];
			else return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{ }
			else base.WritePpu(addr, value);
		}

		public override byte ReadWram(int addr)
		{
			if (!wram_ram_selected)
			{
				addr = CalcWRAMAddress(addr, prg_bank_mask_8k);
				return Rom[addr];
			}
			else if (!wram_ram_enabled || Wram is null)
				return 0xFF; //empty bus
			else
			{
				addr = CalcWRAMAddress(addr, wram_bank_mask_8k);
				return Wram[addr];
			}
		}

		public override void WriteWram(int addr, byte value)
		{
			if (!wram_ram_selected) return;
			else if (!wram_ram_enabled || Wram is null)
				return; //empty bus
			else
			{
				addr = CalcWRAMAddress(addr, wram_bank_mask_8k);
				Wram[addr] = value;
			}
		}
	}
}
