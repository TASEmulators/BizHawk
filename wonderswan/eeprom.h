#ifndef __WSWAN_EEPROM_H
#define __WSWAN_EEPROM_H

#include "system.h"

namespace MDFN_IEN_WSWAN
{


class EEPROM
{
public:
	uint8 Read(uint32 A);
	void Write(uint32 A, uint8 V);
	void Reset();
	void Init(const char *Name, const uint16 BYear, const uint8 BMonth, const uint8 BDay, const uint8 Sex, const uint8 Blood);

private:
	uint8 wsEEPROM[2048];
	uint8 iEEPROM[0x400];
	uint8 iEEPROM_Command, EEPROM_Command;
	uint16 iEEPROM_Address, EEPROM_Address;
	uint32 eeprom_size;

public:
	System *sys;
};


}

#endif
