﻿using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	[NesBoardImplPriority]
	internal sealed class Namcot175_340 : NesBoardBase
	{
		/*
		 * Namcot 175 and 340.  Simpler versions of the 129/163.  Differences:
		 * No IRQs.
		 * No extended ciram control:
		 *   PPU $0000:$1FFF is always CHRROM.
		 *   PPU $2000:$2FFF is always CIRAM.
		 * No sound.
		 * Mirroring varies by type:
		 *   175: Hardwired mirroring (H/V)
		 *   340: Simple mirroring control through $E000.
		 * 
		 * Nesdev mentions that the 340 has no WRAM, but that's because no games
		 * on the 340 ever used it.  (In fact, only 1 (?) 175 game has wram).
		 * In any event, WRAM write protect is different than on 129/163.
		 * 
		 * This should be mapper 210, with mapper 19 being 129/163, but you know how
		 * mapper numbers are.  To complicate things, some 175/340 games run correctly
		 * on a 163 because they make compatibility writes to both sets of mirroring regs.
		 */

		// config
		private int prg_bank_mask_8k;
		private int chr_bank_mask_1k;
		private bool enablemirror;

		// state
		private int[] prg = new int[4];
		private int[] chr = new int[8];
		private bool wramenable;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "NAMCOT-175":
					SetMirrorType(Cart.PadH, Cart.PadV);
					break;
				case "NAMCOT-340":
					enablemirror = true;
					break;
				case "MAPPER210":
					// not sure what to do here because the popular public collection
					// has nothing in mapper 210 except some mortal kombat pirate cart
					enablemirror = true;
					SetMirrorType(Cart.PadH, Cart.PadV);
					break;
				default:
					return false;
			}
			AssertPrg(64, 128, 256, 512);
			AssertChr(64, 128, 256);
			AssertVram(0);

			prg_bank_mask_8k = Cart.PrgSize / 8 - 1;
			chr_bank_mask_1k = Cart.ChrSize / 1 - 1;
			prg[3] = prg_bank_mask_8k;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg), ref prg, false);
			ser.Sync(nameof(chr), ref chr, false);
			ser.Sync(nameof(wramenable), ref wramenable);
		}

		public override void WritePrg(int addr, byte value)
		{
			switch (addr & 0x7800)
			{
				case 0x0000: chr[0] = value & chr_bank_mask_1k; break;
				case 0x0800: chr[1] = value & chr_bank_mask_1k; break;
				case 0x1000: chr[2] = value & chr_bank_mask_1k; break;
				case 0x1800: chr[3] = value & chr_bank_mask_1k; break;
				case 0x2000: chr[4] = value & chr_bank_mask_1k; break;
				case 0x2800: chr[5] = value & chr_bank_mask_1k; break;
				case 0x3000: chr[6] = value & chr_bank_mask_1k; break;
				case 0x3800: chr[7] = value & chr_bank_mask_1k; break;
				case 0x4000: wramenable = value.Bit(0); break;
				case 0x4800: break;
				case 0x5000: break;
				case 0x5800: break;
				case 0x6000: prg[0] = value & 63 & prg_bank_mask_8k; SyncMirror(value); break;
				case 0x6800: prg[1] = value & 63 & prg_bank_mask_8k; break;
				case 0x7000: prg[2] = value & 63 & prg_bank_mask_8k; break;
				case 0x7800: break;
			}
		}

		private void SyncMirror(byte value)
		{
			if (enablemirror)
			{
				switch (value & 0xc0)
				{
					case 0x00: SetMirrorType(EMirrorType.OneScreenA); break;
					case 0x40: SetMirrorType(EMirrorType.Vertical); break;
					case 0x80: SetMirrorType(EMirrorType.OneScreenB); break;
					case 0xc0: SetMirrorType(EMirrorType.Horizontal); break;
				}
			}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[addr & 0x3ff | chr[addr >> 10] << 10];
			}
			else
			{
				return base.ReadPpu(addr);
			}
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[addr & 0x1fff | prg[addr >> 13] << 13];
		}

		public override void WriteWram(int addr, byte value)
		{
			if (wramenable)
				base.WriteWram(addr, value);
		}
		public override byte ReadWram(int addr)
		{
			if (wramenable)
				return base.ReadWram(addr);
			else
				return NES.DB;
		}
	}
}
