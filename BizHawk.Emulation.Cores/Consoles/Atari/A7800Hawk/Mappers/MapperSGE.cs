using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// Super Game mapper but with extra ROM at the start of the file
	// Have to add 1 to bank number to get correct bank value
	public class MapperSGE : MapperBase
	{
		public byte bank = 0;

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
			else if (addr < 0x4000)
			{
				// could be either RAM mirror or ROM
				if (addr >= 0x3000 && Core._hsbios != null)
				{
					return Core._hsbios[addr - 0x3000];
				}
				else if (Core.is_pokey)
				{
					return Core.pokey.ReadReg(addr & 0xF);
				}
				else
				{
					return Core.RAM[0x800 + addr & 0x7FF];
				}
			}
			else
			{
				// cartridge and other OPSYS
				if (addr >= (0x10000 - Core._bios.Length) && !Core.A7800_control_register.Bit(2))
				{
					return Core._bios[addr - (0x10000 - Core._bios.Length)];
				}
				else
				{
					if (addr >=0xC000)
					{
						// bank 7 is fixed
						return Core._rom[Core._rom.Length - (0x10000 - addr)];
					}
					else if (addr >= 0x8000)
					{
						// return whatever bank is there
						// but remember we need to add 1 to adjust for the extra bank at the beginning
						int temp_addr = addr - 0x8000;
						return Core._rom[temp_addr + (bank + 1) * 0x4000];
					}
					/*
					else if (Core.is_pokey)
					{
						return Core.pokey.ReadReg(addr & 0xF);
					}
					*/
					else
					{
					// return the 16k extra ROM (located at beginning of file)
					int temp_addr = addr - 0x4000;
						return Core._rom[temp_addr];
					}				
				}
			}
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
				else if (Core.is_pokey)
				{
					Core.pokey.WriteReg(addr & 0xF, value);
				}
				else
				{
					Core.RAM[0x800 + addr & 0x7FF] = value;
				}
			}
			else
			{
				// cartridge and other OPSYS
				if (addr>=0x8000)
				{
					bank = (byte)(value & 0x7);
				}
				else if (Core.is_pokey)
				{
					Core.pokey.WriteReg(addr & 0xF, value);
				}
			}
		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMemory(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("Bank", ref bank);
		}
	}
}
