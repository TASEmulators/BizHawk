#ifndef __MDFN_PSX_INPUT_GAMEPAD_H
#define __MDFN_PSX_INPUT_GAMEPAD_H

namespace MDFN_IEN_PSX
{

InputDevice *Device_Gamepad_Create(void);

	EW_PACKED(
	struct IO_Gamepad
	{
		u8 buttons[2];
		u8 active;
	});

}
#endif
