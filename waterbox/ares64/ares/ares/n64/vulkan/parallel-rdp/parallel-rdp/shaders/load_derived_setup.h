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

#ifndef LOAD_DERIVED_SETUP_H_
#define LOAD_DERIVED_SETUP_H_

DerivedSetup load_derived_setup(uint index)
{
#if SMALL_TYPES
	return derived_setup.elems[index];
#else
	return DerivedSetup(
			u8x4(derived_setup.elems[index].constant_muladd0),
			u8x4(derived_setup.elems[index].constant_mulsub0),
			u8x4(derived_setup.elems[index].constant_mul0),
			u8x4(derived_setup.elems[index].constant_add0),
			u8x4(derived_setup.elems[index].constant_muladd1),
			u8x4(derived_setup.elems[index].constant_mulsub1),
			u8x4(derived_setup.elems[index].constant_mul1),
			u8x4(derived_setup.elems[index].constant_add1),
			u8x4(derived_setup.elems[index].fog_color),
			u8x4(derived_setup.elems[index].blend_color),
			uint(derived_setup.elems[index].fill_color),
			u16(derived_setup.elems[index].dz),
			u8(derived_setup.elems[index].dz_compressed),
			u8(derived_setup.elems[index].min_lod),
			i16x4(derived_setup.elems[index].factors));
#endif
}

#endif