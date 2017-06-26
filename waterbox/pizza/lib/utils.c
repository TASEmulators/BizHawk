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

#include <emulibc.h>
#include <stdio.h>

#include <stdarg.h>
#include <sys/time.h>

#include "cycles.h"
#include "gpu.h"
#include "utils.h"

uint64_t prev_cycles = 0;

void utils_log(const char *format, ...)
{
    char buf[256];

    va_list args;
    va_start(args, format);
    vsnprintf(buf, 256, format, args);
    _debug_puts(buf);
    va_end(args);
}


void utils_log_urgent(const char *format, ...)
{
    char buf[256];

    va_list args;
    va_start(args, format);
    vsnprintf(buf, 256, format, args);
    _debug_puts(buf);
    va_end(args);
}

void utils_ts_log(const char *format, ...)
{
    va_list args;
    va_start(args, format);

    char buf[256];
	char buf2[512];
    struct timeval tv;


    vsprintf(buf, format, args);
    //gettimeofday(&tv, NULL);
//    printf("%ld - %s\n", tv.tv_sec, buf);
    sprintf(buf2, "LINE %u - CYCLES %lu - DIFF %lu - %ld:%06ld - %s", 
            *(gpu.ly), cycles.cnt, cycles.cnt - prev_cycles,
            tv.tv_sec, tv.tv_usec, buf);
	_debug_puts(buf2);
    prev_cycles = cycles.cnt;

    va_end(args);
}
