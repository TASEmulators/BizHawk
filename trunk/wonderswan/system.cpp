/* Cygne
*
* Copyright notice for this file:
*  Copyright (C) 2002 Dox dox@space.pl
*
* This program is free software; you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation; either version 2 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program; if not, write to the Free Software
* Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

#include "system.h"
#include <cstring>
#include <cstdlib>
#include <cstdio>
#include <cstdarg>

#define EXPORT extern "C" __declspec(dllexport)

namespace MDFN_IEN_WSWAN
{

#include "start.inc"

	typedef struct
	{
		const uint8 id;
		const char *name;
	} DLEntry;

	static const DLEntry Developers[] =
	{
		{ 0x01, "Bandai" },
		{ 0x02, "Taito" },
		{ 0x03, "Tomy" },
		{ 0x04, "Koei" },
		{ 0x05, "Data East" },
		{ 0x06, "Asmik" }, // Asmik Ace?
		{ 0x07, "Media Entertainment" },
		{ 0x08, "Nichibutsu" },
		{ 0x0A, "Coconuts Japan" },
		{ 0x0B, "Sammy" },
		{ 0x0C, "Sunsoft" },
		{ 0x0D, "Mebius" },
		{ 0x0E, "Banpresto" },
		{ 0x10, "Jaleco" },
		{ 0x11, "Imagineer" },
		{ 0x12, "Konami" },
		{ 0x16, "Kobunsha" },
		{ 0x17, "Bottom Up" },
		{ 0x18, "Naxat" },	// Mechanic Arms?  Media Entertainment? Argh!
		{ 0x19, "Sunrise" },
		{ 0x1A, "Cyberfront" },
		{ 0x1B, "Megahouse" },
		{ 0x1D, "Interbec" },
		{ 0x1E, "NAC" },
		{ 0x1F, "Emotion" }, // Bandai Visual??
		{ 0x20, "Athena" },
		{ 0x21, "KID" },
		{ 0x24, "Omega Micott" },
		{ 0x25, "Upstar" },
		{ 0x26, "Kadokawa/Megas" },
		{ 0x27, "Cocktail Soft" },
		{ 0x28, "Squaresoft" },
		{ 0x2B, "TomCreate" },
		{ 0x2D, "Namco" },
		{ 0x2F, "Gust" },
		{ 0x36, "Capcom" },
	};

	void System::Reset()
	{
		cpu.reset();
		memory.Reset();
		gfx.Reset();
		sound.Reset();
		interrupt.Reset();
		rtc.Reset();
		eeprom.Reset();

		for(int u0=0;u0<0xc9;u0++)
		{
			if(u0 != 0xC4 && u0 != 0xC5 && u0 != 0xBA && u0 != 0xBB)
				memory.writeport(u0,startio[u0]);
		}

		cpu.set_reg(NEC_SS,0);
		cpu.set_reg(NEC_SP,0x2000);
	}


	void System::Advance(uint16 buttons, bool novideo, uint32 *surface, int16 *soundbuff, int &soundbuffsize)
	{

		memory.WSButtonStatus = buttons;
		while (!gfx.ExecuteLine(surface, novideo))
		{
		}

		soundbuffsize = sound.Flush(soundbuff, soundbuffsize);

		// cycles elapsed in the frame can be read here
		// how is this OK to reset?  it's only used by the sound code, so once the sound for the frame has
		// been collected, it's OK to zero.  indeed, it should be done as there's no rollover protection
		cpu.timestamp = 0;

	}

	// Source: http://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2
	// Rounds up to the nearest power of 2.
	static INLINE uint64 round_up_pow2(uint64 v)
	{
		v--;
		v |= v >> 1;
		v |= v >> 2;
		v |= v >> 4;
		v |= v >> 8;
		v |= v >> 16;
		v |= v >> 32;
		v++;

		v += (v == 0);

		return(v);
	}

	bool System::Load(const uint8 *data, int length, const Settings &settings)
	{
		uint32 real_rom_size;

		if(length < 65536)
		{
			Debug::puts("Rom image is too small (<64K)");
			return false;
		}

		if(!memcmp(data + length - 0x20, "WSRF", 4))
		{
			Debug::puts("WSRF files not supported");
			return false;
		}


		real_rom_size = (length + 0xFFFF) & ~0xFFFF;
		memory.rom_size = round_up_pow2(real_rom_size);

		memory.wsCartROM = (uint8 *)std::calloc(1, memory.rom_size);


		if(real_rom_size < memory.rom_size)
			memset(memory.wsCartROM, 0xFF, memory.rom_size - real_rom_size);

		memcpy(memory.wsCartROM + (memory.rom_size - real_rom_size), data, length);


		uint8 header[10];
		memcpy(header, memory.wsCartROM + memory.rom_size - 10, 10);

		{
			const char *developer_name = "???";
			for(unsigned int x = 0; x < sizeof(Developers) / sizeof(DLEntry); x++)
			{
				if(Developers[x].id == header[0])
				{
					developer_name = Developers[x].name;
					break;
				}
			}
			Debug::printf("Developer: %s (0x%02x)\n", developer_name, header[0]);
		}

		memory.sram_size = 0;
		memory.eeprom_size = 0;

		switch(header[5])
		{
		case 0x01: memory.sram_size = 8*1024; break;
		case 0x02: memory.sram_size = 32*1024; break;
		case 0x03: memory.sram_size = 16 * 65536; break;
		case 0x04: memory.sram_size = 32 * 65536; break; // Dicing Knight!

		case 0x10: memory.eeprom_size = 128; break;
		case 0x20: memory.eeprom_size = 2*1024; break;
		case 0x50: memory.eeprom_size = 1024; break;
		}

		//printf("%02x\n", header[5]);

		if(memory.eeprom_size)
			Debug::printf("EEPROM:  %d bytes\n", memory.eeprom_size);

		if(memory.sram_size)
			Debug::printf("Battery-backed RAM:  %d bytes\n", memory.sram_size);

		Debug::printf("Recorded Checksum:  0x%04x\n", header[8] | (header[9] << 8));
		{
			uint16 real_crc = 0;
			for(unsigned int i = 0; i < memory.rom_size - 2; i++)
				real_crc += memory.wsCartROM[i];
			Debug::printf("Real Checksum:      0x%04x\n", real_crc);
		}

		if((header[8] | (header[9] << 8)) == 0x8de1 && (header[0]==0x01)&&(header[2]==0x27)) /* Detective Conan */
		{
			Debug::printf("Activating Detective Conan Hack\n");
			/* WS cpu is using cache/pipeline or there's protected ROM bank where pointing CS */
			memory.wsCartROM[0xfffe8]=0xea;
			memory.wsCartROM[0xfffe9]=0x00;
			memory.wsCartROM[0xfffea]=0x00;
			memory.wsCartROM[0xfffeb]=0x00;
			memory.wsCartROM[0xfffec]=0x20;
		}


		if(header[6] & 0x1)
		{
			//MDFNGameInfo->rotated = MDFN_ROTATE90;
		}


		//MDFNMP_Init(16384, (1 << 20) / 1024);

		cpu.init();

		// TODO: control WSC setting
		// TODO: rip out skipsaveload code

		memory.Init(false, settings);

		gfx.Init();
		//MDFNGameInfo->fps = (uint32)((uint64)3072000 * 65536 * 256 / (159*256));

		sound.Init();

		gfx.MakeTiles();

		Reset();

		return true;
	}

	void *System::operator new(std::size_t size)
	{
		void *p = ::operator new(size);
		std::memset(p, 0, size);
		return p;
	}

	System::System()
		:wsc(1)
	{
		gfx.sys = this;
		memory.sys = this;
		eeprom.sys = this;
		rtc.sys = this;
		sound.sys = this;
		cpu.sys = this;
		interrupt.sys = this;
	}

	System::~System()
	{
	}

	// maybe change?
	int Debug::puts ( const char * str )
	{
		return std::puts(str);
	}
	int Debug::printf ( const char * format, ... )
	{
		va_list args;
		va_start(args, format);
		int ret = vprintf(format, args);
		va_end(args);
		return ret;
	}



	EXPORT System *bizswan_new()
	{
		return new System();
	}

	EXPORT void bizswan_delete(System *s)
	{
		delete s;
	}

	EXPORT void bizswan_reset(System *s)
	{
		s->Reset();
	}

	EXPORT void bizswan_advance(System *s, uint16 buttons, bool novideo, uint32 *surface, int16 *soundbuff, int *soundbuffsize)
	{
		s->Advance(buttons, novideo, surface, soundbuff, *soundbuffsize);
	}

	EXPORT int bizswan_load(System *s, const uint8 *data, int length, const Settings *settings)
	{
		return s->Load(data, length, *settings);
	}

}
