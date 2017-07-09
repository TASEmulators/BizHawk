/******************************************************************************/
/* Mednafen NEC PC-FX Emulation Module                                        */
/******************************************************************************/
/* mouse.cpp:
**  Copyright (C) 2007-2016 Mednafen Team
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
#include "mouse.h"

namespace MDFN_IEN_PCFX
{

class PCFX_Input_Mouse : public PCFX_Input_Device
{
  public:
	PCFX_Input_Mouse(int which)
	{
		dx = 0;
		dy = 0;
		button = 0;
	}

	virtual ~PCFX_Input_Mouse() override
	{
	}

	virtual uint32 ReadTransferTime(void) override
	{
		return (1536);
	}

	virtual uint32 WriteTransferTime(void) override
	{
		return (1536);
	}

	virtual uint32 Read(void) override
	{
		return FX_SIG_MOUSE << 28 | button << 16 | dx << 8 | dy;
	}

	virtual void Write(uint32 data) override
	{
	}

	virtual void Power(void) override
	{
		button = 0;
		dx = 0;
		dy = 0;
	}

	virtual void Frame(uint32_t data) override
	{
		dx = data;
		dy = data >> 8;
		button = data >> 16 & 3;
	}

  private:
	int8 dx, dy;

	// 76543210
	// ......RL
	uint8 button;
};

PCFX_Input_Device *PCFXINPUT_MakeMouse(int which)
{
	return new PCFX_Input_Mouse(which);
}
}
