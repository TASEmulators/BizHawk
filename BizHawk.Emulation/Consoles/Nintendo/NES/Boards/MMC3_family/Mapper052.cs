using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper52 : MMC3Board_Base
	{
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_052

		bool lock_regs = false;
		bool prg_block_size = false;
		bool chr_block_size = false;
		int prg_or = 0;
		int chr_or = 0;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER052":
					break;
				default:
					return false;
			}

			BaseSetup();
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("lock_regs", ref lock_regs);
			ser.Sync("prg_block_size", ref prg_block_size);
			ser.Sync("chr_block_size", ref chr_block_size);
			ser.Sync("prg_or", ref prg_or);
			ser.Sync("chr_or", ref chr_or);
			base.SyncState(ser);
		}

		public override void NESSoftReset()
		{
			lock_regs = false;
			prg_block_size = false;
			chr_block_size = false;
			prg_or = 0;
			chr_or = 0;
			base.NESSoftReset();
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (lock_regs)
			{
				base.WriteWRAM(addr, value);
			}
			else
			{
				lock_regs = value.Bit(4);
				prg_block_size = value.Bit(3);
				chr_block_size = value.Bit(6);
				int combo = (value >> 2) & 0x01;
				prg_or = ((value >> 1) & 0x01) << 5;
				prg_or |= (value & 0x01) << 6;
				prg_or |= (combo << 7);
				chr_or = ((value >> 4) & 0x01) << 7;
				chr_or |= combo << 8;
				chr_or |= ((value >> 5) & 0x01) << 9;
			}
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = Get_PRGBank_8K(addr);
			bank_8k &= PRG_AND();
			bank_8k |= PRG_OR();
			bank_8k &= prg_mask;
			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return ROM[addr];
		}

		private int PRG_AND()
		{
			if (prg_block_size)
			{
				return 0x0F;
			}
			else
			{
				return 0x1F;
			}
		}

		private int PRG_OR()
		{
			if (prg_block_size)
			{
				return prg_or;
			}
			else
			{
				return (prg_or & 0x60);
			}
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

		private int CHR_AND()
		{
			if (chr_block_size)
			{
				return 0x7F;
			}
			else
			{
				return 0xFF;
			}
		}

		private int CHR_OR()
		{
			if (chr_block_size)
			{
				return chr_or;
			}
			else
			{
				return (chr_or & 0x300);
			}
		}
	}
}
