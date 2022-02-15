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

#ifndef LOAD_STATIC_RASTER_STATE_H_
#define LOAD_STATIC_RASTER_STATE_H_

StaticRasterizationState load_static_rasterization_state(uint index)
{
#if SMALL_TYPES
	return static_raster_state.elems[index];
#else
	return StaticRasterizationState(
			u8x4(static_raster_state.elems[index].combiner_inputs_rgb0),
			u8x4(static_raster_state.elems[index].combiner_inputs_alpha0),
			u8x4(static_raster_state.elems[index].combiner_inputs_rgb1),
			u8x4(static_raster_state.elems[index].combiner_inputs_alpha1),
			static_raster_state.elems[index].flags,
			static_raster_state.elems[index].dither,
			0, 0);
#endif
}

#endif