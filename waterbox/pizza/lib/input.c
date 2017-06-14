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
char input_key_left;
char input_key_right;
char input_key_up;
char input_key_down;
char input_key_a;
char input_key_b;
char input_key_select;
char input_key_start;

uint8_t input_init()
{
    input_key_left = 0;
    input_key_right = 0;
    input_key_up = 0;
    input_key_down = 0;
    input_key_a = 0;
    input_key_b = 0;
    input_key_select = 0;
    input_key_start = 0;

    return 0;
}

uint8_t input_get_keys(uint8_t line)
{
    uint8_t v = line | 0x0f;

    if ((line & 0x30) == 0x20)
    { 
        /* RIGHT pressed? */
        if (input_key_right)
            v ^= 0x01;

        /* LEFT pressed? */
        if (input_key_left)
            v ^= 0x02;

        /* UP pressed?   */
        if (input_key_up)
            v ^= 0x04;

        /* DOWN pressed? */
        if (input_key_down)
            v ^= 0x08;
    }

    if ((line & 0x30) == 0x10)
    {
        /* A pressed?      */
        if (input_key_a)
            v ^= 0x01;

        /* B pressed?      */
        if (input_key_b)
            v ^= 0x02;

        /* SELECT pressed? */
        if (input_key_select)
            v ^= 0x04;

        /* START pressed?  */
        if (input_key_start)
            v ^= 0x08;
    }

    return (v | 0xc0);
}

void input_set_key_right(char state) { input_key_right = state; }
void input_set_key_left(char state) { input_key_left = state; }
void input_set_key_up(char state) { input_key_up = state; }
void input_set_key_down(char state) { input_key_down = state; }
void input_set_key_a(char state) { input_key_a = state; }
void input_set_key_b(char state) { input_key_b = state; }
void input_set_key_select(char state) { input_key_select = state; }
void input_set_key_start(char state) { input_key_start = state; }
