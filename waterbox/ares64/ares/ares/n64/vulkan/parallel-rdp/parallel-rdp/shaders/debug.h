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

#ifndef DEBUG_H_
#define DEBUG_H_

#if defined(DEBUG_ENABLE) && DEBUG_ENABLE
#include "debug_channel.h"

const uint CODE_ASSERT_EQUAL = 0;
const uint CODE_ASSERT_NOT_EQUAL = 1;
const uint CODE_ASSERT_LESS_THAN = 2;
const uint CODE_ASSERT_LESS_THAN_EQUAL = 3;
const uint CODE_GENERIC = 4;
const uint CODE_HEX = 5;

void ASSERT_EQUAL_(int line, int a, int b)
{
	if (a != b)
		add_debug_message(CODE_ASSERT_EQUAL, gl_GlobalInvocationID, ivec3(line, a, b));
}

void ASSERT_NOT_EQUAL_(int line, int a, int b)
{
	if (a == b)
		add_debug_message(CODE_ASSERT_NOT_EQUAL, gl_GlobalInvocationID, ivec3(line, a, b));
}

void ASSERT_LESS_THAN_(int line, int a, int b)
{
	if (a >= b)
		add_debug_message(CODE_ASSERT_LESS_THAN, gl_GlobalInvocationID, ivec3(line, a, b));
}

void ASSERT_LESS_THAN_EQUAL_(int line, int a, int b)
{
	if (a > b)
		add_debug_message(CODE_ASSERT_LESS_THAN_EQUAL, gl_GlobalInvocationID, ivec3(line, a, b));
}

void ASSERT_EQUAL_(int line, uint a, uint b)
{
	if (a != b)
		add_debug_message(CODE_ASSERT_EQUAL, gl_GlobalInvocationID, ivec3(line, a, b));
}

void ASSERT_NOT_EQUAL_(int line, uint a, uint b)
{
	if (a == b)
		add_debug_message(CODE_ASSERT_NOT_EQUAL, gl_GlobalInvocationID, ivec3(line, a, b));
}

void ASSERT_LESS_THAN_(int line, uint a, uint b)
{
	if (a >= b)
		add_debug_message(CODE_ASSERT_LESS_THAN, gl_GlobalInvocationID, ivec3(line, a, b));
}

void ASSERT_LESS_THAN_EQUAL_(int line, uint a, uint b)
{
	if (a > b)
		add_debug_message(CODE_ASSERT_LESS_THAN_EQUAL, gl_GlobalInvocationID, ivec3(line, a, b));
}

void GENERIC_MESSAGE_(int line)
{
	add_debug_message(CODE_GENERIC, gl_GlobalInvocationID, line);
}

void GENERIC_MESSAGE_(int line, uint v)
{
	add_debug_message(CODE_GENERIC, gl_GlobalInvocationID, uvec2(line, v));
}

void GENERIC_MESSAGE_(int line, uvec2 v)
{
	add_debug_message(CODE_GENERIC, gl_GlobalInvocationID, uvec3(line, v));
}

void GENERIC_MESSAGE_(int line, uvec3 v)
{
	add_debug_message(CODE_GENERIC, gl_GlobalInvocationID, uvec4(line, v));
}

void HEX_MESSAGE_(int line)
{
	add_debug_message(CODE_HEX, gl_GlobalInvocationID, line);
}

void HEX_MESSAGE_(int line, uint v)
{
	add_debug_message(CODE_HEX, gl_GlobalInvocationID, uvec2(line, v));
}

void HEX_MESSAGE_(int line, uvec2 v)
{
	add_debug_message(CODE_HEX, gl_GlobalInvocationID, uvec3(line, v));
}

void HEX_MESSAGE_(int line, uvec3 v)
{
	add_debug_message(CODE_HEX, gl_GlobalInvocationID, uvec4(line, v));
}

#define ASERT_EQUAL(a, b) ASSERT_EQUAL_(__LINE__, a, b)
#define ASERT_NOT_EQUAL(a, b) ASSERT_NOT_EQUAL_(__LINE__, a, b)
#define ASERT_LESS_THAN(a, b) ASSERT_LESS_THAN_(__LINE__, a, b)
#define ASERT_LESS_THAN_EQUAL(a, b) ASSERT_LESS_THAN_EQUAL_(__LINE__, a, b)
#define GENERIC_MESSAGE0() GENERIC_MESSAGE_(__LINE__)
#define GENERIC_MESSAGE1(a) GENERIC_MESSAGE_(__LINE__, a)
#define GENERIC_MESSAGE2(a, b) GENERIC_MESSAGE_(__LINE__, uvec2(a, b))
#define GENERIC_MESSAGE3(a, b, c) GENERIC_MESSAGE_(__LINE__, uvec3(a, b, c))
#define HEX_MESSAGE0() HEX_MESSAGE_(__LINE__)
#define HEX_MESSAGE1(a) HEX_MESSAGE_(__LINE__, a)
#define HEX_MESSAGE2(a, b) HEX_MESSAGE_(__LINE__, uvec2(a, b))
#define HEX_MESSAGE3(a, b, c) HEX_MESSAGE_(__LINE__, uvec3(a, b, c))
#else
#define ASERT_EQUAL(a, b)
#define ASERT_NOT_EQUAL(a, b)
#define ASERT_LESS_THAN(a, b)
#define ASERT_LESS_THAN_EQUAL(a, b)
#define GENERIC_MESSAGE0()
#define GENERIC_MESSAGE1(a)
#define GENERIC_MESSAGE2(a, b)
#define GENERIC_MESSAGE3(a, b, c)
#define HEX_MESSAGE0()
#define HEX_MESSAGE1(a)
#define HEX_MESSAGE2(a, b)
#define HEX_MESSAGE3(a, b, c)
#endif

#endif