#version 450
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
#extension GL_EXT_samplerless_texture_functions : require
#include "small_types.h"
#include "vi_status.h"
#include "vi_debug.h"

layout(set = 0, binding = 0) uniform mediump utexture2D uAAInput;

layout(location = 0) out uvec4 FragColor;
#if defined(FETCH_BUG) && FETCH_BUG
layout(location = 1) out uvec4 FragColorFetchBug;
#endif

layout(push_constant) uniform Registers
{
    ivec2 offset;
} registers;

ivec2 pix;
uvec4 fetch_color_offset(ivec2 offset)
{
    return texelFetch(uAAInput, pix + offset, 0);
}

void check_neighbor(uvec4 candidate,
                    inout uvec3 lo, inout uvec3 hi,
                    inout uvec3 second_lo, inout uvec3 second_hi)
{
    if (candidate.a == 7u)
    {
        second_lo = min(second_lo, max(candidate.rgb, lo));
        second_hi = max(second_hi, min(candidate.rgb, hi));

        lo = min(candidate.rgb, lo);
        hi = max(candidate.rgb, hi);
    }
}

void main()
{
    pix = ivec2(gl_FragCoord.xy) + registers.offset;

    uvec4 mid_pixel = fetch_color_offset(ivec2(0));

    // AA-filter. If coverage is not full, we blend current pixel against background.
    uvec3 color;
#if defined(FETCH_BUG) && FETCH_BUG
    uvec3 color_bug;
#endif

    if (mid_pixel.a != 7u)
    {
        uvec3 lo = mid_pixel.rgb;
        uvec3 hi = lo;
        uvec3 second_lo = lo;
        uvec3 second_hi = lo;

        // Somehow, we're supposed to find the second lowest and second highest neighbor.
        uvec4 left_up = fetch_color_offset(ivec2(-1, -1));
        uvec4 right_up = fetch_color_offset(ivec2(+1, -1));
        uvec4 to_left = fetch_color_offset(ivec2(-2, 0));
        uvec4 to_right = fetch_color_offset(ivec2(+2, 0));
        uvec4 left_down = fetch_color_offset(ivec2(-1, +1));
        uvec4 right_down = fetch_color_offset(ivec2(+1, +1));

        check_neighbor(left_up, lo, hi, second_lo, second_hi);
        check_neighbor(right_up, lo, hi, second_lo, second_hi);
        check_neighbor(to_left, lo, hi, second_lo, second_hi);
        check_neighbor(to_right, lo, hi, second_lo, second_hi);

#if defined(FETCH_BUG) && FETCH_BUG
        // In the fetch-bug state, we apparently do not read the lower values.
        // Instead, the lower values are treated as left and right.
        uvec3 lo_bug = lo;
        uvec3 hi_bug = hi;
        uvec3 second_lo_bug = second_lo;
        uvec3 second_hi_bug = second_hi;
#endif

        check_neighbor(left_down, lo, hi, second_lo, second_hi);
        check_neighbor(right_down, lo, hi, second_lo, second_hi);
#if defined(FETCH_BUG) && FETCH_BUG
        check_neighbor(to_left, lo_bug, hi_bug, second_lo_bug, second_hi_bug);
        check_neighbor(to_right, lo_bug, hi_bug, second_lo_bug, second_hi_bug);
        second_lo = mix(second_lo, lo, equal(mid_pixel.rgb, lo));
        second_hi = mix(second_hi, hi, equal(mid_pixel.rgb, hi));
        second_lo_bug = mix(second_lo_bug, lo_bug, equal(mid_pixel.rgb, lo_bug));
        second_hi_bug = mix(second_hi_bug, hi_bug, equal(mid_pixel.rgb, hi_bug));
#endif

        uvec3 offset = second_lo + second_hi - (mid_pixel.rgb << 1u);
        uint coeff = 7u - mid_pixel.a;
        color = mid_pixel.rgb + (((offset * coeff) + 4u) >> 3u);
        color &= 0xffu;

#if defined(FETCH_BUG) && FETCH_BUG
        uvec3 offset_bug = second_lo_bug + second_hi_bug - (mid_pixel.rgb << 1u);
        color_bug = mid_pixel.rgb + (((offset_bug * coeff) + 4u) >> 3u);
        color_bug &= 0xffu;
#endif
    }
    else if (DITHER_ENABLE)
    {
        // Dither filter.
        ivec3 tmp_color = ivec3(mid_pixel.rgb >> 3u);
        ivec3 tmp_accum = ivec3(0);
        for (int y = -1; y <= 0; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                ivec3 col = ivec3(fetch_color_offset(ivec2(x, y)).rgb >> 3u);
                tmp_accum += clamp(col - tmp_color, ivec3(-1), ivec3(1));
            }
        }

#if defined(FETCH_BUG) && FETCH_BUG
        ivec3 tmp_accum_bug = tmp_accum;
#endif

        tmp_accum += clamp(ivec3(fetch_color_offset(ivec2(-1, 1)).rgb >> 3u) - tmp_color, ivec3(-1), ivec3(1));
        tmp_accum += clamp(ivec3(fetch_color_offset(ivec2(+1, 1)).rgb >> 3u) - tmp_color, ivec3(-1), ivec3(1));
        tmp_accum += clamp(ivec3(fetch_color_offset(ivec2(0, 1)).rgb >> 3u) - tmp_color, ivec3(-1), ivec3(1));
        color = (mid_pixel.rgb & 0xf8u) + tmp_accum;

#if defined(FETCH_BUG) && FETCH_BUG
        tmp_accum_bug += clamp(ivec3(fetch_color_offset(ivec2(-1, 0)).rgb >> 3u) - tmp_color, ivec3(-1), ivec3(1));
        tmp_accum_bug += clamp(ivec3(fetch_color_offset(ivec2(+1, 0)).rgb >> 3u) - tmp_color, ivec3(-1), ivec3(1));
        color_bug = (mid_pixel.rgb & 0xf8u) + tmp_accum_bug;
#endif
    }
    else
    {
        color = mid_pixel.rgb;
#if defined(FETCH_BUG) && FETCH_BUG
        color_bug = mid_pixel.rgb;
#endif
    }

    FragColor = uvec4(color, mid_pixel.a);
#if defined(FETCH_BUG) && FETCH_BUG
    FragColorFetchBug = uvec4(color_bug, mid_pixel.a);
#endif
}