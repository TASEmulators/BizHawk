﻿using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_163
	internal sealed class NanJing : NesBoardBase
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
		private int prg_mask;

		// state
		private byte reg0 = 0;
		private byte reg1 = 0xff;
		private int prg = 0;
		private byte security = 0;
		private bool trigger = false;
		private bool strobe = true;


		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER163":
					AssertChr(0); AssertVram(8); AssertWram(8);
					break;

				default:
					return false;
			}
			prg_mask = (Cart.PrgSize / 32) - 1;
			SetMirrorType(Cart.PadH, Cart.PadV);
			return true;
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[(prg << 15) | addr];
		}

		/*
		public override void WritePRG(int addr, byte value)
		{
		}*/

		public override byte ReadExp(int addr)
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

		public override void WriteExp(int addr, byte value)
		{
			if (addr == 0x1101)
			{
				if (strobe && value is 0) trigger = !trigger;
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

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				if ((reg1 & 0x80) != 0)
				{
					if (NES.ppu.ppur.status.sl <= 128)
						return Vram[addr & 0xfff];
					else
						return Vram[(addr & 0xfff) + 0x1000];
				}
				else
					return Vram[addr];
			}
			else
				return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{

				if ((reg1 & 0x80) != 0 && NES.ppu.ppur.status.rendering && NES.ppu.PPUON)
				{
					if (NES.ppu.ppur.status.sl <= 128)
						Vram[addr & 0xfff] = value;
					else
						Vram[(addr & 0xfff) + 0x1000] = value;
				}
				else
					Vram[addr] = value;
			}
			else
				base.WritePpu(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(reg0), ref reg0);
			ser.Sync(nameof(reg1), ref reg1);
			ser.Sync(nameof(prg), ref prg);
			ser.Sync(nameof(security), ref security);
			ser.Sync(nameof(trigger), ref trigger);
			ser.Sync(nameof(strobe), ref strobe);
		}
	}
}
