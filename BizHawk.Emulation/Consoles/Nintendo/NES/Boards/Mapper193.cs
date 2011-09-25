using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper193 : NES.NESBoardBase 
	{
		int prg, chr0, chr1, chr2;
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "Mapper193":
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
			ser.Sync("prg", ref prg);
			ser.Sync("chr0", ref chr0);
			ser.Sync("chr1", ref chr1);
			ser.Sync("chr2", ref chr2);
		}

		public override void WritePPU(int addr, byte value)
		{
			switch (addr)
			{
				case 0x6000:
					chr0 = value;
					break;
				case 0x6001:
					chr1 = value;
					break;
				case 0x6002:
					chr2 = value;
					break;
				case 0x6003:
					prg = value;
					break;
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x1000)
				return VRAM[addr + (chr0 * 0x1000)];
			else if (addr < 0x1800)
				return VRAM[addr + (chr1 * 0x0800)];
			else if (addr < 0x2000)
				return VRAM[addr + (chr2 * 0x0800)];
			else
				return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0xA000)
				return ROM[addr + (prg * 0x2000)];
			else if (addr < 0xC000)
				return 0xFD;
			else if (addr < 0xE000)
				return 0xFE;
			else
				return 0xFF;
		}
	}
}
