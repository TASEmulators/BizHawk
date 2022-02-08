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
#include "noise.h"

layout(set = 0, binding = 0) uniform mediump utexture2DArray uDivotOutput;
layout(set = 1, binding = 0) uniform mediump utextureBuffer uGammaTable;
layout(location = 0) out vec4 FragColor;

layout(push_constant, std430) uniform Registers
{
    int x_base;
    int y_base;
    int h_offset;
    int v_offset;
    int x_add;
    int y_add;
    int frame_count;

    int serrate_shift;
    int serrate_mask;
    int serrate_select;
} registers;

uvec3 vi_lerp(uvec3 a, uvec3 b, uint l)
{
    return (a + (((b - a) * l + 16u) >> 5u)) & 0xffu;
}

uvec3 integer_gamma(uvec3 color)
{
    uvec3 res;
    if (GAMMA_DITHER)
    {
        color = (color << 6) + noise_get_full_gamma_dither() + 256u;
        res = uvec3(
            texelFetch(uGammaTable, int(color.r)).r,
            texelFetch(uGammaTable, int(color.g)).r,
            texelFetch(uGammaTable, int(color.b)).r);
    }
    else
    {
        res = uvec3(
            texelFetch(uGammaTable, int(color.r)).r,
            texelFetch(uGammaTable, int(color.g)).r,
            texelFetch(uGammaTable, int(color.b)).r);
    }
    return res;
}

layout(constant_id = 2) const bool FETCH_BUG = false;

void main()
{
    ivec2 coord = ivec2(gl_FragCoord.xy) + ivec2(registers.h_offset, registers.v_offset);

    if ((coord.y & registers.serrate_mask) != registers.serrate_select)
        discard;
    coord.y >>= registers.serrate_shift;

    if (GAMMA_DITHER)
        reseed_noise(coord.x, coord.y, registers.frame_count);

    int x = coord.x * registers.x_add + registers.x_base;
    int y = coord.y * registers.y_add + registers.y_base;
    ivec2 base_coord = ivec2(x, y) >> 10;
    uvec3 c00 = texelFetch(uDivotOutput, ivec3(base_coord, 0), 0).rgb;

    int bug_offset = 0;
    if (FETCH_BUG)
    {
        // This is super awkward.
        // Basically there seems to be some kind of issue where if we interpolate in Y,
        // we're going to get buggy output.
        // If we hit this case, the next line we filter against will come from the "buggy" array slice.
        // Why this makes sense, I have no idea.
        int prev_y = (y - registers.y_add) >> 10;
        int next_y = (y + registers.y_add) >> 10;
        if (coord.y != 0 && base_coord.y == prev_y && base_coord.y != next_y)
            bug_offset = 1;
    }

    if (SCALE_AA)
    {
        int x_frac = (x >> 5) & 31;
        int y_frac = (y >> 5) & 31;

        uvec3 c10 = texelFetchOffset(uDivotOutput, ivec3(base_coord, 0), 0, ivec2(1, 0)).rgb;
        uvec3 c01 = texelFetchOffset(uDivotOutput, ivec3(base_coord, bug_offset), 0, ivec2(0, 1)).rgb;
        uvec3 c11 = texelFetchOffset(uDivotOutput, ivec3(base_coord, bug_offset), 0, ivec2(1)).rgb;

        c00 = vi_lerp(c00, c01, y_frac);
        c10 = vi_lerp(c10, c11, y_frac);
        c00 = vi_lerp(c00, c10, x_frac);
    }

    if (GAMMA_ENABLE)
        c00 = integer_gamma(c00);
    else if (GAMMA_DITHER)
        c00 = min(c00 + noise_get_partial_gamma_dither(), uvec3(0xff));

    FragColor = vec4(vec3(c00) / 255.0, 1.0);
}