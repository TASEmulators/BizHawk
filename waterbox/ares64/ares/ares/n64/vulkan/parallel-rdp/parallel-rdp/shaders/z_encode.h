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

#ifndef Z_ENCODE_H_
#define Z_ENCODE_H_

// The Z compression is kind of clever, and uses inverted FP, with more precision close to 1.
// The compressed Z result is 14 bits, and decompresses to 18-bit UNORM.
int z_decompress(u16 z_)
{
	int z = int(z_);
	int exponent = z >> 11;
	int mantissa = z & 0x7ff;
	int shift = max(6 - exponent, 0);
	int base = 0x40000 - (0x40000 >> exponent);
	return (mantissa << shift) + base;
}

u16 z_compress(int z)
{
	int inv_z = max(0x3ffff - z, 1);
	int exponent = 17 - findMSB(inv_z);
	exponent = clamp(exponent, 0, 7);
	int shift = max(6 - exponent, 0);
	int mantissa = (z >> shift) & 0x7ff;
	return u16((exponent << 11) + mantissa);
}

int dz_decompress(int dz)
{
	return 1 << dz;
}

int dz_compress(int dz)
{
	return max(findMSB(dz), 0);
}

#endif