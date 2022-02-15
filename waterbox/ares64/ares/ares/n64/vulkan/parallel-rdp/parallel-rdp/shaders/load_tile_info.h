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

#ifndef LOAD_TILE_INFO_H_
#define LOAD_TILE_INFO_H_

TileInfo load_tile_info(uint index)
{
#if SMALL_TYPES
	return tile_infos.elems[index];
#else
	return TileInfo(
			tile_infos.elems[index].slo,
			tile_infos.elems[index].shi,
			tile_infos.elems[index].tlo,
			tile_infos.elems[index].thi,
			tile_infos.elems[index].offset,
			tile_infos.elems[index].stride,
			u8(tile_infos.elems[index].fmt),
			u8(tile_infos.elems[index].size),
			u8(tile_infos.elems[index].palette),
			u8(tile_infos.elems[index].mask_s),
			u8(tile_infos.elems[index].shift_s),
			u8(tile_infos.elems[index].mask_t),
			u8(tile_infos.elems[index].shift_t),
			u8(tile_infos.elems[index].flags));
#endif
}

#endif