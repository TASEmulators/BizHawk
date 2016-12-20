namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper055 : NES.NESBoardBase
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER055": // Nestopia calls this mapper 55, I know of no dumps with the designation though
				case "UNIF_BTL-MARIO1-MALEE2":
					break;
				default:
					return false;
			}

			WRAM = new byte[0x800];
			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override byte ReadWRAM(int addr)
		{
			if (addr < 0x1000)
			{
				return ROM[0x8000 + (addr & 0x7FF)];
			}

			return WRAM[(addr & 0x7FF)];
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (addr >= 0x1000)
			{
				WRAM[(addr & 0x7FF)] = value;
			}
		}
	}
}
