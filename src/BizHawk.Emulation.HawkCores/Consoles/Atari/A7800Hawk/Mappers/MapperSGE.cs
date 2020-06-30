using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// Super Game mapper but with extra ROM at the start of the file
	// Have to add 1 to bank number to get correct bank value
	public sealed class MapperSGE : MapperBase
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

				if (Core.is_pokey)
				{
					return Core.pokey.ReadReg(addr & 0xF);
				}

				return Core.RAM[0x800 + addr & 0x7FF];
			}

			// cartridge and other OPSYS
			if (addr >= 0x10000 - Core._bios.Length && !Core.A7800_control_register.Bit(2))
			{
				return Core._bios[addr - (0x10000 - Core._bios.Length)];
			}

			if (addr >=0xC000)
			{
				// bank 7 is fixed
				return Core._rom[Core._rom.Length - (0x10000 - addr)];
			}

			int tempAddr;
			if (addr >= 0x8000)
			{
				// return whatever bank is there
				// but remember we need to add 1 to adjust for the extra bank at the beginning
				tempAddr = addr - 0x8000;
				return Core._rom[tempAddr + (_bank + 1) * 0x4000];
			}
			/*
			if (Core.is_pokey)
			{
				return Core.pokey.ReadReg(addr & 0xF);
			}
			*/

			// return the 16k extra ROM (located at beginning of file)
			tempAddr = addr - 0x4000;
			return Core._rom[tempAddr];
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
					_bank = (byte)(value & 0x7);
				}
				else if (Core.is_pokey)
				{
					Core.pokey.WriteReg(addr & 0xF, value);
				}
			}
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("Bank", ref _bank);
		}
	}
}
