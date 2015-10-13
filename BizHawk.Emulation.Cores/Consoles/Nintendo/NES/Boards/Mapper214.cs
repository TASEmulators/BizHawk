using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Super Gun 20-in-1
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_214
	public class Mapper214 : NES.NESBoardBase
	{
		private int _chrReg, _prgReg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER214":
					break;
				default:
					return false;
			}

			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chrReg", ref _chrReg);
		}

		public override void WritePRG(int addr, byte value)
		{
			_chrReg = addr & 3;
			_prgReg = (addr >> 2) & 3;
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[(_prgReg * 0x4000) + (addr & 0x3FFF)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[(_chrReg * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPPU(addr);
		}
	}
}
