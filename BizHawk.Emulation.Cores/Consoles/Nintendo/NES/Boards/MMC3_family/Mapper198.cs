namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class Mapper198 : MMC3Board_Base
	{

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER198":
					break;
				default:
					return false;
			}

			BaseSetup();
			prg_mask = 1024 / 8 - 1;
			return true;
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			var val = base.Get_PRGBank_8K(addr);
			if (val >= 0x50)
			{
				return val & 0x4F;
			}

			return val;
		}

		public override byte ReadExp(int addr)
		{
			if (addr >= 0x1000)
			{
				return Wram[addr - 0x1000];
			}

			return base.ReadExp(addr);
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr >= 0x1000)
			{
				Wram[addr - 0x1000] = value;
			}

			base.WriteExp(addr, value);
		}
	}
}
