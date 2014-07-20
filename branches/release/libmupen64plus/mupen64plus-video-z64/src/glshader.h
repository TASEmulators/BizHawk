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

#ifndef _GLSHADER_H_
#define _GLSHADER_H_

#include "rdp.h"

typedef struct {
    GLhandleARB vs, fs, prog;
#ifdef RDP_DEBUG
    const char * vsrc, * fsrc;
#endif
} rglShader_t;

rglShader_t * rglCreateShader(const char * vsrc, const char * fsrc);
void rglUseShader(rglShader_t * shader);
void rglDeleteShader(rglShader_t * shader);

#endif
