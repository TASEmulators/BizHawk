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

#ifndef LOAD_SPAN_SETUP_H_
#define LOAD_SPAN_SETUP_H_

SpanSetup load_span_setup(uint index)
{
#if SMALL_TYPES
	return span_setups.elems[index];
#else
	return SpanSetup(
			span_setups.elems[index].rgba,
			span_setups.elems[index].stzw,
			u16x4(uvec4(span_setups.elems[index].xleft)),
			u16x4(uvec4(span_setups.elems[index].xright)),
			span_setups.elems[index].interpolation_base_x,
			span_setups.elems[index].start_x,
			span_setups.elems[index].end_x,
			i16(span_setups.elems[index].lodlength),
			u16(span_setups.elems[index].valid_line));
#endif
}

#endif