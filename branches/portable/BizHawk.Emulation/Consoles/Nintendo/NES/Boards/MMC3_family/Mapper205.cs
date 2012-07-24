using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper205 : MMC3Board_Base
	{
		//Mapper 205 info: http://wiki.nesdev.com/w/index.php/INES_Mapper_205

		int block;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER205":
					break;
				default:
					return false;
			}

			block = 0;

			BaseSetup();
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("mode", ref block);
			base.SyncState(ser);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				block = value & 0x03;
			}

			base.WritePPU(addr, value);
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = Get_PRGBank_8K(addr);
			bank_8k &= prg_mask;

			switch (block)
			{
				case 0:
					bank_8k &= 0x1F;
					bank_8k |= 0x00;
					break;
				case 1:
					bank_8k &= 0x1F;
					bank_8k |= 0x10;
					break;
				case 2:
					bank_8k &= 0x0F;
					bank_8k |= 0x20;
					break;
				case 3:
					bank_8k &= 0x0F;
					bank_8k |= 0x30;
					break;
			}

			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return ROM[addr];
		}

		int MapCHR(int addr)
		{
			int bank_1k = Get_CHRBank_1K(addr);
			bank_1k &= chr_mask;
			switch (bank_1k)
			{
				case 0:
					bank_1k &= 0xFF;
					bank_1k |= 0x00;
					break;
				case 1:
					bank_1k &= 0xFF;
					bank_1k |= 0x80;
					break;
				case 2:
					bank_1k &= 0x7F;
					bank_1k |= 0x100;
					break;
				case 3:
					bank_1k &= 0x7F;
					bank_1k |= 0x180;
					break;
			}
			addr = (bank_1k << 10) | (addr & 0x3FF);
			return addr;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				addr = MapCHR(addr);
				if (VROM != null)
					return VROM[addr + extra_vrom];
				else return VRAM[addr];
			}
			else return base.ReadPPU(addr);
		}

		public override void NESSoftReset()
		{
			block = 0;
			base.NESSoftReset();
		}
	}
}
