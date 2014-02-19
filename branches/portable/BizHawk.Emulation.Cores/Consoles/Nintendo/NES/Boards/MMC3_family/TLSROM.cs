namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//aka mapper 118
	//wires the mapper outputs to control the nametables
	public sealed class TLSROM : MMC3Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "NES-TLSROM": //pro sport hockey (U)
					AssertPrg(128); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				case "MAPPER118":
					AssertVram(0);
					break;
				case "HVC-TKSROM": //ys III: wanderers from ys (J)
					AssertPrg(256); AssertChr(128); AssertVram(0); AssertWram(8);
					AssertBattery(true);
					break;
				case "TENGEN-800037": //Alien Syndrome (U)
					// this board is actually a RAMBO-1 (mapper064) with TLS-style rewiring
					// but it seems to work fine here, so lets not worry about it
					AssertPrg(128); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				case "MAPPER158":
					// as above
					AssertVram(0); Cart.wram_size = 0;
					break;
				case "HVC-TLSROM":
					AssertPrg(256); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				default:
					return false;
			}

			BaseSetup();
			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void WritePRG(int addr, byte value)
		{
			base.WritePRG(addr, value);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000) return base.ReadPPU(addr);
			else return base.ReadPPU(RewireNametable_TLSROM(addr, 7));
		}
		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000) base.WritePPU(addr, value);
			else base.WritePPU(RewireNametable_TLSROM(addr, 7), value);
		}

	}
}