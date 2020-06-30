using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// Default mapper with no bank switching
	// Just need to keep track of high score bios stuff
	public sealed class MapperDefault : MapperBase
	{
		public override byte ReadMemory(ushort addr)
		{
			if (addr >=0x1000 && addr < 0x1800)
			{
				//could be hsbios RAM here
				if (Core._hsbios != null)
				{
					return Core._hsram[addr - 0x1000];
				}

				return 0xFF;
			}

			if (addr < 0x4000)
			{
				// could be either RAM mirror or ROM
				if (addr >= 0x3000 && Core._hsbios != null)
				{
					return Core._hsbios[addr - 0x3000];
				}

				return Core.RAM[0x800 + addr & 0x7FF];
			}

			if (addr < 0x8000 && Core.is_pokey)
			{
				return Core.pokey.ReadReg(addr & 0xF);
			}

			// cartridge and other OPSYS
			if (Core._rom.Length >= 0x10000 - addr
				&& Core.A7800_control_register.Bit(2))
			{
				return Core._rom[Core._rom.Length - (0x10000 - addr)];
			}

			if (addr >= (0x10000-Core._bios.Length) && !Core.A7800_control_register.Bit(2))
			{
				return Core._bios[addr - (0x10000 - Core._bios.Length)];
			}

			return 0x00;
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			if (addr >= 0x1000 && addr < 0x1800)
			{
				//could be hsbios RAM here
				if (Core._hsbios != null)
				{
					Core._hsram[addr - 0x1000] = value;
				}
			}
			else if (addr < 0x4000)
			{
				// could be either RAM mirror or ROM
				if (addr >= 0x3000 && Core._hsbios != null)
				{
				}
				else
				{
					Core.RAM[0x800 + addr & 0x7FF] = value;
				}
			}
			else if (addr < 0x8000 && Core.is_pokey)
			{
				Core.pokey.WriteReg(addr & 0xF, value);
			}
			else
			{ 
				// cartridge and other OPSYS
			}
		}
	}
}
