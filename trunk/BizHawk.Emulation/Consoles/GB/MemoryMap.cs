using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.GB
{
	public partial class GB
	{
		// Flag indicating ig BIOS is mapped in.
		private bool inBIOS = true;

		// Memory regions (initialised at reset time)
		private byte[] BIOS;
		private byte[] ROM;
		private byte[] WRAM;
		private byte[] ERAM;
		private byte[] ZRAM;

		public byte ReadMemory(ushort addr)
		{
			switch (addr & 0xF000)
			{
				/*
				[0000-3FFF] Cartridge ROM, bank 0: The first 16,384 bytes of
				the cartridge program are always available at this point in the
				memory map. Special circumstances apply.
				*/
				case 0x0000:
					/*
					[0000-0100] BIOS: When the CPU starts up, PC starts at
					0000h, which is the start of the 256-byte GameBoy BIOS
					code. Once the BIOS has run, it is removed from the memory
					map, and this area of the cartridge rom becomes
					addressable.
					*/
					if (inBIOS)
					{
						if (addr < 0x0100)
							return BIOS[addr];
						else if (addr == 0x0100)
							inBIOS = false;
					}
					/*
					[0100-014F] Cartridge header: This section of the cartridge
					contains data about its name and manufacturer, and must be
					written in a specific format.
					*/
					return ROM[addr];
				case 0x1000:
				case 0x2000:
				case 0x3000:
					return ROM[addr];
				/*
				[4000-7FFF] Cartridge ROM, other banks: Any subsequent 16k
				"banks" of the cartridge program can be made available to the
				CPU here, one by one; a chip on the cartridge is generally used
				to switch between banks, and make a particular area accessible.
				The smallest programs are 32k, which means that no
				bank-selection chip is required.
				*/
				case 0x4000:
				case 0x5000:
				case 0x6000:
				case 0x7000:
					return ROM[addr];
				/*
				[8000-9FFF] Graphics RAM: Data required for the backgrounds and
				sprites used by the graphics subsystem is held here, and can be
				changed by the cartridge program.
				*/
				case 0x8000:
				case 0x9000:
					return VRAM[addr & 0x1FFF];
				/*
				[A000-BFFF] Cartridge (External) RAM: There is a small amount
				of writeable memory available in the GameBoy; if a game is
				produced that requires more RAM than is available in the
				hardware, additional 8k chunks of RAM can be made addressable
				here.
				*/
				case 0xA000:
				case 0xB000:
					return ERAM[addr & 0x1FFF];
				/*
				[C000-DFFF] Working RAM: The GameBoy's internal 8k of RAM,
				which can be read from or written to by the CPU.
				*/
				case 0xC000:
				case 0xD000:
					return WRAM[addr & 0x1FFF];
				/*
				[E000-FDFF] Working RAM (shadow): Due to the wiring of the
				GameBoy hardware, an exact copy of the working RAM is available
				8k higher in the memory map. This copy is available up until
				the last 512 bytes of the map, where other areas are brought
				into access.
				*/
				case 0xE000:
					return WRAM[addr & 0x1FFF];
				case 0xF000:
					switch (addr & 0x0F00)
					{
						case 0x000:
						case 0x100:
						case 0x200:
						case 0x300:
						case 0x400:
						case 0x500:
						case 0x600:
						case 0x700:
						case 0x800:
						case 0x900:
						case 0xA00:
						case 0xB00:
						case 0xC00:
						case 0xD00:
							return WRAM[addr & 0x1FFF];
						/*
						[FE00-FE9F] Graphics: sprite information: Data about
						the sprites rendered by the graphics chip are held
						here, including the sprites' positions and attributes.
						*/
						case 0xE00:
							// OAM is 160 bytes, remaining bytes read as 0.
							if (addr < 0xFEA0)
								return OAM[addr & 0xFF];
							else
								return 0;
						case 0xF00:
							/*
							[FF00-FF7F] Memory-mapped I/O: Each of the
							GameBoy's subsystems (graphics, sound, etc.) has
							control values, to allow programs to create effects
							and use the hardware. These values are available to
							the CPU directly on the address bus, in this area.
							*/
							if (addr < 0xFF80)
								throw new NotImplementedException();
							/*
							[FF80-FFFF] Zero-page RAM: A high-speed area of 128
							bytes of RAM is available at the top of memory.
							Oddly, though this is "page" 255 of the memory, it
							is referred to as page zero, since
							most of the interaction between the program and
							the GameBoy hardware occurs through use of this
							page of memory.
							*/
							else
								return ZRAM[addr & 0x7F];
						default:
							throw new ArgumentException();
					}
				default:
					throw new ArgumentException();
			}
		}

		public void WriteMemory(ushort addr, byte val)
		{
			// Writing is the same as reading with the operations reversed.
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (inBIOS)
					{
						if (addr < 0x0100)
						{
							BIOS[addr] = val;
							break;
						}
						else if (addr == 0x0100)
							inBIOS = false;
					}
					ROM[addr] = val;
					break;
				case 0x1000:
				case 0x2000:
				case 0x3000:
					ROM[addr] = val;
					break;
				case 0x4000:
				case 0x5000:
				case 0x6000:
				case 0x7000:
					ROM[addr] = val;
					break;
				case 0x8000:
				case 0x9000:
					VRAM[addr & 0x1FFF] = val;
					break;
				case 0xA000:
				case 0xB000:
					ERAM[addr & 0x1FFF] = val;
					break;
				case 0xC000:
				case 0xD000:
					WRAM[addr & 0x1FFF] = val;
					break;
				case 0xE000:
					WRAM[addr & 0x1FFF] = val;
					break;
				case 0xF000:
					switch (addr & 0x0F00)
					{
						case 0x000:
						case 0x100:
						case 0x200:
						case 0x300:
						case 0x400:
						case 0x500:
						case 0x600:
						case 0x700:
						case 0x800:
						case 0x900:
						case 0xA00:
						case 0xB00:
						case 0xC00:
						case 0xD00:
							WRAM[addr & 0x1FFF] = val;
							break;
						case 0xE00:
							if (addr < 0xFEA0)
								OAM[addr & 0xFF] = val;
							break;
						case 0xF00:
							if (addr < 0xFF80)
								throw new NotImplementedException();
							else
							{
								ZRAM[addr & 0x7F] = val;
								break;
							}
					}
					break;
			}
		}
	}
}
