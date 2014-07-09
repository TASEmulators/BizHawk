/*
 * z64
 *
 * Copyright (C) 2007  ziggy
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 *
**/

#ifndef _RGL_ASSERT_H_
#define _RGL_ASSERT_H_

#include <stdio.h>

#ifdef RGL_ASSERT
inline void _rglAssert(int test, const char * s, int line, const char * file) {
    if (!test) {
        fprintf(stderr, "z64 assert failed (%s : %d) : %s\n", file, line, s);
        fflush(stdout);
        fflush(stderr);
        *(unsigned int *)0 = 0xdeadbeef; // hopefully will generate a segfault
        exit(-1);
    }
}
#define rglAssert(test) _rglAssert((test), #test, __LINE__, __FILE__)
#else
#define rglAssert(test)
#endif

#endif
