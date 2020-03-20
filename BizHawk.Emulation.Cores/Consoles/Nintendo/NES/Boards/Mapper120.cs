using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper120 : NesBoardBase
	{
		//Used by Tobidase Daisakusen (FDS Conversion).  Undocumented by Disch docs, this implementation is based on FCEUX
		
		byte prg_reg;

		public override bool Configure(EDetectionOrigin origin)
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
			ser.Sync(nameof(prg_reg), ref prg_reg);
			base.SyncState(ser);
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr == 0x01FF)
			{
				prg_reg = (byte)(value & 0x07);
			}
		}

		public override byte ReadWram(int addr)
		{
			return Rom[((prg_reg & 7) * 0x2000) + (addr & 0x1FFF)];
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[0x10000 + addr];
		}
	}
}
