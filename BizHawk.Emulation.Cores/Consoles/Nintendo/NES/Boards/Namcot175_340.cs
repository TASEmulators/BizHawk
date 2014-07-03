using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	[NES.INESBoardImplPriority]
	public class Namcot175_340 : NES.NESBoardBase
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
		int prg_bank_mask_8k;
		int chr_bank_mask_1k;
		bool enablemirror;

		// state
		int[] prg = new int[4];
		int[] chr = new int[8];
		bool wramenable;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "NAMCOT-175":
					//wagyan land 2
					//splatter house
					SetMirrorType(Cart.pad_h, Cart.pad_v);
					break;
				case "NAMCOT-340":
					//family circuit '91
					//dream master
					//famista '92
					enablemirror = true;
					break;
				case "MAPPER210":
					// not sure what to do here because the popular public collection
					// has nothing in mapper 210 except some mortal kombat pirate cart
					enablemirror = true;
					SetMirrorType(Cart.pad_h, Cart.pad_v);
					break;
				default:
					return false;
			}
			AssertPrg(64, 128, 256, 512);
			AssertChr(64, 128, 256);
			AssertVram(0);

			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			chr_bank_mask_1k = Cart.chr_size / 1 - 1;
			prg[3] = prg_bank_mask_8k;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg, false);
			ser.Sync("chr", ref chr, false);
			ser.Sync("wramenable", ref wramenable);
		}

		public override void WritePRG(int addr, byte value)
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

		void SyncMirror(byte value)
		{
			if (enablemirror)
			{
				switch (value & 0xc0)
				{
					case 0x00: SetMirrorType(EMirrorType.OneScreenA); break;
					case 0x40: SetMirrorType(EMirrorType.Vertical); break;
					case 0x80: SetMirrorType(EMirrorType.Horizontal); break;
					case 0xc0: SetMirrorType(EMirrorType.OneScreenB); break;
				}
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[addr & 0x3ff | chr[addr >> 10] << 10];
			}
			else
			{
				return base.ReadPPU(addr);
			}
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr & 0x1fff | prg[addr >> 13] << 13];
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (wramenable)
				base.WriteWRAM(addr, value);
		}
		public override byte ReadWRAM(int addr)
		{
			if (wramenable)
				return base.ReadWRAM(addr);
			else
				return NES.DB;
		}
	}
}
