#include "system.h"
//#include <trio/trio.h>

namespace MDFN_IEN_WSWAN
{
	void Interrupt::Recalc()
	{
		IStatus |= (IAsserted & LevelTriggeredMask) & IEnable;

		IOn_Cache = false;
		IOn_Which = 0;
		IVector_Cache = 0;

		for(int i = 0; i < 8; i++)
		{
			if(IStatus & IEnable & (1U << i))
			{
				IOn_Cache = TRUE;
				IOn_Which = i;
				IVector_Cache = (IVectorBase + i) * 4;
				break;
			}
		}
	}

	void Interrupt::AssertInterrupt(unsigned which, bool asserted)
	{
		const uint8 prev_IAsserted = IAsserted;

		IAsserted &= ~(1U << which);
		IAsserted |= (unsigned)asserted << which;

		IStatus |= ((prev_IAsserted ^ IAsserted) & IAsserted) & IEnable;

		Recalc();
	}

	void Interrupt::DoInterrupt(unsigned which)
	{
		IStatus |= (1U << which) & IEnable;
		Recalc();
	}

	void Interrupt::Write(uint32 A, uint8 V)
	{
		//printf("Write: %04x %02x\n", A, V);
		switch(A)
		{
		case 0xB0: IVectorBase = V; Recalc(); break;
		case 0xB2: IEnable = V; IStatus &= IEnable; Recalc(); break;
		case 0xB6: IStatus &= ~V; Recalc(); break;
		}
	}

	uint8 Interrupt::Read(uint32 A)
	{
		//printf("Read: %04x\n", A);
		switch(A)
		{
		case 0xB0: return(IVectorBase);
		case 0xB2: return(IEnable);
		case 0xB6: return(1 << IOn_Which); //return(IStatus);
		}
		return(0);
	}

	void Interrupt::Check()
	{
		if(IOn_Cache)
		{
			sys->cpu.interrupt(IVector_Cache, FALSE);
		}
	}

	void Interrupt::Reset()
	{
		IAsserted = 0x00;
		IEnable = 0x00;
		IStatus = 0x00;
		IVectorBase = 0x00;
		Recalc();
	}

	SYNCFUNC(Interrupt)
	{
		NSS(IAsserted);
		NSS(IStatus);
		NSS(IEnable);
		NSS(IVectorBase);

		NSS(IOn_Cache);
		NSS(IOn_Which);
		NSS(IVector_Cache);
	}
}
