using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	public class VsUnisys : NES.NESBoardBase
	{

		// this is a copy of UNROM until i figure out what the hell is going on

		int prg_mask;
		int vram_byte_mask;

		int prg;


		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER099":
					break;
				default:
					return false;
			}

			vram_byte_mask = (Cart.vram_size * 1024) - 1;
			prg_mask = (Cart.prg_size / 16) - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			
			return true;
		}

		public override byte ReadPRG(int addr)
		{
			int block = addr >> 14;
			int page = block == 1 ? prg_mask : prg;
			int ofs = addr & 0x3FFF;
			return ROM[(page << 14) | ofs];
		}
		public override void WritePRG(int addr, byte value)
		{
			prg = value & prg_mask;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VRAM[addr & vram_byte_mask];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				VRAM[addr & vram_byte_mask] = value;
			}
			else base.WritePPU(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg);
		}

	}
}
