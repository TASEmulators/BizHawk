/******************************************************************************/
/* Mednafen Sega Saturn Emulation Module                                      */
/******************************************************************************/
/* wheel.cpp:
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
#include "wheel.h"

namespace MDFN_IEN_SS
{

IODevice_Wheel::IODevice_Wheel() : dbuttons(0)
{

}

IODevice_Wheel::~IODevice_Wheel()
{

}

void IODevice_Wheel::Power(void)
{
 phase = -1;
 tl = true;
 data_out = 0x01;
}

void IODevice_Wheel::UpdateInput(const uint8* data, const int32 time_elapsed)
{
 dbuttons = (dbuttons & 0xC) | (MDFN_de16lsb(&data[0]) & 0x07F3);

 //
 {
  int32 tmp = 32767 + MDFN_de16lsb(&data[0x2 + 2]) - MDFN_de16lsb(&data[0x2 + 0]);

  wheel = 1 + tmp * 253 / 65534;

  if(wheel >= 0x6F)
   dbuttons &= ~0x4;
  else if(wheel <= 0x67)
   dbuttons |= 0x4;

  if(wheel <= 0x8F)
   dbuttons &= ~0x8;
  else if(wheel >= 0x97)
   dbuttons |= 0x8;
 }
}

uint8 IODevice_Wheel::UpdateBus(const uint8 smpc_out, const uint8 smpc_out_asserted)
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
   if(phase < 0)
   {
    buffer[ 0] = 0x1;
    buffer[ 1] = 0x3;
    buffer[ 2] = (((dbuttons >>  0) & 0xF) ^ 0xF);
    buffer[ 3] = (((dbuttons >>  4) & 0xF) ^ 0xF);
    buffer[ 4] = (((dbuttons >>  8) & 0xF) ^ 0xF);
    buffer[ 5] = (((dbuttons >> 12) & 0xF) ^ 0xF);
    buffer[ 6] = ((wheel >> 4) & 0xF);
    buffer[ 7] = ((wheel >> 0) & 0xF);
    buffer[ 8] = 0x0;
    buffer[ 9] = 0x1;
    buffer[10] = 0x1;
    buffer[11] = ((wheel >> 0) & 0xF);
    buffer[12] = 0x0;
    buffer[13] = 0x1;
    buffer[14] = 0x1;
    buffer[15] = 0x1;
   }

   phase = (phase + 1) & 0xF;
   data_out = buffer[phase];
   tl = !tl;
  }
 }

 tmp = (tl << 4) | data_out;

 return (smpc_out & (smpc_out_asserted | 0xE0)) | (tmp &~ smpc_out_asserted);
}

IDIISG IODevice_Wheel_IDII =
{
 { "up", "L Gear Shift(Equiv. UP ↑)", 2, IDIT_BUTTON, "down" },
 { "down", "R Gear Shift(Equiv. DOWN ↓)", 3, IDIT_BUTTON, "up" },
 { NULL, "empty", 0, IDIT_BUTTON }, // left
 { NULL, "empty", 0, IDIT_BUTTON }, // right

 { "b", "B (R Group)", 9, IDIT_BUTTON },
 { "c", "C (R Group)", 10, IDIT_BUTTON },
 { "a", "A (R Group)", 8, IDIT_BUTTON },
 { "start", "START", 7, IDIT_BUTTON },

 { "z", "Z (L Group)", 4, IDIT_BUTTON },
 { "y", "Y (L Group)", 5, IDIT_BUTTON },
 { "x", "X (L Group)", 6, IDIT_BUTTON },
 { NULL, "empty", 0, IDIT_BUTTON },

 { NULL, "empty", 0, IDIT_BUTTON },
 { NULL, "empty", 0, IDIT_BUTTON },
 { NULL, "empty", 0, IDIT_BUTTON },
 { NULL, "empty", 0, IDIT_BUTTON },

 { "analog_left", "Analog LEFT ←", 0, IDIT_BUTTON_ANALOG },
 { "analog_right", "Analog RIGHT →", 1, IDIT_BUTTON_ANALOG },
};




}
