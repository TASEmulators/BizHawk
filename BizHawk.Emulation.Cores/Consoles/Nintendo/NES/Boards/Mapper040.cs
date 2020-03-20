using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// smb2j (us pirate)
	internal sealed class Mapper040 : NesBoardBase
	{
		int prg = 0;
		int irqcnt = 0;
		bool irqactive = false;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER040":
					AssertChr(8); AssertPrg(64); AssertVram(0); AssertWram(0);
					break;
				default:
					return false;
			}
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override byte ReadWram(int addr)
		{
			// bank 6 fixed
			return Rom[addr + 0xc000];
		}
		public override byte ReadPrg(int addr)
		{
			if ((addr & 0x6000) == 0x4000)
				addr += prg;
			return Rom[addr + 0x8000];
		}

		public override void WritePrg(int addr, byte value)
		{
			switch (addr & 0x6000)
			{
				case 0x0000:
					irqcnt = 0;
					IrqSignal = false;
					irqactive = false;
					break;
				case 0x2000:
					irqactive = true;
					break;
				case 0x6000:
					prg = value & 7; // bank number
					// adjust for easy usage
					prg *= 0x2000;
					prg -= 0xc000;
					break;
			}
		}

		public override void ClockCpu()
		{
			if (irqactive)
			{
				irqcnt++;
				if (irqcnt >= 4096)
				{
					irqcnt = 4096;
					IrqSignal = true;
				}
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg), ref prg);
			ser.Sync(nameof(irqcnt), ref irqcnt);
			ser.Sync(nameof(irqactive), ref irqactive);
		}
	}
}
