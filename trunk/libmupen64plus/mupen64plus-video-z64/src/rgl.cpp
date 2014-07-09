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

/*
 *
 * TODO
 *
 * - hires framebuffer : scale them accordingly to the GL screen resolution
 *
 * - multitexturing avec tilenum == 7, ca marche comment ?
 *   --> tilenum=7 should be translated to tilenum=0 for triangle and texrec primitives
 *
 * - CvgXAlpha mode not really correct, effect probably depends on the texture format
 *   --> apparently fixed, Intensity textures shouldn't be affected by this effect
 *   --> correction : it had nothing to do with the texture format, but with
 *       the alpha_cvg_select flag
 *
 * - fix fbo depth clear in LEGO racer (also affects beetle and reflection in rally 99)
 *   (also affects Zelda OOT subscreen)
 *   --> DONE
 *
 * - frame buffer ordering (LEGO racer)
 *   --> DONE but required also less conservative framebuffer check
 *
 * - format conversion for hires framebuffers when required (CBFD , Banjo)
 *   --> DONE (quick hack)
 *
 * - some texture (4 ? 8 bits ?) problems
 *  --> either they're fixed, either I forgot which problem it was
 *
 * - mirrored textures
 *  --> done but rely on a GL extension, do we care ?
 *
 * - better blend
 *  --> mostly done slow way, now need to implement the quick way
 *  --> done faster way, seems to work reasonably well
 *
 * - need to sort out combiner clamp modes
 *  --> started but not complete
 *  --> the problem is much more complicated, it's not combiner clamping
 *      but coverage calculation modes.
 *
 * - fog !!
 *  --> done
 *
 * PROBLEMS
 *
 * - this list needs to be updated :)
 * - links not always rendered in the subscreen
 *   --> depth clear problem, anything else ? FIXED
 * - texture problems in beetle
 *   --> mostly fixed, it was multitexturing, the sky still has a weird problem though
 *   --> completely fixed at last (the sky uses tex2 in the second combiner cycle,
 *       which should be interpreted as tex1 (apparently tex2 isn't available in the
 *       second cycle)
 *       UPDATE : in fact tex1 and tex2 need to be swapped in the second step of
 *                the combiner, weird but it fixes a few other problems as well
 *
**/

#include "rdp.h"
#include "rgl.h"

#include <SDL.h>

//#define NOFBO
#define ZTEX
#define FBORGBA

rglTexCache_t rglTexCache[0x1000];
uint8_t rglTmpTex[1024*1024*4];
uint8_t rglTmpTex2[1024*1024*4];

volatile int rglStatus, rglNextStatus;

static int wireframe;

static uint32_t old_vi_origin;

int rglFrameCounter;

extern int viewportOffset;
rglSettings_t rglSettings;

rglDepthBuffer_t zBuffers[MAX_DEPTH_BUFFERS];
int nbZBuffers;

rglRenderBuffer_t rBuffers[MAX_RENDER_BUFFERS];
int nbRBuffers;
rglRenderBuffer_t * curRBuffer;
rglRenderBuffer_t * curZBuffer;
rglRenderBufferHead_t rBufferHead;

int rglTexCacheCounter = 1;

rglTexture_t rglTextures[RGL_TEX_CACHE_SIZE];

rglRenderChunk_t chunks[MAX_RENDER_CHUNKS];
rglRenderChunk_t * curChunk;
int nbChunks, renderedChunks;

rglStrip_t strips[MAX_STRIPS];
rglVertex_t vtxs[6*MAX_STRIPS];
int nbStrips, nbVtxs;

rglRenderMode_t renderModesDb[MAX_RENDER_MODES];
int nbRenderModes;

rglShader_t * rglCopyShader;
rglShader_t * rglCopyDepthShader;

int rglScreenWidth = 320, rglScreenHeight = 240;

#define CHECK_FRAMEBUFFER_STATUS() \
{\
    GLenum status; \
    status = glCheckFramebufferStatusEXT(GL_FRAMEBUFFER_EXT); \
    /*LOGERROR("%x\n", status);*/\
    switch(status) { \
 case GL_FRAMEBUFFER_COMPLETE_EXT: \
 /*LOGERROR("framebuffer complete!\n");*/\
 break; \
 case GL_FRAMEBUFFER_UNSUPPORTED_EXT: \
 LOGERROR("framebuffer GL_FRAMEBUFFER_UNSUPPORTED_EXT\n");\
 /* you gotta choose different formats */ \
 /*rglAssert(0);*/ \
 break; \
 case GL_FRAMEBUFFER_INCOMPLETE_ATTACHMENT_EXT: \
 LOGERROR("framebuffer INCOMPLETE_ATTACHMENT\n");\
 break; \
 case GL_FRAMEBUFFER_INCOMPLETE_MISSING_ATTACHMENT_EXT: \
 LOGERROR("framebuffer FRAMEBUFFER_MISSING_ATTACHMENT\n");\
 break; \
 case GL_FRAMEBUFFER_INCOMPLETE_DIMENSIONS_EXT: \
 LOGERROR("framebuffer FRAMEBUFFER_DIMENSIONS\n");\
 break; \
 case GL_FRAMEBUFFER_INCOMPLETE_FORMATS_EXT: \
 LOGERROR("framebuffer INCOMPLETE_FORMATS\n");\
 break; \
 case GL_FRAMEBUFFER_INCOMPLETE_DRAW_BUFFER_EXT: \
 LOGERROR("framebuffer INCOMPLETE_DRAW_BUFFER\n");\
 break; \
 case GL_FRAMEBUFFER_INCOMPLETE_READ_BUFFER_EXT: \
 LOGERROR("framebuffer INCOMPLETE_READ_BUFFER\n");\
 break; \
 case GL_FRAMEBUFFER_BINDING_EXT: \
 LOGERROR("framebuffer BINDING_EXT\n");\
 break; \
 default: \
 LOGERROR("framebuffer generic error\n");\
 break; \
 /* programming error; will fail on all hardware */ \
 /*rglAssert(0);*/ \
}\
}
/*
  case GL_FRAMEBUFFER_INCOMPLETE_DUPLICATE_ATTACHMENT_EXT: \
    LOGERROR("framebuffer INCOMPLETE_DUPLICATE_ATTACHMENT\n");\
    break; \
*/

rglDepthBuffer_t * rglFindDepthBuffer(uint32_t address, int width, int height)
{
    int i;
    rglDepthBuffer_t * buffer;
    for (i=0; i<nbZBuffers; i++)
        if (zBuffers[i].address == address &&
            zBuffers[i].width == width &&
            zBuffers[i].height == height)
            return zBuffers+i;

    rglAssert(nbZBuffers < MAX_DEPTH_BUFFERS);
    buffer = zBuffers + nbZBuffers++;

    LOG("Creating depth buffer %x %d x %d\n", address, width, height);

    buffer->address = address;
    buffer->width = width;
    buffer->height = height;

    //   glGenRenderbuffersEXT(1, &buffer->zbid);
    //   glBindRenderbufferEXT(GL_RENDERBUFFER_EXT, buffer->zbid);
    //   rglAssert(glGetError() == GL_NO_ERROR);
    //   glRenderbufferStorageEXT( GL_RENDERBUFFER_EXT, GL_DEPTH_COMPONENT,
    //                                 buffer->width, buffer->height);
    //   rglAssert(glGetError() == GL_NO_ERROR);

#ifdef ZTEX
    glGenTextures(1, &buffer->zbid);
    rglAssert(glGetError() == GL_NO_ERROR);
    glBindTexture(GL_TEXTURE_2D, buffer->zbid);
    rglAssert(glGetError() == GL_NO_ERROR);
    glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT,
        buffer->width, buffer->height, 0,
        GL_DEPTH_COMPONENT, GL_UNSIGNED_SHORT, NULL);
    rglAssert(glGetError() == GL_NO_ERROR);
    glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
    glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
    glBindTexture(GL_TEXTURE_2D, 0);
#else
    glGenRenderbuffersEXT(1, &buffer->zbid);
    glBindRenderbufferEXT(GL_RENDERBUFFER_EXT, buffer->zbid);
    glRenderbufferStorageEXT(GL_RENDERBUFFER_EXT, GL_DEPTH_COMPONENT, 
        buffer->width, buffer->height);

#endif

    return buffer;
}

void rglDeleteRenderBuffer(rglRenderBuffer_t & buffer)
{
    buffer.mod.xl = buffer.mod.yl = 0;
    buffer.mod.xh = buffer.mod.yh = 8192;
    buffer.depthBuffer = 0;
#ifndef NOFBO
    if (buffer.fbid) {
        glDeleteFramebuffersEXT(1, &buffer.fbid);
        buffer.fbid = 0;
    }
    if (buffer.texid) {
        glDeleteTextures(1, &buffer.texid);
        buffer.texid = 0;
    }
    buffer.nbDepthSections = 0;
#ifdef RGL_EXACT_BLEND
    glDeleteFramebuffersEXT(1, &buffer.fbid2);
    buffer.fbid2 = 0;
    glDeleteTextures(1, &buffer.texid2);
    buffer.texid2 = 0;
#endif
#endif
}

void rglFullSync()
{
    if (rglSettings.forceSwap)
        // hack for starwars, perfect dark subscreen to prevent filling up our chunk table
        old_vi_origin = ~0; 
}

// note : if "same" is 1 then both tiles use the same texture, in this
//        case we can't safely modify the clamping mode
void rglFixupMapping(rglStrip_t & strip, rglTile_t & tile,
                     float ds, float dt, float ss, float st,
                     float & dsm, float & dtm, int same)
{
    float mins = strip.vtxs[0].s;
    float mint = strip.vtxs[0].t;
    int i;
    if ( (tile.mask_s && !tile.cs) || (tile.mask_t && !tile.ct) )
        for (i=1; i<strip.nbVtxs; i++) {
            if (strip.vtxs[i].s < mins)
                mins = strip.vtxs[i].s;
            if (strip.vtxs[i].t < mint)
                mint = strip.vtxs[i].t;
        }
    if (tile.mask_s && !tile.cs)
        dsm = -((int(mins+0.5f - tile.sl*float(1<<(tile.shift_s+4))/64.0f) + (tile.ms<<tile.mask_s)) & ((~tile.ms)<<(tile.mask_s+tile.shift_s+4)>>4));
    else
        dsm = 0;
    if (tile.mask_t && !tile.ct)
        dtm = -((int(mint+0.5f - tile.tl*float(1<<(tile.shift_t+4))/64.0f) + (tile.mt<<tile.mask_t)) & ((~tile.mt)<<(tile.mask_t+tile.shift_t+4)>>4));
    else
        dtm = 0;

    if (rglSettings.hiresFb && tile.hiresBuffer)
        return;
    else {
        GLuint wws = tile.ws, wwt = tile.wt;

        if (same || wws != GL_REPEAT)
            goto skips;
        for (i=0; i<strip.nbVtxs; i++) {
            float a = (strip.vtxs[i].s + ds + dsm);
            if ((a-0.5f)/ss > 1 || (a+0.5f)/ss < 0) {
                goto skips;
            }
        }
        //LOG("fixing S clamp\n");
        wws = GL_CLAMP_TO_EDGE;
skips:
        if (tile.tex->ws != wws) {
            glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, wws);
            tile.tex->ws = wws;
        }

        if (same || wwt != GL_REPEAT)
            goto skipt;
        for (i=0; i<strip.nbVtxs; i++) {
            float a = (strip.vtxs[i].t + dt + dtm);
            if ((a-0.5f)/st > 1 || (a+0.5f)/st < 0)
                goto skipt;
        }
        //LOG("fixing T clamp\n");
        wwt = GL_CLAMP_TO_EDGE;
skipt:
        if (tile.tex->wt != wwt) {
            glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, wwt);
            tile.tex->wt = wwt;
        }
    }
}

int rglUseTile(rglTile_t & tile, float & ds, float & dt, float & ss, float & st)
{
    int res = 0;
    ds = -tile.sl*float(1<<(tile.shift_s+4))/64.0f;
    dt = -tile.tl*float(1<<(tile.shift_t+4))/64.0f;
    if (rglSettings.hiresFb && tile.hiresBuffer) {
        rglRenderBuffer_t & hbuf = *tile.hiresBuffer;
        //     if (hbuf.flags & RGL_RB_DEPTH) {
        //       glBindTexture(GL_TEXTURE_2D, hbuf.depthBuffer->zbid);
        //       res = RGL_COMB_IN0_DEPTH;
        //     } else
        glBindTexture(GL_TEXTURE_2D, hbuf.texid);
        rglAssert(glGetError() == GL_NO_ERROR);
        ss = -(hbuf.width<<(tile.shift_s+4)>>4);
        st = -(hbuf.height<<(tile.shift_t+4)>>4);
        ds = -ds - (((int32_t(tile.hiresAddress) - int32_t(hbuf.addressStart)) % hbuf.line) >> hbuf.size << 1);
        dt = -dt - (int32_t(tile.hiresAddress) - int32_t(hbuf.addressStart)) / hbuf.line;
        ss /= float(hbuf.realWidth)/hbuf.fboWidth;
        st /= float(hbuf.realHeight)/hbuf.fboHeight;
        ds = ss - ds;
        dt = st - dt;

        DUMP("texture fb %p shift %g x %g (scale %g x %g) tile %d x %d (sl %d tl %d)\n",
            &hbuf, ds, dt, ss, st, tile.w, tile.h,
            tile.sl, tile.tl);
    } else {
        glBindTexture(GL_TEXTURE_2D, tile.tex->id);
        rglAssert(glGetError() == GL_NO_ERROR);
        ss = tile.w<<(tile.shift_s+4)>>4; st = tile.h<<(tile.shift_t+4)>>4;

        if (tile.tex->filter != tile.filter) {
            glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, tile.filter);
            glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, tile.filter);
            rglAssert(glGetError() == GL_NO_ERROR);
            tile.tex->filter = tile.filter;
        }      
    }
    return res;
}

void rglPrepareFramebuffer(rglRenderBuffer_t & buffer)
{
    //int olderased = buffer.flags & RGL_RB_ERASED;

    if (buffer.area.xh == 8192)
        return;

    GLuint restoreId = 0, restoreFbid = 0;
    float d2 = -1;
    float d = 0;
    float restoreW = buffer.width+d2, restoreH = buffer.height+d2;
    int w, h;
    restoreW *= float(buffer.fboWidth+d) / (buffer.realWidth+d);
    restoreH *= float(buffer.fboHeight+d) / (buffer.realHeight+d);

    buffer.flags &= ~RGL_RB_ERASED;

    //     buffer.width = ((buffer.area.xl - buffer.area.xh >>2) + 15)&~15;
    //     buffer.height = ((buffer.area.yl - buffer.area.yh >>2) + 15)&~15;
    //     buffer.width = ((buffer.area.xl >>2) + 3)&~3;
    //     buffer.height = ((buffer.area.yl >>2) + 3)&~3;
    //buffer.width = ((buffer.area.xl >>2))&~7;
    buffer.width = buffer.fbWidth;
    //buffer.height = ((buffer.area.yl >>2))&~7;
    buffer.height = ((buffer.area.yl >>2));
    if (!buffer.width) buffer.width = 1;
    if (!buffer.height) buffer.height = 1;

    buffer.addressStop = buffer.addressStart + buffer.line * ((buffer.area.yl >>2)+1);

    if (rglSettings.lowres) {
        buffer.realWidth = buffer.width;
        buffer.realHeight = buffer.height;
    } else {
        if (buffer.width <= 128 || buffer.height <= 128) {
            buffer.realWidth = buffer.width*4;
            buffer.realHeight = buffer.height*4;
            buffer.flags &= ~RGL_RB_FULL;
        } else {
            buffer.realWidth = screen_width * buffer.width / rglScreenWidth;
            buffer.realHeight = screen_height * buffer.height / rglScreenHeight;
            //     buffer.realWidth = screen_width * buffer.width / vi_width;
            //     if (buffer.height > 250)
            //       buffer.realHeight = screen_height * buffer.height / 480;
            //     else
            //       buffer.realHeight = screen_height * buffer.height / 240;
            buffer.flags |= RGL_RB_FULL;
        }
    }

    if (rglSettings.noNpotFbos) {
        w = 1; h = 1;
        while (w < buffer.realWidth) w <<= 1;
        while (h < buffer.realHeight) h <<= 1;
    } else {
        w = buffer.realWidth;
        h = buffer.realHeight;
    }

#ifndef NOFBO
    if (buffer.fboWidth == w && buffer.fboHeight == h)
        buffer.redimensionStamp = rglFrameCounter;

    if (buffer.fbid &&
        (//buffer.fboWidth < w || buffer.fboHeight < h ||
        (rglFrameCounter - buffer.redimensionStamp > 4))) {
            LOG("Redimensionning buffer\n");
            restoreId = buffer.texid;
            restoreFbid = buffer.fbid;
            buffer.texid = buffer.fbid = 0;
            rglDeleteRenderBuffer(buffer);
    }

    DUMP("Render buffer %p at %x --> %x\n", &buffer,
        buffer.addressStart, buffer.addressStop);

    if (!buffer.fbid) {
        int glfmt;
        switch (buffer.format) {
            //       case RDP_FORMAT_I:
            //       case RDP_FORMAT_CI:
            //         glfmt = GL_LUMINANCE;
            //         break;
      default:
#ifdef FBORGBA
          glfmt = GL_RGBA;
#else
          glfmt = GL_RGB;
#endif
        }

        LOG("creating fbo %x %dx%d (%dx%d) fmt %x\n", buffer.addressStart, buffer.width, buffer.height, w, h, buffer.format);

        buffer.fboWidth = w;
        buffer.fboHeight = h;

#ifdef RGL_EXACT_BLEND
        glGenFramebuffersEXT(1, &buffer.fbid2);
        rglAssert(glGetError() == GL_NO_ERROR);
        glBindFramebufferEXT(GL_FRAMEBUFFER_EXT, buffer.fbid2);

        // FIXME we should not need to allocate a color texture for depth only rendering
        if (1||!(buffer.flags & RGL_RB_DEPTH)) {
            glGenTextures(1, &buffer.texid2);
            rglAssert(glGetError() == GL_NO_ERROR);
            glBindTexture(GL_TEXTURE_2D, buffer.texid2);
            rglAssert(glGetError() == GL_NO_ERROR);
            glTexImage2D(GL_TEXTURE_2D, 0, glfmt, w, h, 0,
                glfmt, GL_UNSIGNED_BYTE, NULL);
            rglAssert(glGetError() == GL_NO_ERROR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);

            //       glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP);
            //       glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP);

            rglAssert(glGetError() == GL_NO_ERROR);
            glBindTexture(GL_TEXTURE_2D, 0);
            glFramebufferTexture2DEXT(GL_FRAMEBUFFER_EXT,
                GL_COLOR_ATTACHMENT0_EXT, GL_TEXTURE_2D,
                buffer.texid2, 0);
        }      
#endif

        if (restoreId) {
            buffer.fbid = restoreFbid;
        } else {
            glGenFramebuffersEXT(1, &buffer.fbid);
            rglAssert(glGetError() == GL_NO_ERROR);
        }
        glBindFramebufferEXT(GL_FRAMEBUFFER_EXT, buffer.fbid);

        // FIXME we should not need to allocate a color texture for depth only rendering
        if (1||!(buffer.flags & RGL_RB_DEPTH)) {
            glGenTextures(1, &buffer.texid);
            rglAssert(glGetError() == GL_NO_ERROR);
            glBindTexture(GL_TEXTURE_2D, buffer.texid);
            rglAssert(glGetError() == GL_NO_ERROR);
            glTexImage2D(GL_TEXTURE_2D, 0, glfmt, w, h, 0,
                glfmt, GL_UNSIGNED_BYTE, NULL);
            rglAssert(glGetError() == GL_NO_ERROR);
            glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
            glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);

            //       glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP);
            //       glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP);

            rglAssert(glGetError() == GL_NO_ERROR);
            glBindTexture(GL_TEXTURE_2D, 0);
            glFramebufferTexture2DEXT(GL_FRAMEBUFFER_EXT,
                GL_COLOR_ATTACHMENT0_EXT, GL_TEXTURE_2D,
                buffer.texid, 0);

            glFramebufferRenderbufferEXT(GL_FRAMEBUFFER_EXT, GL_DEPTH_ATTACHMENT_EXT, GL_RENDERBUFFER_EXT, 0 );

            if (!restoreId) {
                glClearColor(0, 0, 0, 1);
                glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);
                glClear(GL_COLOR_BUFFER_BIT);
            } else {
                glViewport(0, 0, buffer.realWidth, buffer.realHeight);
                glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);
                glDisable(GL_DEPTH_TEST);
                glBindTexture(GL_TEXTURE_2D, restoreId);
                rglUseShader(rglCopyShader);
                glBegin(GL_TRIANGLE_STRIP);
                glTexCoord2f((buffer.width+d2)/restoreW, 0); glVertex2f(1, 0);
                glTexCoord2f(0, 0); glVertex2f(0, 0);
                glTexCoord2f((buffer.width+d2)/restoreW, (buffer.height+d2)/restoreH); glVertex2f(1, 1);
                glTexCoord2f(0, (buffer.height+d2)/restoreH); glVertex2f(0, 1);
                glEnd();
                glDeleteTextures(1, &restoreId);
            }
        }      
    } else
        glBindFramebufferEXT(GL_FRAMEBUFFER_EXT, buffer.fbid);
#endif

    rglAssert(glGetError() == GL_NO_ERROR);

    // hack for LEGO racer, real fix coming soon
    //   if (olderased)
    //   {
    //     glDepthMask(GL_TRUE);
    //     glClearDepth(1);
    //     glClear(GL_DEPTH_BUFFER_BIT);
    //   }
}

void rglRenderChunks(rglRenderBuffer_t * upto)
{
    if (upto->area.xh != 8192 && renderedChunks < upto->chunkId)
        rglRenderChunks(upto->chunkId);
}

void rglRenderChunks(int upto)
{
    int i;
    //printf("vi_origin %x nbChunks %d\n", vi_origin, nbChunks);
    rglRenderBuffer_t * lastBuffer = 0;
    uint32_t lastDepthAddress = ~0;
    float zb = 0.0f;

    DUMP("rendering chunks upto %d / %d\n", upto, nbChunks);

    glEnable(GL_SCISSOR_TEST);
    for (i=renderedChunks; i<upto; i++) {
        int j;
        rglRenderChunk_t & chunk = chunks[i];

        if (chunk.renderBuffer->nbDepthSections) {
            // reselect the renderbuffer with correct width (needed by LEGO racer,
            // because they clear a 320x240 depth buffer to render in small 64x64 framebuffer)
            // and adjust the area to the associated color buffer
            // no need to optimize this search because it's rare (i.e. mainly depth clear,
            // so once per frame)
            for (j=chunk.renderBuffer->nbDepthSections-1; j>=0; j--) {
                //         LOG("j %d %d %d %d\n", j, i, chunk.renderBuffer->depthSections[j].chunkId,
                //             chunk.renderBuffer->depthSections[j].buffer - rBuffers);
                if (i >= chunk.renderBuffer->depthSections[j].chunkId)
                    break;
            }
            //rglAssert(j < chunk.renderBuffer->nbDepthSections-1);
            if (j < chunk.renderBuffer->nbDepthSections-1) {
                rglRenderBuffer_t * cbuffer = chunk.renderBuffer->depthSections[j+1].buffer;
                chunk.renderBuffer = rglSelectRenderBuffer(chunk.renderBuffer->addressStart, cbuffer->fbWidth, chunk.renderBuffer->size, chunk.renderBuffer->format);
                chunk.renderBuffer->area = cbuffer->area;
                chunk.renderBuffer->flags |= RGL_RB_DEPTH;
            }
        }

        rglRenderBuffer_t & buffer = *chunk.renderBuffer;
        int oldFlags = ~0;
        int oldTilenum = ~0;
        int combFormat = 0;

        rglAssert(buffer.area.xh != 8192);

        if (lastBuffer != &buffer)
            rglPrepareFramebuffer(buffer);

        DUMP("Buffer %p at %x area %d -> %d x %d -> %d\n",
            &buffer, buffer.addressStart,
            buffer.area.xh>>2, buffer.area.xl>>2,
            buffer.area.yh>>2, buffer.area.yl>>2);
        //     if (buffer.addressStart != vi_origin)
        //       continue;

        if (buffer.flags & RGL_RB_DEPTH)
            chunk.depthAddress = buffer.addressStart;

        if (lastBuffer != &buffer ||
            lastDepthAddress != chunk.depthAddress) {
                lastBuffer = &buffer;
                lastDepthAddress = chunk.depthAddress;
                int j;
                for (j=0; j<nbRBuffers; j++) {
                    int overlap = int(rBuffers[j].addressStop) - int(buffer.addressStart);
                    if (int(buffer.addressStop) - int(rBuffers[j].addressStart) < overlap)
                        overlap = int(buffer.addressStop) - int(rBuffers[j].addressStart);
                    //         LOG("checking #%d %x --> %x with %x --> %x overlap %x\n",
                    //             j,
                    //             rBuffers[j].addressStart, rBuffers[j].addressStop,
                    //             buffer.addressStart, buffer.addressStop,
                    //             overlap);
                    // check if more than 10% of the buffer was erased
                    if (rBuffers+j != &buffer &&
                        overlap > int(rBuffers[j].addressStop - rBuffers[j].addressStart)/10
                        //             rBuffers[j].addressStop > buffer.addressStart &&
                        //             rBuffers[j].addressStart < buffer.addressStop
                        ) {
                            rBuffers[j].flags |= RGL_RB_ERASED;
                            DUMP("erasing fb #%d\n", j);
                    }
                }

#ifndef NOFBO
                glBindFramebufferEXT(GL_FRAMEBUFFER_EXT, buffer.fbid);

                rglDepthBuffer_t * zbuf = rglFindDepthBuffer(chunk.depthAddress,
                    buffer.fboWidth, buffer.fboHeight);
                if (zbuf != buffer.depthBuffer) {
                    buffer.depthBuffer = zbuf;
#ifdef ZTEX
                    glFramebufferTexture2DEXT(GL_FRAMEBUFFER_EXT,
                        GL_DEPTH_ATTACHMENT_EXT, GL_TEXTURE_2D,
                        buffer.depthBuffer->zbid, 0);
#else
                    glFramebufferRenderbufferEXT(GL_FRAMEBUFFER_EXT, GL_DEPTH_ATTACHMENT_EXT, GL_RENDERBUFFER_EXT, buffer.depthBuffer->zbid );
#endif
                    //     glFramebufferRenderbufferEXT( GL_FRAMEBUFFER_EXT, GL_DEPTH_ATTACHMENT_EXT,
                    //                                   GL_RENDERBUFFER_EXT, depthBuffer->zbid );

                    CHECK_FRAMEBUFFER_STATUS();
                }
#endif

                glViewport(0, 0, buffer.realWidth, buffer.realHeight);
        }

        if (chunk.rdpState.clip.yl < chunk.rdpState.clip.yh ||
            chunk.rdpState.clip.xl < chunk.rdpState.clip.xh)
            continue;

        glScissor((chunk.rdpState.clip.xh >>2)*buffer.realWidth/buffer.width,
            (chunk.rdpState.clip.yh >>2)*buffer.realHeight/buffer.height,
            ((chunk.rdpState.clip.xl-chunk.rdpState.clip.xh) >>2)*buffer.realWidth/buffer.width,
            ((chunk.rdpState.clip.yl-chunk.rdpState.clip.yh) >>2)*buffer.realHeight/buffer.height);
        rglAssert(glGetError() == GL_NO_ERROR);

#ifndef NOFBO
#ifdef RGL_EXACT_BLEND
        glPushAttrib(GL_ALL_ATTRIB_BITS);
        glBindFramebufferEXT(GL_FRAMEBUFFER_EXT, buffer.fbid2);
        glBindTexture(GL_TEXTURE_2D, buffer.texid);
        glEnable(GL_TEXTURE_2D);
        rglUseShader(rglCopyShader);
        glColor4ub(255,255,255,255);
        glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);
        glDisable(GL_DEPTH_TEST);

        for (j=0; j<chunk.nbStrips; j++) {
            rglStrip_t & strip = chunk.strips[j];
            int k;

            glBegin(GL_TRIANGLE_STRIP);
            for (k=0; k<strip.nbVtxs; k++) {
                glTexCoord2f((strip.vtxs[k].x/(buffer.width)),
                    (strip.vtxs[k].y/(buffer.height)));
                glVertex2f((strip.vtxs[k].x/(buffer.width)),
                    (strip.vtxs[k].y/(buffer.height)));
            }
            glEnd();
        }
        glBindTexture(GL_TEXTURE_2D, 0);
        glBindFramebufferEXT(GL_FRAMEBUFFER_EXT, buffer.fbid);
        glPopAttrib();
#endif
#endif

        if (buffer.flags & RGL_RB_DEPTH) {
            DUMP("depth write\n");
            //rglRenderMode(chunk);
            glDepthMask(GL_TRUE);
            glDepthFunc(GL_ALWAYS);
            glDisable( GL_ALPHA_TEST );
            glDisable( GL_POLYGON_OFFSET_FILL ); // ?
            //glColorMask(GL_FALSE, GL_FALSE, GL_FALSE, GL_FALSE);
        } else {
            rglRenderMode(chunk);
        }
        rglAssert(glGetError() == GL_NO_ERROR);

        if (RDP_GETOM_Z_MODE(chunk.rdpState.otherModes) & 1) {
            switch(RDP_GETOM_Z_MODE(chunk.rdpState.otherModes)) {
      case 3:
          zb = 20;
          break;
      default:
          zb = 4;
          break;
            }
        } else {
            zb = 0;
        }
        zb *= 16e-6f;

#ifdef RGL_EXACT_BLEND
        glDisable(GL_BLEND);
        glActiveTextureARB(GL_TEXTURE1_ARB);
        glBindTexture(GL_TEXTURE_2D, buffer.texid2);
        glEnable(GL_TEXTURE_2D);
        rglAssert(glGetError() == GL_NO_ERROR);
        glActiveTextureARB(GL_TEXTURE0_ARB);
#endif

        combFormat = (buffer.size == 1? RGL_COMB_FMT_I : RGL_COMB_FMT_RGBA);
        // not yet
        //     if (buffer.flags & RGL_RB_DEPTH)
        //       combFormat = RGL_COMB_FMT_DEPTH;

        float ds[2], dt[2], ss[2], st[2];
        float dsm[2], dtm[2];
        for (j=0; j<chunk.nbStrips; j++) {
            rglStrip_t & strip = chunk.strips[j];
            int k;
            int tilenum = strip.tilenum;
            if (tilenum == 7 && RDP_GETOM_CYCLE_TYPE(chunk.rdpState.otherModes)==1) {
                tilenum = 0;
                combFormat |= RGL_COMB_TILE7;        
            }

            rglTile_t & tile = chunk.tiles[tilenum];
            rglTile_t & tile2 = chunk.tiles[tilenum+1];

            if (strip.flags != oldFlags || tilenum != oldTilenum) {
                oldTilenum = tilenum;
                if (strip.flags & RGL_STRIP_TEX1) {
                    //if (tile.hiresBuffer) continue;
                    combFormat |= rglUseTile(tile, ds[0], dt[0], ss[0], st[0]);
                    glEnable(GL_TEXTURE_2D);
                } else
                    glDisable(GL_TEXTURE_2D);

                glActiveTextureARB(GL_TEXTURE2_ARB);
                if (strip.flags & RGL_STRIP_TEX2) {
                    //if (tile2.hiresBuffer) continue;
                    combFormat |= rglUseTile(tile2, ds[1], dt[1], ss[1], st[1]) << 1;
                    glEnable(GL_TEXTURE_2D);
                } else
                    glDisable(GL_TEXTURE_2D);
                glActiveTextureARB(GL_TEXTURE0_ARB);
            }

            if (j == 0)
                rglSetCombiner(chunk, combFormat);

            if (strip.flags != oldFlags) {
                oldFlags = strip.flags;
                if (strip.flags & RGL_STRIP_ZBUFFER)
                    glEnable(GL_DEPTH_TEST);
                else
                    glDisable(GL_DEPTH_TEST);

                if (!(strip.flags & RGL_STRIP_SHADE))
                    // TODO take in account the framebuffer size (16b or 32b)
                    glColor4f(RDP_GETC16_R(chunk.rdpState.fillColor)/31.0f,
                    RDP_GETC16_G(chunk.rdpState.fillColor)/31.0f,
                    RDP_GETC16_B(chunk.rdpState.fillColor)/31.0f,
                    RDP_GETC16_A(chunk.rdpState.fillColor));

                if (wireframe) {
                    if (!(strip.flags & (RGL_STRIP_SHADE | RGL_STRIP_TEX1 | RGL_STRIP_TEX2)))
                        glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
                    else
                        glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
                }
            }

            // FIXME
            //       if (RDP_GETOM_CYCLE_TYPE(curChunk->rdpState.otherModes) < 2)
            //         glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_FALSE);
            //       else
            //         glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);


            if (strip.flags & RGL_STRIP_TEX1)
                rglFixupMapping(strip, tile,
                ds[0], dt[0], ss[0], st[0], dsm[0], dtm[0],
                (strip.flags & RGL_STRIP_TEX2) && tile.tex == tile2.tex);
            if (strip.flags & RGL_STRIP_TEX2) {
                glActiveTextureARB(GL_TEXTURE2_ARB);
                rglFixupMapping(strip, tile2,
                    ds[1], dt[1], ss[1], st[1], dsm[1], dtm[1],
                    (strip.flags & RGL_STRIP_TEX1) && tile.tex == tile2.tex);
                glActiveTextureARB(GL_TEXTURE0_ARB);
            }

            glBegin(GL_TRIANGLE_STRIP);
            for (k=0; k<strip.nbVtxs; k++) {
                if (strip.flags & RGL_STRIP_SHADE)
                    glColor4ub(strip.vtxs[k].r, strip.vtxs[k].g, strip.vtxs[k].b,
                    strip.vtxs[k].a);
                if (strip.flags & RGL_STRIP_TEX1)
                    glMultiTexCoord2fARB(GL_TEXTURE0_ARB,
                    1-(strip.vtxs[k].s + ds[0] + dsm[0]) / ss[0],
                    1-(strip.vtxs[k].t + dt[0] + dtm[0]) / st[0]);
                if (strip.flags & RGL_STRIP_TEX2)
                    glMultiTexCoord2fARB(GL_TEXTURE2_ARB,
                    1-(strip.vtxs[k].s + ds[1] + dsm[1]) / ss[1],
                    1-(strip.vtxs[k].t + dt[1] + dtm[1]) / st[1]);
#ifdef RGL_EXACT_BLEND
                //         glMultiTexCoord2fARB(GL_TEXTURE1_ARB,
                //                              (strip.vtxs[k].x/(buffer.width))/**strip.vtxs[k].w*/,
                //                              (strip.vtxs[k].y/(buffer.height))/**strip.vtxs[k].w*/);
                // used only to pass the viewport information :/
                // tried with light position --> but it seems less precise !
                glMultiTexCoord2fARB(GL_TEXTURE1_ARB,
                    buffer.realWidth/2048.f,
                    buffer.realHeight/2048.f);
#endif
                //         if (ds || dt || ss!=1 || st!=1) {
                //           printf("%g x %g --> %g x %g\n",
                //                  strip.vtxs[k].s*tile.w,
                //                  strip.vtxs[k].t*tile.h,
                //                  (strip.vtxs[k].s + ds) * ss,
                //                  (strip.vtxs[k].t + dt) * st);
                //         }

                float
                    x = strip.vtxs[k].x*strip.vtxs[k].w,
                    y = strip.vtxs[k].y*strip.vtxs[k].w;

                if (buffer.flags & RGL_RB_DEPTH)
                    glVertex3f((strip.vtxs[k].x/(buffer.width)),
                    (strip.vtxs[k].y/(buffer.height)),
                    //rglZscale(chunk.rdpState.fillColor&0xffff));
                    float(chunk.rdpState.fillColor&0xffff)/0xffff);
                //           glVertex4f((strip.vtxs[k].x/(buffer.width))*strip.vtxs[k].w,
                //                      (strip.vtxs[k].y/(buffer.height))*strip.vtxs[k].w,
                //                      float(chunk.rdpState.fillColor&0xffff)/0xffff*strip.vtxs[k].w,
                //                      strip.vtxs[k].w);
                else {
                    //           glVertex4f(x/buffer.width, y/buffer.height,
                    //                      (strip.vtxs[k].z - 1.5f*zb)*(strip.vtxs[k].w),
                    //                      strip.vtxs[k].w);

                    float iw = strip.vtxs[k].w;
                    if (iw > 1000) {
                        glVertex4f(x/buffer.width, y/buffer.height,
                            (strip.vtxs[k].z - 1.5f*zb)*strip.vtxs[k].w,
                            strip.vtxs[k].w);
                    } else {
                        iw = 1.0f/iw;
                        glVertex4f(x/buffer.width, y/buffer.height,
                            (strip.vtxs[k].z) / (iw + zb*0.35f),
                            strip.vtxs[k].w);
                    }
                    //           glVertex4f(x/buffer.width, y/buffer.height,
                    //                      (strip.vtxs[k].z)*strip.vtxs[k].w,
                    //                      strip.vtxs[k].w);
                }

                if (x < chunk.rdpState.clip.xh/4)
                    x = chunk.rdpState.clip.xh/4;
                if (x > chunk.rdpState.clip.xl/4)
                    x = chunk.rdpState.clip.xl/4;
                if (y < chunk.rdpState.clip.yh/4)
                    y = chunk.rdpState.clip.yh/4;
                if (y > chunk.rdpState.clip.yl/4)
                    y = chunk.rdpState.clip.yl/4;
                if (buffer.mod.xh > x)
                    buffer.mod.xh = x;
                if (buffer.mod.xl < x)
                    buffer.mod.xl = x;
                if (buffer.mod.yh > y)
                    buffer.mod.yh = y;
                if (buffer.mod.yl < y)
                    buffer.mod.yl = y;

            }
            glEnd();

            // FIXME
            //       glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);

        }

        buffer.flags |= RGL_RB_FBMOD;

#ifdef RGL_EXACT_BLEND
        glActiveTextureARB(GL_TEXTURE1_ARB);
        glDisable(GL_TEXTURE_2D);
        glBindTexture(GL_TEXTURE_2D, 0);
        glActiveTextureARB(GL_TEXTURE0_ARB);
#endif
    }

    glActiveTextureARB(GL_TEXTURE2_ARB);
    glDisable(GL_TEXTURE_2D);
    glBindTexture(GL_TEXTURE_2D, 0);
    glActiveTextureARB(GL_TEXTURE0_ARB);

    renderedChunks = i;
}

void rglDisplayFramebuffers()
{
    if (!(vi_control & 3))
        return;

#ifdef RDP_DEBUG
    extern int nbFullSync;
    LOG("nbFyllSync %d\n", nbFullSync);
    nbFullSync = 0;
#endif

    int height = (vi_control & 0x40) ? 480 : 240;
    int width = vi_width;

    // from glide64
    DWORD scale_x = *gfx.VI_X_SCALE_REG & 0xFFF;
    if (!scale_x) return;
    DWORD scale_y = *gfx.VI_Y_SCALE_REG & 0xFFF;
    if (!scale_y) return;

    float fscale_x = (float)scale_x / 1024.0f;
    float fscale_y = (float)scale_y / 1024.0f;

    DWORD dwHStartReg = *gfx.VI_H_START_REG;
    DWORD dwVStartReg = *gfx.VI_V_START_REG;

    DWORD hstart = dwHStartReg >> 16;
    DWORD hend = dwHStartReg & 0xFFFF;

    // dunno... but sometimes this happens
    if (hend == hstart) {
        LOG("fix hend\n");
        hend = (int)(*gfx.VI_WIDTH_REG / fscale_x);
    }

    if (hstart > hend) {
        DWORD tmp=hstart; hstart=hend; hend=tmp;
        LOG("swap hstart hend\n");
    }

    DWORD vstart = dwVStartReg >> 16;
    DWORD vend = dwVStartReg & 0xFFFF;

    if (vstart > vend) {
        DWORD tmp=vstart; vstart=vend; vend=tmp;
        LOG("swap vstart vend\n");
    }

    //if (*gfx.VI_WIDTH_REG != 0x500)
    if (*gfx.VI_WIDTH_REG < 0x400)
        fscale_y /= 2.0f;

    //   fscale_x *= screen_width / float(vi_width);
    //   fscale_y *= screen_height / height;
    //glViewport(0*hstart*fscale_x, 0*vstart*fscale_y, (hend-hstart)*fscale_x, (vend-vstart)*fscale_y);
    width = (hend-hstart)*fscale_x;
    height = (vend-vstart)*fscale_y;
    if (!width || !height) return;
    static int oldw, oldh;
    if (width == oldw && width > 200)
        rglScreenWidth = width;
    if (height == oldh && height > 200)
        rglScreenHeight = height;
    oldw = width;
    oldh = height;
    int vi_line = vi_width * 2;  // TODO take in account the format
    int vi_start = *gfx.VI_ORIGIN_REG;// - vi_line;
    int vi_stop = vi_start + height * vi_line;


    if (*gfx.VI_WIDTH_REG >= 0x400)
        vi_line /= 2;

    DUMP("%x screen %x --> %x %d --> %d x %d --> %d scale %g x %g clip %g --> %g x %g --> %g %dx%d\n",
        vi_line,
        vi_start, vi_stop,
        hstart, hend, vstart, vend,
        fscale_x, fscale_y,
        hstart*fscale_x, hend*fscale_x, vstart*fscale_y, vend*fscale_y,
        width, height
        );


#ifdef NOFBO
    return;
#endif

    glBindFramebufferEXT(GL_FRAMEBUFFER_EXT, 0);
    glDrawBuffer(GL_BACK);
    glViewport(0, viewportOffset, screen_width, screen_height);
    glDisable(GL_SCISSOR_TEST);
    // wine seems to catch scissor test disabling so need to define an area nevertheless
    glScissor(0, viewportOffset, screen_width, screen_height);
    glClearColor(0, 0, 0, 0);
    glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);
    glClear(GL_COLOR_BUFFER_BIT); // TODO clear a minimal area

    rglRenderBuffer_t * buffer;
    CIRCLEQ_FOREACH(rglRenderBuffer_t, buffer, &rBufferHead, link)
        if (!(buffer->flags & RGL_RB_ERASED) &&
            (uint32_t)vi_stop > buffer->addressStart &&
            (uint32_t)vi_start < buffer->addressStop) {

                if (buffer->size != 2 || buffer->format != RDP_FORMAT_RGBA)
                    continue; // FIXME

                glBindFramebufferEXT(GL_FRAMEBUFFER_EXT, 0);
                glDrawBuffer(GL_BACK);
                glViewport(0, viewportOffset, screen_width, screen_height);

                glDisable(GL_SCISSOR_TEST);
                // wine seems to catch scissor test disabling so need to define an area nevertheless
                glScissor(0, viewportOffset, screen_width, screen_height);

                glDisable(GL_ALPHA_TEST);
                glDisable(GL_BLEND);
                glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
                glActiveTextureARB(GL_TEXTURE1_ARB);
                glDisable(GL_TEXTURE_2D);
                glActiveTextureARB(GL_TEXTURE0_ARB);



                float x = (int32_t(buffer->addressStart - vi_start) % int(vi_line)) / 2;
                float y = height - buffer->height - (int32_t(buffer->addressStart - vi_start) / int(vi_line));
                //x=y=0;
                DUMP("displaying fb %x %d x %d (%d x %d) at %g x %g\n", buffer->addressStart,
                    buffer->width, buffer->height,
                    buffer->realWidth, buffer->realHeight,
                    x, y);
                y -= *gfx.VI_V_CURRENT_LINE_REG & 1; // prevent interlaced modes flickering
                x = x / width;
                y = y / height;
                rglUseShader(rglCopyShader);
                glBindTexture(GL_TEXTURE_2D, buffer->texid);
                glEnable(GL_TEXTURE_2D);
                glDisable(GL_DEPTH_TEST);
                glDisable(GL_BLEND);
                glColor4ub(255, 255, 255, 255);
                glBegin(GL_TRIANGLE_STRIP);
                glTexCoord2f(float(buffer->realWidth)/buffer->fboWidth, float(buffer->realHeight)/buffer->fboHeight);    glVertex2f(x+float(buffer->width-1)/(width-1), y+0);
                glTexCoord2f(0, float(buffer->realHeight)/buffer->fboHeight);    glVertex2f(x+0, y+0);
                glTexCoord2f(float(buffer->realWidth)/buffer->fboWidth, 0);    glVertex2f(x+float(buffer->width-1)/(width-1), y+float(buffer->height-1)/(height-1));
                glTexCoord2f(0, 0);    glVertex2f(x+0, y+float(buffer->height-1)/(height-1));
                glEnd();
        }
}

void rglUpdate()
{
    int i;

    if (old_vi_origin == vi_origin) {
        //printf("same\n");
        return;
    }
    old_vi_origin = vi_origin;

    DUMP("updating vi_origin %x vi_hstart %d vi_vstart %d\n",
        vi_origin, *gfx.VI_H_START_REG, *gfx.VI_V_START_REG);

    glPolygonMode(GL_FRONT_AND_BACK, wireframe? GL_LINE : GL_FILL);

    rglRenderChunks(nbChunks);

    rglDisplayFramebuffers();

#ifndef NOFBO
    glBindFramebufferEXT(GL_FRAMEBUFFER_EXT, 0);
#endif
    rglUseShader(0);
    glDrawBuffer(GL_BACK);
    rglSwapBuffers();

    rglFrameCounter++;

    //   for (i=0; i<nbRBuffers; i++)
    //     rglFramebuffer2Rdram(rBuffers[i],
    //                          rBuffers[i].addressStart, rBuffers[i].addressStop);


#ifdef RDP_DEBUG
    if (rdp_dump) {
        LOG("DUMP %d\n", rdp_dump);
        rdp_dump--;
    }
#ifdef WIN32
    if (GetAsyncKeyState ('P') & 0x0001) {
        rglDebugger();
    }
    if (GetAsyncKeyState ('D') & 0x0001) {
        rdp_dump = 2;
    }
    if (GetAsyncKeyState ('W') & 0x0001) {
        wireframe = !wireframe;
    }
#else
    SDL_Event event;
    while (nbChunks && SDL_PollEvent(&event)) {
        switch (event.type) {
      case SDL_KEYDOWN:
          switch (event.key.keysym.sym) {
      case 'd':
          rdp_dump = 2;
          break;
      case 'w': {
          wireframe = !wireframe;
          break;
                }
      case 'p': {
          rglDebugger();
          break;
                }
          }
          break;
        }
    }
#endif
#endif

#ifdef RDP_DEBUG
    rdpTracePos = 0;
#endif

    renderedChunks = 0;
    nbChunks = 0;
    nbStrips = 0;
    nbVtxs =   0;

    for (i=0; i<nbRBuffers; i++) {
        rglRenderBuffer_t & buffer = rBuffers[i];
        buffer.area.xl = buffer.area.yl = 0;
        buffer.area.xh = buffer.area.yh = 8192;
        buffer.chunkId = 0;
        buffer.nbDepthSections = 0;
    }  

    // force a render buffer update
    rdpChanged |= (RDP_BITS_ZB_SETTINGS | RDP_BITS_FB_SETTINGS);
}

void rglClearRenderBuffers()
{
    int i;
    for (i=0; i<nbRBuffers; i++)
        rglDeleteRenderBuffer(rBuffers[i]);
    for (i=0; i<nbZBuffers; i++) {
        glDeleteRenderbuffersEXT(1, &zBuffers[i].zbid);
        zBuffers[i].zbid = 0;
    }

    for (i=0; i<MAX_RENDER_BUFFERS; i++) {
        rglRenderBuffer_t & buffer = rBuffers[i];
        buffer.mod.xl = buffer.mod.yl = 0;
        buffer.mod.xh = buffer.mod.yh = 8192;
        buffer.area.xl = buffer.area.yl = 0;
        buffer.area.xh = buffer.area.yh = 8192;
    }

    CIRCLEQ_INIT(rglRenderBuffer_t, &rBufferHead);

    nbRBuffers = 0;
    nbZBuffers = 0;
    curRBuffer = 0;
    curZBuffer = 0;
}

rglRenderBuffer_t * rglSelectRenderBuffer(uint32_t addr, int width, int size, int format)
{
    int i;
    for (i=nbRBuffers-1; i>=0; i--)
        if (rBuffers[i].addressStart == addr &&
            rBuffers[i].fbWidth == width &&
            rBuffers[i].size == size)
            break;

    if (i >= 0) {
        return rBuffers + i;
        // TODO need to take care of framebuffer format possible change (?)
    }

    rglAssert(nbRBuffers < MAX_RENDER_BUFFERS);
    //   if (nbRBuffers == MAX_RENDER_BUFFERS)
    //     rglClearRenderBuffers();

    i = nbRBuffers++;
    rglRenderBuffer_t * cur = rBuffers + i;

    cur->addressStart = addr;
    cur->format = format;
    cur->size = size;
    cur->fbWidth = width;
    cur->area = rdpState.clip;
    cur->line = (width << size >> 1);
    cur->flags = 0;
    CIRCLEQ_INSERT_HEAD(rglRenderBuffer_t, &rBufferHead, cur, link);
    return cur;
}

void rglPrepareRendering(int texturing, int tilenum, int recth, int depth)
{
    if (!rdpChanged)
        goto ok;

    //rglUpdate();

    depth = /*depth && */(RDP_GETOM_CYCLE_TYPE(rdpState.otherModes) < 2) &&
        (RDP_GETOM_Z_UPDATE_EN(rdpState.otherModes) ||
        RDP_GETOM_Z_COMPARE_EN(rdpState.otherModes));

    if (curRBuffer)
        curRBuffer->chunkId = nbChunks;

    if (!curZBuffer ||
        (rdpChanged & (RDP_BITS_ZB_SETTINGS | RDP_BITS_FB_SETTINGS)) ||
        curZBuffer->addressStart != rdpZbAddress) {
            // first search the most recent without considering the width of the buffer
            rglRenderBuffer_t * buf;
            curZBuffer = 0;
            CIRCLEQ_FOREACH(rglRenderBuffer_t, buf, &rBufferHead, link)
                if (buf->addressStart == rdpZbAddress) {
                    curZBuffer = buf;
                    break;
                }
                if (!curZBuffer) {
                    curZBuffer = rglSelectRenderBuffer(rdpZbAddress, rdpFbWidth, 2, RDP_FORMAT_RGBA);
                    CIRCLEQ_REMOVE(&rBufferHead, curZBuffer, link);
                    CIRCLEQ_INSERT_HEAD(rglRenderBuffer_t, &rBufferHead, curZBuffer, link);
                }
    }

    if (rdpChanged & (RDP_BITS_ZB_SETTINGS | RDP_BITS_FB_SETTINGS)) {
        curRBuffer = rglSelectRenderBuffer(rdpFbAddress, rdpFbWidth, rdpFbSize, rdpFbFormat);
        CIRCLEQ_REMOVE(&rBufferHead, curRBuffer, link);
        CIRCLEQ_INSERT_HEAD(rglRenderBuffer_t, &rBufferHead, curRBuffer, link);
    }

    if (rdpChanged & (RDP_BITS_TMEM | RDP_BITS_TLUT | RDP_BITS_TILE_SETTINGS))
        rglTouchTMEM();

    if (rdpChanged & (RDP_BITS_CLIP | RDP_BITS_ZB_SETTINGS | RDP_BITS_FB_SETTINGS) &&
        rdpState.clip.xh <= rdpState.clip.xl && rdpState.clip.yh <= rdpState.clip.yl)
    {
        if (curRBuffer->area.xh == 8192)
            curRBuffer->flags &= ~RGL_RB_HASTRIANGLES;

        if (curRBuffer->area.xh > rdpState.clip.xh)
            curRBuffer->area.xh = rdpState.clip.xh;
        if (curRBuffer->area.xl < rdpState.clip.xl)
            curRBuffer->area.xl = rdpState.clip.xl;
        if (curRBuffer->area.yh > rdpState.clip.yh)
            curRBuffer->area.yh = rdpState.clip.yh;
        if (curRBuffer->area.yl < rdpState.clip.yl)
            curRBuffer->area.yl = rdpState.clip.yl;
    }

    curRBuffer->chunkId = nbChunks; // don't include THIS chunk yet in case of feedback rendering (cf CBFD)
    //   if (curZBuffer)
    //     curZBuffer->chunkId = nbChunks;

    curChunk = chunks + nbChunks++;
    rglAssert(nbChunks < MAX_RENDER_CHUNKS);

    curChunk->strips = strips + nbStrips;
    curChunk->nbStrips = 0;
    curChunk->renderBuffer = curRBuffer;
    curChunk->flags = 0;
    curChunk->rdpState = rdpState;
    curChunk->depthAddress = rdpZbAddress;

#ifdef RDP_DEBUG
    curChunk->tracePos = rdpTracePos;
#endif

    if (depth) {
        curZBuffer->flags |= RGL_RB_DEPTH;
        //rglRenderChunks(curZBuffer);

        if (rdpFbAddress != rdpZbAddress) {
            if (!curZBuffer->nbDepthSections ||
                curZBuffer->depthSections[curZBuffer->nbDepthSections-1].buffer != curRBuffer) {
                    rglAssert(curZBuffer->nbDepthSections < RGL_MAX_DEPTH_SECTIONS);
                    curZBuffer->depthSections[curZBuffer->nbDepthSections].buffer = curRBuffer;
                    curZBuffer->nbDepthSections++;
            }
            curZBuffer->depthSections[curZBuffer->nbDepthSections-1].chunkId = nbChunks;
        }
    }  

    {
        // eliminate useless bits
        int cycle = RDP_GETOM_CYCLE_TYPE(curChunk->rdpState.otherModes);
        curChunk->rdpState.otherModes.w2 &= rdpBlendMasks[cycle].w2;
        curChunk->rdpState.combineModes.w1 &= rdpCombineMasks[cycle].w1;
        curChunk->rdpState.combineModes.w2 &= rdpCombineMasks[cycle].w2;
    }

    rdpChanged = 0;

ok:
    if (texturing && !(curChunk->flags & (1<<tilenum))) {
        curChunk->flags |= (1<<tilenum);
        rglTile(rdpTiles[tilenum], curChunk->tiles[tilenum], recth);
    }
}


void rglClose()
{

#ifdef RDP_DEBUG
    rglCloseDebugger();
#endif

    rglClearRenderBuffers();

    rglResetTextureCache();

    nbChunks = 0;
    nbStrips = 0;
    nbVtxs =   0;

    if (rglCopyShader) rglDeleteShader(rglCopyShader);
    rglCopyShader = 0;
    if (rglCopyDepthShader) rglDeleteShader(rglCopyDepthShader);
    rglCopyDepthShader = 0;
    rglClearCombiners();
}


int rglInit()
{
    static int init;
    if (!init) {
        init = 1;
        glewInit();
    }

    glViewport(0, 0, screen_width, screen_height);

    glLoadIdentity();
#ifdef NOFBO
    glScalef(2, -2, 1);
#else
    glScalef(2, 2, 1);
#endif
    glTranslatef(-0.5, -0.5, 0);

    glEnable(GL_DEPTH_TEST);

    rglClose();

    rglCopyShader = rglCreateShader(
        "void main()                                                    \n"
        "{                                                              \n"
        "  gl_Position = ftransform();                                  \n"
        "  gl_FrontColor = gl_Color;                                    \n"
        "  gl_TexCoord[0] = gl_MultiTexCoord0;                          \n"
        "}                                                              \n"
        ,
        "uniform sampler2D texture0;       \n"
        "                                  \n"
        "void main()                       \n"
        "{                                 \n"
        "  gl_FragColor = gl_Color * texture2D(texture0, vec2(gl_TexCoord[0])); \n"
        "}                                 \n"
        );

    rglCopyDepthShader = rglCreateShader(
        "void main()                                                    \n"
        "{                                                              \n"
        "  gl_Position = ftransform();                                  \n"
        "  gl_FrontColor = gl_Color;                                    \n"
        "  gl_TexCoord[0] = gl_MultiTexCoord0;                          \n"
        "}                                                              \n"
        ,
        "uniform sampler2D texture0;       \n"
        "                                  \n"
        "void main()                       \n"
        "{                                 \n"
        "  gl_FragDepth = texture2D(texture0, vec2(gl_TexCoord[0]))[0]; \n"
        "}                                 \n"
        );

    rdpChanged = ~0;
    return 1;
}


#ifdef __cplusplus
extern "C" {
#endif

    EXPORT void CALL FBWrite(DWORD addr, DWORD size)
    {
        if (!rglSettings.fbInfo || rglSettings.async)
            return;
        //LOG("FBWrite %x\n", addr);
        rglRenderBuffer_t * buffer;
        addr &= 0x7fffff;
        CIRCLEQ_FOREACH(rglRenderBuffer_t, buffer, &rBufferHead, link) {
            if (addr >= buffer->addressStart && addr+size <= buffer->addressStop) {
                //LOG("FBWrite in fb #%d\n", buffer - rBuffers);
                buffer->flags &= ~RGL_RB_FBMOD;
                buffer->mod.xl = buffer->mod.yl = 0;
                buffer->mod.xh = buffer->mod.yh = 8192;
                //break;
            }
        }
        //LOG("FBWrite %x %d\n", addr, size);
    }

    //EXPORT void CALL FBWList(FrameBufferModifyEntry *plist, DWORD size)
    //{
    //  LOG("FBWList size %d\n", size);
    //}

    EXPORT void CALL FBRead(DWORD addr)
    {
        if (!rglSettings.fbInfo || rglSettings.async)
            return;
        //LOG("FBRead %x\n", addr);
        rglRenderBuffer_t * buffer;
        addr &= 0x7fffff;
        CIRCLEQ_FOREACH(rglRenderBuffer_t, buffer, &rBufferHead, link) {
            if (addr >= buffer->addressStart && addr < buffer->addressStop) {
                //       LOG("writing to rdram buffer %x --> %x\n",
                //           buffer->addressStart, buffer->addressStop);
                rglFramebuffer2Rdram(*buffer, buffer->addressStart, buffer->addressStop);
                break;
            }
        }
    }

    EXPORT void CALL FBGetFrameBufferInfo(void *p)
    {
        typedef struct
        {
            DWORD addr;
            DWORD size;
            DWORD width;
            DWORD height;
        } FrameBufferInfo;

        FrameBufferInfo * pinfo = (FrameBufferInfo *)p;
        int i;

        if (!rglSettings.fbInfo)
            return;
        //LOG("GetFbInfo\n");

        rglRenderBuffer_t * buffer;
        i=0;
        CIRCLEQ_FOREACH(rglRenderBuffer_t, buffer, &rBufferHead, link) {
            //     printf("#%d (%dx%d) %x --> %x\n", i,
            //            buffer->width, buffer->height,
            //            buffer->addressStart,
            //            buffer->addressStart + buffer->width*buffer->height*2);
            pinfo[i].addr = buffer->addressStart;
            pinfo[i].size = 2; // FIXME
            pinfo[i].width = buffer->width;
            pinfo[i].height = buffer->height;
            i++; if (i>=6) break;
        }
        for ( ; i<6; i++) {
            pinfo[i].addr = 0;
            pinfo[i].size = 0;
            pinfo[i].width = 4;
            pinfo[i].height = 4;
        }
    }


#ifdef __cplusplus
}
#endif

static char exptable[256];

static void build_exptable()
{
    LOG("Building depth exp table\n");
    int i;
    for (i=0; i<256; i++) {
        int s;
        for (s=0; s<7; s++)
            if (!(i&(1<<(6-s))))
                break;
        exptable[i] = s;
    }
}

void rglFramebuffer2Rdram(rglRenderBuffer_t & buffer, uint32_t start, uint32_t stop)
{
    int depth;

    rglRenderChunks(&buffer);

    if (!(buffer.flags & RGL_RB_FBMOD))
        return;

    //   if (buffer.area.xh == 8192)
    //     return;
    //   rglAssert (buffer.area.xh != 8192);

    depth = buffer.flags & RGL_RB_DEPTH;
    //depth = 1;

    int glfmt, packed;
    int x, y;
    int rw, rh;
    int rx, ry;
    uint8_t * ram = gfx.RDRAM + buffer.addressStart;
    static uint8_t * fb = rglTmpTex;
    if (depth) {
        glfmt = GL_DEPTH_COMPONENT;
        //packed = GL_UNSIGNED_SHORT;
        packed = GL_FLOAT;
    } else {
        glfmt = GL_RGBA;
        packed = GL_UNSIGNED_BYTE;
    }

    rx = buffer.mod.xh;
    ry = buffer.mod.yh;
    rw = (int(buffer.mod.xl) - int(buffer.mod.xh));
    rh = (int(buffer.mod.yl) - int(buffer.mod.yh));

    if (rw > buffer.fbWidth)
        rw = buffer.fbWidth;

    LOG("writing to rdram %x %s-%d %d %dx%d %dx%d %dx%d\n",
        buffer.addressStart, depth? "depth":rdpImageFormats[buffer.format], buffer.size,
        buffer.fbWidth,
        buffer.width, buffer.height,
        rx, ry,
        rw, rh);
    fflush(stderr);

    if (rw <= 0  || rh <= 0)
        return;

    //   rx=ry=0;
    //   rw = buffer.width;
    //   rh = buffer.height;

    glPushAttrib(GL_ALL_ATTRIB_BITS);
#ifndef NOFBO
    glBindFramebufferEXT(GL_FRAMEBUFFER_EXT, 0);
#endif
    glDrawBuffer(GL_BACK);
    glReadBuffer(GL_BACK);
    glDisable(GL_SCISSOR_TEST);
    glViewport(0, 0, buffer.width, buffer.height); // FIXME why +1 ?
    // wine seems to catch scissor test disabling so need to define an area nevertheless
    glScissor(0, 0, buffer.width+1, buffer.height+1);
    glEnable(GL_TEXTURE_2D);
    glDisable( GL_ALPHA_TEST );
    if (depth) {
        glBindTexture(GL_TEXTURE_2D, buffer.depthBuffer->zbid);
        rglUseShader(rglCopyDepthShader);
        glEnable(GL_DEPTH_TEST);
        glDepthFunc(GL_ALWAYS);
        glDepthMask(GL_TRUE);
        glDisable( GL_POLYGON_OFFSET_FILL );
    } else {
        glBindTexture(GL_TEXTURE_2D, buffer.texid);
        rglUseShader(rglCopyShader);
        glDisable(GL_DEPTH_TEST);
        glDisable(GL_BLEND);
        glColor4ub(255, 255, 255, 255);
    }
    glBegin(GL_TRIANGLE_STRIP);
    glTexCoord2f(1, 1);    glVertex2f(1, 1);
    glTexCoord2f(0, 1);    glVertex2f(0, 1);
    glTexCoord2f(1, 0);    glVertex2f(1, 0);
    glTexCoord2f(0, 0);    glVertex2f(0, 0);
    glEnd();

    glReadPixels(rx, ry, rw, rh,
        glfmt, packed,
        fb);


    if (depth) {
        if (!exptable[255])
            build_exptable();
        for (x=rx; x<rx+rw; x++) 
            for (y=ry; y<ry+rh; y++) {
                uint32_t a = *(float *)&fb[(x-rx)*4 + (y-ry)*rw*4] * ((1<<18)-1);
                //uint32_t a = uint32_t(*(uint16_t *)&fb[(x-rx)*4 + (y-ry)*rw*4]) << 2;
                int e = exptable[a>>(18-8)];

                a = ( ( (e>=6? a : (a>>(6-e))) & ((1<<11)-1) ) << 2 ) | (e<<(16-3));

                *(uint16_t *)&ram[(x*2 + y*buffer.line) ^ 2] =
                    a;
                //int(*(uint16_t *)&fb[(x-rx)*2 + (y-ry)*rw*2])-2;
                //(*(uint16_t *)&fb[(x-rx)*2 + (y-ry)*rw*2] - int(0x8000))*2;
                //(*(float *)&fb[(x-rx)*4 + (y-ry)*rw*4]-0.5)*0x1ffff;
            }
    } else {
        switch (buffer.size) {
      case 1:
          for (x=rx; x<rx+rw; x++) 
              for (y=ry; y<ry+rh; y++) {
                  int r = fb[(x-rx + (y-ry)*rw)*4 + 0];
                  //             int g = fb[(x-rx + (y-ry)*rw)*4 + 1];
                  //             int b = fb[(x-rx + (y-ry)*rw)*4 + 2];
                  //             int a = fb[(x-rx + (y-ry)*rw)*4 + 3];
                  *(uint8_t *)&ram[(x + y*buffer.line) ^ 3] =
                      r;
                  //(r+g+b)/3; // FIXME just R ?
              }
              break;
      case 2:
          for (x=rx; x<rx+rw; x++) 
              for (y=ry; y<ry+rh; y++) {
                  int r = fb[(x-rx + (y-ry)*rw)*4 + 0];
                  int g = fb[(x-rx + (y-ry)*rw)*4 + 1];
                  int b = fb[(x-rx + (y-ry)*rw)*4 + 2];
                  int a = fb[(x-rx + (y-ry)*rw)*4 + 3];
                  *(uint16_t *)&ram[(x*2 + y*buffer.line) ^ 2] =
                      ((r&0xf8)<<8) | ((g&0xf8)<<3) | ((b&0xf8)>>2) |
                      ((a&0x80)>>7);
              }
              break;
        }
    }

    buffer.mod.xl = buffer.mod.yl = 0;
    buffer.mod.xh = buffer.mod.yh = 8192;

    //if (start <= buffer.addressStart && stop >= buffer.addressStop)
    buffer.flags &= ~RGL_RB_FBMOD;

    glPopAttrib();
}

void rglUpdateStatus()
{
    if (rglNextStatus != rglStatus) {
        const char * status[] = { "closed", "windowed", "fullscreen" };
        LOG("Status %s --> %s\n", status[rglStatus], status[rglNextStatus]);
        rglCloseScreen();
        rglStatus = rglNextStatus;
        if (rglNextStatus != RGL_STATUS_CLOSED)
            rglOpenScreen();
    }
}
