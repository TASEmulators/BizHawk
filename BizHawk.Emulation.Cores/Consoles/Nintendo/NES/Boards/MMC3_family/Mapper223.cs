namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// TODO
	public sealed class Mapper223 : MMC3Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER223":
					break;
				default:
					return false;
			}

			BaseSetup();

			mmc3.wram_enable = true;
			mmc3.wram_write_protect = true;
			return true;
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr>0x1000)
			{
				Wram[addr + 0x4000 - (0x5000 - 0x2000)] = value;
			}
			else 
				base.WriteExp(addr, value);
		}

		public override byte ReadExp(int addr)
		{
			if (addr > 0x1000)
			{
				return Wram[addr + 0x4000 - (0x5000 - 0x2000)];
			}
			else
				return base.ReadExp(addr);
		}
	}
}
