#ifndef __MDFN_PSX_INPUT_DUALANALOG_H
#define __MDFN_PSX_INPUT_DUALANALOG_H

namespace MDFN_IEN_PSX
{
InputDevice *Device_DualAnalog_Create(bool joystick_mode);

EW_PACKED(
struct IO_DualAnalog
{
		u8 buttons[2];
		u8 right_x, right_y;
		u8 left_x, left_y;
		u8 active;
});

}
#endif
