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

#ifndef __INTERRUPTS_HDR__
#define __INTERRUPTS_HDR__

#include <stdint.h>

typedef struct interrupts_flags_s
{ 
    uint8_t lcd_vblank:1;
    uint8_t lcd_ctrl:1;
    uint8_t timer:1;
    uint8_t serial_io:1;
    uint8_t pins1013:1;
    uint8_t spare:3;
} interrupts_flags_t;

#endif