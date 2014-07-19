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

#ifndef _RGL_H_
#define _RGL_H_

#include "queue.h"
#include "rgl_assert.h"
#include "rdp.h"

#include <glew.h>
#if defined(__MACOSX__)
#include <OpenGL/gl.h>
#elif defined(__MACOS__)
#include <gl.h>
#else
#include <GL/gl.h>
#endif

#ifdef RDP_DEBUG
//#include <IL/il.h>
#endif

#include "glshader.h"

#include <limits.h> //for PATH_MAX

#define DWORD unsigned int
extern GFX_INFO gfx;
#define rdram ((uint32_t*)gfx.RDRAM)
#define rsp_imem ((uint32_t*)gfx.IMEM)
#define rsp_dmem ((uint32_t*)gfx.DMEM)
#define vi_origin (*(uint32_t*)gfx.VI_ORIGIN_REG)
#define vi_width (*(uint32_t*)gfx.VI_WIDTH_REG)
#define vi_control (*(uint32_t*)gfx.VI_STATUS_REG)

#define dp_start (*(uint32_t*)gfx.DPC_START_REG)
#define dp_end (*(uint32_t*)gfx.DPC_END_REG)
#define dp_current (*(uint32_t*)gfx.DPC_CURRENT_REG)
#define dp_status (*(uint32_t*)gfx.DPC_STATUS_REG)

// highly experimental AND slow
//#define RGL_EXACT_BLEND

struct rglSettings_t {
    int hiresFb;
    int resX, resY;
    int fsResX, fsResY;
    int fbInfo;
    int forceSwap;
    int threaded;
    int async;
    int noNpotFbos;
    int lowres;
    int fullscreen;
};

extern rglSettings_t rglSettings;
extern void (*render_callback)(int);

struct rglDepthBuffer_t {
    uint32_t address;
    int width, height;
    GLuint zbid;
};
#define MAX_DEPTH_BUFFERS 16
extern rglDepthBuffer_t zBuffers[MAX_DEPTH_BUFFERS];
extern int nbZBuffers;

struct rglRenderBuffer_t;
struct rglDepthSection_t {
    rglRenderBuffer_t * buffer;
    int chunkId;
};
#define RGL_MAX_DEPTH_SECTIONS 16

struct rglRenderBuffer_t {
    CIRCLEQ_ENTRY(rglRenderBuffer_t) link;
    uint32_t addressStart, addressStop;
    int format, size, fbWidth, line;
    int width, height;
    int flags;
    GLuint texid, fbid;
#ifdef RGL_EXACT_BLEND
    GLuint texid2, fbid2;
#endif
    int realWidth, realHeight;
    int fboWidth, fboHeight;
    int redimensionStamp;
    rdpRect_t area;
    rdpRect_t mod;
    rglDepthBuffer_t * depthBuffer;
    int chunkId;
    rglDepthSection_t depthSections[16];
    int nbDepthSections;
};
#define RGL_RB_DEPTH   1
#define RGL_RB_FULL    2
#define RGL_RB_ERASED  4
#define RGL_RB_FBMOD   8 // the GL framebuffer was modified
#define RGL_RB_RAMMOD 16 // the framebuffer was modified in rdram
#define RGL_RB_HASTRIANGLES 32 // we assume it's not a depth buffer in this case

CIRCLEQ_HEAD(rglRenderBufferHead_t, rglRenderBuffer_t);

#define MAX_RENDER_BUFFERS 64
extern rglRenderBuffer_t rBuffers[MAX_RENDER_BUFFERS];
extern int nbRBuffers;
extern rglRenderBuffer_t * curRBuffer;
extern rglRenderBuffer_t * curZBuffer;
extern rglRenderBufferHead_t rBufferHead;

extern int rglTexCacheCounter;
struct rglTexture_t {
    CIRCLEQ_ENTRY(rglTexture_t) byCrc, byUsage;
    GLuint id, zid;
    uint32_t crc;
    int w, h, fmt;
    int clipw, cliph;
    GLuint ws, wt, filter; // current settings
};
CIRCLEQ_HEAD(rglTextureHead_t, rglTexture_t);
#define RGL_TEX_CACHE_SIZE 1024
extern rglTexture_t rglTextures[RGL_TEX_CACHE_SIZE];
struct rglTexCache_t {
    int counter;
    rglTexture_t * tex;
};
extern rglTexCache_t rglTexCache[0x1000];
extern uint8_t rglTmpTex[];
extern uint8_t rglTmpTex2[];

struct rglTile_t : public rdpTile_t {
    rglTexture_t * tex;
    rglRenderBuffer_t * hiresBuffer;
    uint32_t hiresAddress;
    GLuint ws, wt; // GL clamping modes
    GLuint filter; // GL filter mode
};

struct rglVertex_t {
    float x, y, z, w;
    float s, t;
    uint8_t r, g, b, a;
};

struct rglStrip_t {
    int tilenum;
    int nbVtxs;
    int flags;
    rglVertex_t * vtxs;
};

#define RGL_STRIP_TEX1    1
#define RGL_STRIP_TEX2    2
#define RGL_STRIP_SHADE   4
#define RGL_STRIP_ZBUFFER 8

struct rglRenderChunk_t {
    rdpState_t rdpState;
    rglTile_t tiles[8];
    rglRenderBuffer_t * renderBuffer;
    uint32_t depthAddress;
    int flags;
    int nbStrips;
    rglStrip_t * strips;
#ifdef RDP_DEBUG
    rglShader_t * shader;
    int tracePos;
#endif
};

// first 8 bits used for tile usage
#define RGL_CHUNK_CLEAR (1<<8)

#define MAX_RENDER_CHUNKS 40000
extern rglRenderChunk_t chunks[MAX_RENDER_CHUNKS];
extern rglRenderChunk_t * curChunk;
extern int nbChunks;

#define MAX_STRIPS 80000
extern rglStrip_t strips[MAX_STRIPS];
extern rglVertex_t vtxs[6*MAX_STRIPS];
extern int nbStrips, nbVtxs;

struct rglRenderMode_t {
    rdpOtherModes_t otherModes;
    rdpCombineModes_t combineModes;
    uint32_t flags;
};

#define RGL_RM_DEPTH 1

// TODO use a hash table
#define MAX_RENDER_MODES 1024
extern rglRenderMode_t renderModesDb[MAX_RENDER_MODES];
extern int nbRenderModes;

extern rglShader_t * rglCopyShader;
extern rglShader_t * rglCopyDepthShader;


#define RGL_COMB_FMT_RGBA   0
#define RGL_COMB_FMT_I      1
#define RGL_COMB_FMT_DEPTH  2
#define RGL_COMB_FMT        3
#define RGL_COMB_IN0_DEPTH  4
#define RGL_COMB_IN0        4
#define RGL_COMB_IN1_DEPTH  8
#define RGL_COMB_IN1        8
#define RGL_COMB_TILE7      16

extern volatile int rglStatus, rglNextStatus;
#define RGL_STATUS_CLOSED     0
#define RGL_STATUS_WINDOWED   1
#define RGL_STATUS_FULLSCREEN 2


void rglUpdateStatus();
void rglTouchTMEM();
void rglResetTextureCache();
void rglTile(rdpTile_t & tile, rglTile_t & rtile, int recth);
void rglRenderMode(rglRenderChunk_t & chunk);
void rglBlender(rglRenderChunk_t & chunk);
void rglClearCombiners();
void rglSetCombiner(rglRenderChunk_t & chunk, int format);
void rglPrepareRendering(int texturing, int tilenum, int recth, int depth);
rglRenderBuffer_t * rglSelectRenderBuffer(uint32_t addr, int width, int size, int format);
char * rglCombiner2String(rdpState_t & state);


int rglInit();
void rglClose();
int rglOpenScreen();
void rglCloseScreen();
int rglReadSettings();
void rglUpdate();
void rglFullSync();
void rglTextureRectangle(rdpTexRect_t * rect, int flip);
void rglFillRectangle(rdpRect_t * rect);
void rglTriangle(uint32_t w1, uint32_t w2, int shade, int texture, int zbuffer,
                 uint32_t * rdp_cmd);
void rglRenderChunks();
void rglDisplayFramebuffers();
int rglT1Usage(rdpState_t & state);
int rglT2Usage(rdpState_t & state);
void rglDebugger();
void rglCloseDebugger();
void rglFramebuffer2Rdram(rglRenderBuffer_t & buffer, uint32_t start, uint32_t stop);
void rglRdram2Framebuffer(rglRenderBuffer_t & buffer, uint32_t start, uint32_t stop);
void rglRenderChunks(rglRenderBuffer_t * upto);
void rglRenderChunks(int upto);
float rglZscale(uint16_t z);

void rglSwapBuffers();

extern int screen_width, screen_height;

extern void check();

#endif
