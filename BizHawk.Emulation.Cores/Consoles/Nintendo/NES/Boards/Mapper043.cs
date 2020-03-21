using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper043 : NesBoardBase
	{
		int prg = 0;
		int irqcnt = 0;
		bool irqenable = false;
		bool swap;


		private static int[] lut = { 4, 3, 5, 3, 6, 3, 7, 3 };

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER043":
					break;
				default:
					return false;
			}

			Cart.WramSize = 0;
			// not sure on initial mirroring
			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void WriteExp(int addr, byte value)
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
					IrqSignal = false;
					irqcnt = 0;
					break;
			}
		}

		public override void WritePrg(int addr, byte value)
		{
			addr += 0x8000;
			switch (addr & 0xF1FF)
			{
				case 0x8122:
					irqenable = (value & 1) == 1;
					IrqSignal = false;
					irqcnt = 0;
					break;
			}
		}

		public override byte ReadExp(int addr)
		{
			if (addr > 0x1000)
			{
				return Rom[(addr - 0x1000) + 8 * 0x2000];
			}
			else return base.ReadExp(addr);
		}

		public override byte ReadWram(int addr)
		{
			if (swap)
			{
				return Rom[addr];
			}
			else
			{
				return Rom[addr + 0x4000];
			}
		}

		public override byte ReadPrg(int addr)
		{
			if (addr < 0x2000)
			{
				return Rom[addr + 0x2000];
			}
			else if (addr < 0x4000)
			{
				return Rom[addr - 0x2000];
			}
			else if (addr < 0x6000)
			{
				return Rom[(addr - 0x4000) + prg * 0x2000];
			}
			else
			{
				if (swap)
				{
					return Rom[(addr - 0x6000) + 8 * 0x2000];
				}
				else
				{
					return Rom[(addr - 0x6000) + 9 * 0x2000];
				}
			}
		}

		public override void ClockCpu()
		{
			if (irqenable)
			{
				irqcnt++;

				if (irqcnt >= 4096)
				{
					irqenable = false;
					IrqSignal = true;
				}				
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg), ref prg);
			ser.Sync(nameof(irqenable), ref irqenable);
			ser.Sync(nameof(irqcnt), ref irqcnt);
			ser.Sync(nameof(swap), ref swap);
		}
	}
}
