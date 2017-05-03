#ifndef __MDFN_PSX_INPUT_NEGCON_H
#define __MDFN_PSX_INPUT_NEGCON_H

namespace MDFN_IEN_PSX
{
 InputDevice *Device_neGcon_Create(void);

	EW_PACKED(
	struct IO_NegCon
	{
		u8 buttons[2];
		u8 twist;
		u8 anabuttons[3];
		u8 active;
	});

}
#endif
