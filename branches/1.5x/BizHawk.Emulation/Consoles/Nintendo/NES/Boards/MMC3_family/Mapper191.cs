using System;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	public sealed class Mapper191 : MMC3Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER191":
					break;
				default:
					return false;
			}

			//this board has 2k of chr ram
			Cart.vram_size = 2;
			BaseSetup();

			throw new InvalidOperationException("THIS MAPPER ISNT TESTED! WHAT GAME USES IT? PLEASE REPORT!");

			//return true;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr > 0x2000) return base.ReadPPU(addr);
			int bank_1k = Get_CHRBank_1K(addr);
			if (bank_1k.Bit(7))
			{
				//this is referencing chr ram
				return VRAM[addr & 0x7FF];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr > 0x2000) base.WritePPU(addr,value);
			int bank_1k = Get_CHRBank_1K(addr);
			if (bank_1k.Bit(7))
			{
				//this is referencing chr ram
				VRAM[addr & 0x7FF] = value;
			}
			else base.WritePPU(addr, value);
		}

	}
}