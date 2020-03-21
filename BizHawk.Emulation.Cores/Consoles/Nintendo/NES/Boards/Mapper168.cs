using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// RacerMate II
	// 64KB PRGROM, 64KB CHRRAM(!), CHRRAM is battry backed (!!)
	// the "ram protect" function is not emulated.  the notes i found said that it
	// defaults to off, the control regs are write only, and cannot be reenabled.  so...

	// todo: special controller, and IRQ is possibly wrong
	internal sealed class Mapper168 : NesBoardBase
	{
		int prg = 0;
		int chr = 0;
		int irqclock = 2048;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER168":
				case "UNL-RACERMATE":
					break;
				default:
					return false;
			}
			AssertPrg(64);
			Cart.ChrSize = 0; //AssertChr(0); //shitty dumps
			Cart.VramSize = 64; //AssertVram(64); //shitty dumps
			Cart.WramSize = 0; //AssertWram(0); // shitty dumps
			//AssertBattery(true); // battery is handled directly
			SetMirrorType(Cart.PadH, Cart.PadV);
			return true;
		}

		public override byte ReadPrg(int addr)
		{
			return addr >= 0x4000
				? Rom[addr + 0x8000]
				: Rom[addr + (prg << 14)];
		}

		// the chr reg on hardware is supposedly bitscrambled and then inverted from
		// what would be expected.  since it doesn't make a difference and i don't know
		// of any clear source on what it's actually supposed to be, ignore.
		int Scramble(int chr)
		{
			return chr;
		}

		public override void WritePrg(int addr, byte value)
		{
			if (addr < 0x4000)
			{
				chr = value & 15;
				prg = value >> 6 & 3;
			}
			else if (addr == 0x7080) // ack
				IrqSignal = false;
			else if (addr == 0x7000) // start count
				irqclock = 0;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x1000)
				return Vram[addr | Scramble(0) << 12];
			if (addr < 0x2000)
				return Vram[(addr & 0xfff) | Scramble(chr) << 12];
			return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x1000)
				Vram[addr | Scramble(0) << 12] = value;
			else if (addr < 0x2000)
				Vram[(addr & 0xfff) | Scramble(chr) << 12] = value;
			else
				base.WritePpu(addr, value);
		}

		public override byte[] SaveRam => Vram;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg), ref prg);
			ser.Sync(nameof(chr), ref chr);
			ser.Sync(nameof(irqclock), ref irqclock);
		}

		public override void ClockCpu()
		{
			if (irqclock == 2048 - 1)
			{
				irqclock++;
				IrqSignal = true;
			}
			else if (irqclock < 2048 - 1)
			{
				irqclock++;
			}
		}
	}
}
