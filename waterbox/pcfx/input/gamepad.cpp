/******************************************************************************/
/* Mednafen NEC PC-FX Emulation Module                                        */
/******************************************************************************/
/* gamepad.cpp:
**  Copyright (C) 2006-2016 Mednafen Team
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

#include "../pcfx.h"
#include "../input.h"
#include "gamepad.h"

namespace MDFN_IEN_PCFX
{

class PCFX_Input_Gamepad : public PCFX_Input_Device
{
  public:
	PCFX_Input_Gamepad()
	{
		buttons = 0;
	}

	virtual ~PCFX_Input_Gamepad() override
	{
	}

	virtual uint32 ReadTransferTime(void) override
	{
		return 1536;
	}

	virtual uint32 WriteTransferTime(void) override
	{
		return 1536;
	}

	virtual uint32 Read(void) override
	{
		return buttons | FX_SIG_PAD << 28;
	}

	virtual void Write(uint32 data) override
	{
	}

	virtual void Power(void) override
	{
		buttons = 0;
	}

	virtual void Frame(uint32_t data) override
	{
		buttons = data;
	}

  private:
	// 5....098 7......0
	//  m mldru rs654321
	uint16 buttons;
};

PCFX_Input_Device *PCFXINPUT_MakeGamepad(void)
{
	return new PCFX_Input_Gamepad();
}
}
