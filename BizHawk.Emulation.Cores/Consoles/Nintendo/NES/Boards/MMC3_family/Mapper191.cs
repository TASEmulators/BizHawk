using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper191 : MMC3Board_Base
	{
		public override bool Configure(EDetectionOrigin origin)
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

			//theres a possibly bogus Q Boy rom using this mapper but I have no idea what emulator its supposed to boot in, for proof
			//throw new InvalidOperationException("THIS MAPPER ISNT TESTED! WHAT GAME USES IT? PLEASE REPORT!");

			return true;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr >= 0x2000)
			{
				return base.ReadPpu(addr);
			}

			int bank_1k = Get_CHRBank_1K(addr);
			if (bank_1k.Bit(7))
			{
				//this is referencing chr ram
				return Vram[addr & 0x7FF];
			}
			else return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr >= 0x2000)
			{
				base.WritePpu(addr, value);
				return;
			}

			int bank_1k = Get_CHRBank_1K(addr);
			if (bank_1k.Bit(7))
			{
				//this is referencing chr ram
				Vram[addr & 0x7FF] = value;
			}
			else base.WritePpu(addr, value);
		}

	}
}