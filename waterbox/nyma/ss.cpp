#include <src/types.h>
#include <src/mednafen.h>
#include <src/Time.h>
#include <src/ss/ss.h>
#include "nyma.h"
#include <emulibc.h>
#include <waterboxcore.h>
#include <src/ss/ak93c45.h>
#include <src/ss/cart.h>
#include <src/ss/smpc.h>
namespace MDFN_IEN_SS
{
	#include <src/ss/scsp.h>
}

using namespace MDFN_IEN_SS;

extern Mednafen::MDFNGI EmulatedSS;

void SetupMDFNGameInfo()
{
	Mednafen::MDFNGameInfo = &EmulatedSS;
}

namespace MDFN_IEN_SS
{
	extern SS_SCSP SCSP;
	extern uint16 BIOSROM[524288 / sizeof(uint16)];
	extern uint16 WorkRAML[1024 * 1024 / sizeof(uint16)];
	extern uint16 WorkRAMH[1024 * 1024 / sizeof(uint16)];	// Effectively 32-bit in reality, but 16-bit here because of CPU interpreter design(regarding fastmap).
	extern uint8 BackupRAM[32768];
	namespace VDP1
	{
		extern uint16 VRAM[0x40000];
		extern uint16 FB[2][0x20000];
	}
	namespace VDP2
	{
		extern uint16 VRAM[262144];
		extern uint16 CRAM[2048];
	}
	extern uint8 ExtBackupRAM[0x80000];
	extern uint16* CS1RAM;
	extern uint16 ExtRAM[0x200000];
	extern uint16 ROM[0x100000];
	extern uint16 STV_ROM[0x3000000 / sizeof(uint16)];
	extern AK93C45 eep;

	extern int ActiveCartType;

	extern struct SMPC_RTC
	{
		uint64 ClockAccum;
		bool Valid;
		union
		{
			uint8 raw[7];
			struct
			{
				uint8 year[2];
				uint8 wday_mon;
				uint8 mday;
				uint8 hour;
				uint8 minute;
				uint8 second;
			};
		};
	} RTC;

	extern uint8 SaveMem[4];
}

ECL_EXPORT uint32_t GetSaveRamLength()
{
	if (ActiveCartType == CART_BACKUP_MEM)
		return sizeof(BackupRAM) + sizeof(ExtBackupRAM) + sizeof(uint8_t) + sizeof(RTC.raw) + sizeof(SaveMem);

	if (ActiveCartType == CART_STV)
		return sizeof(BackupRAM) + sizeof(eep.mem) + sizeof(uint8_t) + sizeof(RTC.raw) + sizeof(SaveMem);

	return sizeof(BackupRAM) + sizeof(uint8_t) + sizeof(RTC.raw) + sizeof(SaveMem);
}

ECL_EXPORT void GetSaveRam(uint8_t* data)
{
	memcpy(data, BackupRAM, sizeof(BackupRAM));
	data += sizeof(BackupRAM);

	if (ActiveCartType == CART_BACKUP_MEM)
	{
		memcpy(data, ExtBackupRAM, sizeof(ExtBackupRAM));
		data += sizeof(ExtBackupRAM);
	}

	if (ActiveCartType == CART_STV)
	{
		memcpy(data, eep.mem, sizeof(eep.mem));
		data += sizeof(eep.mem);
	}

	*data = RTC.Valid;
	data += sizeof(uint8_t);

	memcpy(data, RTC.raw, sizeof(RTC.raw));
	data += sizeof(RTC.raw);

	memcpy(data, SaveMem, sizeof(SaveMem));
}

ECL_EXPORT void PutSaveRam(uint8_t* data, uint32_t length)
{
	if (length >= sizeof(BackupRAM))
	{
		memcpy(BackupRAM, data, sizeof(BackupRAM));
		data += sizeof(BackupRAM);
		length -= sizeof(BackupRAM);
	}

	if (ActiveCartType == CART_BACKUP_MEM)
	{
		if (length >= sizeof(ExtBackupRAM))
		{
			memcpy(ExtBackupRAM, data, sizeof(ExtBackupRAM));
			data += sizeof(ExtBackupRAM);
			length -= sizeof(ExtBackupRAM);
		}
	}

	if (ActiveCartType == CART_STV)
	{
		if (length >= sizeof(eep.mem))
		{
			memcpy(eep.mem, data, sizeof(eep.mem));
			data += sizeof(eep.mem);
			length -= sizeof(eep.mem);
		}
	}

	if (length >= (sizeof(uint8_t) + sizeof(RTC.raw) + sizeof(SaveMem)))
	{
		RTC.Valid = *data != 0;
		data += sizeof(uint8_t);
		memcpy(RTC.raw, data, sizeof(RTC.raw));
		data += sizeof(RTC.raw);
		memcpy(SaveMem, data, sizeof(SaveMem));
	}

	if (MDFN_GetSettingB("ss.smpc.autortc"))
	{
		struct tm ht = Time::UTCTime();
		SMPC_SetRTC(&ht, MDFN_GetSettingUI("ss.smpc.autortc.lang"));
	}
}

ECL_EXPORT void GetMemoryAreas(MemoryArea* m)
{
	int i = 0;
	#define AddMemoryDomain(name,data,size,flags) do\
	{\
		m[i].Data = data;\
		m[i].Name = name;\
		m[i].Size = size;\
		m[i].Flags = flags;\
		i++;\
	}\
	while (0)
	AddMemoryDomain("Sound Ram", SCSP.GetRAMPtr(), 0x100000, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	AddMemoryDomain("Backup Ram", BackupRAM, sizeof(BackupRAM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	AddMemoryDomain("Boot Rom", BIOSROM, 524288, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	AddMemoryDomain("Work Ram Low", WorkRAML, sizeof(WorkRAML), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	AddMemoryDomain("Work Ram High", WorkRAMH, sizeof(WorkRAMH), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_PRIMARY);
	AddMemoryDomain("VDP1 Ram", VDP1::VRAM, sizeof(VDP1::VRAM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	AddMemoryDomain("VDP1 Framebuffer", VDP1::FB, sizeof(VDP1::FB), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	AddMemoryDomain("VDP2 Ram", VDP2::VRAM, sizeof(VDP2::VRAM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	AddMemoryDomain("VDP2 CRam", VDP2::CRAM, sizeof(VDP2::CRAM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	if (ActiveCartType == CART_BACKUP_MEM)
		AddMemoryDomain("Backup Cart", ExtBackupRAM, sizeof(ExtBackupRAM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	if (ActiveCartType == CART_CS1RAM_16M)
		AddMemoryDomain("CS1 Cart", CS1RAM, 0x1000000, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	if (ActiveCartType == CART_EXTRAM_4M)
		AddMemoryDomain("Ram Cart", ExtRAM, sizeof(ExtRAM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	if (ActiveCartType == CART_EXTRAM_1M)
		AddMemoryDomain("Ram Cart", ExtRAM, sizeof(ExtRAM) / 4, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	if (ActiveCartType == CART_KOF95 || ActiveCartType == CART_ULTRAMAN)
		AddMemoryDomain("Rom Cart", ROM, sizeof(ROM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	if (ActiveCartType == CART_STV)
	{
		AddMemoryDomain("Rom Cart", STV_ROM, sizeof(STV_ROM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
		AddMemoryDomain("STV EEPROM", eep.mem, sizeof(eep.mem), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	}
	AddMemoryDomain("SMPC RTC", RTC.raw, sizeof(RTC.raw), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1);
	AddMemoryDomain("SMPC SaveMem", SaveMem, sizeof(SaveMem), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1);
}
