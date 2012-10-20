using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Atari._2600
{
	/*
	3E (Boulderdash
	-----

	This works similar to 3F (Tigervision) above, except RAM has been added.  The range of
	addresses has been restricted, too.  Only 3E and 3F can be written to now.

	1000-17FF - this bank is selectable
	1800-1FFF - this bank is the last 2K of the ROM

	To select a particular 2K ROM bank, its number is poked into address 3F.  Because there's
	8 bits, there's enough for 256 2K banks, or a maximum of 512K of ROM.

	Writing to 3E, however, is what's new.  Writing here selects a 1K RAM bank into 
	1000-17FF.  The example (Boulderdash) uses 16K of RAM, however there's theoretically
	enough space for 256K of RAM.  When RAM is selected, 1000-13FF is the read port while
	1400-17FF is the write port.
	*/
	class m3E : MapperBase 
	{
		int lowbank_2k = 0;
		int rambank_1k = 0;
		bool hasRam = false;
		ByteBuffer ram = new ByteBuffer(262144); //Up to 256k

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("lowbank_2k", ref lowbank_2k);
			ser.Sync("rambank_1k", ref rambank_1k);
			ser.Sync("cart_ram", ref ram);
			ser.Sync("hasRam", ref hasRam);
		}

		public override void Dispose()
		{
			base.Dispose();
			ram.Dispose();
		}

		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}
			else if (addr < 0x17FF) //Low 2k Bank
			{
				if (hasRam)
				{
					if (addr < 0x13FF)
					{
						return ram[(addr & 0x03FF) + (rambank_1k << 10)];
					}
					else
					{
						return ram[(addr & 0x03FF) + (rambank_1k << 10)] = 0xFF; //Reading from the write port triggers an unwanted write
					}
				}
				else
				{
					int a = addr & 0x07FF; //2K
					int bank = lowbank_2k << 11;
					return core.rom[bank + a];
				}
			}
			else if (addr < 0x2000) //High bank fixed to last 2k of ROM
			{
				return core.rom[(core.rom.Length - 2048) + (addr & 0x07FF)];
			}
			return base.ReadMemory(addr);
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x1000)
			{
				if (addr == 0x003E)
				{
					hasRam = true;
					rambank_1k = value;
				}
				else if (addr == 0x003F)
				{
					hasRam = false;
					if ((value << 11) < core.rom.Length)
					{
						lowbank_2k = value;
					}
					else
					{
						lowbank_2k = value & (core.rom.Length >> 11);
					}
				}

				base.WriteMemory(addr, value);
			}
			else if (addr < 0x1400)
			{
				//Writing to the read port, for shame!
			}
			else if (addr < 0x1800) //Write port
			{
				ram[(rambank_1k << 10) + (addr & 0x3FF)] = value;
			}
		}
	}
}
