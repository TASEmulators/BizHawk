namespace BizHawk.Emulation.Consoles.Nintendo
{
	/*
	PRG-ROM - 32kb/16kb
	CHR-ROM - 16kb
	Mirroring - Vertical
	ines Mapper 87
	
	Example Games:
	--------------------------
	City Connection (J) - JF_05
	Ninja Jajamaru Kun - JF_06
	Argus (J) - JF_07
	*/
	public sealed class JALECO_JF_05_06_07 : NES.NESBoardBase
	{
		bool hibit, lowbit;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER087":
					break;
				case "JALECO-JF-05":
				case "JALECO-JF-06":
					AssertPrg(16); AssertChr(16); AssertVram(0); AssertWram(0);
					break;
				case "JALECO-JF-07":
					AssertPrg(32); AssertChr(16); AssertVram(0); AssertWram(0);
					break;
				default:
					return false;
			}
			SetMirrorType(NES.NESBoardBase.EMirrorType.Vertical);
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("hibit", ref hibit);
			ser.Sync("lowbit", ref lowbit);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			hibit = value.Bit(0);
			lowbit = value.Bit(1);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				if (lowbit)
				{
					if (hibit)
						return VROM[addr + 0x6000];
					return VROM[addr + 0x2000];
				}
				else
				{
					if (hibit)
						return VROM[addr + 0x4000];
					return VROM[addr];
				}
			}
			return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			if (addr > 0x4000) addr -= 0x4000;
			return base.ReadPRG(addr);
		}
	}
}
