using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// pirate FDS conversion
	// this is probably two different boards, but they seem to work well enough the same
	internal sealed class Mapper042 : NesBoardBase
	{
		private int prg = 0;
		private int chr = 0;
		private int irqcnt = 0;
		private bool irqenable = false;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER042":
					break;
				default:
					return false;
			}
			AssertPrg(128);

			if (Cart.VramSize == 0)
				AssertChr(128);
			else
			{
				AssertVram(8);
				AssertChr(0);
			}

			Cart.WramSize = 0;
			// not sure on initial mirroring
			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void WritePrg(int addr, byte value)
		{
			addr &= 0x6003;
			switch (addr)
			{
				case 0x0000:
					chr = value & 15;
					break;
				case 0x6000:
					prg = value & 15;
					break;
				case 0x6001:
					if ((value & 8) != 0)
						SetMirrorType(EMirrorType.Horizontal);
					else
						SetMirrorType(EMirrorType.Vertical);
					break;
				case 0x6002:
					Console.WriteLine("{0:x4}:{1:x2}  @{2}", addr + 0x8000, value, NES.ppu.ppur.status.sl);
					if ((value & 2) == 0)
					{
						irqcnt = 0;
						irqenable = false;
						IrqSignal = false;
					}
					else
						irqenable = true;
					break;
			}
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[addr | 0x18000];
		}
		public override byte ReadWram(int addr)
		{
			return Rom[addr | prg << 13];
		}

		public override void ClockCpu()
		{
			if (irqenable)
			{
				irqcnt++;

				if (irqcnt >= 32768)
					irqcnt -= 32768;

				IrqSignal = irqcnt >= 24576;
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg), ref prg);
			ser.Sync(nameof(chr), ref chr);
			ser.Sync(nameof(irqenable), ref irqenable);
			ser.Sync(nameof(irqcnt), ref irqcnt);
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				if (Vram != null)
					return Vram[addr];
				else
					return Vrom[addr | chr << 13];
			}
			else
				return base.ReadPpu(addr);
		}
	}
}
