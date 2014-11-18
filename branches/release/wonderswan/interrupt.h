#ifndef __WSWAN_INTERRUPT_H
#define __WSWAN_INTERRUPT_H

#include "system.h"

namespace MDFN_IEN_WSWAN
{
enum
{
	WSINT_SERIAL_SEND = 0,
	WSINT_KEY_PRESS,
	WSINT_RTC_ALARM,
	WSINT_SERIAL_RECV,
	WSINT_LINE_HIT,
	WSINT_VBLANK_TIMER,
	WSINT_VBLANK,
	WSINT_HBLANK_TIMER
};

class Interrupt
{
public:
	void DoInterrupt(unsigned);
	void AssertInterrupt(unsigned which, bool asserted);
	void Write(uint32 A, uint8 V);
	uint8 Read(uint32 A);
	void Check();
	void Reset();

private:
	uint8 IAsserted;
	uint8 IStatus;
	uint8 IEnable;
	uint8 IVectorBase;

	bool IOn_Cache;
	uint32 IOn_Which;
	uint32 IVector_Cache;

	static const uint8 LevelTriggeredMask = (1U << WSINT_SERIAL_RECV);

private:
	void Recalc();
public:
	System *sys;
	template<bool isReader>void SyncState(NewState *ns);
};

}

#endif
