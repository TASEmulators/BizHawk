/******************************************************************************/
/* Mednafen Sega Saturn Emulation Module                                      */
/******************************************************************************/
/* multitap.cpp:
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

#include "common.h"
#include "multitap.h"

namespace MDFN_IEN_SS
{

IODevice_Multitap::IODevice_Multitap()
{

}

IODevice_Multitap::~IODevice_Multitap()
{

}

void IODevice_Multitap::Power(void)
{
 phase = -2;
 tl = true;
 data_out = 0x01;

 memset(tmp, 0x00, sizeof(tmp));
 id1 = 0;
 id2 = 0;
 port_counter = 0;
 read_counter = 0;

 for(unsigned i = 0; i < 6; i++)
 {
  if(devices[i])
  {
   sub_state[i] = 0x60;
   devices[i]->UpdateBus(sub_state[i], 0x60);
   devices[i]->Power();
  }
 }
}

void IODevice_Multitap::SetSubDevice(unsigned sub_index, IODevice* device)
{
 assert(sub_index < 6);
 devices[sub_index] = device;
 devices[sub_index]->UpdateBus(sub_state[sub_index], 0x60);
}

IODevice* IODevice_Multitap::GetSubDevice(unsigned sub_index)
{
 assert(sub_index < 6);

 return devices[sub_index];
}

enum { PhaseBias = __COUNTER__ + 1 };

#define WAIT_UNTIL(cond)  {					\
			    case __COUNTER__:				\
			    if(!(cond))					\
			    {						\
			     phase = __COUNTER__ - PhaseBias - 1;	\
			     goto BreakOut;				\
			    }						\
			   }

#define WR_NYB(v) { WAIT_UNTIL((bool)(smpc_out & 0x20) != tl); data_out = (v) & 0xF; tl = !tl; }


INLINE uint8 IODevice_Multitap::UASB(void)
{
 return devices[port_counter]->UpdateBus(sub_state[port_counter], 0x60);
}

uint8 IODevice_Multitap::UpdateBus(const uint8 smpc_out, const uint8 smpc_out_asserted)
{
 if(smpc_out & 0x40)
 {
  phase = -1;
  tl = true;
  data_out = 0x01;
 }
 else
 {
  switch(phase + PhaseBias)
  {
   for(;;)
   {
    default:
    case __COUNTER__:

    WAIT_UNTIL(phase == -1);

    WR_NYB(0x4);
    WR_NYB(0x1);
    WR_NYB(0x6);
    WR_NYB(0x0);
    //
    //
    port_counter = 0;

    do
    {
     sub_state[port_counter] = 0x60;
     UASB();
     // ...
     tmp[0] = UASB();
     id1 = ((((tmp[0] >> 3) | (tmp[0] >> 2)) & 1) << 3) | ((((tmp[0] >> 1) | (tmp[0] >> 0)) & 1) << 2);

     sub_state[port_counter] = 0x20;
     UASB();
     // ...
     tmp[1] = UASB();
     id1 |= ((((tmp[1] >> 3) | (tmp[1] >> 2)) & 1) << 1) | ((((tmp[1] >> 1) | (tmp[1] >> 0)) & 1) << 0);

     //printf("%d, %01x\n", port_counter, id1);

     if(id1 == 0xB) // Digital pad
     {
      WR_NYB(0x0);
      WR_NYB(0x2);

      sub_state[port_counter] = 0x40;
      UASB();
      WR_NYB(tmp[1] & 0xF);
      tmp[2] = UASB();

      sub_state[port_counter] = 0x00;
      UASB();
      WR_NYB(tmp[2] & 0xF);
      tmp[3] = UASB();

      WR_NYB(tmp[3] & 0xF);
      WR_NYB((tmp[0] & 0xF) | 0x7);
     }
     else if(id1 == 0x3 || id1 == 0x5) // Analog
     {
      sub_state[port_counter] = 0x00;
      WAIT_UNTIL(!(UASB() & 0x10));
      id2 = ((UASB() & 0xF) << 4);

      sub_state[port_counter] = 0x20;
      WAIT_UNTIL(UASB() & 0x10);
      id2 |= ((UASB() & 0xF) << 0);

      if(id1 == 0x3)
       id2 = 0xE3;

      WR_NYB(id2 >> 4);
      WR_NYB(id2 >> 0);

      read_counter = 0;
      while(read_counter < (id2 & 0xF))
      {
       sub_state[port_counter] = 0x00;
       WAIT_UNTIL(!(UASB() & 0x10));
       WR_NYB(UASB() & 0xF);

       sub_state[port_counter] = 0x20;
       WAIT_UNTIL(UASB() & 0x10);
       WR_NYB(UASB() & 0xF);

       read_counter++;
      }
     }
     else
     {
      WR_NYB(0xF);
      WR_NYB(0xF);
     }

     sub_state[port_counter] = 0x60;
     UASB();
    } while(++port_counter < 6);

    //
    //
    WR_NYB(0x0);
    WR_NYB(0x1);
   }
  }
 }

 BreakOut:;

 return (smpc_out & (smpc_out_asserted | 0xE0)) | (((tl << 4) | data_out) &~ smpc_out_asserted);
}


}
