using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// Mapper only used by F-18 Hornet
	public class MapperF18 : MapperBase
	{
		private byte _bank;

		public override byte ReadMemory(ushort addr)
		{
			if (addr >= 0x1000 && addr < 0x1800)
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

			// cartridge and other OPSYS
			if (addr >= (0x10000 - Core._bios.Length) && !Core.A7800_control_register.Bit(2))
			{
				return Core._bios[addr - (0x10000 - Core._bios.Length)];
			}

			if (addr >= 0x8000)
			{
				// top 32k is fixed
				return Core._rom[Core._rom.Length - (0x10000 - addr)];
			}

			// return whichever extra 16k bank is swapped in
			int tempAddr = addr - 0x4000;

			return Core._rom[tempAddr + _bank * 0x4000];
		}

		public override byte PeekMemory(ushort addr) => ReadMemory(addr);

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
				if (addr == 0x8000) // might be other addresses, but only 0x8000 is used
				{
					_bank = (byte)(value & 3);
					_bank -= 1;
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
