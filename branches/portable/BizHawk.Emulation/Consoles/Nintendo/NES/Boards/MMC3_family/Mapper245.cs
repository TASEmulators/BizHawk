namespace BizHawk.Emulation.Consoles.Nintendo
{
	public sealed class Mapper245 : MMC3Board_Base
	{
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_245
		bool chr_mode;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER245":
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
			ser.Sync("chr_mode", ref chr_mode);
			base.SyncState(ser);
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = Get_PRGBank_8K(addr);
			bank_8k &= prg_mask;
			bank_8k &= 0x3F;
			int reg0 = ((base.mmc3.chr_regs_1k[0] >> 1) & 0x01);
			if (reg0 == 1)
			{
				addr |= 0x40;
			}
			else
			{
				addr |= 0x00;
			}

			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return ROM[addr];
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr == 0)
			{
				chr_mode = value.Bit(7);
			}
			base.WritePRG(addr, value);
		}

		public override byte  ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				if (chr_mode)
				{
					if (addr < 0x1000)
					{
						return VRAM[addr + 0x1000];
					}
					else
					{
						return VRAM[addr - 0x1000];
					}
				}
				else
				{
					return VRAM[addr];
				}
			}
			else
			{
				return base.ReadPPU(addr);
			}
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (chr_mode)
				{
					if (addr < 0x1000)
					{
						VRAM[addr + 0x1000] = value;
					}
					else
					{
						VRAM[addr - 0x1000] = value;
					}
				}
				else
				{
					VRAM[addr] = value;
				}
			}
			else
			{
				base.WritePPU(addr, value);
			}
		}
	}
}
