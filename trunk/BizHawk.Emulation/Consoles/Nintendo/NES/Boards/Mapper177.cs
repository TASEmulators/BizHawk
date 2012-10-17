using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	// china
	// behavior from fceux
	public class Mapper177 : NES.NESBoardBase
	{
		int prg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER177":
					break;
				default:
					return false;
			}
			AssertPrg(1024);
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void WritePRG(int addr, byte value)
		{
			prg = value & 0x1f;

			if ((value & 0x20) != 0)
				SetMirrorType(EMirrorType.Horizontal);
			else
				SetMirrorType(EMirrorType.Vertical);
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr | prg << 15];
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg);
		}
	}
}
