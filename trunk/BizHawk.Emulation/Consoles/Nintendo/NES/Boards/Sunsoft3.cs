using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Sunsoft3 : NES.NESBoardBase
	{
		int chr, prg;
		

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "SUNSOFT-3":
					break;
				default:
					return false;
			}
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr", ref chr);
			ser.Sync("prg", ref prg);
		}

		public override void WritePRG(int addr, byte value)
		{
			if (value.Bit(3))
				SetMirrorType(EMirrorType.OneScreenA);
			else
				SetMirrorType(EMirrorType.OneScreenB);

			chr = ((value & 0x07) + (value >> 7 * 0x07));
			prg = (value >> 4) & 7;
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x4000)
				return ROM[addr + (prg * 0x4000)];
			else
				return base.ReadPRG(addr);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[(addr & 0x1FFF) + (chr * 0x2000)];
			else
				return base.ReadPPU(addr);
		}
	}
}
