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

// Utility header to smooth over the difference between
// 8/16-bit integer arithmetic vs. just 8/16-bit storage.

#ifndef SMALL_INTEGERS_H_
#define SMALL_INTEGERS_H_

#extension GL_EXT_shader_16bit_storage : require
#extension GL_EXT_shader_8bit_storage : require

#if SMALL_TYPES
#extension GL_EXT_shader_explicit_arithmetic_types_int8 : require
#extension GL_EXT_shader_explicit_arithmetic_types_int16 : require

#define mem_u8 uint8_t
#define mem_u16 uint16_t
#define mem_u8x2 u8vec2
#define mem_u16x2 u16vec2
#define mem_u8x3 u8vec3
#define mem_u16x3 u16vec3
#define mem_u8x4 u8vec4
#define mem_u16x4 u16vec4

#define mem_i8 int8_t
#define mem_i16 int16_t
#define mem_i8x2 i8vec2
#define mem_i16x2 i16vec2
#define mem_i8x3 i8vec3
#define mem_i16x3 i16vec3
#define mem_i8x4 i8vec4
#define mem_i16x4 i16vec4

#define u8 uint8_t
#define u16 uint16_t
#define u8x2 u8vec2
#define u16x2 u16vec2
#define u8x3 u8vec3
#define u16x3 u16vec3
#define u8x4 u8vec4
#define u16x4 u16vec4

#define i8 int8_t
#define i16 int16_t
#define i8x2 i8vec2
#define i16x2 i16vec2
#define i8x3 i8vec3
#define i16x3 i16vec3
#define i8x4 i8vec4
#define i16x4 i16vec4

#define U8_C(x) uint8_t(x)
#define I8_C(x) int8_t(x)
#define U16_C(x) uint16_t(x)
#define I16_C(x) int16_t(x)

#else

#define mem_u8 uint8_t
#define mem_u16 uint16_t
#define mem_u8x2 u8vec2
#define mem_u16x2 u16vec2
#define mem_u8x3 u8vec3
#define mem_u16x3 u16vec3
#define mem_u8x4 u8vec4
#define mem_u16x4 u16vec4

#define mem_i8 int8_t
#define mem_i16 int16_t
#define mem_i8x2 i8vec2
#define mem_i16x2 i16vec2
#define mem_i8x3 i8vec3
#define mem_i16x3 i16vec3
#define mem_i8x4 i8vec4
#define mem_i16x4 i16vec4

#define u8 int
#define u16 int
#define u8x2 ivec2
#define u16x2 ivec2
#define u8x3 ivec3
#define u16x3 ivec3
#define u8x4 ivec4
#define u16x4 ivec4

#define i8 int
#define i16 int
#define i8x2 ivec2
#define i16x2 ivec2
#define i8x3 ivec3
#define i16x3 ivec3
#define i8x4 ivec4
#define i16x4 ivec4

#define U8_C(x) int(x)
#define I8_C(x) int(x)
#define U16_C(x) int(x)
#define I16_C(x) int(x)

#endif
#endif