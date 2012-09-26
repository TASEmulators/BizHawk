using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	public class NanJing : NES.NESBoardBase
	{
		/* 
		 * China Pirate Stuff.  Not very tested.
		 * 
		 * switches prg in 32K blocks, uses exp space for io ports
		 * 8k wram, 8k vram, supports swapping 4k blocks of vram at scanline 128
		 * 
		 * TODO: The mapper telepathically switches VRAM based on scanline.
		 * For more accurate emulation, the actual method used to count scanlines
		 * (MMC3?) must be implemented.
		 */

		// config
		int prg_mask;

		// state
		byte reg0 = 0;
		byte reg1 = 0xff;
		int prg = 15;
		byte security = 0;
		bool trigger = false;
		bool strobe = true;


		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER163":
					AssertChr(0); AssertVram(8); AssertWram(8);
					break;

				default:
					return false;
			}
			prg_mask = (Cart.prg_size / 32) - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[(prg << 15) | addr];
		}
		
		/*
		public override void WritePRG(int addr, byte value)
		{
		}*/

		public override byte ReadEXP(int addr)
		{
			if (addr >= 0x1000)
			{
				switch (addr & 0x3700)
				{
					case 0x1100:
						return security;
					case 0x1500:
						if (trigger)
							return security;
						else
							return 0;
					default:
						return 4;
				}
			}
			else
				return 0;
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr == 0x1101)
			{
				if (strobe && value == 0)
					trigger ^= true;
				strobe = (value != 0);
			}
			else if (addr == 0x1100)
			{
				if (value == 6)
					prg = 3;
			}
			else
			{
				switch (addr & 0x3300)
				{
					case 0x1000:
						reg1 = value;
						prg = (reg1 & 0xf) | (reg0 << 4) & prg_mask;
						break;
					case 0x1200:
						reg0 = value;
						prg = (reg1 & 0xf) | (reg0 << 4) & prg_mask;
						break;
					case 0x1300:
						security = value;
						break;
				}
			}
		}

		/*
		public override byte ReadWRAM(int addr)
		{
			return base.ReadWRAM(addr);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			base.WriteWRAM(addr, value);
		}*/

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				if ((reg1 & 0x80) != 0)
				{
					if (NES.ppu.ppur.status.sl <= 128)
						return VRAM[addr & 0xfff];
					else
						return VRAM[(addr & 0xfff) + 0x1000];
				}
				else
					return VRAM[addr];
			}
			else
				return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if ((reg1 & 0x80) != 0 && NES.ppu.ppur.status.rendering)
				{
					if (NES.ppu.ppur.status.sl <= 128)
						VRAM[addr & 0xfff] = value;
					else
						VRAM[(addr & 0xfff) + 0x1000] = value;
				}
				else
					VRAM[addr] = value;
			}
			else
				base.WritePPU(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg0", ref reg0);
			ser.Sync("reg1", ref reg1);
			ser.Sync("prg", ref prg);
			ser.Sync("security", ref security);
			ser.Sync("trigger", ref trigger);
			ser.Sync("strobe", ref strobe);
		}
	}
}
