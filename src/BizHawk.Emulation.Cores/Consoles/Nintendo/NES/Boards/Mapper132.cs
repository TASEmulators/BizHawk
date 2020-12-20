using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Creatom
	// specs pulled from Nintendulator sources
	internal sealed class Mapper132 : NesBoardBase
	{
		private byte[] reg = new byte[4];

		//configuraton
		private int prg_mask, chr_mask;
		//state
		private int prg, chr;

		private bool is172, is173;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER132":
				case "UNIF_UNL-22211":
					break;
				case "MAPPER172":
					is172 = true;
					break;
				case "MAPPER173":
					is173 = true;
					break;
				default:
					return false;
			}

			prg_mask = Cart.PrgSize / 32 - 1;
			chr_mask = Cart.ChrSize / 8 - 1;
			SetMirrorType(Cart.PadH, Cart.PadV);
			//SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public void sync(byte value)
		{
			prg=reg[2]>>2;
			prg &= prg_mask;

			if (is172)
			{
				chr = ((value ^ reg[2]) >> 3 & 2) | ((value ^ reg[2]) >> 5 & 1);
			}
			else
			{
				chr = (reg[2] & 0x3);
			}

			chr &= chr_mask;
		}

		public override void WritePrg(int addr, byte value)
		{
			sync(value);
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr <= 0x103 && addr >= 0x100)
				reg[addr & 0x03] = value;
		}

		public override byte ReadExp(int addr)
		{
			if (addr == 0x100)
				return (byte)((reg[1] ^ reg[2]) | (0x40 | (is173 ? 0x01 : 0x00)));
			return NES.DB;
		}

		public override byte ReadPrg(int addr)
		{
			// Xiao Ma Li (Ch) has 16k prg (mapped to both 0x8000 and 0xC000)
			if (Cart.PrgSize == 16)
			{
				return Rom[addr & 0x3FFF];
			}

			return Rom[addr + ((prg & prg_mask) << 15)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[addr + (chr << 13)];
			}

			return base.ReadPpu(addr);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(chr), ref chr);
			ser.Sync(nameof(prg), ref prg);
			ser.Sync(nameof(is172), ref is172);
			ser.Sync(nameof(is173), ref is173);
			ser.Sync(nameof(reg), ref reg, false);
		}
	}
}
