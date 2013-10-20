namespace BizHawk.Emulation.Consoles.Nintendo
{
	//pretty much just one game. 
	//wires the mapper outputs to control the nametables. check out the companion board TLSROM
	public sealed class Mapper095 : Namcot108Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "NAMCOT-3425": //dragon buster (J)
					AssertPrg(128); AssertChr(32); AssertVram(0); AssertWram(0);
					break;
				default:
					return false;
			}

			BaseSetup();
			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		int RewireNametable(int addr, int bitsel)
		{
			int bank_1k = mapper.Get_CHRBank_1K(addr & 0x1FFF);
			int nt = (bank_1k >> bitsel) & 1;
			int ofs = addr & 0x3FF;
			addr = 0x2000 + (nt << 10);
			addr |= (ofs);
			return addr;
		}

		//mapper 095's chief unique contribution is to add this nametable rewiring logic: CHR A15 directly controls CIRAM A10
		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000) return base.ReadPPU(addr);
			else return base.ReadPPU(RewireNametable(addr, 5));
		}
		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000) base.WritePPU(addr, value);
			else base.WritePPU(RewireNametable(addr, 5), value);
		}
	}
}