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
