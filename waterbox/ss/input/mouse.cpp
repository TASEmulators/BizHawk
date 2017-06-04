/******************************************************************************/
/* Mednafen Sega Saturn Emulation Module                                      */
/******************************************************************************/
/* mouse.cpp:
**  Copyright (C) 2016-2017 Mednafen Team
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
#include "mouse.h"

namespace MDFN_IEN_SS
{

IODevice_Mouse::IODevice_Mouse() : buttons(0)
{

}

IODevice_Mouse::~IODevice_Mouse()
{

}

void IODevice_Mouse::Power(void)
{
 phase = -1;
 tl = true;
 data_out = 0x00;
 accum_xdelta = 0;
 accum_ydelta = 0;
}

void IODevice_Mouse::UpdateInput(const uint8* data, const int32 time_elapsed)
{
 accum_xdelta += MDFN_de32lsb(&data[0]);
 accum_ydelta -= MDFN_de32lsb(&data[4]);
 buttons = data[8] & 0xF;
}

uint8 IODevice_Mouse::UpdateBus(const uint8 smpc_out, const uint8 smpc_out_asserted)
{
 uint8 tmp;

 if(smpc_out & 0x40)
 {
  if(smpc_out & 0x20)
  {
   if(!tl)
    accum_xdelta = accum_ydelta = 0;

   phase = -1;
   tl = true;
   data_out = 0x00;
  }
  else
  {
   if(tl)
    tl = false;
  }
 }
 else
 {
  if(phase < 0)
  {
   uint8 flags = 0;

   if(accum_xdelta < 0)
    flags |= 0x1;
 
   if(accum_ydelta < 0)
    flags |= 0x2;

   if(accum_xdelta > 255 || accum_xdelta < -256)
   {
    flags |= 0x4;
    accum_xdelta = (accum_xdelta < 0) ? -256 : 255;
   }

   if(accum_ydelta > 255 || accum_ydelta < -256)
   {
    flags |= 0x8;
    accum_ydelta = (accum_ydelta < 0) ? -256 : 255;
   }

   buffer[0] = 0xB;
   buffer[1] = 0xF;
   buffer[2] = 0xF;
   buffer[3] = flags;
   buffer[4] = buttons;
   buffer[5] = (accum_xdelta >> 4) & 0xF;
   buffer[6] = (accum_xdelta >> 0) & 0xF;
   buffer[7] = (accum_ydelta >> 4) & 0xF;
   buffer[8] = (accum_ydelta >> 0) & 0xF;

   for(int i = 9; i < 16; i++)
    buffer[i] = buffer[8];

   phase++;
  }

  if((bool)(smpc_out & 0x20) != tl)
  {
   phase = (phase + 1) & 0xF;
   tl = !tl;

   if(phase == 8)
    accum_xdelta = accum_ydelta = 0;
  }
  data_out = buffer[phase];
 }

 tmp = (tl << 4) | data_out;

 return (smpc_out & (smpc_out_asserted | 0xE0)) | (tmp &~ smpc_out_asserted);
}

IDIISG IODevice_Mouse_IDII =
{
 { "x_axis", "X Axis", -1, IDIT_X_AXIS_REL },
 { "y_axis", "Y Axis", -1, IDIT_Y_AXIS_REL },

 { "left", "Left Button", 0, IDIT_BUTTON },
 { "right", "Right Button", 2, IDIT_BUTTON },
 { "middle", "Middle Button", 1, IDIT_BUTTON },
 { "start", "Start", 3, IDIT_BUTTON },
};


}
