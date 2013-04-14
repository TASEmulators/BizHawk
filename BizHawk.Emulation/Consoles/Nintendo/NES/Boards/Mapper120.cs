namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper120 : NES.NESBoardBase
	{
		//Used by Tobidase Daisakusen (FDS Conversion).  Undocumented by Disch docs, this implementation is based on FCEUX
		
		byte prg_reg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER120":
					break;
				default:
					return false;
			}
			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_reg", ref prg_reg);
			base.SyncState(ser);
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr == 0x01FF)
			{
				prg_reg = (byte)(value & 0x07);
			}
		}

		public override byte ReadWRAM(int addr)
		{
			return ROM[((prg_reg & 7) * 0x2000) + (addr & 0x1FFF)];
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[0x10000 + addr];
		}
	}
}
