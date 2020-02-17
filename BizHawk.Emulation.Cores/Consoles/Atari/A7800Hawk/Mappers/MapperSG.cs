using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// Default Bank Switching Mapper used by most games
	public class MapperSG : MapperBase
	{
		public byte bank = 0;
		public byte[] RAM = new byte[0x4000];

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
					if (addr >= 0xC000)
					{
						// bank 7 is fixed
						return Core._rom[Core._rom.Length - (0x10000 - addr)];
					}
					else if (addr >= 0x8000)
					{
						// return whatever bank is there
						int temp_addr = addr - 0x8000;
						return Core._rom[temp_addr + bank * 0x4000];
					}
					else
					{
						if (Core.cart_RAM == 0 && !Core.is_pokey)
						{
							// return bank 6
							int temp_addr = addr - 0x4000;

							if (!Core.small_flag)
							{
								return Core._rom[temp_addr + 6 * 0x4000];
							}
							else
							{
								if (Core.PAL_Kara)
								{
									return Core._rom[temp_addr + 2 * 0x4000];
								}
								else
								{
									// Should never get here, but in case we do just return FF
									return 0xFF;
								}
							}
						}
						else if (Core.cart_RAM > 0)
						{
							// return RAM
							if (Core.cart_RAM == 8 && addr >= 0x6000)
							{
								return RAM[addr - 0x6000];
							}
							else if (Core.cart_RAM == 16)
							{
								return RAM[addr - 0x4000];
							}
							else
							{
								// this would coorespond to reading from 0x4000-0x5FFF with only 8k of RAM
								// Let's just return FF for now
								return 0xFF;
							}
						}
						else if (Core.is_pokey)
						{
							return Core.pokey.ReadReg(addr & 0xF);
						}
						else
						{
							return 0xFF;
						}
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
					bank = (byte)(value & (Core.small_flag ? 0x3 : mask));
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

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMemory(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("Bank", ref bank);
			ser.Sync(nameof(RAM), ref RAM, false);
		}
	}
}
