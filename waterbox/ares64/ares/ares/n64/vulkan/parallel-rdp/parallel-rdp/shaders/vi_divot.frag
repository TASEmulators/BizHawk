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

#include "vi_debug.h"

layout(location = 0) out uvec4 FragColor;
#if defined(FETCH_BUG) && FETCH_BUG
layout(location = 1) out uvec4 FragColorFetchBug;
#endif

layout(set = 0, binding = 0) uniform mediump utexture2DArray uFetchCache;

void swap(inout uint a, inout uint b)
{
    uint tmp = a;
    a = b;
    b = tmp;
}

uint median3(uint left, uint center, uint right)
{
    if (left < center)
        swap(left, center);
    if (center < right)
        swap(center, right);
    if (left < center)
        swap(left, center);

    return center;
}

void main()
{
    ivec2 pix = ivec2(gl_FragCoord.xy);

    uvec4 left = texelFetch(uFetchCache, ivec3(pix, 0), 0);
    uvec4 mid = texelFetchOffset(uFetchCache, ivec3(pix, 0), 0, ivec2(1, 0));
    uvec4 right = texelFetchOffset(uFetchCache, ivec3(pix, 0), 0, ivec2(2, 0));

    if ((left.a & mid.a & right.a) == 7u)
    {
        FragColor = mid;
    }
    else
    {
        // Median filter. TODO: Optimize with mid3?
        uint r = median3(left.r, mid.r, right.r);
        uint g = median3(left.g, mid.g, right.g);
        uint b = median3(left.b, mid.b, right.b);
        FragColor = uvec4(r, g, b, mid.a);
    }

#if defined(FETCH_BUG) && FETCH_BUG
    left = texelFetch(uFetchCache, ivec3(pix, 1), 0);
    mid = texelFetchOffset(uFetchCache, ivec3(pix, 1), 0, ivec2(1, 0));
    right = texelFetchOffset(uFetchCache, ivec3(pix, 1), 0, ivec2(2, 0));

    if ((left.a & mid.a & right.a) == 7u)
    {
        FragColorFetchBug = mid;
    }
    else
    {
        // Median filter. TODO: Optimize with mid3?
        uint r = median3(left.r, mid.r, right.r);
        uint g = median3(left.g, mid.g, right.g);
        uint b = median3(left.b, mid.b, right.b);
        FragColorFetchBug = uvec4(r, g, b, mid.a);
    }
#endif
}