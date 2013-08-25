namespace BizHawk.Emulation.Consoles.Nintendo
{
	public sealed class MLT_MAX15 : NES.NESBoardBase
	{
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_234

		bool mode = false;
		int block_high = 0;
		int block_low = 0;
		byte prg_bank = 0;
		byte chr_bank_high = 0;
		byte chr_bank_low = 0;
		int prg_bank_mask_32k = 0;
		int chr_bank_mask_8k = 0;
		bool reg_0_locked = false;
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER234":
				case "MLT-MAXI15":
					break;
				default:
					return false;
			}

			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			chr_bank_mask_8k = Cart.chr_size / 8 - 1;

			return true;
		}

		public override void NESSoftReset()
		{
			mode = false;
			block_high = 0;
			block_low = 0;
			prg_bank = 0;
			chr_bank_high = 0;
			reg_0_locked = false;
			base.NESSoftReset();
			SetMirrorType(EMirrorType.Vertical);
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("mode", ref mode);
			ser.Sync("block_high", ref block_high);
			ser.Sync("block_low", ref block_low);
			ser.Sync("prg_bank", ref prg_bank);
			ser.Sync("chr_bank", ref chr_bank_high);
			ser.Sync("reg_0_locked", ref reg_0_locked);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr < 0x7F80)
			{
				base.WritePRG(addr, value);
			}
			else
			{
				switch (addr & 0x7FF8)
				{
					case 0x7F80:
					case 0x7F88:
					case 0x7F90:
					case 0x7F98:
						if (!reg_0_locked)
						{
							if (value > 0)
							{
								reg_0_locked = true;
							}
							block_high = (value >> 1) & 0x07;
							block_low = value & 0x01;
							mode = value.Bit(6);
							if (value.Bit(7))
							{
								SetMirrorType(EMirrorType.Horizontal);
							}
							else
							{
								SetMirrorType(EMirrorType.Vertical);
							}
						}
						break;
					case 0x7FC0:
					case 0x7FC8:
					case 0x7FD0:
					case 0x7FD8:
						//Console.WriteLine("This mapper is doing undocumented register writes!");
						break;
					case 0x7FE8:
					case 0x7FF0:
						prg_bank = (byte)(value & 0x01);
						chr_bank_high = (byte)((value >> 4) & 0x03);
						chr_bank_low = (byte)((value >> 6) & 0x01);
						//Console.WriteLine("chr_lw: {0}, chr_hg: {1}, value: {2}", chr_bank_low, chr_bank_high, value);
						break;
				}
			}
		}

		public override byte ReadPRG(int addr)
		{
			int bank;
			if (mode)
			{
				bank = (block_high << 1) | prg_bank;
			}
			else
			{
				bank = (block_high << 1) | block_low;
			}
			
			byte value = ROM[((bank & prg_bank_mask_32k) * 0x8000) + (addr & 0x7FFF)];

			if (addr >= 0x7F80)
			{
				WritePRG(addr, value);
			}
			
			return value;
			
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank;
				if (mode)
				{
					bank = (block_high << 3) | (chr_bank_low << 2) | chr_bank_high;
				}
				else
				{
					bank = (block_high << 3) | (block_low << 2) | chr_bank_high;
				}

				return VROM[((bank & chr_bank_mask_8k) * 0x2000) + (addr & 0x1FFF)];
			}
			return base.ReadPPU(addr);
		}
	}
}
