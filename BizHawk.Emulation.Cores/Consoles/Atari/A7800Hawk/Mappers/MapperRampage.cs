using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// Mapper only used by Rampage and Double Dragon
	public class MapperRampage : MapperBase
	{
		private byte _bank;

		public override byte ReadMemory(ushort addr)
		{
			if (addr >= 0x1000 && addr < 0x1800)
			{
				// could be hsbios RAM here
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

			// cartridge and other OPSYS
			if (addr >= (0x10000 - Core._bios.Length) && !Core.A7800_control_register.Bit(2))
			{
				return Core._bios[addr - (0x10000 - Core._bios.Length)];
			}

			/*
			$4000 -$5fff second 8kb of bank 6
			$6000 -$7fff first 8kb of bank 6
			$8000 -$9fff second 8kb of bank 7
			$e000 -$ffff first 8kb of bank 7

			$a000-$dfff Banked 
			*/
			int tempAddr;
			if (addr >= 0x4000 && addr < 0x6000)
			{
				tempAddr = addr - 0x4000;
				return Core._rom[6 * 0x4000 + 0x2000 + tempAddr];
			}

			if (addr >= 0x6000 && addr < 0x8000)
			{
				tempAddr = addr - 0x6000;
				return Core._rom[6 * 0x4000 + tempAddr];
			}

			if (addr >= 0x8000 && addr < 0xA000)
			{
				tempAddr = addr - 0x8000;
				return Core._rom[7 * 0x4000 + 0x2000 + tempAddr];
			}

			if (addr >= 0xA000 && addr < 0xE000)
			{
				tempAddr = addr - 0xA000;
				return Core._rom[_bank * 0x4000 + tempAddr];
			}

			tempAddr = addr - 0xE000;
			return Core._rom[7 * 0x4000 + tempAddr];
		}

		public override byte PeekMemory(ushort addr)
		{
			return ReadMemory(addr);
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
			else
			{
				// cartridge and other OPSYS
				if (addr >= 0xFF80 && addr < 0xFF88) // might be other addresses, but only these are used
				{
					_bank = (byte)(addr & 7);
				}
			}
		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMemory(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("Bank", ref _bank);
		}
	}
}
