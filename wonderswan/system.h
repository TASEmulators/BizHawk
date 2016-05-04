#ifndef SYSTEM_H
#define SYSTEM_H

namespace MDFN_IEN_WSWAN
{
class System;
struct SyncSettings;
struct Settings;
}

#include "wswan.h"

#include "newstate.h"

#include "gfx.h"
#include "memory.h"
#include "eeprom.h"
#include "rtc.h"
#include "sound.h"
#include "v30mz.h"
#include "interrupt.h"

#include <cstddef>

namespace MDFN_IEN_WSWAN
{
class System
{
public:
	System();
	~System();

	static void* operator new(std::size_t size);

	void Reset();
	bool Advance(uint32 buttons, bool novideo, uint32 *surface, int16 *soundbuff, int &soundbuffsize);
	bool Load(const uint8 *data, int length, const SyncSettings &s);
	void PutSettings(const Settings &s);

	int SaveRamSize() const;
	bool SaveRamLoad(const uint8 *data, int size);
	bool SaveRamSave(uint8 *dest, int maxsize) const;

	uint32 GetNECReg(int which) const;

	bool GetMemoryArea(int index, const char *&name, int &size, uint8 *&data);

public:
	GFX gfx;
	Memory memory;
	EEPROM eeprom;
	RTC rtc;
	Sound sound;
	V30MZ cpu;
	Interrupt interrupt;
	
	bool rotate; // rotate screen and controls left 90
	uint32 oldbuttons;

	template<bool isReader>void SyncState(NewState *ns);
};

struct SyncSettings
{
	uint64 initialtime; // when userealtime is false, the initial time in unix format
	uint32 byear; // birth year, 0000-9999
	uint32 bmonth; // birth month, 1-12
	uint32 bday; // birth day, 1-31
	uint32 color; // true if wonderswan is in color mode
	uint32 userealtime; // true to use the system's actual clock; false to use an emulation pegged clock
	uint32 language; // 0 = J, 1 = E; only affects "Digimon Tamers - Battle Spirit"
	uint32 sex; // sex, 1 = male, 2 = female
	uint32 blood; // 1 = a, 2 = b, 3 = o, 4 = ab
	char name[17]; // up to 16 chars long, most chars don't work (conversion from ascii is internal)
};

struct Settings
{
	uint32 LayerMask; // 1 = enable bg, 2 = enable fg, 4 = enable sprites
	uint32 BWPalette[16]; // map 16 b&w shades to output colors
	uint32 ColorPalette[4096]; // map 4096 color shades to output colors
};

namespace Debug
{
int puts ( const char * str );
int printf ( const char * format, ... );
}

}

#endif
