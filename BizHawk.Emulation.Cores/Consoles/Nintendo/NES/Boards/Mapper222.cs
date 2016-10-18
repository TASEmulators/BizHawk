using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Bad Dudes.7z|Dragon Ninja (J) [p1][!].nes
	// irq doesn't work right; easily seen in any level but level 1
	public sealed class Mapper222 : NES.NESBoardBase
	{
		int prg_bank_mask_8k;
		int chr_bank_mask_1k;

		int[] prg = new int[4];
		int[] chr = new int[8];

		int irq_time = 0;
		bool irq_counting = false;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER222":
					break;
				default:
					return false;
			}

			SetMirrorType(EMirrorType.Vertical);

			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			chr_bank_mask_1k = Cart.chr_size - 1;

			prg[3] = prg_bank_mask_8k;
			prg[2] = prg[3] - 1;
			return true;
		}

		public override void WritePRG(int addr, byte value)
		{
			addr &= 0x7003;
			switch (addr)
			{
				case 0x0000:
					prg[0] = value & prg_bank_mask_8k;
					break;
				case 0x2000:
					prg[1] = value & prg_bank_mask_8k;
					break;
				case 0x7000:
				//case 0x7001:
					// this is of course sort of VRC like... except it doesn't work right
					irq_time = value;
					IRQSignal = false;
					irq_counting = true;
					if (value == 0)
						irq_counting = false;
					Console.WriteLine("IRQ Set\\Ack: SL {0} val {1}", NES.ppu.ppur.status.sl, value);
					break;
				//case 0x7002:
				//irq_counting = true;
				//Console.WriteLine("IRQ GO: SL {0} val {1}", NES.ppu.ppur.status.sl, value);
				//break;
				case 0x1000: SetMirrorType(!value.Bit(0) ? EMirrorType.Vertical : EMirrorType.Horizontal);break;
				case 0x3000: chr[0] = value & chr_bank_mask_1k; ; break;
				case 0x3002: chr[1] = value & chr_bank_mask_1k; ; break;
				case 0x4000: chr[2] = value & chr_bank_mask_1k; ; break;
				case 0x4002: chr[3] = value & chr_bank_mask_1k; ; break;
				case 0x5000: chr[4] = value & chr_bank_mask_1k; ; break;
				case 0x5002: chr[5] = value & chr_bank_mask_1k; ; break;
				case 0x6000: chr[6] = value & chr_bank_mask_1k; ; break;
				case 0x6002: chr[7] = value & chr_bank_mask_1k; ; break;


			}
			/*
			if (addr >= 0x3000 && addr < 0x7000)	
			{
				int b = (addr >> 11) - 6;
				b |= addr >> 1 & 1;

				if ((addr & 1) != 0)
					chr[b] = (chr[b] & 0x0f | value << 4) & chr_bank_mask_1k;
				else
					chr[b] = (chr[b] & 0xf0 | value & 0x0f) & chr_bank_mask_1k;
			}*/
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr & 0x1fff | prg[addr >> 13] << 13];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[addr & 0x3ff | chr[addr >> 10] << 10];
			else
				return base.ReadPPU(addr);
		}

		public override void ClockCPU()
		{
			if (irq_counting)
			{
				irq_time++;
				if (irq_time >= 240)
				{
					//irq_counting = false;
					IRQSignal = true;
					//irq_time = 0;
					Console.WriteLine("IRQ TRIG: SL {0}", NES.ppu.ppur.status.sl);
				}
			}
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg", ref prg, false);
			ser.Sync("chr", ref chr, false);
			ser.Sync("irq_time", ref irq_time);
			ser.Sync("irq_counting", ref irq_counting);
			base.SyncState(ser);
		}
	}
}
