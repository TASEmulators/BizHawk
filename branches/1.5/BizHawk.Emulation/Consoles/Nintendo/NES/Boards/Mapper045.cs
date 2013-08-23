namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper045 : MMC3Board_Base
	{
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_045

		int chr_bank_mask_2k;
		int prg_bank_mask_8k;
		int cur_reg = 0;
		ByteBuffer regs = new ByteBuffer(4);
		bool lock_regs = false;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER045":
					break;
				default:
					return false;
			}

			chr_bank_mask_2k = Cart.chr_size / 2 - 1;
			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			BaseSetup();
			return true;
		}

		public override void Dispose()
		{
			regs.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("regs", ref regs);
			ser.Sync("lock_regs", ref lock_regs);
			base.SyncState(ser);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (lock_regs)
			{
				base.WriteWRAM(addr, value);
			}
			else
			{
				regs[cur_reg] = value;
				IncrementCounter();
				Sync45();
			}
		}

		private void Sync45()
		{
			lock_regs = regs[3].Bit(6);
		}

		private void IncrementCounter()
		{
			if (cur_reg == 3)
			{
				cur_reg = 0;
			}
			else
			{
				cur_reg++;
			}
		}

		public override void NESSoftReset()
		{
			lock_regs = false;
			cur_reg = 0;
			regs = new ByteBuffer(4);
			base.NESSoftReset();
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = Get_PRGBank_8K(addr);
			bank_8k &= (regs[3] & 0x3F) ^ 0x3F;
			bank_8k |= regs[1];
			bank_8k &= prg_mask;
			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return ROM[addr];
		}

		private int CHR_AND()
		{
			switch (regs[2] & 0x0F)
			{
				default:
				case 0:
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
					return 0x00;
				case 8:
					return 0x01;
				case 9:
				case 0xA:
				case 0xB:
				case 0xC:
				
				case 0xD:
					return 0x3F;
				case 0xE:
					return 0x7F;
				case 0xF:
					return 0xFF;
			}
		}

		private int CHR_OR()
		{
			int x = regs[0] | ((regs[2] >> 4) << 8);
			return x;
		}

		protected override int MapCHR(int addr)
		{
			int bank_1k = Get_CHRBank_1K(addr);
			
			bank_1k &= CHR_AND();
			bank_1k |= CHR_OR();
			bank_1k &= chr_mask;
			addr = (bank_1k << 10) | (addr & 0x3FF);
			return addr;
		}
	}
}
