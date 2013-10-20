namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA mapper 184
	//Sunsoft-1 chips, EXCEPT for fantasy zone.
	//this is confusing. see docs/sunsoft.txt
	public sealed class Sunsoft1 : NES.NESBoardBase
	{
		int chr_mask;
		int left_piece = 0;
		int right_piece = 3;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER184":
					break;

				case "SUNSOFT-1":
					//this will catch fantasy zone, which isn't emulated the same as the other SUNSOFT-1 boards
					if (Cart.pcb == "SUNSOFT-4")
						return false;
					break;
				default:
					return false;
			}
			chr_mask = (Cart.chr_size / 4) - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}



		public override byte ReadPPU(int addr)
		{

			if (addr < 0x1000)
			{
				return VROM[(addr & 0xFFF) + (left_piece * 0x1000)];
			}
			else if (addr < 0x2000)
			{
				return VROM[(addr & 0xFFF) + (right_piece * 0x1000)];
			}

			return base.ReadPPU(addr);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			left_piece = value & 7 & chr_mask;
			right_piece = (value >> 4) & 7 & chr_mask;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("left_piece", ref left_piece);
			ser.Sync("right_piece", ref right_piece);
		}
	}
}
