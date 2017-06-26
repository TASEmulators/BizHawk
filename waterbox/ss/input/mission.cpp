/******************************************************************************/
/* Mednafen Sega Saturn Emulation Module                                      */
/******************************************************************************/
/* mission.cpp:
**  Copyright (C) 2017 Mednafen Team
**
** This program is free software; you can redistribute it and/or
** modify it under the terms of the GNU General Public License
** as published by the Free Software Foundation; either version 2
** of the License, or (at your option) any later version.
**
** This program is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
** GNU General Public License for more details.
**
** You should have received a copy of the GNU General Public License
** along with this program; if not, write to the Free Software Foundation, Inc.,
** 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

/*
 Real mission stick has bugs and quirks that aren't emulated here(like apparently latching/updating the physical input state at the end of the
 read sequence instead of near the beginning like other controllers do, resulting in increased latency).
*/


#include "common.h"
#include "mission.h"

namespace MDFN_IEN_SS
{

IODevice_Mission::IODevice_Mission(const bool dual_) : dbuttons(0), afeswitches(0), afspeed(0), dual(dual_)
{

}

IODevice_Mission::~IODevice_Mission()
{

}

void IODevice_Mission::Power(void)
{
 phase = -1;
 tl = true;
 data_out = 0x01;

 // Power-on state not tested:
 afcounter = 0;
 afphase = false;
}

void IODevice_Mission::UpdateInput(const uint8 *data, const int32 time_elapsed)
{
	const uint32 dtmp = MDFN_de32lsb(&data[0]);

	dbuttons = (dbuttons & 0xF) | ((dtmp & 0xFFF) << 4);
	afeswitches = ((dtmp >> 12) & 0x8FF) << 4;
	afspeed = (dtmp >> 20) & 0x7;

	int offs = 4;
	for (unsigned stick = 0; stick < (dual ? 2 : 1); stick++)
	{
		for (unsigned axis = 0; axis < 3; axis++)
		{
			axes[stick][axis] = data[offs++];
		}
	}

	//printf("Update: %02x %02x %02x\n", axes[0][0], axes[0][1], axes[0][2]);
}

uint8 IODevice_Mission::UpdateBus(const uint8 smpc_out, const uint8 smpc_out_asserted)
{
 uint8 tmp;

 if(smpc_out & 0x40)
 {
  phase = -1;
  tl = true;
  data_out = 0x01;
 }
 else
 {
  if((bool)(smpc_out & 0x20) != tl)
  {
   if(phase < (dual ? 21 : 13))
   {
    tl = !tl;
    phase++;
   }

   if(!phase)
   {
    unsigned dbaf = dbuttons & ((afphase - 1) | ~afeswitches);
    unsigned c = 0;

    // Digital Left
    dbuttons |=  ((axes[0][0] <= 0x56) ? 0x4 : 0);
    dbuttons &= ~((axes[0][0] >= 0x6C) ? 0x4 : 0);

    // Digital Right
    dbuttons |=  ((axes[0][0] >= 0xAB) ? 0x8 : 0);
    dbuttons &= ~((axes[0][0] <= 0x95) ? 0x8 : 0);

    // Digital Up
    dbuttons |=  ((axes[0][1] <= 0x54) ? 0x1 : 0);
    dbuttons &= ~((axes[0][1] >= 0x6A) ? 0x1 : 0);

    // Digital Down
    dbuttons |=  ((axes[0][1] >= 0xA9) ? 0x2 : 0);
    dbuttons &= ~((axes[0][1] <= 0x94) ? 0x2 : 0);

    if(!afcounter)
    {
     static const uint8 speedtab[7] = { 12, 8, 7, 5, 4, 4/* ? */, 1 };
     afphase = !afphase;
     afcounter = speedtab[afspeed];
    }
    afcounter--;

    buffer[c++] = 0x1;
    buffer[c++] = dual ? 0x9 : 0x5;
    buffer[c++] = (((dbaf >>  0) & 0xF) ^ 0xF);
    buffer[c++] = (((dbaf >>  4) & 0xF) ^ 0xF);
    buffer[c++] = (((dbaf >>  8) & 0xF) ^ 0xF);
    buffer[c++] = (((dbaf >> 12) & 0xF) ^ 0xF);

    for(unsigned stick = 0; stick < (dual ? 2 : 1); stick++)
    {
     if(stick)
     {
      // Not sure, looks like something buggy.
      buffer[c++] = 0x0;
      buffer[c++] = 0x0;
     }

     buffer[c++] = (axes[stick][0] >> 4) & 0xF;
     buffer[c++] = (axes[stick][0] >> 0) & 0xF;
     buffer[c++] = (axes[stick][1] >> 4) & 0xF;
     buffer[c++] = (axes[stick][1] >> 0) & 0xF;
     buffer[c++] = (axes[stick][2] >> 4) & 0xF;
     buffer[c++] = (axes[stick][2] >> 0) & 0xF;
    }
    buffer[c++] = 0x0;
    buffer[c++] = 0x1;
   }

   data_out = buffer[phase];
  }
 }

 tmp = (tl << 4) | data_out;

 return (smpc_out & (smpc_out_asserted | 0xE0)) | (tmp &~ smpc_out_asserted);
}

}
