//
// filedb.h: File database definition
//

#ifndef __FILEDB_H__
#define __FILEDB_H__

#include <stdint.h>

// Useful enumerations

enum FileFlags { FF_ROM=0x01, FF_ALPINE=0x02, FF_BIOS=0x04, FF_REQ_DSP=0x08, FF_REQ_BIOS=0x10, FF_NON_WORKING=0x20, FF_BAD_DUMP=0x40, FF_VERIFIED=0x80, FF_STARS_1=0x00, FF_STARS_2=0x100, FF_STARS_3=0x200, FF_STARS_4=0x300, FF_STARS_5=0x400 };

// Useful structs

struct RomIdentifier
{
	const uint32_t crc32;
	const char name[128];
//	const uint8_t compatibility;
	const uint32_t flags;
};

// So other stuff can pull this in...

extern RomIdentifier romList[];

#endif	// __FILEDB_H__
