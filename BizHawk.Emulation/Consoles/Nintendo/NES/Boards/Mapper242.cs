namespace BizHawk.Emulation.Consoles.Nintendo
{
	/*
PCB Class: Unknown
iNES Mapper 242
PRG-ROM: 32KB
PRG-RAM: None
CHR-ROM: 16KB
CHR-RAM: None
Battery is not available
mirroring - both
	 * 
	 * Games:
	 * Wai Xing Zhan Shi (Ch)
	 */

	class Mapper242 : NES.NESBoardBase
	{
		int prg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER242":
					break;
				default:
					return false;
			}
			SetMirrorType(NES.NESBoardBase.EMirrorType.Vertical); 
			return true;
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr + (prg * 0x8000)];
		}

		public override void WritePRG(int addr, byte value)
		{
			prg = (addr >> 3) & 15;
			//fceux had different logic here for the mirroring, but that didnt match with experiments on dragon quest 8 nor disch's docs
			//i changed it at the same time
			bool mirror = addr.Bit(1);
			if (mirror)
				SetMirrorType(NES.NESBoardBase.EMirrorType.Horizontal);
			else
				SetMirrorType(NES.NESBoardBase.EMirrorType.Vertical);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg);
		}
	}
}
