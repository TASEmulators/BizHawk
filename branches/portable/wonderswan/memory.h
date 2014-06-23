#ifndef __WSWAN_MEMORY_H
#define __WSWAN_MEMORY_H

#include "system.h"

namespace MDFN_IEN_WSWAN
{
class Memory
{
public:
	~Memory();

	uint8 Read20(uint32);
	void Write20(uint32 address,uint8 data);

	void Init(const SyncSettings &settings);

	void CheckSoundDMA();
	void Reset();
	void writeport(uint32 IOPort, uint8 V);
	uint8 readport(uint32 number);
	uint32 GetRegister(const unsigned int id, char *special, const uint32 special_len);
	void SetRegister(const unsigned int id, uint32 value);

public:
	uint8 wsRAM[65536];
	uint8 *wsCartROM;
	uint32 rom_size;
	uint32 sram_size;
	uint8 *wsSRAM; // = NULL;

	uint16 WSButtonStatus; // bitfield of buttons, indeed
	bool Lagged;

private:


	uint8 ButtonWhich, ButtonReadLatch;

	uint32 DMASource, DMADest;
	uint16 DMALength;
	uint8 DMAControl;

	uint32 SoundDMASource;
	uint16 SoundDMALength;
	uint8 SoundDMAControl;

	uint8 BankSelector[4];

	uint8 CommControl, CommData;

	bool language;

public:
	void (*ButtonHook)();

public:
	System *sys;
	template<bool isReader>void SyncState(NewState *ns);
private:
	void CheckDMA();

};

enum
{
 MEMORY_GSREG_ROMBBSLCT = 0,
 MEMORY_GSREG_BNK1SLCT,
 MEMORY_GSREG_BNK2SLCT,
 MEMORY_GSREG_BNK3SLCT,
};

}

#endif
