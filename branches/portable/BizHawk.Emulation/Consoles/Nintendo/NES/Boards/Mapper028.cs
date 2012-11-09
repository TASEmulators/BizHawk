using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	// http://wiki.nesdev.com/w/index.php/User:Tepples/Multi-discrete_mapper
	public class Mapper028 : NES.NESBoardBase
	{
		// config
		int chr_mask_8k;
		int prg_mask_16k;

		// state
		int reg;
		int chr;
		int prg;
		int mode;
		int outer;

		// regennable state
		int prglo;
		int prghi;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER028":
					break;
				default:
					return false;
			}
			AssertPrg(32, 64, 128, 256, 512, 1024, 2048);
			AssertChr(0);
			chr_mask_8k = Cart.chr_size / 8 - 1;
			prg_mask_16k = Cart.prg_size / 16 - 1;
			Cart.wram_size = 0;
			Cart.vram_size = 32;
			// the only part of initial state that is important is that
			// C000:FFFF contains the tail end of the rom
			outer = 63;
			prg = 15;
			Sync();
			return true;
		}

		void Sync()
		{
			int outb = outer << 1;
			// this can probably be rolled up, but i have no motivation to do so
			// until it's been tested
			switch (mode & 0x3c)
			{
				// 32K modes
				case 0x00:
				case 0x04:
					prglo = outb;
					prghi = outb | 1;
					break;
				case 0x10:
				case 0x14:
					prglo = outb & ~2 | prg << 1 & 2;
					prghi = outb & ~2 | prg << 1 & 2 | 1;
					break;
				case 0x20:
				case 0x24:
					prglo = outb & ~6 | prg << 1 & 6;
					prghi = outb & ~6 | prg << 1 & 6 | 1;
					break;
				case 0x30:
				case 0x34:
					prglo = outb & ~14 | prg << 1 & 14;
					prghi = outb & ~14 | prg << 1 & 14 | 1;
					break;
				// bottom fixed modes
				case 0x08:
					prglo = outb;
					prghi = outb | prg & 1;
					break;
				case 0x18:
					prglo = outb;
					prghi = outb & ~2 | prg & 3;
					break;
				case 0x28:
					prglo = outb;
					prghi = outb & ~6 | prg & 7;
					break;
				case 0x38:
					prglo = outb;
					prghi = outb & ~14 | prg & 15;
					break;
				// top fixed modes
				case 0x0c:
					prglo = outb | prg & 1;
					prghi = outb | 1;
					break;
				case 0x1c:
					prglo = outb & ~2 | prg & 3;
					prghi = outb | 1;
					break;
				case 0x2c:
					prglo = outb & ~6 | prg & 7;
					prghi = outb | 1;
					break;
				case 0x3c:
					prglo = outb & ~14 | prg & 15;
					prghi = outb | 1;
					break;
			}
			prglo &= prg_mask_16k;
			prghi &= prg_mask_16k;
		}

		void Mirror(byte value)
		{
			if ((mode & 2) == 0)
			{
				mode &= 0xfe;
				mode |= value >> 4 & 1;
			}
			SyncMirror();
		}

		void SyncMirror()
		{
			switch (mode & 3)
			{
				case 0: SetMirrorType(EMirrorType.OneScreenA); break;
				case 1: SetMirrorType(EMirrorType.OneScreenB); break;
				case 2: SetMirrorType(EMirrorType.Vertical); break;
				case 3: SetMirrorType(EMirrorType.Horizontal); break;
			}
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr >= 0x1000)
				reg = value & 0x81;
		}

		public override void WritePRG(int addr, byte value)
		{
			switch (reg)
			{
				case 0x00:
					chr = value & 3;
					Mirror(value);
					break;
				case 0x01:
					prg = value & 15;
					Mirror(value);
					Sync();
					break;
				case 0x80:
					mode = value & 63;
					SyncMirror();
					Sync();
					break;
				case 0x81:
					outer = value & 63;
					Sync();
					break;
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VRAM[addr | chr << 13];
			else
				return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
				VRAM[addr | chr << 13] = value;
			else
				base.WritePPU(addr, value);
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[(addr & 0x3fff) | (addr < 0x4000 ? prglo : prghi) << 14];
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg", ref reg);
			ser.Sync("chr", ref chr);
			ser.Sync("prg", ref prg);
			ser.Sync("mode", ref mode);
			ser.Sync("outer", ref outer);
			if (!ser.IsWriter)
				Sync();
		}
	}
}
