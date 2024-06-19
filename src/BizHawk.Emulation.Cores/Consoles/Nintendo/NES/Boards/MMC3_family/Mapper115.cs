using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//this class also handles mapper 248
	//FCEUX uses 115 to implement 248 as well (as of 09-apr-2012 it does it buggily in the case of Bao Qing Tian (As))
	//VirtuaNES has its own class that implements 248. I think it's wrong (MAME and/or MESS may have switched to using 115 at some point)
	internal sealed class Mapper115 : MMC3Board_Base
	{
		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MAPPER115":
				case "MAPPER248":
					break;
				default:
					return false;
			}

			BaseSetup();

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_mode_mapper), ref prg_mode_mapper);
			ser.Sync(nameof(prg_page), ref prg_page);
			ser.Sync(nameof(chr_block_or), ref chr_block_or);
		}

		private bool prg_mode_mapper;
		private int prg_page, chr_block_or;

		public override void WriteWram(int addr, byte value)
		{
			base.WriteWram(addr, value);
			switch (addr & 1)
			{
				case 0:
					prg_mode_mapper = value.Bit(7);
					prg_page = (value & 0xF) >> 1;
					break;
				case 1:
					chr_block_or = (value & 0x1)<<8;
					break;
			}
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			int bank_8k = mmc3.Get_PRGBank_8K(addr);
			if (!prg_mode_mapper) return bank_8k;
			else if (addr < 0x2000)
			{
				return prg_page*4;
			}
			else if (addr < 0x4000)
			{
				return prg_page*4 + 1;
			}
			else if (addr < 0x6000)
			{
				return prg_page*4 + 2;
			}
			else
			{
				return prg_page*4 + 3;
			}
			
		}

		protected override int Get_CHRBank_1K(int addr)
		{
			int bank_1k = base.Get_CHRBank_1K(addr);
			return bank_1k | chr_block_or;
		}
	}
}
