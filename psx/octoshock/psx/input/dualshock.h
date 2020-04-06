/******************************************************************************/
/* Mednafen Sony PS1 Emulation Module                                         */
/******************************************************************************/
/* dualshock.h:
**  Copyright (C) 2012-2016 Mednafen Team
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

#ifndef __MDFN_PSX_INPUT_DUALSHOCK_H
#define __MDFN_PSX_INPUT_DUALSHOCK_H

#include "octoshock.h"

namespace MDFN_IEN_PSX
{
	InputDevice *Device_DualShock_Create();
	
	EW_PACKED(
	struct IO_Dualshock
	{
		u8 buttons[3];
		u8 right_x, right_y;
		u8 left_x, left_y;
		u8 active;
		u8 pad[8];
		u8 pad2[3];
		u16 rumble;
		u8 pad3[11];
	});
}


#endif
