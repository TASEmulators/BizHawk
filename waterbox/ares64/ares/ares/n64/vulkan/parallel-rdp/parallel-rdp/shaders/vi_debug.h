/* Copyright (c) 2020 Themaister
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#ifndef VI_DEBUG_H_
#define VI_DEBUG_H_

#if defined(DEBUG_ENABLE) && DEBUG_ENABLE
#include "debug_channel.h"

void GENERIC_MESSAGE_(int line)
{
	add_debug_message(0, uvec3(gl_FragCoord.xy, 0), line);
}

void GENERIC_MESSAGE_(int line, uint v)
{
	add_debug_message(0, uvec3(gl_FragCoord.xy, 0), uvec2(line, v));
}

void GENERIC_MESSAGE_(int line, uvec2 v)
{
	add_debug_message(0, uvec3(gl_FragCoord.xy, 0), uvec3(line, v));
}

void GENERIC_MESSAGE_(int line, uvec3 v)
{
	add_debug_message(0, uvec3(gl_FragCoord.xy, 0), uvec4(line, v));
}

#define GENERIC_MESSAGE0() GENERIC_MESSAGE_(__LINE__)
#define GENERIC_MESSAGE1(a) GENERIC_MESSAGE_(__LINE__, a)
#define GENERIC_MESSAGE2(a, b) GENERIC_MESSAGE_(__LINE__, uvec2(a, b))
#define GENERIC_MESSAGE3(a, b, c) GENERIC_MESSAGE_(__LINE__, uvec3(a, b, c))
#else
#define GENERIC_MESSAGE0()
#define GENERIC_MESSAGE1(a)
#define GENERIC_MESSAGE2(a, b)
#define GENERIC_MESSAGE3(a, b, c)
#endif

#endif