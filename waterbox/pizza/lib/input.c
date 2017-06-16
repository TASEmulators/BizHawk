/*

    This file is part of Emu-Pizza

    Emu-Pizza is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Emu-Pizza is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Emu-Pizza.  If not, see <http://www.gnu.org/licenses/>.

*/

#include "global.h"
#include "utils.h"

#include <stdint.h>

/* button states */
static uint16_t input_keys;

void input_set_keys(uint16_t keys)
{
	// 7......0
	// DULRSsBA
	input_keys = keys & 0xff;
}

uint8_t input_get_keys(uint8_t line)
{
    uint8_t v = line | 0x0f;

    if ((line & 0x30) == 0x20)
    { 
		v ^= input_keys >> 4;
    }

    if ((line & 0x30) == 0x10)
    {
		v ^= input_keys & 0x0f;
    }

    return v | 0xc0;
}
