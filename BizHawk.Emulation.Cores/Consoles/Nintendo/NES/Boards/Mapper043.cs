using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper043 : NES.NESBoardBase
	{
		int prg = 0;
		int irqcnt = 0;
		bool irqenable = false;
		bool swap;


		private static int[] lut = { 4, 3, 5, 3, 6, 3, 7, 3 };

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER043":
					break;
				default:
					return false;
			}

			Cart.wram_size = 0;
			// not sure on initial mirroring
			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void WriteEXP(int addr, byte value)
		{
			addr += 0x4000;

			switch (addr & 0xF1FF)
			{
				case 0x4022:
					prg = lut[value & 0x7];
					break;

				case 0x4120:
					swap = (value & 1) == 1;
					break;

				case 0x4122:
					irqenable = (value & 1) == 1;
					IRQSignal = false;
					irqcnt = 0;
					break;
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			addr += 0x8000;
			switch (addr & 0xF1FF)
			{
				case 0x8122:
					irqenable = (value & 1) == 1;
					IRQSignal = false;
					irqcnt = 0;
					break;
			}
		}

		public override byte ReadEXP(int addr)
		{
			if (addr > 0x1000)
			{
				return ROM[(addr - 0x1000) + 8 * 0x2000];
			}
			else return base.ReadEXP(addr);
		}

		public override byte ReadWRAM(int addr)
		{
			if (swap)
			{
				return ROM[addr];
			}
			else
			{
				return ROM[addr + 0x4000];
			}
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x2000)
			{
				return ROM[addr + 0x2000];
			}
			else if (addr < 0x4000)
			{
				return ROM[addr - 0x2000];
			}
			else if (addr < 0x6000)
			{
				return ROM[(addr - 0x4000) + prg * 0x2000];
			}
			else
			{
				if (swap)
				{
					return ROM[(addr - 0x6000) + 8 * 0x2000];
				}
				else
				{
					return ROM[(addr - 0x6000) + 9 * 0x2000];
				}
			}
		}

		public override void ClockCPU()
		{
			if (irqenable)
			{
				irqcnt++;

				if (irqcnt >= 4096)
				{
					irqenable = false;
					IRQSignal = true;
				}				
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg);
			ser.Sync("irqenable", ref irqenable);
			ser.Sync("irqcnt", ref irqcnt);
			ser.Sync("swap", ref swap);
		}
	}
}
