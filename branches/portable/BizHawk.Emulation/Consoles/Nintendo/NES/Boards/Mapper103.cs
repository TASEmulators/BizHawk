namespace BizHawk.Emulation.Consoles.Nintendo
{
	// Doki Doki Panic (FDS port)
	// "BTL 2708"
	public class Mapper103 : NES.NESBoardBase
	{
		int prg;
		bool romenable;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER103": // ines identification
					Cart.wram_size = 16;
					Cart.vram_size = 8;
					AssertPrg(128);
					break;
				case "BTL-2708": // ??
					break;
				default:
					return false;
			}
			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void WriteWRAM(int addr, byte value)
		{
			// writes always go to wram, even if rom is mapped in for read
			WRAM[addr] = value;
		}

		public override byte ReadWRAM(int addr)
		{
			if (romenable)
				return ROM[addr | prg << 13];
			else
				return WRAM[addr];
		}

		public override byte ReadPRG(int addr)
		{
			if (!romenable && addr >= 0x3800 && addr < 0x5800)
				return WRAM[addr - 0x1800];
			else
				return ROM[addr | 0x18000];
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr >= 0x3800 && addr < 0x5800)
				WRAM[addr - 0x1800] = value;
			else
			{
				switch (addr & 0x7000)
				{
					case 0x0000:
						prg = value & 15;
						break;
					case 0x6000:
						SetMirrorType((value & 8) != 0 ? EMirrorType.Horizontal : EMirrorType.Vertical);
						break;
					case 0x7000:
						romenable = (value & 16) != 0;
						break;
				}
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("romenable", ref romenable);
			ser.Sync("prg", ref prg);
		}

	}
}
