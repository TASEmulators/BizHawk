using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper245 : MMC3Board_Base
	{
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_245
		bool chr_mode;

		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER245":
					AssertVram(8);
					break;
				default:
					return false;
			}
			chr_mode = false;
			BaseSetup();
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(chr_mode), ref chr_mode);
			base.SyncState(ser);
		}

		public override byte ReadPrg(int addr)
		{
			int bank_8k = Get_PRGBank_8K(addr);
			bank_8k &= 0x3F;
			int reg0 = ((base.mmc3.chr_regs_1k[0] >> 1) & 0x01);
			if (reg0 == 1)
			{
				bank_8k |= 0x40;
			}
			else
			{
				bank_8k |= 0x00;
			}

			bank_8k &= prg_mask;
			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return Rom[addr];
		}

		public override void WritePrg(int addr, byte value)
		{
			if (addr == 0)
			{
				chr_mode = value.Bit(7);
			}
			base.WritePrg(addr, value);
		}

		public override byte  ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				if (chr_mode)
				{
					if (addr < 0x1000)
					{
						return Vram[addr + 0x1000];
					}
					else
					{
						return Vram[addr - 0x1000];
					}
				}
				else
				{
					return Vram[addr];
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
				if (chr_mode)
				{
					if (addr < 0x1000)
					{
						Vram[addr + 0x1000] = value;
					}
					else
					{
						Vram[addr - 0x1000] = value;
					}
				}
				else
				{
					Vram[addr] = value;
				}
			}
			else
			{
				base.WritePpu(addr, value);
			}
		}
	}
}
