using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Logic copied from FCEUX
	// Super Mario Bros. Pocker Mali (Unl)
	public class UNIF_UNL_AX5705 : NES.NESBoardBase
	{
		private IntBuffer prg_reg = new IntBuffer(2);
		private IntBuffer chr_reg = new IntBuffer(8);

		private int _prgMask8k;
		private int _chrMask1k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-AX5705":
					break;
				default:
					return false;
			}

			_prgMask8k = Cart.prg_size / 8 - 1;
			_chrMask1k = Cart.chr_size / 1 - 1;

			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_reg", ref prg_reg);
			ser.Sync("chr_reg", ref chr_reg);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr += 0x8000;
			byte V = value;
			int mirr = 0;

			switch (addr & 0xF00F)
			{
				case 0x8000: prg_reg[0] = ((V & 2) << 2) | ((V & 8) >> 2) | (V & 5); break; // EPROM dump have mixed PRG and CHR banks, data lines to mapper seems to be mixed
				case 0x8008: mirr = V & 1; break;
				case 0xA000: prg_reg[1] = ((V & 2) << 2) | ((V & 8) >> 2) | (V & 5); break;
				case 0xA008: chr_reg[0] = (chr_reg[0] & 0xF0) | (V & 0x0F); break;
				case 0xA009: chr_reg[0] = (chr_reg[0] & 0x0F) | ((((V & 4) >> 1) | ((V & 2) << 1) | (V & 0x09)) << 4); break;
				case 0xA00A: chr_reg[1] = (chr_reg[1] & 0xF0) | (V & 0x0F); break;
				case 0xA00B: chr_reg[1] = (chr_reg[1] & 0x0F) | ((((V & 4) >> 1) | ((V & 2) << 1) | (V & 0x09)) << 4); break;
				case 0xC000: chr_reg[2] = (chr_reg[2] & 0xF0) | (V & 0x0F); break;
				case 0xC001: chr_reg[2] = (chr_reg[2] & 0x0F) | ((((V & 4) >> 1) | ((V & 2) << 1) | (V & 0x09)) << 4); break;
				case 0xC002: chr_reg[3] = (chr_reg[3] & 0xF0) | (V & 0x0F); break;
				case 0xC003: chr_reg[3] = (chr_reg[3] & 0x0F) | ((((V & 4) >> 1) | ((V & 2) << 1) | (V & 0x09)) << 4); break;
				case 0xC008: chr_reg[4] = (chr_reg[4] & 0xF0) | (V & 0x0F); break;
				case 0xC009: chr_reg[4] = (chr_reg[4] & 0x0F) | ((((V & 4) >> 1) | ((V & 2) << 1) | (V & 0x09)) << 4); break;
				case 0xC00A: chr_reg[5] = (chr_reg[5] & 0xF0) | (V & 0x0F); break;
				case 0xC00B: chr_reg[5] = (chr_reg[5] & 0x0F) | ((((V & 4) >> 1) | ((V & 2) << 1) | (V & 0x09)) << 4); break;
				case 0xE000: chr_reg[6] = (chr_reg[6] & 0xF0) | (V & 0x0F); break;
				case 0xE001: chr_reg[6] = (chr_reg[6] & 0x0F) | ((((V & 4) >> 1) | ((V & 2) << 1) | (V & 0x09)) << 4); break;
				case 0xE002: chr_reg[7] = (chr_reg[7] & 0xF0) | (V & 0x0F); break;
				case 0xE003: chr_reg[7] = (chr_reg[7] & 0x0F) | ((((V & 4) >> 1) | ((V & 2) << 1) | (V & 0x09)) << 4); break;
			}

			SetMirrorType(mirr > 0 ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = chr_reg[addr / 0x400];
				bank &= _chrMask1k;
				return VROM[(bank * 0x400) + (addr & 0x3FF)];
			}

			return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			addr += 0x8000;
			if (addr < 0xA000)
			{
				return ROM[(prg_reg[0] * 0x2000) + (addr & 0x1FFF)];
			}
			else if (addr < 0xC000)
			{
				return ROM[(prg_reg[1] * 0x2000) + (addr & 0x1FFF)];
			}
			else if (addr < 0xE000)
			{
				return ROM[((0xFE & _prgMask8k) * 0x2000) + (addr & 0x1FFF)];
			}
			else
			{
				return ROM[((0xFF & _prgMask8k) * 0x2000) + (addr & 0x1FFF)];
			}
		}
	}
}
