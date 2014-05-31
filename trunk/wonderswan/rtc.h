#ifndef __WSWAN_RTC_H
#define __WSWAN_RTC_H

#include "system.h"

namespace MDFN_IEN_WSWAN
{
class RTC
{
public:
	void Write(uint32 A, uint8 V);
	uint8 Read(uint32 A);
	void Init(uint64 initialtime, bool realtime);
	void Clock(uint32 cycles);

private:
	uint64 CurrentTime;
	bool userealtime;

	uint32 ClockCycleCounter;
	uint8 wsCA15;
	uint8 Command, Data;
public:
	System *sys;
	template<bool isReader>void SyncState(NewState *ns);

};

}

#endif
