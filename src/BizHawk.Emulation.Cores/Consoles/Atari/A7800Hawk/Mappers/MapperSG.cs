using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// Default Bank Switching Mapper used by most games
	public sealed class MapperSG : MapperBase
	{
		private byte _bank;
		private byte[] RAM = new byte[0x4000];

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
				else if (Core.is_pokey_450 && (addr >= 0x450) && (addr < 0x480))
				{
					if (addr < 0x460)
					{
						return Core.pokey.ReadReg(addr & 0xF);
					}
					return 0;
				}

				return Core.RAM[0x800 + addr & 0x7FF];
			}

			// cartridge and other OPSYS
			if (addr >= (0x10000 - Core._bios.Length) && !Core.A7800_control_register.Bit(2))
			{
				return Core._bios[addr - (0x10000 - Core._bios.Length)];
			}

			if (addr >= 0xC000)
			{
				// bank 7 is fixed
				return Core._rom[Core._rom.Length - (0x10000 - addr)];
			}

			if (addr >= 0x8000)
			{
				// return whatever bank is there
				int tempAddr = addr - 0x8000;
				return Core._rom[tempAddr + _bank * 0x4000];
			}

			if (Core.cart_RAM == 0 && !Core.is_pokey)
			{
				// return bank 6
				int tempAddr = addr - 0x4000;

				if (!Core.small_flag)
				{
					return Core._rom[tempAddr + 6 * 0x4000];
				}

				if (Core.PAL_Kara)
				{
					return Core._rom[tempAddr + 2 * 0x4000];
				}

				// Should never get here, but in case we do just return FF
				return 0xFF;
			}

			if (Core.cart_RAM > 0)
			{
				// return RAM
				if (Core.cart_RAM == 8 && addr >= 0x6000)
				{
					return RAM[addr - 0x6000];
				}

				if (Core.cart_RAM == 16)
				{
					return RAM[addr - 0x4000];
				}

				// this would correspond to reading from 0x4000-0x5FFF with only 8k of RAM
				// Let's just return FF for now
				return 0xFF;
			}

			if (Core.is_pokey)
			{
				return Core.pokey.ReadReg(addr & 0xF);
			}

			return 0xFF;
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
				else if (Core.is_pokey_450 && (addr >= 0x450) && (addr < 0x480))
				{
					if (addr < 0x460)
					{
						Core.pokey.WriteReg(addr & 0xF, value);
					}					
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
					_bank = (byte)(value & (Core.small_flag ? 0x3 : mask));
				}
				else if (Core.is_pokey)
				{
					Core.pokey.WriteReg(addr & 0xF, value);
				}
				else if (Core.cart_RAM > 0)
				{
					if (Core.cart_RAM==8 && addr >= 0x6000)
					{
						RAM[addr - 0x6000] = value;
					}
					else if (Core.cart_RAM==16) 
					{
						RAM[addr - 0x4000] = value;
					}
				}
			}
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("Bank", ref _bank);
			ser.Sync(nameof(RAM), ref RAM, false);
		}
	}
}
