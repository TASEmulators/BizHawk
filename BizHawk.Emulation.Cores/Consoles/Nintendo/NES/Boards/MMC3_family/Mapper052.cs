using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper52 : MMC3Board_Base
	{
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_052

		bool lock_regs = false;
		bool prg_block_size = false;
		bool chr_block_size = false;
		int prg_or = 0;
		int chr_or = 0;

		public override bool Configure(EDetectionOrigin origin)
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
			ser.Sync(nameof(lock_regs), ref lock_regs);
			ser.Sync(nameof(prg_block_size), ref prg_block_size);
			ser.Sync(nameof(chr_block_size), ref chr_block_size);
			ser.Sync(nameof(prg_or), ref prg_or);
			ser.Sync(nameof(chr_or), ref chr_or);
			base.SyncState(ser);
		}

		public override void NesSoftReset()
		{
			lock_regs = false;
			prg_block_size = false;
			chr_block_size = false;
			prg_or = 0;
			chr_or = 0;
			base.NesSoftReset();
		}

		public override void WriteWram(int addr, byte value)
		{
			if (lock_regs)
			{
				base.WriteWram(addr, value);
			}
			else
			{
				lock_regs = true;
				prg_block_size = value.Bit(3);
				chr_block_size = value.Bit(6);
				int combo = (value >> 2) & 0x01;
				prg_or = ((value >> 1) & 0x01) << 5;
				prg_or |= (value & 0x01) << 4;
				prg_or |= (combo << 6);
				chr_or = ((value >> 4) & 0x01) << 7;
				chr_or |= combo << 8;
				chr_or |= ((value >> 5) & 0x01) << 9;
			}
		}

		public override byte ReadPrg(int addr)
		{
			int bank_8k = Get_PRGBank_8K(addr);
			bank_8k &= PRG_AND();
			bank_8k |= PRG_OR();
			bank_8k &= prg_mask;
			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return Rom[addr];
		}

		private int PRG_AND()
		{
			if (prg_block_size)
			{
				return 0x0F;
			}

			return 0x1F;
		}

		private int PRG_OR()
		{
			if (prg_block_size)
			{
				return prg_or;
			}

			return (prg_or & 0x60);
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

			return 0xFF;
		}

		private int CHR_OR()
		{
			if (chr_block_size)
			{
				return chr_or;
			}

			return (chr_or & 0x300);
		}
	}
}
