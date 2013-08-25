namespace BizHawk.Emulation.Consoles.Nintendo
{
	//http://wiki.nesdev.com/w/index.php/INES_Mapper_044
	public class Mapper044 : MMC3Board_Base
	{
		public sealed override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER044": 
					break;
				default:
					return false;
			}

			BaseSetup();

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("block_select", ref block_select);
		}

		int block_select;

		public override void WritePRG(int addr, byte value)
		{
			base.WritePRG(addr, value);

			switch (addr & 0x6001)
			{
				case 0x2001: //$A001
					block_select = value & 0x7;
					break;
			}
		}

		static readonly int[] PRG_AND = new int[] {0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0f };
		static readonly int[] PRG_OR = new int[] { 0x00, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x60 };
		static readonly int[] CHR_AND = new int[] { 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7f };
		static readonly int[] CHR_OR = new int[] { 0x000, 0x080, 0x100, 0x180, 0x200, 0x280, 0x300, 0x300 };

		protected override int Get_PRGBank_8K(int addr)
		{
			int bank_8k = mmc3.Get_PRGBank_8K(addr);
			return (bank_8k & PRG_AND[block_select]) | PRG_OR[block_select];
		}

		protected override int Get_CHRBank_1K(int addr)
		{
			int bank_1k = base.Get_CHRBank_1K(addr);
			return (bank_1k & CHR_AND[block_select]) | CHR_OR[block_select];
		}

	}
}