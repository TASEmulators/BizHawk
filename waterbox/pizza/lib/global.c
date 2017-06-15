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

#include <stdio.h>
#include <strings.h>

#include "global.h"

char global_cart_name[256];
char global_cgb; // if true, in CGB mode
char global_cpu_double_speed;
char global_debug;
char global_rumble;
char global_window; // if true, show window

void global_init()
{
    global_window = 1;
    global_debug = 0;
    global_cgb = 0;
    global_cpu_double_speed = 0;
    global_rumble = 0;
    sprintf(global_cart_name, "NOCARTIRDGE");
}
