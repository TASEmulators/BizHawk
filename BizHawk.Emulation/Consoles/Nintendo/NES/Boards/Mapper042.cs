using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	// pirate FDS conversion
	// this is probably two different boards, but they seem to work well enough the same
	public class Mapper042 : NES.NESBoardBase
	{
		int prg = 0;
		int chr = 0;
		int irqcnt = 0;
		bool irqenable = false;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER042":
					break;
				default:
					return false;
			}
			AssertPrg(128);

			if (Cart.vram_size == 0)
				AssertChr(128);
			else
			{
				AssertVram(8);
				AssertChr(0);
			}

			AssertWram(0);
			// not sure on initial mirroring
			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void WritePRG(int addr, byte value)
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
						IRQSignal = false;
					}
					else
						irqenable = true;
					break;
			}
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr | 0x18000];
		}
		public override byte ReadWRAM(int addr)
		{
			return ROM[addr | prg << 13];
		}

		public override void ClockCPU()
		{
			if (irqenable)
			{
				irqcnt++;

				if (irqcnt >= 32768)
					irqcnt -= 32768;

				IRQSignal = irqcnt >= 24576;
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg);
			ser.Sync("chr", ref chr);
			ser.Sync("irqenable", ref irqenable);
			ser.Sync("irqcnt", ref irqcnt);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				if (VRAM != null)
					return VRAM[addr];
				else
					return VROM[addr | chr << 13];
			}
			else
				return base.ReadPPU(addr);
		}
	}
}
