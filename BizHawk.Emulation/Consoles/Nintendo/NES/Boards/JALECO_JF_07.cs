using System;
using System.IO;
using System.Diagnostics;


namespace BizHawk.Emulation.Consoles.Nintendo
{
	/*
	PRG-ROM - 32kb
	CHR-ROM - 16kb
	Mirroring - Vertical
	ines Mapper 87 (Board JALECO_JF_05 is also classified under mapper 87)
	
	Example Games:
	--------------------------
	Argus (J)
	*/
	class JALECO_JF_07: NES.NESBoardBase
	{
		bool hibit, lowbit;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "JALECO-JF-07":
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
	}
}
