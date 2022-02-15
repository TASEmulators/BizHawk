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

#ifndef CLAMPING_H_
#define CLAMPING_H_

#if SMALL_TYPES && 0
// This path is buggy on RADV LLVM, disable for time being.
i16x4 clamp_9bit_notrunc(i16x4 color)
{
	// [-129, -256] should clamp to 0xff, subtracting by 0x80 will underflow back to positive numbers.
	// [-128, -1] should clamp to 0.
	color -= I16_C(0x80);
	// Sign-extend to 9-bit.
	color <<= I16_C(7);
	color >>= I16_C(7);
	color += I16_C(0x80);
	return clamp(color, i16x4(0), i16x4(0xff));
}
#else
i16x4 clamp_9bit_notrunc(ivec4 color)
{
	// [-129, -256] should clamp to 0xff, subtracting by 0x80 will underflow back to positive numbers.
	// [-128, -1] should clamp to 0.
	color -= 0x80;
	// Sign-extend to 9-bit.
	color = bitfieldExtract(color, 0, 9);
	color += 0x80;
	return i16x4(clamp(color, ivec4(0), ivec4(0xff)));
}
#endif

u8x4 clamp_9bit(i16x4 color)
{
	return u8x4(clamp_9bit_notrunc(color));
}

int clamp_9bit(int color)
{
	return clamp(bitfieldExtract(color - 0x80, 0, 9) + 0x80, 0, 0xff);
}

// Returns 18-bit UNORM depth.
int clamp_z(int z)
{
	// Similar to RGBA, we reserve an extra bit to deal with overflow and underflow.
	z -= (1 << 17);
	z <<= (31 - 18);
	z >>= (31 - 18);
	z += (1 << 17);

	// [0x00000, 0x3ffff] maps to self.
	// [0x40000, 0x5ffff] maps to 0x3ffff.
	// [0x60000, 0x7ffff] maps to 0.

	return clamp(z, 0, 0x3ffff);
}

#endif
