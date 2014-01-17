/*  src/psp/texcache.c: PSP texture cache management
    Copyright 2009-2010 Andrew Church

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

#include "common.h"

#include "../vdp1.h"
#include "../vdp2.h"

#include "display.h"
#include "gu.h"
#include "psp-video.h"
#include "psp-video-internal.h"
#include "texcache.h"

/*************************************************************************/
/************************* Configuration options *************************/
/*************************************************************************/

/**
 * TEXCACHE_SIZE:  Number of entries in the texture cache.  This value does
 * not have any direct impact on processing speed, but each entry requires
 * 28 bytes of memory.
 *
 * Note that a single background layer at maximum resolution (704x512)
 * using 8x8 tiles requires (88+2)*(64+2) = 5940 textures.
 */
#ifndef TEXCACHE_SIZE
# define TEXCACHE_SIZE 30000
#endif

/**
 * TEXCACHE_HASH_SIZE:  Number of hash table slots in the texture cache.
 * A larger value increases the fixed setup time for each frame, but can
 * reduce time consumed in traversing hash table collision chains.  This
 * value should always be a prime number.
 */
#ifndef TEXCACHE_HASH_SIZE
# define TEXCACHE_HASH_SIZE 1009
#endif

/**
 * TEXCACHE_PERSISTENT_CACHE_SIZE:  Size of the persistent cache for pixel
 * data, in bytes.
 */
#ifndef TEXCACHE_PERSISTENT_CACHE_SIZE
# define TEXCACHE_PERSISTENT_CACHE_SIZE 1048576
#endif

/**
 * CLUT_ENTRIES:  Number of entries per CLUT (color lookup table) cache
 * slot.  The CLUT cache stores generated tables in the slot designated by
 * the upper 7 bits of the base color index; this value sets how many
 * different CLUTs (i.e. with different color offsets, transparency flags,
 * or color_ofs values) will be cached for any given base color index.
 *
 * If all entries for a given CLUT cache slot are filled, any color set
 * that does not match a cached set will cause a new CLUT to be generated
 * for each texture using that color set.
 */
#ifndef CLUT_ENTRIES
# define CLUT_ENTRIES 4
#endif

/*************************************************************************/
/****************************** Local data *******************************/
/*************************************************************************/

/* Structure for cached texture data */

typedef struct TexInfo_ TexInfo;
struct TexInfo_ {
    TexInfo *next;      // Collision chain pointer
    uint32_t key;       // Texture key value (HASHKEY_TEXTURE_*(...))
    void *vram_address; // Address of texture in VRAM
    void *clut_address; // Address of color table in VRAM (NULL if none)
    uint16_t clut_size; // Number of entries in CLUT table
    uint8_t clut_dynamic; // Do we need to regenerate the CLUT every frame?
                          // (persistent textures only)
    uint16_t stride;    // Line length in pixels
    uint16_t pixfmt;    // Texture pixel format (GU_PSM_*)
    uint16_t width;     // Texture width (always a power of 2)
    uint16_t height;    // Texture height (always a power of 2)
};

/* Structure for cached color lookup table data */

typedef struct CLUTInfo_ CLUTInfo;
struct CLUTInfo_ {
    uint16_t size;      // Number of colors (0 = unused entry)
    uint8_t color_ofs;  // color_ofs value ORed into pixel
    uint32_t color_set; // (rofs&0x1FF)<<0 | (gofs&0x1FF)<<9 | (bofs&0x1FF)<<18
                        // | (transparent ? 1<<27 : 0)
    void *clut_address; // CLUT data pointer
};

/*************************************************************************/

/* Pixel data buffer for persistent textures and next free offset */
static __attribute__((aligned(64)))
    uint8_t pixdata_cache[TEXCACHE_PERSISTENT_CACHE_SIZE];
static uint32_t pixdata_cache_next_alloc;

/* Cached texture hash tables (one for persistent and one for transient
 * textures) and associated data buffer */
static TexInfo *tex_table_persistent[TEXCACHE_HASH_SIZE];
static TexInfo *tex_table_transient[TEXCACHE_HASH_SIZE];
static TexInfo tex_buffer[TEXCACHE_SIZE];

/* Next free entry for persistent and transient caching; persistent
 * textures are allocated from the front of the array, transient textures
 * from the back */ 
static int tex_buffer_nextfree_persistent;
static int tex_buffer_nextfree_transient;

/* Cached color lookup tables, indexed by upper 7 bits of global color number*/
static CLUTInfo clut_cache[128][CLUT_ENTRIES];

/* Hash functions */
#define HASHKEY_TEXTURE_SPRITE(CMDSRCA,CMDPMOD,CMDCOLR) \
    ((CMDSRCA) | ((CMDPMOD>>3 & 7) >= 5 ? 0 : (CMDCOLR&0x7FF)<<16) \
               | ((CMDPMOD>>3 & 15) << 27) | 0x80000000)
#define HASHKEY_TEXTURE_TILE(tilesize,address,pixfmt,color_base,color_ofs,transparent) \
    (tilesize>>4 | (address>>3 & ~1) \
                 | (pixfmt >= 3 ? 0 : ((color_ofs|color_base)&0x7FF)<<16) \
                 | (pixfmt<<27) | (transparent<<30))
#define HASH_TEXTURE(key)  ((key) % TEXCACHE_HASH_SIZE)

/*************************************************************************/

/* Local routine declarations */

static TexInfo *alloc_texture(uint32_t key, uint32_t hash, int persistent);
static TexInfo *find_texture(uint32_t key, unsigned int *hash_ret);
static void load_texture(const TexInfo *tex);
static void *alloc_pixdata(uint32_t size);
static void *alloc_vram(uint32_t size);

/* Force GCC to inline the texture/CLUT caching functions, to save the
 * expense of register save/restore and optimize constant cases
 * (particularly for tile loading). */

__attribute__((always_inline)) static inline int cache_sprite(
    TexInfo *tex, uint16_t CMDSRCA, uint16_t CMDPMOD, uint16_t CMDCOLR,
    uint16_t pixel_mask, unsigned int width, unsigned int height, int rofs,
    int gofs, int bofs, int persistent);
__attribute__((always_inline)) static inline int cache_tile(
    TexInfo *tex, uint32_t address, unsigned int width, unsigned int height,
    unsigned int stride, int array, int pixfmt, int transparent,
    uint16_t color_base, uint16_t color_ofs, int rofs, int gofs, int bofs,
    int persistent);

__attribute__((always_inline)) static inline int alloc_texture_pixels(
    TexInfo *tex, unsigned int width, unsigned int height, unsigned int psm,
    int persistent);

__attribute__((always_inline)) static inline int gen_clut(
    TexInfo *tex, unsigned int size, uint32_t color_base, uint8_t color_ofs,
    int rofs, int gofs, int bofs, int transparent, int endcodes,
    int can_shadow, int persistent);
__attribute__((always_inline)) static inline int gen_clut_t4ind(
    TexInfo *tex, const uint8_t *color_lut, uint16_t pixel_mask,
    int rofs, int gofs, int bofs, int transparent, int endcodes,
    int persistent);

__attribute__((always_inline)) static inline int cache_texture_t4(
    TexInfo *tex, const uint8_t *src,
    unsigned int width, unsigned int height, unsigned int stride);
__attribute__((always_inline)) static inline int cache_texture_t8(
    TexInfo *tex, const uint8_t *src, uint8_t pixmask,
    unsigned int width, unsigned int height, unsigned int stride);
__attribute__((always_inline)) static inline int cache_texture_t16(
    TexInfo *tex, const uint8_t *src, uint32_t color_base,
    unsigned int width, unsigned int height, unsigned int stride,
    int rofs, int gofs, int bofs, int transparent);
__attribute__((always_inline)) static inline int cache_texture_16(
    TexInfo *tex, const uint8_t *src,
    unsigned int width, unsigned int height, unsigned int stride,
    int rofs, int gofs, int bofs, int transparent);
__attribute__((always_inline)) static inline int cache_texture_32(
    TexInfo *tex, const uint8_t *src,
    unsigned int width, unsigned int height, unsigned int stride,
    int rofs, int gofs, int bofs, int transparent);

/*************************************************************************/
/************************** Interface routines ***************************/
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
void texcache_reset(void)
{
    memset(tex_table_persistent, 0, sizeof(tex_table_persistent));
    memset(tex_table_transient, 0, sizeof(tex_table_transient));
    tex_buffer_nextfree_persistent = 0;
    tex_buffer_nextfree_transient = lenof(tex_buffer) - 1;
    pixdata_cache_next_alloc = 0;

    unsigned int i;
    for (i = 0; i < lenof(clut_cache); i++) {
        unsigned int j;
        for (j = 0; j < lenof(clut_cache[i]); j++) {
            clut_cache[i][j].size = 0;
        }
    }
}

/*-----------------------------------------------------------------------*/

/**
 * texcache_clean:  Clean all transient textures from the texture cache.
 * Persistent textures are not affected.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void texcache_clean(void)
{
    memset(tex_table_transient, 0, sizeof(tex_table_transient));
    tex_buffer_nextfree_transient = lenof(tex_buffer) - 1;

    unsigned int i;
    for (i = 0; i < lenof(clut_cache); i++) {
        unsigned int j;
        for (j = 0; j < lenof(clut_cache[i]); j++) {
            clut_cache[i][j].size = 0;
        }
    }
}

/*************************************************************************/

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
uint32_t texcache_cache_sprite(const vdp1cmd_struct *cmd,
                               uint16_t pixel_mask,
                               unsigned int width, unsigned int height,
                               int persistent)
{
    const uint32_t tex_key =
        HASHKEY_TEXTURE_SPRITE(cmd->CMDSRCA, cmd->CMDPMOD, cmd->CMDCOLR);
    uint32_t tex_hash;
    TexInfo *tex = find_texture(tex_key, &tex_hash);

    if (tex) {
        /* Already cached, so just generate a CLUT if needed and return the
         * texture key. */
        if (tex->clut_dynamic) {
            const int transparent = !(cmd->CMDPMOD & 0x40);
            const int endcodes = !(cmd->CMDPMOD & 0x80);
            const uint32_t color_base = (Vdp2Regs->CRAOFB & 0x0070) << 4;
            switch (cmd->CMDPMOD>>3 & 7) {
              case 0:
                gen_clut(tex, 16, (color_base + cmd->CMDCOLR) & 0x7F0,
                         cmd->CMDCOLR & 0x00F, vdp1_rofs, vdp1_gofs, vdp1_bofs,
                         transparent, endcodes,
                         ((cmd->CMDCOLR | 0xF) & pixel_mask) == pixel_mask, 0);
                break;
              case 1:
                gen_clut_t4ind(tex, Vdp1Ram + (cmd->CMDCOLR << 3), pixel_mask,
                               vdp1_rofs, vdp1_gofs, vdp1_bofs,
                               transparent, endcodes, 0);
                break;
              case 2:
                gen_clut(tex, 64, (color_base + cmd->CMDCOLR) & 0x7C0,
                         cmd->CMDCOLR & 0x03F, vdp1_rofs, vdp1_gofs, vdp1_bofs,
                         transparent, endcodes,
                         ((cmd->CMDCOLR | 0x3F) & pixel_mask) == pixel_mask, 0);
                break;
              case 3:
                gen_clut(tex, 128,
                         (color_base + cmd->CMDCOLR) & 0x780,
                         cmd->CMDCOLR & 0x07F, vdp1_rofs, vdp1_gofs, vdp1_bofs,
                         transparent, endcodes,
                         ((cmd->CMDCOLR | 0x7F) & pixel_mask) == pixel_mask, 0);
                break;
              case 4:
                gen_clut(tex, 256, (color_base + cmd->CMDCOLR) & 0x700,
                         cmd->CMDCOLR & 0x0FF, vdp1_rofs, vdp1_gofs, vdp1_bofs,
                         transparent, endcodes,
                         ((cmd->CMDCOLR | 0xFF) & pixel_mask) == pixel_mask, 0);
                break;
            }
        }
        return tex_key;
    }  // if (tex)

    tex = alloc_texture(tex_key, tex_hash, persistent);
    if (UNLIKELY(!tex)) {
        DMSG("Texture buffer full, can't cache");
        return 0;
    }
    if (UNLIKELY(!cache_sprite(tex, cmd->CMDSRCA, cmd->CMDPMOD, cmd->CMDCOLR,
                               pixel_mask, width, height,
                               vdp1_rofs, vdp1_gofs, vdp1_bofs, persistent))) {
        if (persistent) {
            tex_table_persistent[tex_hash] = tex->next;
            tex_buffer_nextfree_persistent--;
            return texcache_cache_sprite(cmd, pixel_mask, width, height, 0);
        } else {
            return 0;
        }
    }

    return tex_key;
}

/*-----------------------------------------------------------------------*/

/**
 * texcache_load_sprite:  Load the sprite texture indicated by the given
 * texture key.
 *
 * [Parameters]
 *     key: Texture key returned by texcache_cache_sprite
 * [Return value]
 *     None
 */
void texcache_load_sprite(uint32_t key)
{
    TexInfo *tex = find_texture(key, NULL);
    if (UNLIKELY(!tex)) {
        DMSG("No texture found for key %08X", (int)key);
        return;
    }

    load_texture(tex);
}

/*-----------------------------------------------------------------------*/

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
void texcache_load_tile(int tilesize, uint32_t address,
                        int pixfmt, int transparent,
                        uint16_t color_base, uint16_t color_ofs,
                        int rofs, int gofs, int bofs, int persistent)
{
    const uint32_t tex_key =
        HASHKEY_TEXTURE_TILE(tilesize, address, pixfmt, color_base,
                             color_ofs, transparent);
    uint32_t tex_hash;
    TexInfo *tex = find_texture(tex_key, &tex_hash);

    if (tex) {
        if (tex->clut_dynamic) {
            if (pixfmt == 0) {
                gen_clut(tex, 16,
                         (color_base + color_ofs) & 0x7F0, color_ofs & 0x00F,
                         rofs, gofs, bofs, transparent, 0, 0, 0);
            } else {  // pixfmt == 1
                gen_clut(tex, 256,
                         (color_base + color_ofs) & 0x700, color_ofs & 0x0FF,
                         rofs, gofs, bofs, transparent, 0, 0, 0);
            }
        }
    } else {  // !tex
        tex = alloc_texture(tex_key, tex_hash, persistent);
        if (UNLIKELY(!tex)) {
            DMSG("Texture buffer full, can't cache");
            return;
        }
        int ok;
        if (tilesize == 16) {
            ok = cache_tile(tex, address, 16, 16, 8, 2, pixfmt,
                            transparent, color_base, color_ofs,
                            rofs, gofs, bofs, persistent);
        } else {
            ok = cache_tile(tex, address, 8, 8, 8, 1, pixfmt,
                            transparent, color_base, color_ofs,
                            rofs, gofs, bofs, persistent);
        }
        if (UNLIKELY(!ok)) {
            if (persistent) {
                tex_table_persistent[tex_hash] = tex->next;
                tex_buffer_nextfree_persistent--;
                return texcache_load_tile(tilesize, address, pixfmt,
                                          transparent, color_base, color_ofs,
                                          rofs, gofs, bofs, 0);
            } else {
                return;
            }
        }
    }  // if (tex)

    /* Load the texture data (and color table, if appropriate). */
    load_texture(tex);
}

/*-----------------------------------------------------------------------*/

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
void texcache_load_bitmap(uint32_t address, unsigned int width,
                          unsigned int height, unsigned int stride,
                          int pixfmt, int transparent,
                          uint16_t color_base, uint16_t color_ofs,
                          int rofs, int gofs, int bofs, int persistent)
{
    const uint32_t tex_key =
        HASHKEY_TEXTURE_TILE(8, address, pixfmt, color_base,
                             color_ofs, transparent);
    uint32_t tex_hash;
    TexInfo *tex = find_texture(tex_key, &tex_hash);

    if (tex) {
        if (tex->clut_dynamic) {
            if (pixfmt == 0) {
                gen_clut(tex, 16,
                         (color_base + color_ofs) & 0x7F0, color_ofs & 0x00F,
                         rofs, gofs, bofs, transparent, 0, 0, 0);
            } else {  // pixfmt == 1
                gen_clut(tex, 256,
                         (color_base + color_ofs) & 0x700, color_ofs & 0x0FF,
                         rofs, gofs, bofs, transparent, 0, 0, 0);
            }
        }
    } else {  // !tex
        tex = alloc_texture(tex_key, tex_hash, persistent);
        if (UNLIKELY(!tex)) {
            DMSG("Texture buffer full, can't cache");
            return;
        }
        if (UNLIKELY(!cache_tile(tex, address, width, height, stride, 1,
                                 pixfmt, transparent, color_base, color_ofs,
                                 rofs, gofs, bofs, persistent))) {
            if (persistent) {
                tex_table_persistent[tex_hash] = tex->next;
                tex_buffer_nextfree_persistent--;
                return texcache_load_bitmap(address, width, height, stride,
                                            pixfmt, transparent, color_base,
                                            color_ofs, rofs, gofs, bofs, 0);
            } else {
                return;
            }
        }
    }  // if (!tex)

    /* Load the texture data (and color table, if appropriate). */
    load_texture(tex);
}

/*************************************************************************/
/**************************** Local routines *****************************/
/*************************************************************************/

/**
 * alloc_texture:  Allocate a new texture entry and insert it into the hash
 * table.  The entry's data fields (other than those for hash table
 * management) are _not_ initialized.
 *
 * [Parameters]
 *            key: Hash key for texture
 *           hash: Hash index for texture (== HASH_TEXTURE(key), passed in
 *                    separately so precomputed values can be reused)
 *     persistent: Nonzero if a persistent texture, zero if a transient texture
 * [Return value]
 *     Allocated texture entry, or NULL on failure (table full)
 */
static TexInfo *alloc_texture(uint32_t key, uint32_t hash, int persistent)
{
    if (persistent) {
        if (UNLIKELY(tex_buffer_nextfree_persistent > tex_buffer_nextfree_transient)) {
            return NULL;
        }
        TexInfo *tex = &tex_buffer[tex_buffer_nextfree_persistent++];
        tex->next = tex_table_persistent[hash];
        tex_table_persistent[hash] = tex;
        tex->key = key;
        return tex;
    } else {
        if (UNLIKELY(tex_buffer_nextfree_transient < tex_buffer_nextfree_persistent)) {
            return NULL;
        }
        TexInfo *tex = &tex_buffer[tex_buffer_nextfree_transient--];
        tex->next = tex_table_transient[hash];
        tex_table_transient[hash] = tex;
        tex->key = key;
        return tex;
    }
}

/*-----------------------------------------------------------------------*/

/**
 * find_texture:  Return the texture corresponding to the given texture
 * key, or NULL if no such texture is cached.
 *
 * [Parameters]
 *          key: Texture hash key
 *     hash_ret: Pointer to variable to receive hash index, or NULL if unneeded
 * [Return value]
 *     Cached texture, or NULL if none
 */
static TexInfo *find_texture(uint32_t key, unsigned int *hash_ret)
{
    const uint32_t hash = HASH_TEXTURE(key);
    if (hash_ret) {
        *hash_ret = hash;
    }

    TexInfo *tex;
    for (tex = tex_table_persistent[hash]; tex != NULL; tex = tex->next) {
        if (tex->key == key) {
            return tex;
        }
    }
    for (tex = tex_table_transient[hash]; tex != NULL; tex = tex->next) {
        if (tex->key == key) {
            return tex;
        }
    }
    return NULL;
}

/*-----------------------------------------------------------------------*/

/**
 * load_texture:  Load the given texture into the GE texture registers.
 *
 * [Parameters]
 *     tex: Texture to load
 * [Return value]
 *     None
 */
static void load_texture(const TexInfo *tex)
{
    if (tex->clut_address) {
        guClutMode(GU_PSM_8888, 0, tex->clut_size-1, 0);
        guClutLoad(tex->clut_size/8, tex->clut_address);
    }
    guTexFlush();
    guTexMode(tex->pixfmt, 0, 0, /*swizzled*/ 1);
    guTexImage(0, tex->width, tex->height, tex->stride, tex->vram_address);
}

/*************************************************************************/

/**
 * alloc_pixdata:  Allocate memory from the persistent pixel data cache
 * buffer, returning a pointer to the area in the uncached address region
 * (0x4nnnnnnn).
 *
 * [Parameters]
 *     size: Amount of memory to allocate, in bytes
 * [Return value]
 *     Pointer to allocated memory, or NULL on failure (out of memory)
 */
static void *alloc_pixdata(uint32_t size)
{
    if (UNLIKELY(pixdata_cache_next_alloc + size > sizeof(pixdata_cache))) {
        return NULL;
    }
    void *ptr = &pixdata_cache[pixdata_cache_next_alloc];
    pixdata_cache_next_alloc += (size + 63) & -64;
    return (void *)((uint32_t)ptr | 0x40000000);
}

/*-----------------------------------------------------------------------*/

/**
 * alloc_vram:  Allocate memory from the spare VRAM area, returning a
 * pointer to the area in the uncached address region (0x4nnnnnnn).
 *
 * [Parameters]
 *     size: Amount of memory to allocate, in bytes
 * [Return value]
 *     Pointer to allocated memory, or NULL on failure (out of memory)
 */
static void *alloc_vram(uint32_t size)
{
    void *ptr = display_alloc_vram(size);
    if (UNLIKELY(!ptr)) {
        return NULL;
    }
    return (void *)((uint32_t)ptr | 0x40000000);
}

/*************************************************************************/

/**
 * cache_sprite:  Cache a texture from sprite (VDP1) memory.  Implements
 * caching code for texcache_cache_sprite().
 *
 * [Parameters]
 *                           tex: TexInfo structure for caching texture
 *     CMDSRCA, CMDPMOD, CMDCOLR: Values of like-named fields in VDP1 command
 *                    pixel_mask: Mask to apply to paletted pixel data
 *                 width, height: Size of texture (in pixels)
 *              rofs, gofs, bofs: Color offset values for texture
 *                    persistent: Nonzero if a persistent texture, zero if a
 *                                   transient texture
 * [Return value]
 *     Nonzero on success, zero on error
 */
static inline int cache_sprite(
    TexInfo *tex, uint16_t CMDSRCA, uint16_t CMDPMOD, uint16_t CMDCOLR,
    uint16_t pixel_mask, unsigned int width, unsigned int height, int rofs,
    int gofs, int bofs, int persistent)
{
    const uint32_t address = CMDSRCA << 3;
    const int pixfmt = CMDPMOD>>3 & 7;
    const int transparent = !(CMDPMOD & 0x40);
    const int endcodes = !(CMDPMOD & 0x80);
    uint32_t color_base = (Vdp2Regs->CRAOFB & 0x0070) << 4;

    if (UNLIKELY(address + (pixfmt<=1 ? width/2 : pixfmt<=4 ? width : width*2) * height > 0x80000)) {
        DMSG("%dx%d texture at 0x%X extends past end of VDP1 RAM"
             " and will be incorrectly drawn", width, height, address);
    }

    const uint8_t *src = Vdp1Ram + address;
    switch (pixfmt) {
      case 0:
        tex->clut_dynamic = persistent;
        CMDCOLR &= pixel_mask;
        return gen_clut(tex, 16,
                        (color_base + CMDCOLR) & 0x7F0, CMDCOLR & 0x00F,
                        rofs, gofs, bofs, transparent, endcodes,
                        ((CMDCOLR | 0xF) & pixel_mask) == pixel_mask, 0)
            && alloc_texture_pixels(tex, width, height, GU_PSM_T4, persistent)
            && cache_texture_t4(tex, src, width, height, width);
      case 1: {
        const int clut_persistent =
            persistent && (int16_t)T1ReadWord(Vdp1Ram, CMDCOLR<<3) < 0;
        tex->clut_dynamic = persistent && !clut_persistent;
        return gen_clut_t4ind(tex, Vdp1Ram + (CMDCOLR << 3), pixel_mask,
                              rofs, gofs, bofs, transparent, endcodes,
                              clut_persistent)
            && alloc_texture_pixels(tex, width, height, GU_PSM_T4, persistent)
            && cache_texture_t4(tex, src, width, height, width);
      }
      case 2:
        tex->clut_dynamic = persistent;
        return gen_clut(tex, 64,
                        (color_base + CMDCOLR) & 0x7C0, CMDCOLR & 0x03F,
                        rofs, gofs, bofs, transparent, endcodes,
                        ((CMDCOLR | 0x3F) & pixel_mask) == pixel_mask, 0)
            && alloc_texture_pixels(tex, width, height, GU_PSM_T8, persistent)
            && cache_texture_t8(tex, src, 0x3F, width, height, width);
      case 3:
        tex->clut_dynamic = persistent;
        return gen_clut(tex, 128,
                        (color_base + CMDCOLR) & 0x780, CMDCOLR & 0x07F,
                        rofs, gofs, bofs, transparent, endcodes,
                        ((CMDCOLR | 0x7F) & pixel_mask) == pixel_mask, 0)
            && alloc_texture_pixels(tex, width, height, GU_PSM_T8, persistent)
            && cache_texture_t8(tex, src, 0x7F, width, height, width);
      case 4:
        tex->clut_dynamic = persistent;
        return gen_clut(tex, 256,
                        (color_base + CMDCOLR) & 0x700, CMDCOLR & 0x0FF,
                        rofs, gofs, bofs, transparent, endcodes,
                        ((CMDCOLR | 0xFF) & pixel_mask) == pixel_mask, 0)
            && alloc_texture_pixels(tex, width, height, GU_PSM_T8, persistent)
            && cache_texture_t8(tex, src, 0xFF, width, height, width);
      default:
        DMSG("unsupported pixel mode %d, assuming 16-bit", pixfmt);
        /* Fall through to... */
      case 5:
        tex->clut_dynamic = 0;
        return alloc_texture_pixels(tex, width, height, GU_PSM_8888, persistent)
            && cache_texture_16(tex, src, width, height, width,
                                rofs, gofs, bofs, transparent);
    }
}

/*-----------------------------------------------------------------------*/

/**
 * cache_tile:  Cache a texture from tile (VDP2) memory.  Implements common
 * code for texcache_load_tile() and texcache_load_bitmap().
 *
 * [Parameters]
 *                  tex: TexInfo structure for caching texture
 *              address: Bitmap data address within VDP2 RAM
 *        width, height: Size of texture (in pixels)
 *               stride: Line size of source data (in pixels)
 *                array: Number of VDP2 cells across and down texture
 *                          (2 for 16x16 tiles, 1 otherwise)
 *               pixfmt: Bitmap pixel format
 *          transparent: Nonzero if index 0 or alpha 0 should be transparent
 *           color_base: Color table base (for indexed formats)
 *            color_ofs: Color table offset (for indexed formats)
 *     rofs, gofs, bofs: Color offset values for texture
 *           persistent: Nonzero if a persistent texture, zero if a
 *                          transient texture
 * [Return value]
 *     Nonzero on success, zero on error
 */
static inline int cache_tile(
    TexInfo *tex, uint32_t address, unsigned int width, unsigned int height,
    unsigned int stride, int array, int pixfmt, int transparent,
    uint16_t color_base, uint16_t color_ofs, int rofs, int gofs, int bofs,
    int persistent)
{
    if (UNLIKELY(address + (pixfmt==0 ? width/2 : pixfmt==1 ? width*1 : pixfmt<=3 ? width*2 : width*4) * height > 0x80000)) {
        DMSG("%dx%d texture at 0x%X extends past end of VDP2 RAM"
             " and will be incorrectly drawn", width, height, address);
    }

    if (pixfmt == 0) {
        tex->clut_dynamic = persistent;
        if (!gen_clut(tex, 16,
                      (color_base + color_ofs) & 0x7F0, color_ofs & 0x00F,
                      rofs, gofs, bofs, transparent, 0, 0, 0)) {
            return 0;
        }
    } else if (pixfmt == 1) {
        tex->clut_dynamic = persistent;
        if (!gen_clut(tex, 256,
                      (color_base + color_ofs) & 0x700, color_ofs & 0x0FF,
                      rofs, gofs, bofs, transparent, 0, 0, 0)) {
            return 0;
        }
    } else {
        tex->clut_dynamic = 0;
    }

    const uint8_t *src = Vdp2Ram + address;

    if (array == 2) {

        switch (pixfmt) {

          case 0: {
            if (!alloc_texture_pixels(tex, 16, 16, GU_PSM_T4, persistent)) {
                return 0;
            }
            void * const saved_vram_address = tex->vram_address;

            /* Fake out the texture_cache_X() functions so they don't try
             * to draw boundary pixels. */
            tex->width = tex->height = 8;

            unsigned int i;
            for (i = 0; i < 4; i++, src += 32) {
                tex->vram_address =  (void *)((uintptr_t)saved_vram_address
                                              + (i%2 ? 4 : 0)
                                              + (i/2 ? 128 : 0));
                if (!cache_texture_t4(tex, src, 8, 8, 8)) {
                    return 0;
                }
            }

            tex->width = tex->height = 16;
            tex->vram_address = saved_vram_address;
            return 1;
          }  // case 0

          case 1: {
            if (!alloc_texture_pixels(tex, 16, 16, GU_PSM_T8, persistent)) {
                return 0;
            }
            void * const saved_vram_address = tex->vram_address;
            tex->width = tex->height = 8;
            unsigned int i;
            for (i = 0; i < 4; i++, src += 64) {
                tex->vram_address = (void *)((uintptr_t)saved_vram_address
                                             + (i%2 ? 8 : 0)
                                             + (i/2 ? 128 : 0));
                if (!cache_texture_t8(tex, src, 0xFF, 8, 8, 8)) {
                    return 0;
                }
            }
            tex->width = tex->height = 16;
            tex->vram_address = saved_vram_address;
            return 1;
          }  // case 1

          case 2: {
            if (!alloc_texture_pixels(tex, 16, 16, GU_PSM_8888, persistent)) {
                return 0;
            }
            void * const saved_vram_address = tex->vram_address;
            tex->width = tex->height = 8;
            const uint32_t tilesize = (pixfmt < 4) ? 128 : 256;
            unsigned int i;
            for (i = 0; i < 4; i++, src += tilesize) {
                tex->vram_address = (void *)((uintptr_t)saved_vram_address
                                             + (i%2 ? 32 : 0)
                                             + (i/2 ? 512 : 0));
                if (!cache_texture_t16(tex, src, color_base, 8, 8, 8,
                                       rofs, gofs, bofs, transparent)) {
                    return 0;
                }
            }
            tex->width = tex->height = 16;
            tex->vram_address = saved_vram_address;
            return 1;
          }  // case 2

          case 3: {
            if (!alloc_texture_pixels(tex, 16, 16, GU_PSM_8888, persistent)) {
                return 0;
            }
            void * const saved_vram_address = tex->vram_address;
            tex->width = tex->height = 8;
            const uint32_t tilesize = (pixfmt < 4) ? 128 : 256;
            unsigned int i;
            for (i = 0; i < 4; i++, src += tilesize) {
                tex->vram_address = (void *)((uintptr_t)saved_vram_address
                                             + (i%2 ? 32 : 0)
                                             + (i/2 ? 512 : 0));
                if (!cache_texture_16(tex, src, 8, 8, 8,
                                      rofs, gofs, bofs, transparent)) {
                    return 0;
                }
            }
            tex->width = tex->height = 16;
            tex->vram_address = saved_vram_address;
            return 1;
          }  // case 3

          case 4: {
            if (!alloc_texture_pixels(tex, 16, 16, GU_PSM_8888, persistent)) {
                return 0;
            }
            void * const saved_vram_address = tex->vram_address;
            tex->width = tex->height = 8;
            const uint32_t tilesize = (pixfmt < 4) ? 128 : 256;
            unsigned int i;
            for (i = 0; i < 4; i++, src += tilesize) {
                tex->vram_address = (void *)((uintptr_t)saved_vram_address
                                             + (i%2 ? 32 : 0)
                                             + (i/2 ? 512 : 0));
                if (!cache_texture_32(tex, src, 8, 8, 8,
                                      rofs, gofs, bofs, transparent)) {
                    return 0;
                }
            }
            tex->width = tex->height = 16;
            tex->vram_address = saved_vram_address;
            return 1;
          }  // case 4

          default:
            DMSG("Invalid tile pixel format %d", pixfmt);
            return 0;

        }  // switch (pixfmt)

    } else {  // array == 1

        switch (pixfmt) {
          case 0:
            return alloc_texture_pixels(tex, width, height, GU_PSM_T4,
                                        persistent)
                && cache_texture_t4(tex, src, width, height, stride);
          case 1:
            return alloc_texture_pixels(tex, width, height, GU_PSM_T8,
                                        persistent)
                && cache_texture_t8(tex, src, 0xFF, width, height, stride);
          case 2:
            return alloc_texture_pixels(tex, width, height, GU_PSM_8888,
                                        persistent)
                && cache_texture_t16(tex, src, color_base, width, height,
                                     stride, rofs, gofs, bofs, transparent);
          case 3:
            return alloc_texture_pixels(tex, width, height, GU_PSM_8888,
                                        persistent)
                && cache_texture_16(tex, src, width, height, stride,
                                    rofs, gofs, bofs, transparent);
          case 4:
            return alloc_texture_pixels(tex, width, height, GU_PSM_8888,
                                        persistent)
                && cache_texture_32(tex, src, width, height, stride,
                                    rofs, gofs, bofs, transparent);
          default:
            DMSG("Invalid tile pixel format %d", pixfmt);
            return 0;
        }

    }
}

/*************************************************************************/
/*************************************************************************/

/**
 * alloc_texture_pixels:  Allocate memory for a texture's pixel data, and
 * fill in the vram_address, width, height, stride, and pixfmt fields of
 * the TexInfo structure.
 *
 * [Parameters]
 *            tex: TexInfo structure for texture
 *          width: Texture width in pixels
 *         height: Texture height in pixels
 *            psm: Native texture pixel format (GU_PSM_*)
 *     persistent: Nonzero if a persistent texture, zero if a transient texture
 * [Return value]
 *     Nonzero on success, zero on error
 */
static inline int alloc_texture_pixels(
    TexInfo *tex, unsigned int width, unsigned int height, unsigned int psm,
    int persistent)
{
    /* Calculate the power-of-2 sizes we need for registering the texture
     * with the GE. */
    unsigned int texwidth  = 1 << (32 - __builtin_clz(width-1));
    unsigned int texheight = 1 << (32 - __builtin_clz(height-1));

    /* If the texture width or height aren't powers of 2, we add an extra
     * 1-pixel border on the right and bottom edges to avoid graphics
     * glitches resulting from the GE trying to read one pixel beyond the
     * edge of the texture data. */
    unsigned int outwidth  = width + (width != texwidth ? 1 : 0);
    unsigned int outheight = height + (height != texheight ? 1 : 0);

    unsigned int stride, linebytes;
    if (psm == GU_PSM_T4) {
        stride = (outwidth+31) & -32;
        linebytes = stride/2;
    } else if (psm == GU_PSM_T8) {
        stride = (outwidth+15) & -16;
        linebytes = stride;
    } else {  // Must be GU_PSM_8888, since we don't use any others
        stride = (outwidth+3) & -4;
        linebytes = stride*4;
    }

    const unsigned int size = linebytes * ((outheight+7) & -8);
    if (persistent) {
        tex->vram_address = alloc_pixdata(size);
    } else {
        tex->vram_address = alloc_vram(size);
    }
    if (UNLIKELY(!tex->vram_address)) {
        DMSG("%s buffer full, can't cache",
             persistent ? "Persistent cache" : "DRAM");
        return 0;
    }

    tex->width  = texwidth;
    tex->height = texheight;
    tex->stride = stride;
    tex->pixfmt = psm;
    return 1;
}

/*************************************************************************/
/*************************************************************************/

/**
 * gen_clut:  Generate a color lookup table for a 4-bit or 8-bit indexed
 * texture.
 *
 * [Parameters]
 *                  tex: TexInfo structure for color table
 *                 size: Number of color entries to generate
 *           color_base: Base color index
 *            color_ofs: Color index offset ORed together with pixel
 *     rofs, gofs, bofs: Color offset values for texture
 *          transparent: Nonzero if pixel value 0 should be transparent
 *             endcodes: Nonzero if pixel value 0b11...11 should be transparent
 *           can_shadow: Nonzero if pixel value 0b11...10 is a shadow pixel
 *           persistent: Nonzero if a persistent texture, zero if a
 *                          transient texture
 * [Return value]
 *     Nonzero on success, zero on error
 */
static inline int gen_clut(
    TexInfo *tex, unsigned int size, uint32_t color_base, uint8_t color_ofs,
    int rofs, int gofs, int bofs, int transparent, int endcodes,
    int can_shadow, int persistent)
{
    tex->clut_size = size;

    if (!endcodes && !transparent && !can_shadow && !color_ofs
     && !rofs && !gofs && !bofs
    ) {
        /* There are no changes to apply to the palette, so just use the
         * global CLUT directly. */
        tex->clut_address = &global_clut_32[color_base + color_ofs];
        return 1;
    }

    const uint32_t color_set = (rofs & 0x1FF) <<  0
                             | (gofs & 0x1FF) <<  9
                             | (bofs & 0x1FF) << 18
                             | (transparent ? 1<<27 : 0)
                             | (endcodes ? 1<<28 : 0);
    const unsigned int cache_slot = color_base >> 4;
    unsigned int cache_entry;
    for (cache_entry = 0; cache_entry < CLUT_ENTRIES; cache_entry++) {
        if (clut_cache[cache_slot][cache_entry].size == 0) {
            /* This entry is empty, so we'll generate a palette and store
             * it here. */
            clut_cache[cache_slot][cache_entry].size = size;
            clut_cache[cache_slot][cache_entry].color_ofs = color_ofs;
            clut_cache[cache_slot][cache_entry].color_set = color_set;
            break;
        } else if (clut_cache[cache_slot][cache_entry].size == size
                && clut_cache[cache_slot][cache_entry].color_ofs == color_ofs
                && clut_cache[cache_slot][cache_entry].color_set == color_set){
            /* Found a match, so return it. */
            tex->clut_address =
                clut_cache[cache_slot][cache_entry].clut_address;
            return 1;
        }
    }
    if (UNLIKELY(cache_entry >= CLUT_ENTRIES)) {
        DMSG("Warning: no free entries for CLUT cache slot 0x%02X", cache_slot);
    }

    if (persistent) {
        tex->clut_address = alloc_pixdata(size*4);
    } else {
        tex->clut_address = alloc_vram(size*4);
    }
    if (UNLIKELY(!tex->clut_address)) {
        DMSG("%s buffer full, can't cache CLUT",
             persistent ? "Persistent cache" : "VRAM");
        return 0;
    }
    if (cache_entry < CLUT_ENTRIES) {
        clut_cache[cache_slot][cache_entry].clut_address = tex->clut_address;
    }

    uint32_t *dest = (uint32_t *)tex->clut_address;
    int i;

    /* Apply the color offset values to transparent or shadow pixels as
     * well; this prevents dark rims around interpolated textures when
     * positive color offsets are applied. */
    const uint32_t transparent_rgb = (rofs>0 ? rofs<< 0 : 0)
                                   | (gofs>0 ? gofs<< 8 : 0)
                                   | (bofs>0 ? bofs<<16 : 0);

    if (transparent) {
        *dest++ = 0x00<<24 | transparent_rgb;
        i = 1;
    } else {
        i = 0;
    }

    const uint32_t *clut_32 = &global_clut_32[color_base];
    if (rofs | gofs | bofs) {
        for (; i < size; i++, dest++) {
            uint32_t color = clut_32[i | color_ofs];
            *dest = adjust_color_32_32(color, rofs, gofs, bofs);
        }
    } else {
        for (; i < size; i++, dest++) {
            *dest = clut_32[i | color_ofs];
        }
    }

    if (endcodes) {
        dest[-1] = 0x00<<24 | transparent_rgb;
    }

    if (can_shadow) {
        dest[-1] = 0x80<<24 | transparent_rgb;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * gen_clut_t4ind:  Generate a color lookup table for a 4-bit indirect
 * indexed texture.
 *
 * [Parameters]
 *                  tex: TexInfo structure for color table
 *            color_lut: Pointer to VDP1 color lookup table
 *           pixel_mask: Mask to apply to palette indices in lookup table
 *     rofs, gofs, bofs: Color offset values for texture
 *          transparent: Nonzero if pixel value 0 should be transparent
 *             endcodes: Nonzero if pixel value 0b11...11 should be transparent
 *           persistent: Nonzero if a persistent texture, zero if a
 *                          transient texture
 * [Return value]
 *     Nonzero on success, zero on error
 */
static inline int gen_clut_t4ind(
    TexInfo *tex, const uint8_t *color_lut, uint16_t pixel_mask,
    int rofs, int gofs, int bofs, int transparent, int endcodes,
    int persistent)
{
    tex->clut_size = 16;

    if (persistent) {
        tex->clut_address = alloc_pixdata(16*4);
    } else {
        tex->clut_address = alloc_vram(16*4);
    }
    if (UNLIKELY(!tex->clut_address)) {
        DMSG("%s buffer full, can't cache CLUT",
             persistent ? "Persistent cache" : "VRAM");
        return 0;
    }

    const uint16_t *src = (const uint16_t *)color_lut;
    uint32_t *dest = (uint32_t *)tex->clut_address;
    const uint32_t *clut_32 = global_clut_32;

    if (rofs | gofs | bofs) {

        const uint32_t transparent_rgb = (rofs>0 ? rofs<< 0 : 0)
                                       | (gofs>0 ? gofs<< 8 : 0)
                                       | (bofs>0 ? bofs<<16 : 0);
        const uint16_t *top;
        if (endcodes) {
            dest[15] = 0x00<<24 | transparent_rgb;
            top = src + 15;
        } else {
            top = src + 16;
        }
        if (transparent) {
            src++;
            *dest++ = 0x00<<24 | transparent_rgb;
        }

        for (; src < top; src++, dest++) {
            uint16_t color16 = BSWAP16(*src);
            if (color16 & 0x8000) {
                *dest = adjust_color_16_32(color16, rofs, gofs, bofs);
            } else {
                color16 &= pixel_mask;
                if (color16 == pixel_mask - 1) {
                    *dest = 0x80<<24 | transparent_rgb;
                } else {
                    *dest = adjust_color_32_32(clut_32[color16],
                                               rofs, gofs, bofs);
                }
            }
        }

    } else {  // No color offset

        const uint16_t *top;
        if (endcodes) {
            dest[15] = 0x00000000;
            top = src + 15;
        } else {
            top = src + 16;
        }
        if (transparent) {
            src++;
            *dest++ = 0x00000000;
        }

        for (; src < top; src++, dest++) {
            uint16_t color16 = BSWAP16(*src);
            if (color16 & 0x8000) {
                *dest = 0xFF000000 | (color16 & 0x7C00) << 9
                                   | (color16 & 0x03E0) << 6
                                   | (color16 & 0x001F) << 3;
            } else {
                color16 &= pixel_mask;
                if (color16 == pixel_mask - 1) {
                    *dest = 0x80000000;
                } else {
                    *dest = clut_32[color16];
                }
            }
        }

    }

    return 1;
}

/*************************************************************************/
/*************************************************************************/

/*
 * A note about the texture caching routines--
 *
 * While the texture caching logic is divided into five separate caching
 * routines (one for each pixel type: 4/8/16-bit indexed and 16/32-bit
 * direct color), each with multiple branches conditioned on the texture
 * parameters, all of them follow the same basic flow:
 *
 *     for (each row of 16-byte-by-8-line swizzled blocks) {
 *         for (each swizzled block in the row) {
 *             for (each of 8 lines in the block) {
 *                 read enough input pixels for 16 output bytes
 *                 convert and store pixels into the output buffer
 *             }
 *         }
 *         if (width is not a power of 2) {
 *             copy rightmost column one pixel to the right
 *         }
 *     }
 *     if (height is not a power of 2) {
 *         copy bottommost row one pixel down
 *     }
 *
 * The reason for all the repetition is to help the compiler optimize
 * specific common cases, particularly for tile caching in which case the
 * size parameters are all a constant 8 or 16.
 */

/*************************************************************************/

/**
 * cache_texture_t4:  Cache a 4-bit indexed texture.
 *
 * [Parameters]
 *               tex: TexInfo structure for texture
 *               src: Pointer to VDP1/VDP2 texture data
 *     width, height: Size of texture (in pixels)
 *            stride: Line size of source data (in pixels)
 * [Return value]
 *     Nonzero on success, zero on error
 */
static inline int cache_texture_t4(
    TexInfo *tex, const uint8_t *src,
    unsigned int width, unsigned int height, unsigned int stride)
{
    const int outwidth  = width  + (width  == tex->width  ? 0 : 1);
    const int outheight = height + (height == tex->height ? 0 : 1);

    /* Cache the value of this locally so we don't have to load it on
     * every loop. */
    const unsigned int tex_stride = tex->stride;

    uint8_t *dest = (uint8_t *)tex->vram_address;

    if (tex_stride == 32) {

        if (width == 8) {
            uint8_t *dest_top = dest + (height * 16);
            for (; dest != dest_top; src += stride/2, dest += 16) {
                const uint32_t src_word0 = ((const uint32_t *)src)[0];
                ((uint32_t *)dest)[0] = ((src_word0 & 0x0F0F0F0F) << 4)
                                      | ((src_word0 >> 4) & 0x0F0F0F0F);
            }
        } else if (width == 16) {
            uint8_t *dest_top = dest + (height * 16);
            for (; dest != dest_top; src += stride/2, dest += 16) {
                const uint32_t src_word0 = ((const uint32_t *)src)[0];
                const uint32_t src_word1 = ((const uint32_t *)src)[1];
                ((uint32_t *)dest)[0] = ((src_word0 & 0x0F0F0F0F) << 4)
                                      | ((src_word0 >> 4) & 0x0F0F0F0F);
                ((uint32_t *)dest)[1] = ((src_word1 & 0x0F0F0F0F) << 4)
                                      | ((src_word1 >> 4) & 0x0F0F0F0F);
            }
        } else {
            uint8_t *dest_top = dest + (height * 16);
            for (; dest != dest_top; src += stride/2, dest += 16) {
                const uint32_t src_word0 = ((const uint32_t *)src)[0];
                const uint32_t src_word1 = ((const uint32_t *)src)[1];
                const uint32_t src_word2 = ((const uint32_t *)src)[2];
                const uint32_t src_word3 = ((const uint32_t *)src)[3];
                ((uint32_t *)dest)[0] = ((src_word0 & 0x0F0F0F0F) << 4)
                                      | ((src_word0 >> 4) & 0x0F0F0F0F);
                ((uint32_t *)dest)[1] = ((src_word1 & 0x0F0F0F0F) << 4)
                                      | ((src_word1 >> 4) & 0x0F0F0F0F);
                ((uint32_t *)dest)[2] = ((src_word2 & 0x0F0F0F0F) << 4)
                                      | ((src_word2 >> 4) & 0x0F0F0F0F);
                ((uint32_t *)dest)[3] = ((src_word3 & 0x0F0F0F0F) << 4)
                                      | ((src_word3 >> 4) & 0x0F0F0F0F);
                if (width != 32) {
                    dest[width/2] = dest[width/2-1] >> 4;  // Copy last pixel
                }
            }
        }

        if (outheight > height) {  // Copy last line
            ((uint32_t *)dest)[0] = ((uint32_t *)dest)[-4];
            ((uint32_t *)dest)[1] = ((uint32_t *)dest)[-3];
            ((uint32_t *)dest)[2] = ((uint32_t *)dest)[-2];
            ((uint32_t *)dest)[3] = ((uint32_t *)dest)[-1];
        }

    } else {  // tex_stride > 32

        unsigned int y;
        for (y = 0; y < height; y += 8, src += stride*4 - tex_stride/2) {
            uint8_t *dest_top = dest + tex_stride*4;
            for (; dest != dest_top; src += 16) {
                const uint8_t *line_src = src;
                uint8_t *line_end = dest + 128;
                for (; dest != line_end; line_src += stride/2, dest += 16) {
                    const uint32_t src_word0 = ((const uint32_t *)line_src)[0];
                    const uint32_t src_word1 = ((const uint32_t *)line_src)[1];
                    const uint32_t src_word2 = ((const uint32_t *)line_src)[2];
                    const uint32_t src_word3 = ((const uint32_t *)line_src)[3];
                    ((uint32_t *)dest)[0] = ((src_word0 & 0x0F0F0F0F) << 4)
                                          | ((src_word0 >> 4) & 0x0F0F0F0F);
                    ((uint32_t *)dest)[1] = ((src_word1 & 0x0F0F0F0F) << 4)
                                          | ((src_word1 >> 4) & 0x0F0F0F0F);
                    ((uint32_t *)dest)[2] = ((src_word2 & 0x0F0F0F0F) << 4)
                                          | ((src_word2 >> 4) & 0x0F0F0F0F);
                    ((uint32_t *)dest)[3] = ((src_word3 & 0x0F0F0F0F) << 4)
                                          | ((src_word3 >> 4) & 0x0F0F0F0F);
                }
            }
            if (outwidth > width) {
                uint8_t *eol_ptr = dest - 128 + ((width/2) & 15);
                const int eol_ofs = (width & 31) ? -1 : -128 + 15;
                uint8_t *eol_top = eol_ptr + 128;
                for (; eol_ptr != eol_top; eol_ptr += 16) {
                    *eol_ptr = eol_ptr[eol_ofs] >> 4;
                }
            }
        }

        if (outheight > height) {
            if ((height & 7) != 0) {
                dest = dest - tex_stride*4 + (height & 7)*16;
                src = dest - 16;
            } else {
                src = dest - tex_stride*4 + 7*16;
            }
            uint8_t *dest_top = dest + tex_stride*4;
            for (; dest < dest_top; src += 128, dest += 128) {
                const uint32_t src_word0 = ((const uint32_t *)src)[0];
                const uint32_t src_word1 = ((const uint32_t *)src)[1];
                const uint32_t src_word2 = ((const uint32_t *)src)[2];
                const uint32_t src_word3 = ((const uint32_t *)src)[3];
                ((uint32_t *)dest)[0] = src_word0;
                ((uint32_t *)dest)[1] = src_word1;
                ((uint32_t *)dest)[2] = src_word2;
                ((uint32_t *)dest)[3] = src_word3;
            }
        }

    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * cache_texture_t8:  Cache an 8-bit indexed texture.
 *
 * [Parameters]
 *               tex: TexInfo structure for texture
 *               src: Pointer to VDP1/VDP2 texture data
 *           pixmask: Pixel data mask
 *     width, height: Size of texture (in pixels)
 *            stride: Line size of source data (in pixels)
 * [Return value]
 *     Nonzero on success, zero on error
 */
static inline int cache_texture_t8(
    TexInfo *tex, const uint8_t *src, uint8_t pixmask,
    unsigned int width, unsigned int height, unsigned int stride)
{
    const int outwidth  = width  + (width  == tex->width  ? 0 : 1);
    const int outheight = height + (height == tex->height ? 0 : 1);

    const unsigned int tex_stride = tex->stride;
    uint8_t *dest = (uint8_t *)tex->vram_address;

    if (pixmask != 0xFF) {

        const uint32_t pixmask32 = pixmask * 0x01010101;
        if (stride == 8) {
            unsigned int y;
            for (y = 0; y < height; y++, src += 8, dest += 16) {
                const uint32_t src_word0 = ((const uint32_t *)src)[0];
                const uint32_t src_word1 = ((const uint32_t *)src)[1];
                ((uint32_t *)dest)[0] = src_word0 & pixmask32;
                ((uint32_t *)dest)[1] = src_word1 & pixmask32;
            }
        } else if (stride == 16) {
            uint8_t *dest_top = dest + (height * stride);
            for (; dest != dest_top; dest += 16) {
                const uint32_t src_word0 = ((const uint32_t *)src)[0];
                const uint32_t src_word1 = ((const uint32_t *)src)[1];
                const uint32_t src_word2 = ((const uint32_t *)src)[2];
                const uint32_t src_word3 = ((const uint32_t *)src)[3];
                ((uint32_t *)dest)[0] = src_word0 & pixmask32;
                ((uint32_t *)dest)[1] = src_word1 & pixmask32;
                ((uint32_t *)dest)[2] = src_word2 & pixmask32;
                ((uint32_t *)dest)[3] = src_word3 & pixmask32;
            }
        } else {  // stride > 16
            unsigned int y;
            for (y = 0; y < height; y += 8, src += stride*8 - tex_stride) {
                uint8_t *dest_top = dest + tex_stride*8;
                for (; dest != dest_top; src += 16) {
                    const uint8_t *line_src = src;
                    uint8_t *line_end = dest + 128;
                    for (; dest != line_end; line_src += stride, dest += 16) {
                        const uint32_t src_word0 = ((const uint32_t *)line_src)[0];
                        const uint32_t src_word1 = ((const uint32_t *)line_src)[1];
                        const uint32_t src_word2 = ((const uint32_t *)line_src)[2];
                        const uint32_t src_word3 = ((const uint32_t *)line_src)[3];
                        ((uint32_t *)dest)[0] = src_word0 & pixmask32;
                        ((uint32_t *)dest)[1] = src_word1 & pixmask32;
                        ((uint32_t *)dest)[2] = src_word2 & pixmask32;
                        ((uint32_t *)dest)[3] = src_word3 & pixmask32;
                    }
                }
                if (outwidth > width) {
                    uint8_t *eol_ptr = dest - 128 + (width & 15);
                    const int eol_ofs = (width & 15) ? -1 : -128 + 15;
                    uint8_t *eol_top = eol_ptr + 128;
                    for (; eol_ptr != eol_top; eol_ptr += 16) {
                        *eol_ptr = eol_ptr[eol_ofs];
                    }
                }
            }
        }

    } else {  // pixmask == 0xFF

        if (stride == 8) {
            unsigned int y;
            for (y = 0; y < height; y++, src += 8, dest += 16) {
                const uint32_t src_word0 = ((const uint32_t *)src)[0];
                const uint32_t src_word1 = ((const uint32_t *)src)[1];
                ((uint32_t *)dest)[0] = src_word0;
                ((uint32_t *)dest)[1] = src_word1;
            }
        } else if (stride == 16) {
            uint8_t *dest_top = dest + (height * stride);
            for (; dest != dest_top; dest += 16) {
                const uint32_t src_word0 = ((const uint32_t *)src)[0];
                const uint32_t src_word1 = ((const uint32_t *)src)[1];
                const uint32_t src_word2 = ((const uint32_t *)src)[2];
                const uint32_t src_word3 = ((const uint32_t *)src)[3];
                ((uint32_t *)dest)[0] = src_word0;
                ((uint32_t *)dest)[1] = src_word1;
                ((uint32_t *)dest)[2] = src_word2;
                ((uint32_t *)dest)[3] = src_word3;
            }
        } else {  // stride > 16
            unsigned int y;
            for (y = 0; y < height; y += 8, src += stride*8 - tex_stride) {
                uint8_t *dest_top = dest + tex_stride*8;
                for (; dest != dest_top; src += 16) {
                    const uint8_t *line_src = src;
                    uint8_t *line_end = dest + 128;
                    for (; dest != line_end; line_src += stride, dest += 16) {
                        const uint32_t src_word0 = ((const uint32_t *)line_src)[0];
                        const uint32_t src_word1 = ((const uint32_t *)line_src)[1];
                        const uint32_t src_word2 = ((const uint32_t *)line_src)[2];
                        const uint32_t src_word3 = ((const uint32_t *)line_src)[3];
                        ((uint32_t *)dest)[0] = src_word0;
                        ((uint32_t *)dest)[1] = src_word1;
                        ((uint32_t *)dest)[2] = src_word2;
                        ((uint32_t *)dest)[3] = src_word3;
                    }
                }
                if (outwidth > width) {
                    uint8_t *eol_ptr = dest - 128 + (width & 15);
                    const int eol_ofs = (width & 15) ? -1 : -128 + 15;
                    uint8_t *eol_top = eol_ptr + 128;
                    for (; eol_ptr != eol_top; eol_ptr += 16) {
                        *eol_ptr = eol_ptr[eol_ofs];
                    }
                }
            }
        }

    }  // if (pixmask != 0xFF)

    if (outheight > height) {
        if (tex_stride == 16) {
            ((uint32_t *)dest)[0] = ((uint32_t *)dest)[-4];
            ((uint32_t *)dest)[1] = ((uint32_t *)dest)[-3];
            ((uint32_t *)dest)[2] = ((uint32_t *)dest)[-2];
            ((uint32_t *)dest)[3] = ((uint32_t *)dest)[-1];
        } else {
            if ((height & 7) != 0) {
                dest = dest - tex_stride*8 + (height & 7)*16;
                src = dest - 16;
            } else {
                src = dest - tex_stride*8 + 7*16;
            }
            uint8_t *dest_top = dest + tex_stride*8;
            for (; dest < dest_top; src += 128, dest += 128) {
                const uint32_t src_word0 = ((const uint32_t *)src)[0];
                const uint32_t src_word1 = ((const uint32_t *)src)[1];
                const uint32_t src_word2 = ((const uint32_t *)src)[2];
                const uint32_t src_word3 = ((const uint32_t *)src)[3];
                ((uint32_t *)dest)[0] = src_word0;
                ((uint32_t *)dest)[1] = src_word1;
                ((uint32_t *)dest)[2] = src_word2;
                ((uint32_t *)dest)[3] = src_word3;
            }
        }
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * cache_texture_t16:  Cache a 16-bit indexed texture.
 *
 * [Parameters]
 *                  tex: TexInfo structure for texture
 *                  src: Pointer to VDP1/VDP2 texture data
 *           color_base: Base color index
 *        width, height: Size of texture (in pixels)
 *               stride: Line size of source data (in pixels)
 *     rofs, gofs, bofs: Color offset values for texture
 *          transparent: Nonzero if pixel value 0 should be transparent
 * [Return value]
 *     Nonzero on success, zero on error
 */
static inline int cache_texture_t16(
    TexInfo *tex, const uint8_t *src, uint32_t color_base,
    unsigned int width, unsigned int height, unsigned int stride,
    int rofs, int gofs, int bofs, int transparent)
{
    const int outwidth  = width  + (width  == tex->width  ? 0 : 1);
    const int outheight = height + (height == tex->height ? 0 : 1);

    const unsigned int tex_stride = tex->stride;
    uint32_t *dest = (uint32_t *)tex->vram_address;
    const uint32_t *clut_32 = &global_clut_32[color_base];

    if (rofs | gofs | bofs) {

        const uint32_t transparent_pixel = 0x00000000
                                         | (rofs>0 ? rofs<< 0 : 0)
                                         | (gofs>0 ? gofs<< 8 : 0)
                                         | (bofs>0 ? bofs<<16 : 0);
        unsigned int y;
        for (y = 0; y < height; y += 8, src += stride*16 - width*2, dest += (tex_stride - width)*8) {
            uint32_t *dest_top = dest + width*8;
            for (; dest != dest_top; src += 8) {
                const uint16_t *line_src = (const uint16_t *)src;
                uint32_t *line_end = dest + 32;
                for (; dest != line_end; line_src += stride, dest += 4) {
                    const uint16_t pixel0 = BSWAP16(line_src[0]);
                    const uint16_t pixel1 = BSWAP16(line_src[1]);
                    const uint16_t pixel2 = BSWAP16(line_src[2]);
                    const uint16_t pixel3 = BSWAP16(line_src[3]);
                    dest[0] = (transparent && !pixel0) ? transparent_pixel
                              : adjust_color_32_32(clut_32[pixel0 & 0x7FF],
                                                   rofs, gofs, bofs);
                    dest[1] = (transparent && !pixel1) ? transparent_pixel
                              : adjust_color_32_32(clut_32[pixel1 & 0x7FF],
                                                   rofs, gofs, bofs);
                    dest[2] = (transparent && !pixel2) ? transparent_pixel
                              : adjust_color_32_32(clut_32[pixel2 & 0x7FF],
                                                   rofs, gofs, bofs);
                    dest[3] = (transparent && !pixel3) ? transparent_pixel
                              : adjust_color_32_32(clut_32[pixel3 & 0x7FF],
                                                   rofs, gofs, bofs);
                }
            }
            if (outwidth > width) {
                int line;
                for (line = 0; line < 8; line++) {
                    dest[line*4] = dest[line*4-32];
                }
            }
        }

    } else if (transparent) {

        unsigned int y;
        for (y = 0; y < height; y += 8, src += stride*16 - width*2, dest += (tex_stride - width)*8) {
            uint32_t *dest_top = dest + width*8;
            for (; dest != dest_top; src += 8) {
                const uint16_t *line_src = (const uint16_t *)src;
                uint32_t *line_end = dest + 32;
                for (; dest != line_end; line_src += stride, dest += 4) {
                    const uint16_t pixel0 = BSWAP16(line_src[0]);
                    const uint16_t pixel1 = BSWAP16(line_src[1]);
                    const uint16_t pixel2 = BSWAP16(line_src[2]);
                    const uint16_t pixel3 = BSWAP16(line_src[3]);
                    dest[0] = (pixel0 == 0) ? 0 : clut_32[pixel0 & 0x7FF];
                    dest[1] = (pixel1 == 0) ? 0 : clut_32[pixel1 & 0x7FF];
                    dest[2] = (pixel2 == 0) ? 0 : clut_32[pixel2 & 0x7FF];
                    dest[3] = (pixel3 == 0) ? 0 : clut_32[pixel3 & 0x7FF];
                }
            }
            if (outwidth > width) {
                int line;
                for (line = 0; line < 8; line++) {
                    dest[line*4] = dest[line*4-32];
                }
            }
        }

    } else {  // !(rofs | gofs | bofs) && !transparent

        unsigned int y;
        for (y = 0; y < height; y += 8, src += stride*16 - width*2, dest += (tex_stride - width)*8) {
            uint32_t *dest_top = dest + width*8;
            for (; dest != dest_top; src += 8) {
                const uint16_t *line_src = (const uint16_t *)src;
                uint32_t *line_end = dest + 32;
                for (; dest != line_end; line_src += stride, dest += 4) {
                    const uint16_t pixel0 = BSWAP16(line_src[0]);
                    const uint16_t pixel1 = BSWAP16(line_src[1]);
                    const uint16_t pixel2 = BSWAP16(line_src[2]);
                    const uint16_t pixel3 = BSWAP16(line_src[3]);
                    dest[0] = clut_32[pixel0 & 0x7FF];
                    dest[1] = clut_32[pixel1 & 0x7FF];
                    dest[2] = clut_32[pixel2 & 0x7FF];
                    dest[3] = clut_32[pixel3 & 0x7FF];
                }
            }
            if (outwidth > width) {
                int line;
                for (line = 0; line < 8; line++) {
                    dest[line*4] = dest[line*4-32];
                }
            }
        }

    }  // if (rofs | gofs | bofs)

    if (outheight > height) {
        const uint32_t *src32;
        if ((height & 7) != 0) {
            dest = dest - tex_stride*8 + (height & 7)*4;
            src32 = dest - 4;
        } else {
            src32 = dest - tex_stride*8 + 7*4;
        }
        uint32_t *dest_top = dest + tex_stride;
        for (; dest < dest_top; src32 += 32, dest += 32) {
            const uint32_t pixel0 = src32[0];
            const uint32_t pixel1 = src32[1];
            const uint32_t pixel2 = src32[2];
            const uint32_t pixel3 = src32[3];
            dest[0] = pixel0;
            dest[1] = pixel1;
            dest[2] = pixel2;
            dest[3] = pixel3;
        }
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * cache_texture_16:  Cache a 16-bit ARGB1555 texture.
 *
 * [Parameters]
 *                  tex: TexInfo structure for texture
 *                  src: Pointer to VDP1/VDP2 texture data
 *        width, height: Size of texture (in pixels)
 *               stride: Line size of source data (in pixels)
 *     rofs, gofs, bofs: Color offset values for texture
 *          transparent: Nonzero if alpha-0 pixels should be transparent
 * [Return value]
 *     Nonzero on success, zero on error
 */
static inline int cache_texture_16(
    TexInfo *tex, const uint8_t *src,
    unsigned int width, unsigned int height, unsigned int stride,
    int rofs, int gofs, int bofs, int transparent)
{
    const int outwidth  = width  + (width  == tex->width  ? 0 : 1);
    const int outheight = height + (height == tex->height ? 0 : 1);

    const unsigned int tex_stride = tex->stride;
    uint32_t *dest = (uint32_t *)tex->vram_address;

    if (rofs | gofs | bofs) {

        const uint32_t transparent_pixel = 0x00000000
                                         | (rofs>0 ? rofs<< 0 : 0)
                                         | (gofs>0 ? gofs<< 8 : 0)
                                         | (bofs>0 ? bofs<<16 : 0);
        unsigned int y;
        for (y = 0; y < height; y += 8, src += stride*16 - width*2, dest += (tex_stride - width)*8) {
            uint32_t *dest_top = dest + width*8;
            for (; dest != dest_top; src += 8) {
                /* We load these as signed values to simplify checking the
                 * high (transparency) bit. */
                const int16_t *line_src = (const int16_t *)src;
                uint32_t *line_end = dest + 32;
                for (; dest != line_end; line_src += stride, dest += 4) {
                    const int16_t pixel0 = BSWAP16(line_src[0]);
                    const int16_t pixel1 = BSWAP16(line_src[1]);
                    const int16_t pixel2 = BSWAP16(line_src[2]);
                    const int16_t pixel3 = BSWAP16(line_src[3]);
                    dest[0] = (transparent && !(pixel0<0)) ? transparent_pixel
                              : adjust_color_16_32(pixel0, rofs, gofs, bofs);
                    dest[1] = (transparent && !(pixel1<0)) ? transparent_pixel
                              : adjust_color_16_32(pixel1, rofs, gofs, bofs);
                    dest[2] = (transparent && !(pixel2<0)) ? transparent_pixel
                              : adjust_color_16_32(pixel2, rofs, gofs, bofs);
                    dest[3] = (transparent && !(pixel3<0)) ? transparent_pixel
                              : adjust_color_16_32(pixel3, rofs, gofs, bofs);
                }
            }
            if (outwidth > width) {
                int line;
                for (line = 0; line < 8; line++) {
                    dest[line*4] = dest[line*4-32];
                }
            }
        }

    } else if (transparent) {

        unsigned int y;
        for (y = 0; y < height; y += 8, src += stride*16 - width*2, dest += (tex_stride - width)*8) {
            uint32_t *dest_top = dest + width*8;
            for (; dest != dest_top; src += 8) {
                const int16_t *line_src = (const int16_t *)src;
                uint32_t *line_end = dest + 32;
                for (; dest != line_end; line_src += stride, dest += 4) {
                    const int16_t pixel0 = BSWAP16(line_src[0]);
                    const int16_t pixel1 = BSWAP16(line_src[1]);
                    const int16_t pixel2 = BSWAP16(line_src[2]);
                    const int16_t pixel3 = BSWAP16(line_src[3]);
                    dest[0] = (pixel0 >= 0) ? 0
                              : 0xFF000000 | (pixel0 & 0x7C00) << 9
                                           | (pixel0 & 0x03E0) << 6
                                           | (pixel0 & 0x001F) << 3;
                    dest[1] = (pixel1 >= 0) ? 0
                              : 0xFF000000 | (pixel1 & 0x7C00) << 9
                                           | (pixel1 & 0x03E0) << 6
                                           | (pixel1 & 0x001F) << 3;
                    dest[2] = (pixel2 >= 0) ? 0
                              : 0xFF000000 | (pixel2 & 0x7C00) << 9
                                           | (pixel2 & 0x03E0) << 6
                                           | (pixel2 & 0x001F) << 3;
                    dest[3] = (pixel3 >= 0) ? 0
                              : 0xFF000000 | (pixel3 & 0x7C00) << 9
                                           | (pixel3 & 0x03E0) << 6
                                           | (pixel3 & 0x001F) << 3;
                }
            }
            if (outwidth > width) {
                int line;
                for (line = 0; line < 8; line++) {
                    dest[line*4] = dest[line*4-32];
                }
            }
        }

    } else {  // !(rofs | gofs | bofs) && !transparent

        unsigned int y;
        for (y = 0; y < height; y += 8, src += stride*16 - width*2, dest += (tex_stride - width)*8) {
            uint32_t *dest_top = dest + width*8;
            for (; dest != dest_top; src += 8) {
                const int16_t *line_src = (const int16_t *)src;
                uint32_t *line_end = dest + 32;
                for (; dest != line_end; line_src += stride, dest += 4) {
                    const int16_t pixel0 = BSWAP16(line_src[0]);
                    const int16_t pixel1 = BSWAP16(line_src[1]);
                    const int16_t pixel2 = BSWAP16(line_src[2]);
                    const int16_t pixel3 = BSWAP16(line_src[3]);
                    dest[0] = 0xFF000000 | (pixel0 & 0x7C00) << 9
                                         | (pixel0 & 0x03E0) << 6
                                         | (pixel0 & 0x001F) << 3;
                    dest[1] = 0xFF000000 | (pixel1 & 0x7C00) << 9
                                         | (pixel1 & 0x03E0) << 6
                                         | (pixel1 & 0x001F) << 3;
                    dest[2] = 0xFF000000 | (pixel2 & 0x7C00) << 9
                                         | (pixel2 & 0x03E0) << 6
                                         | (pixel2 & 0x001F) << 3;
                    dest[3] = 0xFF000000 | (pixel3 & 0x7C00) << 9
                                         | (pixel3 & 0x03E0) << 6
                                         | (pixel3 & 0x001F) << 3;
                }
            }
            if (outwidth > width) {
                int line;
                for (line = 0; line < 8; line++) {
                    dest[line*4] = dest[line*4-32];
                }
            }
        }

    }  // if (rofs | gofs | bofs)

    if (outheight > height) {
        const uint32_t *src32;
        if ((height & 7) != 0) {
            dest = dest - tex_stride*8 + (height & 7)*4;
            src32 = dest - 4;
        } else {
            src32 = dest - tex_stride*8 + 7*4;
        }
        uint32_t *dest_top = dest + tex_stride;
        for (; dest < dest_top; src32 += 32, dest += 32) {
            const uint32_t pixel0 = src32[0];
            const uint32_t pixel1 = src32[1];
            const uint32_t pixel2 = src32[2];
            const uint32_t pixel3 = src32[3];
            dest[0] = pixel0;
            dest[1] = pixel1;
            dest[2] = pixel2;
            dest[3] = pixel3;
        }
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * cache_texture_32:  Cache a 32-bit ARGB1888 texture.
 *
 * [Parameters]
 *                  tex: TexInfo structure for texture
 *                  src: Pointer to VDP1/VDP2 texture data
 *        width, height: Size of texture (in pixels)
 *               stride: Line size of source data (in pixels)
 *     rofs, gofs, bofs: Color offset values for texture
 *          transparent: Nonzero if alpha-0 pixels should be transparent
 * [Return value]
 *     Nonzero on success, zero on error
 */
static inline int cache_texture_32(
    TexInfo *tex, const uint8_t *src,
    unsigned int width, unsigned int height, unsigned int stride,
    int rofs, int gofs, int bofs, int transparent)
{
    const int outwidth  = width  + (width  == tex->width  ? 0 : 1);
    const int outheight = height + (height == tex->height ? 0 : 1);

    const unsigned int tex_stride = tex->stride;
    uint32_t *dest = (uint32_t *)tex->vram_address;

    if (rofs | gofs | bofs) {

        const uint32_t transparent_pixel = 0x00000000
                                         | (rofs>0 ? rofs<< 0 : 0)
                                         | (gofs>0 ? gofs<< 8 : 0)
                                         | (bofs>0 ? bofs<<16 : 0);
        unsigned int y;
        for (y = 0; y < height; y += 8, src += stride*32 - width*4, dest += (tex_stride - width)*8) {
            uint32_t *dest_top = dest + width*8;
            for (; dest != dest_top; src += 16) {
                const int32_t *line_src = (const int32_t *)src;
                uint32_t *line_end = dest + 32;
                for (; dest != line_end; line_src += stride, dest += 4) {
                    const int32_t pixel0 = BSWAP32(line_src[0]);
                    const int32_t pixel1 = BSWAP32(line_src[1]);
                    const int32_t pixel2 = BSWAP32(line_src[2]);
                    const int32_t pixel3 = BSWAP32(line_src[3]);
                    dest[0] = (transparent && !(pixel0<0)) ? transparent_pixel
                              : adjust_color_32_32(pixel0, rofs, gofs, bofs);
                    dest[1] = (transparent && !(pixel1<0)) ? transparent_pixel
                              : adjust_color_32_32(pixel1, rofs, gofs, bofs);
                    dest[2] = (transparent && !(pixel2<0)) ? transparent_pixel
                              : adjust_color_32_32(pixel2, rofs, gofs, bofs);
                    dest[3] = (transparent && !(pixel3<0)) ? transparent_pixel
                              : adjust_color_32_32(pixel3, rofs, gofs, bofs);
                }
            }
            if (outwidth > width) {
                int line;
                for (line = 0; line < 8; line++) {
                    dest[line*4] = dest[line*4-32];
                }
            }
        }

    } else if (transparent) {

        unsigned int y;
        for (y = 0; y < height; y += 8, src += stride*32 - width*4, dest += (tex_stride - width)*8) {
            uint32_t *dest_top = dest + width*8;
            for (; dest != dest_top; src += 16) {
                const int32_t *line_src = (const int32_t *)src;
                uint32_t *line_end = dest + 32;
                for (; dest != line_end; line_src += stride, dest += 4) {
                    const int32_t pixel0 = BSWAP32(line_src[0]);
                    const int32_t pixel1 = BSWAP32(line_src[1]);
                    const int32_t pixel2 = BSWAP32(line_src[2]);
                    const int32_t pixel3 = BSWAP32(line_src[3]);
                    dest[0] = (pixel0 >= 0) ? 0 : 0xFF000000 | pixel0;
                    dest[1] = (pixel1 >= 0) ? 0 : 0xFF000000 | pixel1;
                    dest[2] = (pixel2 >= 0) ? 0 : 0xFF000000 | pixel2;
                    dest[3] = (pixel3 >= 0) ? 0 : 0xFF000000 | pixel3;
                }
            }
            if (outwidth > width) {
                int line;
                for (line = 0; line < 8; line++) {
                    dest[line*4] = dest[line*4-32];
                }
            }
        }

    } else {  // !(rofs | gofs | bofs) && !transparent

        unsigned int y;
        for (y = 0; y < height; y += 8, src += stride*32 - width*4, dest += (tex_stride - width)*8) {
            uint32_t *dest_top = dest + width*8;
            for (; dest != dest_top; src += 16) {
                const int32_t *line_src = (const int32_t *)src;
                uint32_t *line_end = dest + 32;
                for (; dest != line_end; line_src += stride, dest += 4) {
                    const int32_t pixel0 = BSWAP32(line_src[0]);
                    const int32_t pixel1 = BSWAP32(line_src[1]);
                    const int32_t pixel2 = BSWAP32(line_src[2]);
                    const int32_t pixel3 = BSWAP32(line_src[3]);
                    dest[0] = 0xFF000000 | pixel0;
                    dest[1] = 0xFF000000 | pixel1;
                    dest[2] = 0xFF000000 | pixel2;
                    dest[3] = 0xFF000000 | pixel3;
                }
            }
            if (outwidth > width) {
                int line;
                for (line = 0; line < 8; line++) {
                    dest[line*4] = dest[line*4-32];
                }
            }
        }

    }  // if (rofs | gofs | bofs)

    if (outheight > height) {
        const uint32_t *src32;
        if ((height & 7) != 0) {
            dest = dest - tex_stride*8 + (height & 7)*4;
            src32 = dest - 4;
        } else {
            src32 = dest - tex_stride*8 + 7*4;
        }
        uint32_t *dest_top = dest + tex_stride;
        for (; dest < dest_top; src32 += 32, dest += 32) {
            const uint32_t pixel0 = src32[0];
            const uint32_t pixel1 = src32[1];
            const uint32_t pixel2 = src32[2];
            const uint32_t pixel3 = src32[3];
            dest[0] = pixel0;
            dest[1] = pixel1;
            dest[2] = pixel2;
            dest[3] = pixel3;
        }
    }

    return 1;
}

/*************************************************************************/
/*************************************************************************/

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
