using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//this class also handles mapper 248
	//FCEUX uses 115 to implement 248 as well (as of 09-apr-2012 it does it buggily in the case of Bao Qing Tian (As))
	//VirtuaNES has its own class that implements 248. I think it's wrong (MAME and/or MESS may have switched to using 115 at some point)
	public class Mapper115 : MMC3Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
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
			ser.Sync("prg_mode_mapper", ref prg_mode_mapper);
			ser.Sync("prg_page", ref prg_page);
			ser.Sync("chr_block_or", ref chr_block_or);
		}

		bool prg_mode_mapper;
		int prg_page, chr_block_or;

		public override void WriteWRAM(int addr, byte value)
		{
			base.WriteWRAM(addr, value);
			switch (addr & 1)
			{
				case 0:
					prg_mode_mapper = value.Bit(7);
					prg_page = (value & 0xF) * 2;
					break;
				case 1:
					chr_block_or = (value & 0x1)<<8;
					break;
			}
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			int bank_8k = mmc3.Get_PRGBank_8K(addr);
			if (prg_mode_mapper == false) return bank_8k;
			else if (addr < 0x4000)
			{
				return (addr >> 13) + prg_page;
			}
			else return bank_8k;
		}

		protected override int Get_CHRBank_1K(int addr)
		{
			int bank_1k = base.Get_CHRBank_1K(addr);
			return bank_1k | chr_block_or;
		}
	}
}