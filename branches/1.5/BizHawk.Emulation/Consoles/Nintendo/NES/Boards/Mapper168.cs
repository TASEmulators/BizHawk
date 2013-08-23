namespace BizHawk.Emulation.Consoles.Nintendo
{
	// RacerMate II
	// 64KB PRGROM, 64KB CHRRAM(!), CHRRAM is battry backed (!!)
	// the "ram protect" function is not emulated.  the notes i found said that it
	// defaults to off, the control regs are write only, and cannot be reenabled.  so...

	// todo: special controller, and IRQ is possibly wrong
	public class Mapper168 : NES.NESBoardBase
	{
		int prg = 0;
		int chr = 0;
		int irqclock = 2048;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER168":
				case "UNL-RACERMATE":
					break;
				default:
					return false;
			}
			AssertPrg(64);
			Cart.chr_size = 0; //AssertChr(0); //shitty dumps
			Cart.vram_size = 64; //AssertVram(64); //shitty dumps
			AssertWram(0);
			//AssertBattery(true); // battery is handled directly
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override byte ReadPRG(int addr)
		{
			if (addr >= 0x4000)
				return ROM[addr + 0x8000];
			else
				return ROM[addr + (prg << 14)];
		}

		// the chr reg on hardware is supposedly bitscrambled and then inverted from
		// what would be expected.  since it doesn't make a difference and i don't know
		// of any clear source on what it's actually supposed to be, ignore.
		int Scramble(int chr)
		{
			return chr;
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr < 0x4000)
			{
				chr = value & 15;
				prg = value >> 6 & 3;
			}
			else if (addr == 0x7080) // ack
				IRQSignal = false;
			else if (addr == 0x7000) // start count
				irqclock = 0;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x1000)
				return VRAM[addr | Scramble(0) << 12];
			else if (addr < 0x2000)
				return VRAM[(addr & 0xfff) | Scramble(chr) << 12];
			else
				return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x1000)
				VRAM[addr | Scramble(0) << 12] = value;
			else if (addr < 0x2000)
				VRAM[(addr & 0xfff) | Scramble(chr) << 12] = value;
			else
				base.WritePPU(addr, value);
		}

		public override byte[] SaveRam { get { return VRAM; } }

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg);
			ser.Sync("chr", ref chr);
			ser.Sync("irqclock", ref irqclock);
		}

		public override void ClockCPU()
		{
			if (irqclock == 2048 - 1)
			{
				irqclock++;
				IRQSignal = true;
			}
			else if (irqclock < 2048 - 1)
			{
				irqclock++;
			}
		}
	}
}
