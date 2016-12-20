using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Creatom
	// specs pulled from Nintendulator sources
	public sealed class Mapper132 : NES.NESBoardBase
	{
		private ByteBuffer reg = new ByteBuffer(4);

		//configuraton
		int prg_mask, chr_mask;
		//state
		int prg, chr;

		bool is172, is173;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
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

			prg_mask = Cart.prg_size / 32 - 1;
			chr_mask = Cart.chr_size / 8 - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			//SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void Dispose()
		{
			reg.Dispose();
			base.Dispose();
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

		public override void WritePRG(int addr, byte value)
		{
			sync(value);
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr <= 0x103 && addr >= 0x100)
				reg[addr & 0x03] = value;
					//reg[addr&0x03] = (byte)(value & 0x0f);

		}

		public override byte ReadEXP(int addr)
		{

			/*if ((addr & 0x100) != 0)
				return (byte)((NES.DB & (is173 ? 0x01 : 0xf0)) | reg[2]);
			else if ((addr & 0x1000) == 0)
				return NES.DB;
			else
				return 0xff;
				*/
			if (addr==0x100)
				return (byte)((reg[1] ^ reg[2]) | (0x40 | (is173 ? 0x01 : 0x00)));
			else
				return NES.DB;
		}

		public override byte ReadPRG(int addr)
		{
			// Xiao Ma Li (Ch) has 16k prg (mapped to both 0x8000 and 0xC000)
			if (Cart.prg_size == 16)
			{
				return ROM[addr & 0x3FFF];
			}
			else
			{
				return ROM[addr + ((prg & prg_mask) << 15)];
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[addr + (chr << 13)];
			}

			return base.ReadPPU(addr);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr", ref chr);
			ser.Sync("prg", ref prg);
			ser.Sync("is172", ref is172);
			ser.Sync("is173", ref is173);
			ser.Sync("reg", ref reg);
		}

	}
}
