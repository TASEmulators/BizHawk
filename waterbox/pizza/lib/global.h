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

#ifndef __GLOBAL__
#define __GLOBAL__

#include <stdint.h>

extern char global_window;
extern char global_debug;
extern char global_cgb;
extern char global_sgb;
// extern char global_started;
extern char global_cpu_double_speed;
extern char global_rumble;
extern char global_cart_name[256];
extern int global_lagged;
extern void (*global_input_callback)(void);
extern int64_t global_currenttime;

/* prototypes */
void global_init();

#endif
