using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_096
	public sealed class BANDAI_74_161_02_74 : NesBoardBase
	{
		int chr_block;
		int chr_pos = 0;
		int prg_bank_mask_32k;
		byte prg_bank_32k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER096":
				case "BANDAI-74*161/02/74":
					break;
				default:
					return false;
			}
			chr_block = 0;
			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(chr_block), ref chr_block);
			ser.Sync(nameof(chr_pos), ref chr_pos);
			ser.Sync("prg_bank_mask_16k", ref prg_bank_mask_32k);
			ser.Sync("prg_bank_16k", ref prg_bank_32k);
		}

		public override void WritePrg(int addr, byte value)
		{
			prg_bank_32k = (byte)(value & 0x03);
			chr_block = (value >> 2) & 0x01;
		}

		public override byte ReadPrg(int addr)
		{
			int bank_32k = prg_bank_32k & prg_bank_mask_32k;
			return Rom[(bank_32k << 15) + addr];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				if (addr >= 0x1000)
				{
					if (chr_block == 1)
					{
						return Vram[(0x1000 * 3 * 2) + addr];
					}
					else
					{
						return Vram[(0x1000 * 3) + addr];
					}
				}
				else
				{
					if (chr_block == 1)
					{
						return Vram[(0x1000 * chr_pos * 2) + addr];
					}
					else
					{
						return Vram[(0x1000 * chr_pos * 2) + addr];
					}
				}
			}
			else
			{
				return base.ReadPpu(addr);
			}
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (addr >= 0x1000)
				{
					if (chr_block == 1)
					{
						Vram[(0x1000 * 3 * 2) + addr] = value;
					}
					else
					{
						Vram[(0x1000 * 3) + addr] = value;
					}
				}
				{
					if (chr_block == 1)
					{
						Vram[(0x1000 * chr_pos * 2) + addr] = value;
					}
					else
					{
						Vram[(0x1000 * chr_pos * 2) + addr] = value;
					}
				}
			}
			else
			{
				base.WritePpu(addr, value);
			}
		}

		public override void AddressPpu(int addr)
		{
			byte newpos;
			if ((addr & 0x3000) != 0x2000) return;
			if ((addr & 0x3FF) >= 0x3C0) return;
			newpos = (byte)((addr >> 8) & 3);
			if (chr_pos != newpos)
			{
				chr_pos = newpos;
			}

			base.AddressPpu(addr);
		}
	}
}
