/*
 * z64
 *
 * Copyright (C) 2007  ziggy
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 *
**/

#include "rdp.h"
#include "rgl.h"

#include <SDL.h>

rglTextureHead_t freeTextures;
rglTextureHead_t texturesByCrc[256];
rglTextureHead_t texturesByUsage;

void rglTouchTMEM()
{
    rglTexCacheCounter++;
    if (!rglTexCacheCounter) {
        // shouldn't happen too often but let's do things correctly for the hell of it ;)
        rglResetTextureCache();
    }
}

inline int crc8(uint32_t crc)
{
    uint8_t res;
    res = crc^(crc>>8)^(crc>>16)^(crc>>24);
    return res;
}

void rglDeleteTexture(rglTexture_t * tex)
{
    //LOG("deleting texture %x\n", tex);
    glDeleteTextures(1, &tex->id);
    if (tex->zid)
        glDeleteTextures(1, &tex->zid);
    rglAssert(glGetError() == GL_NO_ERROR);
    tex->id = tex->zid = 0;
    CIRCLEQ_REMOVE(&texturesByUsage, tex, byUsage);
    CIRCLEQ_REMOVE(&texturesByCrc[crc8(tex->crc)], tex, byCrc);
    CIRCLEQ_INSERT_TAIL(rglTexture_t, &freeTextures, tex, byUsage);
}

rglTexture_t * rglNewTexture(uint32_t crc)
{
    rglTexture_t * res;

    if (CIRCLEQ_EMPTY(&freeTextures))
        rglDeleteTexture(CIRCLEQ_FIRST(&texturesByUsage));

    res = CIRCLEQ_FIRST(&freeTextures);
    //LOG("new texture %x %x crc %x\n", res, crc, crc8(crc));
    CIRCLEQ_REMOVE(&freeTextures, res, byUsage);
    CIRCLEQ_INSERT_TAIL(rglTexture_t, &texturesByUsage, res, byUsage);
    CIRCLEQ_INSERT_TAIL(rglTexture_t, &texturesByCrc[crc8(crc)], res, byCrc);

    res->wt = res->ws = res->filter = 0;

    return res;
}

void rglInitTextureCache()
{
    int i;
    // initialize textures lists
    CIRCLEQ_INIT(rglTexture_t, &freeTextures);
    CIRCLEQ_INIT(rglTexture_t, &texturesByUsage);
    for (i=0; i<256; i++)
        CIRCLEQ_INIT(rglTexture_t, &texturesByCrc[i]);
    for (i=0; i<RGL_TEX_CACHE_SIZE; i++) {
        CIRCLEQ_INSERT_TAIL(rglTexture_t, &freeTextures, rglTextures+i, byUsage);
    }
}

void rglResetTextureCache()
{
    static int init;
    if (!init) {
        rglInitTextureCache();
        init = 1;
    }

    memset(rglTexCache, 0, sizeof(rglTexCache));
    rglTexCacheCounter = 1;
    while (!CIRCLEQ_EMPTY(&texturesByUsage))
        rglDeleteTexture(CIRCLEQ_FIRST(&texturesByUsage));

    rglInitTextureCache();
}

void rglTile(rdpTile_t & tile, rglTile_t & rtile, int recth)
{
    rglTexture_t * tex;
    int ws, wt;
    int line = tile.line;
    //int cs, ct;
    int clipw = ((tile.sh - tile.sl) >>2)+1;
    int cliph = ((tile.th - tile.tl) >>2)+1;
    int indirect = 1;
    uint8_t * from = rdpTmem;
    int ow, oh;

    //   if (recth && cliph == recth+1) // hack for Mario Party (not necessary if we handle filter for texrect)
    //     cliph = recth;

    //   if (tile.ms && tile.mask_s && (2<<tile.mask_s)<clipw)
    //     tile.ms = 0;
    //   if (tile.mt && tile.mask_t && (2<<tile.mask_t)<cliph)
    //     tile.mt = 0;
    //   if (tile.ms) clipw /= 2;
    //   if (tile.mt) cliph /= 2;

    if (!line) line = 1;

    //tile.format = ti_format;

    if (tile.size == 3) line <<= 1; // why why WHY ?
    //if (tile.size == 0) clipw *= 2;
    tile.w = line << 1 >> tile.size;
    //if (tile.mask_s && (1<<tile.mask_s) < tile.w*2) // HACK
    if (tile.mask_s && (1<<tile.mask_s) < tile.w)
        tile.w = 1<<tile.mask_s;
    if (tile.cs && ((clipw+3)&~3) < tile.w) // GL wants width divisible by 4 at least ?
        tile.w = ((clipw+3)&~3);

    tile.h = ((tile.th - tile.tl) >>2)+1; // FIXME why not cliph ???
    //tile.h = (tile.th >>2)+1;
    //   if (tile.h <= 0)
    //     tile.h = (tile.th >>2)+1;
    // FIXME remove test on mt ? 
    if (tile.mask_t && ((1<<tile.mask_t) < tile.h || (!tile.ct && !tile.mt)))
        tile.h = 1<<tile.mask_t;
    else
    {
        //     if (tile.h < 0 || (tile.h & 3)) {
        //       tile.h = 1; while (tile.h<(tile.th>>2)) tile.h <<= 1;
        //     }
    }

    //   if (!tile.mask_t && !tile.ct/* && !tile.mt*/)
    //     tile.h = (0x1000-tile.tmem)/line;

    //   if (tile.sl && !tile.mask_s) {
    //     printf("shifting sl %d\n", tile.sl);
    //     tile.tmem += tile.sl << tile.format >> 1;
    //     tile.tmem &= 0xfff;
    //     tile.sl = 0;
    //   }
    //   if (tile.tl && !tile.mask_t) {
    //     printf("shifting tl %d\n", tile.tl);
    //     tile.tmem += tile.tl * line;
    //     tile.tmem &= 0xfff;
    //     tile.tl = 0;
    //   }

    if (recth && tile.h == 1)
        // +1 for yoshi
        tile.h = recth+1;

    if (/*tile.h == 1 || */tile.w*tile.h << tile.size >> 1 > 0x1000-tile.tmem) {
        DUMP("fixing tile size from %dx%d to ", tile.w, tile.h);
        //tile.w = (line << 3) >> tile.size + 2;
        //tile.h = 1; while (tile.h<(tile.th>>2)) tile.h <<= 1;
        tile.h = (0x1000-tile.tmem)/line;
        DUMP("%dx%d\n", tile.w, tile.h);
    }

    // this is a warkaround for a bug in pj64 rsp plugin
    // now fixed
    if (0&&recth && /*tile.line == 8 && */tile.h == 1) {
        //LOG("direct\n");
        tile.w = rdpTiWidth << rdpTiSize >> tile.size;
        tile.h = recth;
        from = gfx.RDRAM + rdpTiAddress;
        if (recth > 1 || rdpTiWidth > 1)
            line = rdpTiWidth << rdpTiSize >> 1;
        indirect = 0;
    }

    {
        int fromLine, stop, fromFormat, fromSize;
        uint32_t address = rdpGetTmemOrigin(tile.tmem, &fromLine, &stop, &fromFormat, &fromSize);
        DUMP("tmem %x rdram %x\n", tile.tmem, address);
        if (address != (uint32_t)~0) {
            rglRenderBuffer_t * buffer;
            if (!fromLine) fromLine = line;
            if (!tile.mask_t)
                tile.h = (stop-tile.tmem)/line;
            rtile.hiresBuffer = 0;
            //while (0) {
            CIRCLEQ_FOREACH(rglRenderBuffer_t, buffer, &rBufferHead, link) {
                //if (buffer->flags & RGL_RB_DEPTH) continue;
                if (buffer->area.xh != 8192)
                    buffer->addressStop = buffer->addressStart + buffer->line * ((buffer->area.yl >>2)+1);

                // conservative
                //         if (address + tile.h * line > buffer->addressStart &&
                //             address < buffer->addressStop)
                if (address >= buffer->addressStart/* + buffer->line * ((buffer->area.yh >>2)+1)*/ && // oops cannot use yh, might not be initialized
                    address + tile.h * line <= buffer->addressStop)
                    DUMP("check %x --> %x with %x --> %x (%x %x %d %x)\n",
                    address, address + tile.h * line,
                    buffer->addressStart, buffer->addressStop,
                    fromLine, buffer->line, tile.h, line);

                // TODO store real address stop, it's not necessarily the same as
                // address + tile.h * line
                // conservative
                //         if (address + tile.h * line > buffer->addressStart &&
                //             address < buffer->addressStop &&
                // more strict (better for LEGO racer)
                // general solution would be : find all candidates, pick the one that covers
                // the biggest area
                if ((!rtile.hiresBuffer || buffer->addressStart > rtile.hiresBuffer->addressStart) &&
                    address >= buffer->addressStart/* + buffer->line * ((buffer->area.yh >>2)+1)*/ && // oops cannot use yh, might not be initialized
                    address + tile.h * line <= buffer->addressStop &&
                    (tile.h <= 1 || fromLine == buffer->line)) {
                        DUMP("texture buffer at %x %d x %d %d %d fmt %d fromfmt %d\n",
                            buffer->addressStart, tile.w, tile.h,
                            fromLine, buffer->line, tile.format, fromFormat);

                        rtile.hiresBuffer = buffer;
                        rtile.hiresAddress = address;

                        break;
                }
            }

            if (rtile.hiresBuffer) {
                // FIXME current buffer could be a depth buffer, in this case
                // we want the texture rendered as depth too
                rtile.hiresBuffer->flags &= ~RGL_RB_DEPTH;
                //rglRenderChunks(rtile.hiresBuffer);
            }

            if (rglSettings.hiresFb && rtile.hiresBuffer) {
                memcpy(&rtile, &tile, sizeof(tile));
                return;
            }

            if (rtile.hiresBuffer) {
                LOG("updating rdram %x\n", address);
                rglFramebuffer2Rdram(*rtile.hiresBuffer, address, address + tile.h * line);
                line = fromLine;
                from = gfx.RDRAM + address;
                indirect = 0;
            }

        }
    }
    rtile.hiresBuffer = 0;

    if (tile.w > 1024) tile.w = 1024;
    if (tile.h > 1024) tile.h = 1024;

    ow = tile.w; oh = tile.h; // save w/h before making it a power of 2
    {
        int w=1, h=1;
        while (w < tile.w) w*=2;
        while (h < tile.h) h*=2;
        tile.w = rtile.w = w;
        tile.h = rtile.h = h;
    }

    memcpy(&rtile, &tile, sizeof(tile));
    rtile.line = line;

    // NOTE more general solutions would involve subdividing the geometry
    // or writing clamping/mirroring in glsl
    int badmirror_s =
        tile.mask_s && tile.cs && tile.ms && (clipw/2) < (1<<tile.mask_s);
    int badmirror_t =
        tile.mask_t && tile.ct && tile.mt && (cliph/2) < (1<<tile.mask_t);
    int clipmw = clipw, clipmh = cliph;
    if (tile.ms && !badmirror_s) clipmw /= 2;
    if (tile.mt && !badmirror_t) clipmh /= 2;
    int badclamp_s =
        tile.mask_s && tile.cs && clipmw > (1<<tile.mask_s);
    int badclamp_t =
        tile.mask_t && tile.ct && clipmh > (1<<tile.mask_t);

    int npot_s = (tile.w-1)&tile.w;
    int npot_t = (tile.h-1)&tile.h;

    ws = GL_REPEAT;
    //ws = GL_CLAMP_TO_EDGE;
    if ((!tile.mask_s || tile.cs) && !badclamp_s) {
        //     tile.tmem += (tile.sl>>2) << tile.size >> 1;
        //     tile.sh -= tile.sl;
        //     tile.sl = 0;
        ws = GL_CLAMP_TO_EDGE;
    }
    if (tile.ms && !badmirror_s)
        ws = ((!tile.mask_s || tile.cs) && !badclamp_s)?
GL_MIRROR_CLAMP_TO_EDGE_EXT : GL_MIRRORED_REPEAT;

    wt = GL_REPEAT;
    //wt = GL_CLAMP_TO_EDGE;
    if ((!tile.mask_t || tile.ct) && !badclamp_t) {
        //     tile.tmem += (tile.tl>>2) * line;
        //     tile.th -= tile.tl;
        //     tile.tl = 0;
        wt = GL_CLAMP_TO_EDGE;
    }
    if (tile.mt && !badmirror_t)
        wt = ((!tile.mask_t || tile.ct) && !badclamp_t)?
GL_MIRROR_CLAMP_TO_EDGE_EXT : GL_MIRRORED_REPEAT;

#if 1
    if ((npot_s||npot_t) && ws != GL_CLAMP_TO_EDGE) {
        //LOG("Fixup npot clamp s\n");
        ws = GL_CLAMP_TO_EDGE;
    }
    if ((npot_t||npot_s) && wt != GL_CLAMP_TO_EDGE) {
        //LOG("Fixup npot clamp t\n");
        wt = GL_CLAMP_TO_EDGE;
    }
#else  
    //   ws = GL_CLAMP_TO_EDGE;
    //   wt = GL_CLAMP_TO_EDGE;
#endif

    rtile.ws = ws;
    rtile.wt = wt;

    rglAssert(!(tile.tmem & ~ 0xfff));
    if (rglTexCache[tile.tmem].counter == rglTexCacheCounter &&
        rglTexCache[tile.tmem].tex->fmt == tile.format &&
        rglTexCache[tile.tmem].tex->w == tile.w
        &&
        rglTexCache[tile.tmem].tex->h == tile.h
        //       rglTexCache[tile.tmem].tex->h > (tile.th>>2)
        ) {
            tex = rglTexCache[tile.tmem].tex;
            goto ok;
    }

    //     printf("tile #%d fmt %s sz %d w %d mask %d %dx%d (%d %d)\n", &tile-rdpTiles, rdpImageFormats[tile.format], tile.size, line, tile.mask_s, (tile.sh - tile.sl >>2)+1, (tile.th - tile.tl >>2)+1, tile.sl>>2, tile.tl>>2);

    //rglAssert(tile.w == (tile.sh - tile.sl >>2)+1);

    {
        int h, i, j, x, y;
        int palette = 0;
        uint32_t crc = 0;
        rglTextureHead_t * list;

#if 1
        if (tile.format == RDP_FORMAT_CI ||
            (tile.size <= 1 && RDP_GETOM_EN_TLUT(rdpState.otherModes))) {
                // tlut crc
                h = tile.size? 256:16;
                if (tile.size == 0) palette = (tile.palette<<4)&0xff;
                for (i=0; i<h; i++)
                    crc = ((crc>>3)|(crc<<(32-3)))+(rdpTlut[(i+palette)*4]);
        }

        for (y=0; y<oh; y++) {
            uint32_t * p = (uint32_t *) &from[(tile.tmem + y*line)/*&0x3fff*/];
            for (x=0; x<(line>>2); x++)
                crc = ((crc>>3)|(crc<<(32-3)))+(*p++);
        }

        list = texturesByCrc + crc8(crc);
        CIRCLEQ_FOREACH(rglTexture_t, tex, list, byCrc) {
            //LOG("comparing %x with %x\n", tex->crc, crc);
            if (tex->crc == crc &&
                tex->fmt == tile.format &&
                tex->clipw >= clipw &&
                tex->cliph >= cliph &&
                tex->w == tile.w &&
                tex->h >= tile.h) {
                    CIRCLEQ_REMOVE(&texturesByUsage, tex, byUsage);
                    CIRCLEQ_INSERT_TAIL(rglTexture_t, &texturesByUsage, tex, byUsage);
                    goto ok2;
            }
            //     if (tex->crc == crc)
            //       LOG("Same CRC %x !\n", crc);
        }
#endif

        tex = rglNewTexture(crc);
        tex->fmt = tile.format;
        tex->w = tile.w;
        tex->h = tile.h;
        tex->clipw = clipw;
        tex->cliph = cliph;
        tex->crc = crc;
        glGenTextures(1, &tex->id);
        rglAssert(glGetError() == GL_NO_ERROR);


        glBindTexture(GL_TEXTURE_2D, tex->id);
        rglAssert(glGetError() == GL_NO_ERROR);
        uint8_t * ptr;
        GLuint packed = 0;
        GLuint glfmt = 0, glpixfmt = 0;

        ptr = rglTmpTex2;

#define XOR_SWAP_BYTE	3
#define XOR_SWAP_WORD	2
#define XOR_SWAP_DWORD	2
        // ugly but it works ...
        if (tile.cs || !tile.mask_s) ow = tile.w;
        if (tile.ct || !tile.mask_t) oh = tile.h;
#define CLAMP                                                             \
    int ci = i;                                                           \
    int cj = j;                                                           \
    if ((tile.cs || !tile.mask_s) && ci >= clipw) ci = clipw-1;           \
    if ((tile.ct || !tile.mask_t) && cj >= cliph) cj = cliph-1;           \

        switch (tile.size) {
    case 3:
        for (j=0; j<oh; j++)
            for (i=0; i<ow; i++) {
                CLAMP;
                uint32_t *tc = (uint32_t*)from;
                int taddr = ((tile.tmem/4) + ((cj) * (line/4)) + (ci)) ^ ((cj & indirect) ? XOR_SWAP_DWORD : 0);
                uint32_t a = tc[taddr/*&0xfff*/];
                //uint32_t a = *(uint32_t *)&from[j*line + i*4 + tile.tmem ^ ((j&1)<<1) ^ XOR_SWAP_DWORD];
                *(uint32_t *)&ptr[(tile.h-1-j)*tile.w*4 + (tile.w-1-i)*4] = a;
            }
            break;
    case 2:
        for (j=0; j<oh; j++)
            for (i=0; i<ow; i++) {
                CLAMP;
                uint16_t *tc = (uint16_t*)from;
                int taddr = ((tile.tmem/2) + ((cj) * (line/2)) + (ci)) ^ ((cj & indirect) ? XOR_SWAP_WORD : 0);
                uint16_t a = tc[(taddr ^ WORD_ADDR_XOR)/*&0x1fff*/];
                //           uint16_t a = *(uint16_t *)&from[j*line + i*2 + tile.tmem ^ ((j&1)<<2) ^ XOR_SWAP_WORD];
                *(uint16_t *)&ptr[(tile.h-1-j)*tile.w*2 + (tile.w-1-i)*2] = a;
            }
            break;
    case 1:
        for (j=0; j<oh; j++)
            for (i=0; i<ow; i++) {
                CLAMP;
                uint8_t a = *(uint8_t *)&from[((cj*line + ci + tile.tmem) ^ ((cj & indirect)<<2) ^ XOR_SWAP_BYTE)/*&0xfff*/];
                *(uint8_t *)&ptr[(tile.h-1-j)*tile.w + (tile.w-1-i)] = a;
            }
            break;
    case 0:
        // FIXME
        for (j=0; j<tile.h; j++)
            for (i=0; i<tile.w; i+=2) {
                CLAMP;
                uint8_t a = *(uint8_t *)&from[((cj*line + ci/2 + tile.tmem) ^ ((cj & indirect)<<2) ^ XOR_SWAP_BYTE)/*&0x3fff*/];
                *(uint8_t *)&ptr[(tile.h-1-j)*tile.w/2 + (tile.w/2-1-i/2)] = a; //(a>>4)|(a<<4);
            }
            break;
        }
        from = ptr;

        i = tile.format;

        // in Tom Clancy, they do this, using I texture with TLUT enabled
        if (i != RDP_FORMAT_CI && tile.size <= 1 && RDP_GETOM_EN_TLUT(rdpState.otherModes)) {
            LOG("fixing %s-%d tile to CI\n", rdpImageFormats[i], tile.size);
            i = RDP_FORMAT_CI;
        }

        if (tile.size <= 1 && i == RDP_FORMAT_RGBA) {
            LOG("fixing RGBA tile to I\n");
            i = RDP_FORMAT_I;
        }

        switch (i) {
    case RDP_FORMAT_CI: {
        if (!RDP_GETOM_TLUT_TYPE(rdpState.otherModes)) {
            glfmt = GL_RGBA;
            packed = GL_UNSIGNED_SHORT_5_5_5_1;
        } else {
            glfmt = GL_RGBA;
            glpixfmt = GL_LUMINANCE_ALPHA;
            //glfmt = GL_LUMINANCE_ALPHA;
            packed = GL_UNSIGNED_BYTE;
        }
        switch (tile.size) {
    case 0:
        ptr = rglTmpTex;
        for (i=0; i<tile.w*tile.h/2; i++) {
            uint16_t a = rdpTlut[((from[i]&0xf) + palette/* ^ WORD_ADDR_XOR*/)*4];
            uint16_t b = rdpTlut[((from[i]>>4) + palette/* ^ WORD_ADDR_XOR*/)*4];
            if (RDP_GETOM_TLUT_TYPE(rdpState.otherModes)) {
                a = (a>>8)|(a<<8);
                b = (b>>8)|(b<<8);
            }
            *(uint16_t *)&ptr[i*4] = a;
            *(uint16_t *)&ptr[i*4+2] = b;
        }
        break;
    case 1:
        ptr = rglTmpTex;
        //rdpTlut[palette] = 0;
        for (i=0; i<tile.w*tile.h; i++) {
            uint16_t a = rdpTlut[(from[i] + palette/* ^ WORD_ADDR_XOR*/)*4];
            if (RDP_GETOM_TLUT_TYPE(rdpState.otherModes))
                a = (a>>8)|(a<<8);
            *(uint16_t *)&ptr[i*2] = a;
        }
        break;
        }
        break;
                        }
    case RDP_FORMAT_RGBA: {
        glfmt = GL_RGBA;
        switch (tile.size) {
    case 2:
        //packed = GL_UNSIGNED_SHORT_4_4_4_4_REV;
        packed = GL_UNSIGNED_SHORT_5_5_5_1;
        break;
    case 3:
        packed = GL_UNSIGNED_INT_8_8_8_8;
        break;
        }
        break;
                          }
    case RDP_FORMAT_IA: {
        glfmt = GL_RGBA;
        glpixfmt = GL_LUMINANCE_ALPHA;
        //if (tile.size == 0) line *= 2;
        switch (tile.size) {
    case 0: {
        packed = GL_UNSIGNED_BYTE;
        ptr = rglTmpTex;
        for (i=0; i<tile.h*tile.w/2; i++) {
            uint32_t a = (from[i]&0xe0) >> 5;
            int8_t b = (from[i]&0x10) >> 4;
            ptr[i*4+2] = (a<<5) | (a<<2) | (a>>1);
            ptr[i*4+3] = -b;
            a = (from[i]&0xe) >> 1;
            b = (from[i]&0x1);
            ptr[i*4+0] = (a<<5) | (a<<2) | (a>>1);
            ptr[i*4+1] = -b;
        }
        break;
            }
    case 1: {
        packed = GL_UNSIGNED_BYTE;
        ptr = rglTmpTex;
        for (i=0; i<tile.h*tile.w; i++) {
            uint32_t a = from[i]&0xF0;
            a = a | (a>>4);
            ptr[i*2] = a | (a>>4);
            a = from[i]&0x0F;
            a = a | (a<<4);
            ptr[i*2+1] = a;
        }
        break;
            }
    case 2:
        packed = GL_UNSIGNED_BYTE;
        ptr = rglTmpTex;
        for (i=0; i<tile.h*tile.w*2; i+=2) {
            ptr[i] = from[i+1];
            ptr[i+1] = from[i];
        }
        break;
        }
        break;
                        }
    case RDP_FORMAT_I: {
        glfmt = GL_INTENSITY;
        //       if (RDP_GETOM_ALPHA_CVG_SELECT(rdpState.otherModes))
        //         glfmt = GL_LUMINANCE;
        glpixfmt = GL_LUMINANCE;
        switch (tile.size) {
    case 0: {
        packed = GL_UNSIGNED_BYTE;
        ptr = rglTmpTex;
        for (i=0; i<tile.h*tile.w/2; i++) {
            uint32_t a = from[i]&0xF0;
            ptr[i*2+1] = a | (a>>4);
            a = from[i]&0x0F;
            ptr[i*2] = a | (a<<4);
        }
        break;
            }
    case 1: {
        packed = GL_UNSIGNED_BYTE;
        break;
            }
        }
        break;
                       }
        }

        if (packed) {
            DUMP("loading texture %dx%d fmt %s size %x (%x %x %x %p)\n", tile.w, tile.h, rdpImageFormats[tile.format], tile.size, glfmt, glpixfmt, packed, ptr);
            //     printf("cycle type = %d\n", chunk.rdpState.otherModes.cycle_type);
            if (!glpixfmt)
                glpixfmt = glfmt;
            rglAssert(glGetError() == GL_NO_ERROR);
            glTexImage2D(GL_TEXTURE_2D, 0, glfmt, tile.w, tile.h, 0, glpixfmt, packed,
                ptr);
            rglAssert(glGetError() == GL_NO_ERROR);


#if 0
            if (1||RDP_GETOM_CYCLE_TYPE(rdpState.otherModes) == RDP_CYCLE_TYPE_COPY) {
                uint32_t * pixels = (uint32_t *) malloc(tile.w*tile.h*4);
                // 0x1902 is another constant meaning GL_DEPTH_COMPONENT
                // (but isn't defined in gl's headers !!)
                if (1/*fmt != GL_DEPTH_COMPONENT && fmt != 0x1902*/) {
                    glGetTexImage(GL_TEXTURE_2D, 0, GL_RGBA, GL_UNSIGNED_BYTE, pixels);
                    ilTexImage(tile.w, tile.h, 1, 4, IL_RGBA, IL_UNSIGNED_BYTE, pixels);
                } else {
                    glGetTexImage(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, GL_UNSIGNED_SHORT, pixels);
                    int i;
                    for (i=0; i<tile.w*tile.h; i++)
                        ((unsigned char *)ptr)[i] = ((unsigned short *)pixels)[i]/256;
                    ilTexImage(tile.w, tile.h, 1, 1, IL_LUMINANCE, IL_UNSIGNED_BYTE, ptr);
                }
                char name[128];
                //     sprintf(name, "mkdir -p dump ; rm -f dump/tex%04d.png", i);
                //     system(name);
                static int num;
                sprintf(name, "dump/tex%04d-%s-%d-%d-%d.png", num++, rdpImageFormats[tile.format], tile.size, &tile - rdpTiles, tile.tmem);
                fprintf(stderr, "Writing '%s'\n", name);
                ilSaveImage(name);

                free(pixels);
            }
#endif
        }
        if (!packed) {
            LOGERROR("unsuported format %s size %d\n", rdpImageFormats[tile.format], tile.size);
        }


    }
ok2:
    rglTexCache[tile.tmem].counter = rglTexCacheCounter;
    rglTexCache[tile.tmem].tex = tex;

ok:
    rtile.tex = tex;

    {
        GLuint filter;
        if (recth) {
            switch (RDP_GETOM_SAMPLE_TYPE(rdpState.otherModes)) {
        case 0:
            filter = GL_NEAREST;
            break;
        default:
            filter = GL_LINEAR;
            break;
            }
        } else
            filter = GL_LINEAR;

        rtile.filter = filter;
        //     glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, filter);
        //     glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, filter);
    }
}

