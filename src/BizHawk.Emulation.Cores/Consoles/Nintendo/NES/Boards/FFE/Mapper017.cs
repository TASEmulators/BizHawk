﻿using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper017 : NesBoardBase
	{
		private byte[] prg_regs_8k = new byte[4];
		private byte[] chr_regs_1k = new byte[8];

		private int prg_mask_8k;
		private int chr_mask_1k;

		private bool irq_enable;
		private bool irq_pending;
		private int irq_count;
		private const int IRQ_DESTINATION = 0x10000;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER017":
					break;
				default:
					return false;
			}

			prg_mask_8k = Cart.PrgSize / 8 - 1;
			chr_mask_1k = Cart.ChrSize / 1 - 1;

			//Initial State
			prg_regs_8k[0] = 0x00;
			prg_regs_8k[1] = 0x01;
			prg_regs_8k[2] = 0xFE;
			prg_regs_8k[3] = 0xFF;

			SetMirrorType(Cart.PadH, Cart.PadV);

			return true;
		}

		public override void WriteExp(int addr, byte value)
		{
			switch (addr & 0x7FF)
			{
				//Mirroring:
				case 0x2FE:
				case 0x2FF:
					int mirroring = ((addr << 1) & 2) | ((value >> 4) & 1);
					switch (mirroring)
					{
						case 0: SetMirrorType(EMirrorType.OneScreenA); break;
						case 1: SetMirrorType(EMirrorType.OneScreenB); break;
						case 2: SetMirrorType(EMirrorType.Vertical); break;
						case 3: SetMirrorType(EMirrorType.Horizontal); break;
					}
					break;

				//IRQ
				case 0x501:
					irq_enable = value.Bit(0);
					irq_pending = false;
					irq_count = 0;
					SyncIRQ();
					break;
				case 0x502:
					irq_count &= 0xFF00;
					irq_count |= value;
					break;
				case 0x503:
					irq_count &= 0x00FF;
					irq_count |= value << 8;
					irq_enable = true;
					irq_pending = false;
					SyncIRQ();
					break;

				//PRG
				case 0x504:
				case 0x505:
				case 0x506:
				case 0x507:
					prg_regs_8k[addr & 3] = value;
					break;

				//CHR
				case 0x510:
				case 0x511:
				case 0x512:
				case 0x513:
				case 0x514:
				case 0x515:
				case 0x516:
				case 0x517:
					chr_regs_1k[addr & 7] = value;
					break;
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);

			ser.Sync(nameof(prg_regs_8k), ref prg_regs_8k, false);
			ser.Sync(nameof(chr_regs_1k), ref chr_regs_1k, false);

			ser.Sync(nameof(irq_enable), ref irq_enable);
			ser.Sync(nameof(irq_pending), ref irq_pending);
			ser.Sync(nameof(irq_count), ref irq_count);
		}

		public override byte ReadPrg(int addr)
		{
			int bank_8k = prg_regs_8k[addr >> 13];
			bank_8k &= prg_mask_8k;
			int offset = addr & 0x1FFF;
			return Rom[bank_8k << 13 | offset];
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000 && Vram != null)
			{
				Vram[addr] = value;
			}
			base.WritePpu(addr, value);
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				if (Vram != null) return Vram[addr];

				int bank_1k = chr_regs_1k[addr >> 10];
				bank_1k &= chr_mask_1k;
				int offset = addr & 0x3FF;
				return Vrom[bank_1k << 10 | offset];
			}
			return base.ReadPpu(addr);
		}

		public override void ClockCpu()
		{
			if (irq_enable)
			{
				ClockIRQ();
			}
		}

		private void ClockIRQ()
		{
			irq_count++;
			if (irq_count >= IRQ_DESTINATION)
			{
				irq_enable = false;
				irq_pending = true;
			}

			SyncIRQ();
		}

		private void SyncIRQ()
		{
			SyncIRQ(irq_pending);
		}
	}
}
