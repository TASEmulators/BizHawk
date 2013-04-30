/*  src/psp/texcache.h: PSP texture cache management header
    Copyright 2009 Andrew Church

    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

#ifndef PSP_TEXCACHE_H
#define PSP_TEXCACHE_H

#include "../vdp1.h"

/*************************************************************************/

/**
 * texcache_reset:  Reset the texture cache, including all persistent
 * textures.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void texcache_reset(void);

/**
 * texcache_clean:  Clean all transient textures from the texture cache.
 * Persistent textures are not affected.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void texcache_clean(void);

/**
 * texcache_cache_sprite:  Cache the given sprite texture if it has not
 * been cached already.  Returns a texture key for later use in loading the
 * texture.  The texture width must be a multiple of 8.
 *
 * [Parameters]
 *               cmd: VDP1 command structure
 *        pixel_mask: Mask to apply to paletted pixel data
 *     width, height: Size of texture (in pixels)
 *        persistent: Nonzero to cache this texture persistently across
 *                       frames, zero to cache the texture for this frame only
 * [Return value]
 *     Texture key (zero on error)
 */
extern uint32_t texcache_cache_sprite(const vdp1cmd_struct *cmd,
                                      uint16_t pixel_mask,
                                      unsigned int width, unsigned int height,
                                      int persistent);

/**
 * texcache_load_sprite:  Load the sprite texture indicated by the given
 * texture key.
 *
 * [Parameters]
 *     key: Texture key returned by texcache_cache_sprite
 * [Return value]
 *     None
 */
extern void texcache_load_sprite(uint32_t key);

/**
 * texcache_load_tile:  Load the specified 8x8 or 16x16 tile texture into
 * the GE registers for drawing, first caching the texture if necessary.
 *
 * [Parameters]
 *             tilesize: Pixel size (width and height) of tile, either 8 or 16
 *              address: Tile data address within VDP2 RAM
 *               pixfmt: Tile pixel format
 *          transparent: Nonzero if index 0 or alpha 0 should be transparent
 *           color_base: Color table base (for indexed formats)
 *            color_ofs: Color table offset (for indexed formats)
 *     rofs, gofs, bofs: Color offset values for texture
 *           persistent: Nonzero to cache this texture persistently across
 *                          frames, zero to cache the texture for this
 *                          frame only
 * [Return value]
 *     None
 */
extern void texcache_load_tile(int tilesize, uint32_t address,
                               int pixfmt, int transparent,
                               uint16_t color_base, uint16_t color_ofs,
                               int rofs, int gofs, int bofs, int persistent);

/**
 * texcache_load_bitmap:  Load the specified bitmap texture into the GE
 * registers for drawing, first caching the texture if necessary.  The
 * texture width must be a multiple of 8.
 *
 * [Parameters]
 *              address: Bitmap data address within VDP2 RAM
 *        width, height: Size of texture (in pixels)
 *               stride: Line size of source data (in pixels)
 *               pixfmt: Bitmap pixel format
 *          transparent: Nonzero if index 0 or alpha 0 should be transparent
 *           color_base: Color table base (for indexed formats)
 *            color_ofs: Color table offset (for indexed formats)
 *     rofs, gofs, bofs: Color offset values for texture
 *           persistent: Nonzero to cache this texture persistently across
 *                          frames, zero to cache the texture for this
 *                          frame only
 * [Return value]
 *     None
 */
extern void texcache_load_bitmap(uint32_t address, unsigned int width,
                                 unsigned int height, unsigned int stride,
                                 int pixfmt, int transparent,
                                 uint16_t color_base, uint16_t color_ofs,
                                 int rofs, int gofs, int bof, int persistent);

/*************************************************************************/

#endif  // PSP_TEXCACHE_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
