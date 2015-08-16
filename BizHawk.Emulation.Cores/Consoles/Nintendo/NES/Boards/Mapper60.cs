using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper60 : NES.NESBoardBase
	{
		// http://wiki.nesdev.com/w/index.php/INES_Mapper_060

		int reg = 0;
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER060":
					break;
				default:
					return false;
			}

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("reg", ref reg);
			base.SyncState(ser);
		}

		public override void NESSoftReset()
		{
			if (reg >= 3)
			{
				reg = 0;
			}
			else
			{
				reg++;
			}
		}

		public override byte ReadPRG(int addr)
		{
			addr &= 0x3FFF;
			return ROM[addr + (reg * 0x4000)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[(reg * 0x2000) + addr];
			}
			return base.ReadPPU(addr);
		}
	}
}
