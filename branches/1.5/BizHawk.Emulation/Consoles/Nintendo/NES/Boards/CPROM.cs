namespace BizHawk.Emulation.Consoles.Nintendo
{

	public class CPROM : NES.NESBoardBase
	{
		//generally mapper 13

		//state
		int chr;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER013":
					break;

				case "NES-CPROM": //videomation
					AssertPrg(32); AssertChr(0); AssertVram(16); AssertWram(0);
					break;
				
				default:
					return false;
			}

			//TODO - assert that mirror type is vertical?
			//set it in the cart?

			SetMirrorType(NES.NESBoardBase.EMirrorType.Vertical);

			return true;
		}
		
		public override void WritePRG(int addr, byte value)
		{
			value = HandleNormalPRGConflict(addr,value);
			chr = value&3;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x1000)
				return VRAM[addr];
			else if(addr<0x2000)
				return VRAM[addr - 0x1000 + (chr << 12)];
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x1000)
				VRAM[addr] = value;
			else if (addr < 0x2000)
				VRAM[addr - 0x1000 + (chr << 12)] = value;
			else base.WritePPU(addr,value);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr",ref chr);
		}


	}
}