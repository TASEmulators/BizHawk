using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Bonza (R)
	// Bonza is some kind of gambling game requiring an outside interface of some kind
	// this is not implemented


	// Magic Jewelry 2 (Unl)
	public class Bonza : NES.NESBoardBase
	{
		private int _chrReg;
		private int _prgReg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER216":
					break;
				default:
					return false;
			}

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chrReg", ref _chrReg);
			ser.Sync("prgReg", ref _prgReg);
		}

		public override void WritePRG(int addr, byte value)
		{
			_prgReg = addr & 1;
			_chrReg = (addr >> 1) & 7;
		}

		public override byte ReadEXP(int addr)
		{
			if (addr == 0x1000)
			{
				return 0;
			}

			return base.ReadEXP(addr);
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[(_prgReg * 0x8000) + (addr & 0x7FFF)];
		}

		public override byte ReadPPU(int addr)
		{
			// Magic Jewelry has no VROM and does not write chr regs
			if (addr < 0x2000 && VROM != null)
			{
				return VROM[(_chrReg * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPPU(addr);
		}
	}
}
