/******************************************************************************/
/* Mednafen Sega Saturn Emulation Module                                      */
/******************************************************************************/
/* 3dpad.cpp:
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
#include "3dpad.h"

namespace MDFN_IEN_SS
{

IODevice_3DPad::IODevice_3DPad() : dbuttons(0), mode(false)
{

}

IODevice_3DPad::~IODevice_3DPad()
{

}

void IODevice_3DPad::Power(void)
{
 phase = -1;
 tl = true;
 data_out = 0x01;
}

void IODevice_3DPad::UpdateInput(const uint8* data, const int32 time_elapsed)
{
 const uint16 dtmp = MDFN_de16lsb(&data[0]);

 dbuttons = (dbuttons & 0x8800) | (dtmp & 0x0FFF);
 mode = (bool)(dtmp & 0x1000);

 for(unsigned axis = 0; axis < 2; axis++)
 {
  int32 tmp = 32767 + MDFN_de16lsb(&data[0x2 + (axis << 2) + 2]) - MDFN_de16lsb(&data[0x2 + (axis << 2) + 0]);

  if(tmp >= (32767 - 128) && tmp < 32767)
   tmp = 32767;

  tmp = (tmp * 255 + 32767) / 65534;
  thumb[axis] = tmp;
 }

 for(unsigned w = 0; w < 2; w++)
 {
  shoulder[w] = (MDFN_de16lsb(&data[0xA + (w << 1)]) * 255 + 16383) / 32767;

  // May not be right for digital mode, but shouldn't matter too much:
  if(shoulder[w] <= 0x55)
   dbuttons &= ~(0x0800 << (w << 2));
  else if(shoulder[w] >= 0x8E)
   dbuttons |= 0x0800 << (w << 2);
 }
}

uint8 IODevice_3DPad::UpdateBus(const uint8 smpc_out, const uint8 smpc_out_asserted)
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
   if(phase < 15)
   {
    tl = !tl;
    phase++;
   }

   if(!phase)
   {
    if(mode)
    {   
     buffer[ 0] = 0x1;
     buffer[ 1] = 0x6;
     buffer[ 2] = (((dbuttons >>  0) & 0xF) ^ 0xF);
     buffer[ 3] = (((dbuttons >>  4) & 0xF) ^ 0xF);
     buffer[ 4] = (((dbuttons >>  8) & 0xF) ^ 0xF);
     buffer[ 5] = (((dbuttons >> 12) & 0xF) ^ 0xF);
     buffer[ 6] = (thumb[0] >> 4) & 0xF;
     buffer[ 7] = (thumb[0] >> 0) & 0xF;
     buffer[ 8] = (thumb[1] >> 4) & 0xF;
     buffer[ 9] = (thumb[1] >> 0) & 0xF;
     buffer[10] = (shoulder[0] >> 4) & 0xF;
     buffer[11] = (shoulder[0] >> 0) & 0xF;
     buffer[12] = (shoulder[1] >> 4) & 0xF;
     buffer[13] = (shoulder[1] >> 0) & 0xF;
     buffer[14] = 0x0;
     buffer[15] = 0x1;
    }
    else
    {
     phase = 8;
     buffer[ 8] = 0x0;
     buffer[ 9] = 0x2;
     buffer[10] = (((dbuttons >>  0) & 0xF) ^ 0xF);
     buffer[11] = (((dbuttons >>  4) & 0xF) ^ 0xF);
     buffer[12] = (((dbuttons >>  8) & 0xF) ^ 0xF);
     buffer[13] = (((dbuttons >> 12) & 0xF) ^ 0xF);
     buffer[14] = 0x0;
     buffer[15] = 0x1;
    }
   }

   data_out = buffer[phase];
  }
 }

 tmp = (tl << 4) | data_out;

 return (smpc_out & (smpc_out_asserted | 0xE0)) | (tmp &~ smpc_out_asserted);
}

static const char* const ModeSwitchPositions[] =
{
 gettext_noop("Digital(+)"),
 gettext_noop("Analog(○)"),
};

IDIISG IODevice_3DPad_IDII =
{
 { "up", "D-Pad UP ↑", 0, IDIT_BUTTON, "down" },
 { "down", "D-Pad DOWN ↓", 1, IDIT_BUTTON, "up" },
 { "left", "D-Pad LEFT ←", 2, IDIT_BUTTON, "right" },
 { "right", "D-Pad RIGHT →", 3, IDIT_BUTTON, "left" },

 { "b", "B", 6, IDIT_BUTTON },
 { "c", "C", 7, IDIT_BUTTON },
 { "a", "A", 5, IDIT_BUTTON },
 { "start", "START", 4, IDIT_BUTTON },

 { "z", "Z", 10, IDIT_BUTTON },
 { "y", "Y", 9, IDIT_BUTTON },
 { "x", "X", 8, IDIT_BUTTON },
 { NULL, "empty", 0, IDIT_BUTTON },

 IDIIS_Switch("mode", "Mode", 17, ModeSwitchPositions, sizeof(ModeSwitchPositions) / sizeof(ModeSwitchPositions[0])),

 { "analog_left", "Analog LEFT ←", 15, IDIT_BUTTON_ANALOG },
 { "analog_right", "Analog RIGHT →", 16, IDIT_BUTTON_ANALOG },
 { "analog_up", "Analog UP ↑", 13, IDIT_BUTTON_ANALOG },
 { "analog_down", "Analog DOWN ↓", 14, IDIT_BUTTON_ANALOG },

 { "rs", "Right Shoulder (Analog)", 12, IDIT_BUTTON_ANALOG },
 { "ls", "Left Shoulder (Analog)", 11, IDIT_BUTTON_ANALOG },
};




}
