using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_082
	internal sealed class Taito_X1_017 : NesBoardBase
	{
		private int prg_bank_mask, chr_bank_mask;
		private byte[] prg_regs_8k = new byte[4];
		private byte[] chr_regs_1k = new byte[8];
		private bool ChrMode;
		private bool[] wramenable = new bool[3];

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_regs_8k), ref prg_regs_8k, false);
			ser.Sync(nameof(chr_regs_1k), ref chr_regs_1k, false);
			ser.Sync(nameof(ChrMode), ref ChrMode);
			for (int i = 0; i < wramenable.Length; i++)
				ser.Sync("wramenable_" + i, ref wramenable[i]);
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER082":
					break;
				case "TAITO-X1-017":
					break;
				default:
					return false;
			}

			// actually internal to the mapper
			Cart.WramSize = 5;

			SetMirrorType(EMirrorType.Vertical);
			chr_bank_mask = Cart.ChrSize / 1 - 1;
			prg_bank_mask = Cart.PrgSize / 8 - 1;
			prg_regs_8k[3] = 0xFF;
			return true;
		}

		public override byte ReadWram(int addr)
		{
			if (addr < 0x1400 && wramenable[addr >> 11])
			{
				return Wram[addr];
			}

			return NES.DB;
		}

		public override void WriteWram(int addr, byte value)
		{
			if (addr < 0x1400)
			{
				if (wramenable[addr >> 11])
					Wram[addr] = value;
				return;
			}

			switch (addr)
			{
				case 0x1EF0:
					chr_regs_1k[0] = (byte)(value & ~1);
					chr_regs_1k[1] = (byte)(value | 1);
					break;
				case 0x1EF1:
					chr_regs_1k[2] = (byte)(value & ~1);
					chr_regs_1k[3] = (byte)(value | 1);
					break;

				case 0x1EF2:
					chr_regs_1k[4] = value;
					break;
				case 0x1EF3:
					chr_regs_1k[5] = value;
					break;
				case 0x1EF4:
					chr_regs_1k[6] = value;
					break;
				case 0X1EF5:
					chr_regs_1k[7] = value;
					break;

				case 0x1EF6:
					ChrMode = value.Bit(1);
					if (value.Bit(0))
						SetMirrorType(EMirrorType.Vertical);
					else
						SetMirrorType(EMirrorType.Horizontal);
					break;

				case 0x1EF7: wramenable[0] = value == 0xca; break;
				case 0x1EF8: wramenable[1] = value == 0x69; break;
				case 0x1EF9: wramenable[2] = value == 0x84; break;

				case 0x1EFA:
					prg_regs_8k[0] = (byte)(value >> 2);
					break;
				case 0x1EFB:
					prg_regs_8k[1] = (byte)(value >> 2);
					break;
				case 0x1EFC:
					prg_regs_8k[2] = (byte)(value >> 2);
					break;
			}
		}

		public override byte ReadPrg(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_regs_8k[bank_8k];
			bank_8k &= prg_bank_mask;
			addr = (bank_8k << 13) | ofs;
			return Rom[addr];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				if (ChrMode)
					addr ^= 1 << 12;
				int bank_1k = addr >> 10;
				int ofs = addr & ((1 << 10) - 1);
				bank_1k = chr_regs_1k[bank_1k];
				bank_1k &= chr_bank_mask;
				addr = (bank_1k << 10) | ofs;
				return Vrom[addr];
			}

			return base.ReadPpu(addr);
		}
	}
}
