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
#include <time.h>

namespace MDFN_IEN_WSWAN
{
	// TODO: replace all use of libc time functions with internal stuff for movie recording
	// (when userealtime == true, OK to use libc stuff)

	static void GMTime(uint64 ticks, tm &time)
	{
		time_t t = ticks;
		#if defined _MSC_VER
		gmtime_s(&time, &t);
		#elif defined __MINGW32__
		tm *tmp = gmtime(&t);
		time = *tmp;		
		#elif defined __GNUC__
		gmtime_r(&t, &time);
		#endif
	}

	void RTC::Write(uint32 A, uint8 V)
	{
		switch(A)
		{
		case 0xca: 
			if(V==0x15)
				wsCA15=0; 
			Command = V;
			break;
		case 0xcb: Data = V; break;
		}

	}


	uint8 RTC::Read(uint32 A)
	{
		switch(A)
		{
		case 0xca : return (Command|0x80);
		case 0xcb :
			if(Command == 0x15)
			{
				tm newtime;
				uint64 now = userealtime ? time(nullptr) : CurrentTime;
				GMTime(now, newtime);

				switch(wsCA15)
				{
				case 0: wsCA15++;return mBCD(newtime.tm_year-100);
				case 1: wsCA15++;return mBCD(newtime.tm_mon);
				case 2: wsCA15++;return mBCD(newtime.tm_mday);
				case 3: wsCA15++;return mBCD(newtime.tm_wday);
				case 4: wsCA15++;return mBCD(newtime.tm_hour);
				case 5: wsCA15++;return mBCD(newtime.tm_min);
				case 6: wsCA15=0;return mBCD(newtime.tm_sec);
				}
				return 0;
			}
			else
				return Data | 0x80;

		}
		return(0);
	}

	void RTC::Init(uint64 initialtime, bool realtime)
	{
		if (realtime)
		{
			userealtime = true;
			CurrentTime = time(nullptr);
		}
		else
		{
			userealtime = false;
			CurrentTime = initialtime;
		}

		ClockCycleCounter = 0;
		wsCA15 = 0; // is this also possibly set to 0 on reset?
	}

	void RTC::Clock(uint32 cycles)
	{
		if (!userealtime)
		{
			ClockCycleCounter += cycles;
			while(ClockCycleCounter >= 3072000)
			{
				ClockCycleCounter -= 3072000;
				CurrentTime++;
			}
		}
	}

	SYNCFUNC(RTC)
	{
		NSS(CurrentTime);
		NSS(userealtime);

		NSS(ClockCycleCounter);
		NSS(wsCA15);
		NSS(Command);
		NSS(Data);
	}
}
