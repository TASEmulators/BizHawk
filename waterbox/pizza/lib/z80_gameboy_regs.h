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


#ifndef Z80_REGS_H
#define Z80_REGS_H

#include <stdint.h>

/* structs emulating z80 registers and flags */
typedef struct z80_flags_s
{
    uint8_t  spare:4;   
    uint8_t  cy:1;
    uint8_t  ac:1;
    uint8_t  n:1;
    uint8_t  z:1;
} z80_flags_t;


/* flags offsets */
#if __BYTE_ORDER__ == __ORDER_LITTLE_ENDIAN__

    #define FLAG_OFFSET_CY 4
    #define FLAG_OFFSET_AC 5
    #define FLAG_OFFSET_N  6
    #define FLAG_OFFSET_Z  7

#endif


#endif
