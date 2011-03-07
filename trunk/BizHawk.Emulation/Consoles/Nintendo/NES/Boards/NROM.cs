using System;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo.Boards
{
	public class NROM : NES.NESBoardBase
	{
		//configuration
		int prg_byte_mask;

		//state
		//(none)

		public override bool Configure(NES.BootGodDB.Cart cart)
		{
			//configure
			switch (cart.board_type)
			{
				case "HVC-NROM-256": 
					BoardInfo.PRG_Size = 32; 
					BoardInfo.CHR_Size = 8; 
					break;

				case "HVC-RROM":
				case "HVC-NROM-128":
				case "IREM-NROM-128":
				case "KONAMI-NROM-128":
				case "NES-NROM-128":
				case "NAMCOT-3301":
					BoardInfo.PRG_Size = 16;
					BoardInfo.CHR_Size = 8;
					break;

				default:
					return false;
			}

			prg_byte_mask = (BoardInfo.PRG_Size << 10) - 1;
			SetMirrorType(cart.pad_h, cart.pad_v);
			
			//validate
			Assert(cart.prg_size == BoardInfo.PRG_Size);
			Assert(cart.chr_size == BoardInfo.CHR_Size);

			return true;
		}

		public override byte ReadPRG(int addr)
		{
			addr &= prg_byte_mask;
			return ROM[addr];
		}
	}
}