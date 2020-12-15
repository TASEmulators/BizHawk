using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// http://wiki.nesdev.com/w/index.php/User:Tepples/Multi-discrete_mapper
	internal sealed class Mapper028 : NesBoardBase
	{
		// config
		private int chr_mask_8k;
		private int prg_mask_16k;

		// state
		private int reg;
		private int chr;
		private int prg;
		private int mode;
		private int outer;

		// regennable state
		private int prglo;
		private int prghi;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER028":
					break;
				default:
					return false;
			}
			AssertPrg(32, 64, 128, 256, 512, 1024, 2048);
			AssertChr(0);
			chr_mask_8k = Cart.ChrSize / 8 - 1;
			prg_mask_16k = Cart.PrgSize / 16 - 1;
			Cart.WramSize = 0;
			Cart.VramSize = 32;
			// the only part of initial state that is important is that
			// C000:FFFF contains the tail end of the rom
			outer = 63;
			prg = 15;
			Sync();
			return true;
		}

		private void Sync()
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

		private void Mirror(byte value)
		{
			if ((mode & 2) == 0)
			{
				mode &= 0xfe;
				mode |= value >> 4 & 1;
			}
			SyncMirror();
		}

		private void SyncMirror()
		{
			switch (mode & 3)
			{
				case 0: SetMirrorType(EMirrorType.OneScreenA); break;
				case 1: SetMirrorType(EMirrorType.OneScreenB); break;
				case 2: SetMirrorType(EMirrorType.Vertical); break;
				case 3: SetMirrorType(EMirrorType.Horizontal); break;
			}
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr >= 0x1000)
				reg = value & 0x81;
		}

		public override void WritePrg(int addr, byte value)
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

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
				return Vram[addr | chr << 13];
			else
				return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
				Vram[addr | chr << 13] = value;
			else
				base.WritePpu(addr, value);
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[(addr & 0x3fff) | (addr < 0x4000 ? prglo : prghi) << 14];
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(reg), ref reg);
			ser.Sync(nameof(chr), ref chr);
			ser.Sync(nameof(prg), ref prg);
			ser.Sync(nameof(mode), ref mode);
			ser.Sync(nameof(outer), ref outer);
			if (!ser.IsWriter)
				Sync();
		}
	}
}
