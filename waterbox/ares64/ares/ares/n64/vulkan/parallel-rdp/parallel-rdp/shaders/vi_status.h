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

#ifndef VI_STATUS_H_
#define VI_STATUS_H_

layout(constant_id = 1) const int VI_STATUS = 0;
const int VI_CONTROL_TYPE_BLANK_BIT = 0 << 0;
const int VI_CONTROL_TYPE_RESERVED_BIT = 1 << 0;
const int VI_CONTROL_TYPE_RGBA5551_BIT = 2 << 0;
const int VI_CONTROL_TYPE_RGBA8888_BIT = 3 << 0;
const int VI_CONTROL_TYPE_MASK = 3 << 0;
const int VI_CONTROL_GAMMA_DITHER_ENABLE_BIT = 1 << 2;
const int VI_CONTROL_GAMMA_ENABLE_BIT = 1 << 3;
const int VI_CONTROL_DIVOT_ENABLE_BIT = 1 << 4;
const int VI_CONTROL_SERRATE_BIT = 1 << 6;
const int VI_CONTROL_DITHER_FILTER_ENABLE_BIT = 1 << 16;
const int VI_CONTROL_META_AA_BIT = 1 << 17;
const int VI_CONTROL_META_SCALE_BIT = 1 << 18;

const bool FMT_RGBA5551 = (VI_STATUS & VI_CONTROL_TYPE_MASK) == VI_CONTROL_TYPE_RGBA5551_BIT;
const bool FMT_RGBA8888 = (VI_STATUS & VI_CONTROL_TYPE_MASK) == VI_CONTROL_TYPE_RGBA8888_BIT;
const bool DITHER_ENABLE = (VI_STATUS & VI_CONTROL_DITHER_FILTER_ENABLE_BIT) != 0;
const bool FETCH_AA = (VI_STATUS & VI_CONTROL_META_AA_BIT) != 0;
const bool SCALE_AA = (VI_STATUS & VI_CONTROL_META_SCALE_BIT) != 0;
const bool GAMMA_ENABLE = (VI_STATUS & VI_CONTROL_GAMMA_ENABLE_BIT) != 0;
const bool GAMMA_DITHER = (VI_STATUS & VI_CONTROL_GAMMA_DITHER_ENABLE_BIT) != 0;

#endif