/******************************************************************************/
/* Mednafen Sega Saturn Emulation Module                                      */
/******************************************************************************/
/* gamepad.cpp - Digital Gamepad Emulation
**  Copyright (C) 2015-2017 Mednafen Team
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
#include "gamepad.h"

namespace MDFN_IEN_SS
{

IODevice_Gamepad::IODevice_Gamepad() : buttons(~3)
{

}

IODevice_Gamepad::~IODevice_Gamepad()
{

}

void IODevice_Gamepad::Power(void)
{

}

void IODevice_Gamepad::UpdateInput(const uint8* data, const int32 time_elapsed)
{
 buttons = (~(data[0] | (data[1] << 8))) &~ 0x3000;
}

uint8 IODevice_Gamepad::UpdateBus(const uint8 smpc_out, const uint8 smpc_out_asserted)
{
 uint8 tmp;

 tmp = (buttons >> ((smpc_out >> 5) << 2)) & 0xF;

 return 0x10 | (smpc_out & (smpc_out_asserted | 0xE0)) | (tmp &~ smpc_out_asserted);
}

IDIISG IODevice_Gamepad_IDII =
{
 { "z", "Z", 10, IDIT_BUTTON },
 { "y", "Y", 9, IDIT_BUTTON },
 { "x", "X", 8, IDIT_BUTTON },
 { "rs", "Right Shoulder", 12, IDIT_BUTTON },

 { "up", "UP ↑", 0, IDIT_BUTTON, "down" },
 { "down", "DOWN ↓", 1, IDIT_BUTTON, "up" },
 { "left", "LEFT ←", 2, IDIT_BUTTON, "right" },
 { "right", "RIGHT →", 3, IDIT_BUTTON, "left" },

 { "b", "B", 6, IDIT_BUTTON },
 { "c", "C", 7, IDIT_BUTTON },
 { "a", "A", 5, IDIT_BUTTON },
 { "start", "START", 4, IDIT_BUTTON },

 { NULL, "empty", 0, IDIT_BUTTON },
 { NULL, "empty", 0, IDIT_BUTTON },
 { NULL, "empty", 0, IDIT_BUTTON },
 { "ls", "Left Shoulder", 11, IDIT_BUTTON },
};


}
