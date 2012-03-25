using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//iNES Mapper 72
	//Example Games:
	//--------------------------
	//Pinball Quest (J)
	//Moero!! Pro Tennis
	//Moero!! Juudou Warriors

	class JALECO_JF_17 : NES.NESBoardBase
	{
		int command;
		int prg_bank_mask_16k;
		byte prg_bank_16k;
		ByteBuffer prg_banks_16k = new ByteBuffer(2);

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER072":
					break;
				case "JALECO-JF-17":
					break;
				default:
					return false;
			}
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			prg_bank_mask_16k = (Cart.prg_size / 16) - 1;
			prg_banks_16k[1] = 0xFF;
			return true;
		}

		public override void Dispose()
		{
			prg_banks_16k.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_bank_mask_16k", ref prg_bank_mask_16k);
			ser.Sync("prg_bank_16k", ref prg_bank_16k);
			ser.Sync("prg_banks_16k", ref prg_banks_16k);
			ser.Sync("command", ref command);
		}

		public override void WritePRG(int addr, byte value)
		{
			switch (command)
			{
				case 0:
					break;
				case 1:
					break;
				case 2:
					prg_bank_16k = (byte)(value & 15);
					break;
				case 3:
					break;
			}

			command = value >> 6;
		}

		public override byte ReadPRG(int addr)
		{
			int bank_16k = addr >> 14;
			int ofs = addr & ((1 << 14) - 1);
			bank_16k = prg_banks_16k[bank_16k];
			bank_16k &= prg_bank_mask_16k;
			addr = (bank_16k << 14) | ofs;
			return ROM[addr];
		}
	}
}
