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

static const char *saRGBText[] =
{
    "PREV",               "TEXEL0",               "TEXEL1",               "PRIM", 
    "SHADE",              "ENV",                  "NOISE",                "1",
    "0",                  "0",                    "0",                    "0",
    "0",                  "0",                    "0",                    "0"
};

static const char *mRGBText[] =
{
    "PREV",               "TEXEL0",               "TEXEL1",               "PRIM", 
    "SHADE",              "ENV",                  "SCALE",                "PREV_ALPHA",
    "TEXEL0_ALPHA",       "TEXEL1_ALPHA",         "PRIM_ALPHA",           "SHADE_ALPHA",
    "ENV_ALPHA",          "LOD_FRACTION",         "PRIM_LOD_FRAC",        "K5",
    "0",                  "0",                    "0",                    "0",
    "0",                  "0",                    "0",                    "0",
    "0",                  "0",                    "0",                    "0",
    "0",                  "0",                    "0",                    "0"
};

static const char *aRGBText[] =
{
    "PREV",               "TEXEL0",               "TEXEL1",               "PRIM", 
    "SHADE",              "ENV",                  "1",                    "0",
};

static const char *saAText[] =
{
    "PREV",               "TEXEL0",               "TEXEL1",               "PRIM", 
    "SHADE",              "ENV",                  "1",                    "0",
};

static const char *sbAText[] =
{
    "PREV",               "TEXEL0",               "TEXEL1",               "PRIM", 
    "SHADE",              "ENV",                  "1",                    "0",
};

static const char *mAText[] =
{
    "LOD_FRACTION",       "TEXEL0",               "TEXEL1",               "PRIM", 
    "SHADE",              "ENV",                  "PRIM_LOD_FRAC",        "0",
};

static const char *aAText[] =
{
    "PREV",               "TEXEL0",               "TEXEL1",               "PRIM", 
    "SHADE",              "ENV",                  "1",                    "0",
};

const static char * bRGBText[] = { "PREV", "FRAG", "BLEND", "FOG" };
const static char * bAText[2][4] = { {"PREVA", "FOGA", "SHADEA", "0"},
{"(1.0-ALPHA)", "FRAGA", "1", "0"}};

char * rglCombiner2String(rdpState_t & state)
{
    rdpOtherModes_t om = state.otherModes;
    int cycle = RDP_GETOM_CYCLE_TYPE(om);
    static char res[256];
    char * p = res;
    if (cycle < 2) {
        p += sprintf(
            p,
            "c = [ (%s - %s) * %s + %s | (%s - %s) * %s + %s ]\n",
            saRGBText[RDP_GETCM_SUB_A_RGB0(state.combineModes)],
            saRGBText[RDP_GETCM_SUB_B_RGB0(state.combineModes)],
            mRGBText[RDP_GETCM_MUL_RGB0(state.combineModes)],
            aRGBText[RDP_GETCM_ADD_RGB0(state.combineModes)],
            saAText[RDP_GETCM_SUB_A_A0(state.combineModes)],
            sbAText[RDP_GETCM_SUB_B_A0(state.combineModes)],
            mAText[RDP_GETCM_MUL_A0(state.combineModes)],
            aAText[RDP_GETCM_ADD_A0(state.combineModes)]);
    }
    if (cycle == 1) {
        p += sprintf(
            p,
            "c = [ (%s - %s) * %s + %s | (%s - %s) * %s + %s ]\n",
            saRGBText[RDP_GETCM_SUB_A_RGB1(state.combineModes)],
            saRGBText[RDP_GETCM_SUB_B_RGB1(state.combineModes)],
            mRGBText[RDP_GETCM_MUL_RGB1(state.combineModes)],
            aRGBText[RDP_GETCM_ADD_RGB1(state.combineModes)],
            saAText[RDP_GETCM_SUB_A_A1(state.combineModes)],
            sbAText[RDP_GETCM_SUB_B_A1(state.combineModes)],
            mAText[RDP_GETCM_MUL_A1(state.combineModes)],
            aAText[RDP_GETCM_ADD_A1(state.combineModes)]);
    }
    if (cycle < 2) {
        p += sprintf(
            p,
            "%s*%s + %s*%s\n"
            ,bAText[0][RDP_GETOM_BLEND_M1B_0(state.otherModes)],
            bRGBText[RDP_GETOM_BLEND_M1A_0(state.otherModes)],
            bAText[1][RDP_GETOM_BLEND_M2B_0(state.otherModes)],
            bRGBText[RDP_GETOM_BLEND_M2A_0(state.otherModes)]
        );
    }
    if (cycle == 1) {
        p += sprintf(
            p,
            "%s*%s + %s*%s\n"
            ,bAText[0][RDP_GETOM_BLEND_M1B_1(state.otherModes)],
            bRGBText[RDP_GETOM_BLEND_M1A_1(state.otherModes)],
            bAText[1][RDP_GETOM_BLEND_M2B_1(state.otherModes)],
            bRGBText[RDP_GETOM_BLEND_M2A_1(state.otherModes)]
        );
    }
    return res;
}

#ifdef RDP_DEBUG

#include <SDL.h>
//#include <IL/il.h>
#include <assert.h>

#include <FTGLTextureFont.h>

#define FONT "LucidaTypewriterRegular.ttf"
#define SMALLFONT "LucidaTypewriterRegular.ttf"
//#define SMALLFONT "/usr/share/fonts/corefonts/arial.ttf"
#define FS 12
#define SMALLFS 12

static FTFont * font;
static FTFont * smallfont;
static FTFont * curfont;

static int fbindex;
static int chunkindex, stripindex;
static int mx, my;
static float scalex, scaley;
static rglShader_t * alphaShader;

static int lines[0x10000], nblines;
static char dasm[512];

void gglPrint(int x, int y, const char * text)
{
    glPushAttrib(GL_ALL_ATTRIB_BITS);
    glPushMatrix();
    glTranslatef(x, y, 0);

    glEnable( GL_TEXTURE_2D);
    glDisable( GL_DEPTH_TEST);
    //glRasterPos2i( x , y);
    curfont->Render(text);

    glPopMatrix();
    glPopAttrib();

    //printf("%s\n", text);
}

void gglPrintf(int x, int y, const char * s, ...)
{
    char buf[1024];
    va_list ap;
    va_start(ap, s);
    vsprintf(buf, s, ap);
    va_end(ap);
    gglPrint(x, y, buf);
}

void rglDisplayTrace(int x, int y, int start, int lines)
{
    glBlendFunc( GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA );
    rglUseShader(0);
    curfont = smallfont;
    start = ::lines[start];
    while (lines-->0 && start <= rdpTracePos) {
        glColor4f(0,0,0, 0.5);
        glEnable(GL_BLEND);
        glDisable(GL_TEXTURE_2D);
        glBegin(GL_TRIANGLE_STRIP);
        glVertex2f(x, y);
        glVertex2f(x+2*screen_width*3/4, y);
        glVertex2f(x, y-(SMALLFS+2));
        glVertex2f(x+2*screen_width*3/4, y-(SMALLFS+2));
        glEnd();

        glColor4f(1,1,0.5,1);
        glDisable(GL_BLEND);
        start += rdp_dasm(rdpTraceBuf, start, start+256, dasm)/4;
        gglPrintf(x, y-(SMALLFS), "%4x %s", start, dasm);
        y -= (SMALLFS+2);
    }
    curfont = font;
    //   glDisable(GL_BLEND);
}

void rglDisplayColor(uint32_t color, int x , int y, const char * name, int sixteen = 0)
{
    float r, g, b, a;
    if (sixteen) {
        r = RDP_GETC16_R(color)/ 31.0f;
        g = RDP_GETC16_G(color)/ 31.0f;
        b = RDP_GETC16_B(color)/ 31.0f;
        a = RDP_GETC16_A(color)/  1.0f;
    } else {
        r = RDP_GETC32_R(color)/255.0f;
        g = RDP_GETC32_G(color)/255.0f;
        b = RDP_GETC32_B(color)/255.0f;
        a = RDP_GETC32_A(color)/255.0f;
    }
    y -= FS+2;
    glColor4f(r, g, b, 1);
    glDisable(GL_TEXTURE_2D);
    glBegin(GL_TRIANGLE_STRIP);
    glVertex2f(x, y);
    glVertex2f(x+128, y);
    glVertex2f(x, y-16);
    glVertex2f(x+128, y-16);
    glEnd();

    glEnable(GL_TEXTURE_2D);
    glColor4f(1,1,1,1);
    gglPrintf(x, y+2, "%5s %08x", name, color);

}

void rglDisplayChunkInfo(rglRenderChunk_t & chunk)
{
    int x = 0, y = screen_height;
    int i;
    rdpState_t & state = chunk.rdpState;
    rdpOtherModes_t om = state.otherModes;
    rdpCombineModes_t cm = state.combineModes;
    int cycle = RDP_GETOM_CYCLE_TYPE(om);

    rglDisplayColor(chunk.rdpState.primColor, x, y, "prim");
    y -= 16+FS+10;
    rglDisplayColor(chunk.rdpState.blendColor, x, y, "blend");
    y -= 16+FS+10;
    rglDisplayColor(chunk.rdpState.envColor, x, y, "env");
    y -= 16+FS+10;
    rglDisplayColor(chunk.rdpState.fogColor, x, y, "fog");
    y -= 16+FS+10;
    rglDisplayColor(chunk.rdpState.fillColor, x, y, "fill", 1);
    y -= 16+FS+10;

    y += 5*(16+FS+10);
    x += 128+20;

    glColor4f(1,1,1,1);
    int oldy = y;
    for (i=0; i<8; i++) {
        int j;
        int oldx = x;
        if (!(chunk.flags & (1<<i))) continue;
        rglTile_t & tile = chunk.tiles[i];
        int w = tile.w, h = tile.h;
        if (w > 64) w = 64;
        if (h > 64) h = 64;
        gglPrintf(x, y-h-FS, "#%d %dx%d %x", i, tile.w, tile.h, tile.hiresBuffer? 0 : tile.tex->crc);
        gglPrintf(x, y-h-2*FS, "fmt %s-%d %d %d", rdpImageFormats[tile.format], tile.size, tile.line, tile.hiresBuffer? tile.hiresBuffer-rBuffers : -1);
        gglPrintf(x, y-h-3*FS, "clip %dx%d %dx%d", tile.sl, tile.tl, tile.sh, tile.th);
        gglPrintf(x, y-h-4*FS, "mask %dx%d shift %dx%d", tile.mask_s, tile.mask_t, tile.shift_s, tile.shift_t);
        gglPrintf(x, y-h-5*FS, "%d %d %d %d pal %d", tile.cs, tile.ms, tile.ct, tile.ms, tile.palette);
        glEnable(GL_TEXTURE_2D);
        if (tile.hiresBuffer)
            glBindTexture(GL_TEXTURE_2D, tile.hiresBuffer->texid);
        else
            glBindTexture(GL_TEXTURE_2D, tile.tex->id);
        for (j=0; j<2; j++) {
            glBegin(GL_TRIANGLE_STRIP);
            glTexCoord2f(0, 0); glVertex2f(x, y);
            glTexCoord2f(0, 1); glVertex2f(x, y-h);
            glTexCoord2f(1, 0); glVertex2f(x+w, y);
            glTexCoord2f(1, 1); glVertex2f(x+w, y-h);
            glEnd();
            rglUseShader(alphaShader);
            x += w+2;
        }
        rglUseShader(0);
        //     if ((tile.w+2)*2 < 256)
        //       x += 256 - (tile.w+2)*2;
        x = oldx;
        y -= h + 5*FS + 5;
    }

    y = oldy;
    x = 128+210;

    y -= FS;
    gglPrintf(x, y, "cycle %d persp %d detail %d sharpen %d tex_lod %d en_tlut %d tlut_type %d clipm %d",
        RDP_GETOM_CYCLE_TYPE(om),
        RDP_GETOM_PERSP_TEX_EN(om),
        RDP_GETOM_DETAIL_TEX_EN(om),
        RDP_GETOM_SHARPEN_TEX_EN(om),
        RDP_GETOM_TEX_LOD_EN(om),
        RDP_GETOM_EN_TLUT(om),
        RDP_GETOM_TLUT_TYPE(om),
        chunk.rdpState.clipMode);

    y -= FS;
    gglPrintf(x, y, "sample_type %d mid %d lerp0 %d lerp1 %d convert1 %d key_en %d rgb_dith_sel %d",
        RDP_GETOM_SAMPLE_TYPE(om),
        RDP_GETOM_MID_TEXEL(om),
        RDP_GETOM_BI_LERP0(om),
        RDP_GETOM_BI_LERP1(om),
        RDP_GETOM_CONVERT_ONE(om),
        RDP_GETOM_KEY_EN(om),
        RDP_GETOM_RGB_DITHER_SEL(om));

    y -= FS;
    gglPrintf(x, y, "A_dith_sel %d force_blend %d A_cvg_sel %d cvgXA %d Zmode %d cvg_dest %d col_on %d",
        RDP_GETOM_ALPHA_DITHER_SEL(om),
        RDP_GETOM_FORCE_BLEND(om),
        RDP_GETOM_ALPHA_CVG_SELECT(om),
        RDP_GETOM_CVG_TIMES_ALPHA(om),
        RDP_GETOM_Z_MODE(om),
        RDP_GETOM_CVG_DEST(om),
        RDP_GETOM_COLOR_ON_CVG(om));

    y -= FS;
    gglPrintf(x, y, "img_read %d Zupdate %d Zcmp_sel %d antialias %d Zsource %d dith_A_en %d A_cmp %d",
        RDP_GETOM_IMAGE_READ_EN(om),
        RDP_GETOM_Z_UPDATE_EN(om),
        RDP_GETOM_Z_COMPARE_EN(om),
        RDP_GETOM_ANTIALIAS_EN(om),
        RDP_GETOM_Z_SOURCE_SEL(om),
        RDP_GETOM_DITHER_ALPHA_EN(om),
        RDP_GETOM_ALPHA_COMPARE_EN(om));

    y -= 2*FS;

    if (cycle < 2) {
        gglPrintf(x, y,
            "c = [ (%s - %s) * %s + %s | (%s - %s) * %s + %s ];",
            saRGBText[RDP_GETCM_SUB_A_RGB0(state.combineModes)],
            saRGBText[RDP_GETCM_SUB_B_RGB0(state.combineModes)],
            mRGBText[RDP_GETCM_MUL_RGB0(state.combineModes)],
            aRGBText[RDP_GETCM_ADD_RGB0(state.combineModes)],
            saAText[RDP_GETCM_SUB_A_A0(state.combineModes)],
            sbAText[RDP_GETCM_SUB_B_A0(state.combineModes)],
            mAText[RDP_GETCM_MUL_A0(state.combineModes)],
            aAText[RDP_GETCM_ADD_A0(state.combineModes)]);

        y -= FS;
    }
    if (cycle == 1) {
        //if (cycle < 2) {
        gglPrintf(x, y,
            "c = [ (%s - %s) * %s + %s | (%s - %s) * %s + %s ];",
            saRGBText[RDP_GETCM_SUB_A_RGB1(state.combineModes)],
            saRGBText[RDP_GETCM_SUB_B_RGB1(state.combineModes)],
            mRGBText[RDP_GETCM_MUL_RGB1(state.combineModes)],
            aRGBText[RDP_GETCM_ADD_RGB1(state.combineModes)],
            saAText[RDP_GETCM_SUB_A_A1(state.combineModes)],
            sbAText[RDP_GETCM_SUB_B_A1(state.combineModes)],
            mAText[RDP_GETCM_MUL_A1(state.combineModes)],
            aAText[RDP_GETCM_ADD_A1(state.combineModes)]);

        y -= FS;
    }
    if (cycle < 2) {
        gglPrintf(x, y,
            "%s*%s + %s*%s"
            ,bAText[0][RDP_GETOM_BLEND_M1B_0(state.otherModes)],
            bRGBText[RDP_GETOM_BLEND_M1A_0(state.otherModes)],
            bAText[1][RDP_GETOM_BLEND_M2B_0(state.otherModes)],
            bRGBText[RDP_GETOM_BLEND_M2A_0(state.otherModes)]
        );

        y -= FS;
    }
    if (cycle == 1) {
        //if (cycle < 2) {
        gglPrintf(x, y,
            "%s*%s + %s*%s"
            ,bAText[0][RDP_GETOM_BLEND_M1B_1(state.otherModes)],
            bRGBText[RDP_GETOM_BLEND_M1A_1(state.otherModes)],
            bAText[1][RDP_GETOM_BLEND_M2B_1(state.otherModes)],
            bRGBText[RDP_GETOM_BLEND_M2A_1(state.otherModes)]
        );

        y -= FS;
    }

    if (chunk.nbStrips) {
        y -= FS;
        rglStrip_t & strip = chunk.strips[chunkindex >= 0? stripindex:0];

        int i;
        for (i=0; i<strip.nbVtxs; i++) {
            rglVertex_t vtx = strip.vtxs[i];
            int oldx;
            gglPrintf(x, y, "%g %g %g %g", vtx.x, vtx.y, vtx.z, vtx.w);
            x += 256;
            if (strip.flags & RGL_STRIP_SHADE) {
                gglPrintf(x, y, "%d %d %d %d", vtx.r, vtx.g, vtx.b, vtx.a);
                x += 200;
            }
            if (strip.flags & (RGL_STRIP_TEX1|RGL_STRIP_TEX2)) {
                gglPrintf(x, y, "%g %g", vtx.s, vtx.t);
                x += 192;
            }
            y -= FS;
            x = oldx;
        }
    }

    //   LOG("missing om %x %x (%x %x)\n",
    //       chunk.rdpState.otherModes.w1&RDP_OM_MISSING1,
    //       chunk.rdpState.otherModes.w2&RDP_OM_MISSING2,
    //       RDP_OM_MISSING1,
    //       RDP_OM_MISSING2);
    //   LOG("missing cm %x %x\n",
    //       chunk.rdpState.combineModes.w1&~(RDP_COMBINE_MASK11|RDP_COMBINE_MASK21),
    //       chunk.rdpState.combineModes.w2&~(RDP_COMBINE_MASK12|RDP_COMBINE_MASK22));
}

void rglDisplayFramebuffer(rglRenderBuffer_t & buffer, int alpha)
{
    int i;

    if (alpha)
        rglUseShader(alphaShader);
    else
        rglUseShader(rglCopyShader);
    glBindTexture(GL_TEXTURE_2D, buffer.texid);
    glEnable(GL_TEXTURE_2D);
    glDisable(GL_DEPTH_TEST);
    glDisable(GL_BLEND);
    glColor4ub(255, 255, 255, 255);
    glBegin(GL_TRIANGLE_STRIP);
    glTexCoord2f(1, 1);    glVertex2f(1, 0);
    glTexCoord2f(0, 1);    glVertex2f(0, 0);
    glTexCoord2f(1, 0);    glVertex2f(1, 1);
    glTexCoord2f(0, 0);    glVertex2f(0, 1);
    glEnd();
}

void rglDisplayFlat(rglRenderChunk_t & chunk)
{
    int j;
    rglRenderBuffer_t & buffer = *chunk.renderBuffer;

    glPushAttrib(GL_ALL_ATTRIB_BITS);
    //glEnable(GL_SCISSOR_TEST);
    rglUseShader(0);
    glDisable(GL_TEXTURE_2D);
    glDisable(GL_CULL_FACE);

    //   glScissor((chunk.rdpState.clip.xh >>2)*buffer.realWidth/buffer.width,
    //             (chunk.rdpState.clip.yh >>2)*buffer.realHeight/buffer.height,
    //             (chunk.rdpState.clip.xl-chunk.rdpState.clip.xh >>2)*buffer.realWidth/buffer.width,
    //             (chunk.rdpState.clip.yl-chunk.rdpState.clip.yh >>2)*buffer.realHeight/buffer.height);


    for (j=0; j<chunk.nbStrips; j++) {
        rglStrip_t & strip = chunk.strips[j];
        int k;

        if (chunkindex >= 0 && j == stripindex) {
            glPushAttrib(GL_ALL_ATTRIB_BITS);
            glColor4ub(255, 255, 128, 255);
        }
        glBegin(GL_TRIANGLE_STRIP);
        for (k=0; k<strip.nbVtxs; k++) {
            glVertex2f((strip.vtxs[k].x/(buffer.width)),
                1-(strip.vtxs[k].y/(buffer.height)));
        }
        glEnd();
        if (chunkindex >= 0 && j == stripindex)
            glPopAttrib();
    }

    glPopAttrib();
}

int rglFindStrip(rglRenderChunk_t & chunk, float mx, float my)
{
    int j;
    rglRenderBuffer_t & buffer = *chunk.renderBuffer;
    for (j=chunk.nbStrips-1; j>=0; j--) {
        rglStrip_t & strip = chunk.strips[j];
        int k;
        struct { float x, y; } s[3];

        for (k=0; k<strip.nbVtxs; k++) {
            s[0] = s[1];
            s[1] = s[2];
            s[2].x = strip.vtxs[k].x/(buffer.width);
            s[2].y = 1 - strip.vtxs[k].y/(buffer.height);
            if (k >= 2) {
                float last = 0;
                int i;
                for (i=0; i<3; i++) {
                    float dx1 = s[(i+1)%3].x - s[i].x;
                    float dy1 = s[(i+1)%3].y - s[i].y;
                    float dx2 = mx - s[i].x;
                    float dy2 = my - s[i].y;
                    dx1 = dx1*dy2-dx2*dy1;
                    if (dx1 == 0) goto next;
                    if (last*dx1 < 0)
                        goto next;
                    last = dx1;
                }
                stripindex = j;
                return j;
next:;
            }
        }
    }
    return -1;
}

void rglDisplayFlat(rglRenderBuffer_t & buffer)
{
    int i;
    for (i=0; i<nbChunks; i++) {
        rglRenderChunk_t & chunk = chunks[i];
        if (chunk.renderBuffer != &buffer) continue;
        rglDisplayFlat(chunk);
    }
}

int rglFindChunk(rglRenderBuffer_t & buffer, float mx, float my)
{
    int i;
    if (chunkindex <= 0)
        i = nbChunks-1;
    else
        i = chunkindex-1;
    for (; i>=0; i--) {
        rglRenderChunk_t & chunk = chunks[i];
        if (chunk.renderBuffer != &buffer) continue;
        if (rglFindStrip(chunk, mx, my) >= 0)
            return i;
    }
    return -1;
}

void rglShowCursor(int state)
{
#ifdef WIN32
#else
    SDL_ShowCursor(state);
#endif
}

#ifndef WIN32
static int keys[512];
#define MOUSEBUT 511
#else
# define MOUSEBUT       VK_LBUTTON
# define SDLK_ESCAPE    VK_ESCAPE
# define SDLK_KP_PLUS   VK_ADD
# define SDLK_KP_MINUS  VK_SUBTRACT
# define SDLK_TAB       VK_TAB
# define SDLK_UP        VK_UP
# define SDLK_DOWN      VK_DOWN
# define SDLK_PAGEUP    VK_PRIOR
# define SDLK_PAGEDOWN  VK_NEXT
#endif

int rglCheckKey(int key)
{
#ifdef WIN32
    return GetAsyncKeyState (key) & 1;
#else
    if (key >= 'A' && key <= 'Z') key += 'a' - 'A';
    int res = keys[key];
    keys[key] = 0;
    return res;
#endif  
}

void rglDebugger()
{
    SDL_Event event;
    int paused = 1;
    int i, j;
    int traceX = 1;
    int tracepos = 0;
    int tracepage = (screen_height*3/4)/(SMALLFS+2);
    int oldchunkindex = -1;

    fbindex = 0;
    chunkindex = -1;

    void rglInitDebugger();
    rglInitDebugger();

    rglShowCursor(SDL_ENABLE);

    glActiveTextureARB(GL_TEXTURE1_ARB);
    glDisable(GL_TEXTURE_2D);
    glActiveTextureARB(GL_TEXTURE2_ARB);
    glDisable(GL_TEXTURE_2D);
    glActiveTextureARB(GL_TEXTURE0_ARB);
    glDrawBuffer(GL_BACK);

    for (i=nblines=0; i<=rdpTracePos; i += rdp_dasm(rdpTraceBuf, i, i+256, dasm)/4, nblines++)
        lines[nblines] = i;

    if (nbChunks > 1)
        // skip chunk 0 as it's usually depth clear
        fbindex = chunks[1].renderBuffer - rBuffers;

    while (paused) {
#ifndef WIN32
        int res = SDL_WaitEvent(&event);
        while (res) {
            switch (event.type) {
        case SDL_MOUSEBUTTONDOWN:
            keys[MOUSEBUT] = 1;
            break;
        case SDL_MOUSEBUTTONUP:
            keys[MOUSEBUT] = 0;
            break;
        case SDL_KEYDOWN:
            if (event.key.keysym.sym < MOUSEBUT)
                keys[event.key.keysym.sym] = 1;
            break;
        case SDL_KEYUP:
            if (event.key.keysym.sym < MOUSEBUT)
                keys[event.key.keysym.sym] = 0;
            break;
            }      
            res = SDL_PollEvent(&event);      
        }
#endif
        rglRenderBuffer_t & buffer = rBuffers[fbindex];
        scalex = buffer.realWidth; scaley = buffer.realHeight;

        if (rBuffers[fbindex].fbid) {
            if (buffer.realWidth > scalex*3/4 ||
                buffer.realHeight > scaley*3/4) {
                    scalex = scalex*3/4;
                    scaley = scaley*3/4;
            }
        }

        if (rglCheckKey(MOUSEBUT)) {
            if (buffer.fbid) {
#ifdef WIN32
                POINT pt;
                GetCursorPos(&pt);
                mx = pt.x;
                my = pt.y;
#else
                SDL_GetMouseState(&mx, &my);
#endif
                int old = chunkindex;
                if (old >= 0)
                    chunkindex++;
                chunkindex = rglFindChunk(buffer, float(mx)/scalex, float(screen_height - my)/scaley);
                if (old >= 0 && chunkindex == old) {
                } else {
                    chunkindex = -1;
                }
                chunkindex = rglFindChunk(buffer, float(mx)/scalex, float(screen_height - my)/scaley);
                if (chunkindex >= 0 && nbChunks)
                    printf("%s\n", chunks[chunkindex].shader->fsrc);
            }
        }
        if (rglCheckKey('P') || rglCheckKey(SDLK_ESCAPE))
            paused = 0;
        if (rglCheckKey(SDLK_KP_PLUS)) {
            if (fbindex < MAX_RENDER_BUFFERS-1/* &&
                                              rBuffers[fbindex+1].fbid*/)
                                              fbindex++;
            chunkindex = -1;
        }
        if (rglCheckKey(SDLK_KP_MINUS)) {
            if (fbindex > 0/* &&
                           rBuffers[fbindex-1].fbid*/)
                           fbindex--;
            chunkindex = -1;
        }
        if (rglCheckKey(SDLK_TAB))
            traceX = !traceX;
        if (rglCheckKey(SDLK_UP))
            tracepos--;
        if (rglCheckKey(SDLK_DOWN))
            tracepos++;
        if (rglCheckKey(SDLK_PAGEUP))
            tracepos -= tracepage/2;
        if (rglCheckKey(SDLK_PAGEDOWN))
            tracepos += tracepage/2;
        if (tracepos < 0)
            tracepos = 0;
        if (tracepos > nblines-tracepage/2)
            tracepos = nblines-tracepage/2;

        //rglRenderChunks();

        glBindFramebufferEXT(GL_FRAMEBUFFER_EXT, 0);
        glDrawBuffer(GL_BACK);
        glDisable(GL_SCISSOR_TEST);
        glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
        glActiveTextureARB(GL_TEXTURE1_ARB);
        glDisable(GL_TEXTURE_2D);
        glActiveTextureARB(GL_TEXTURE0_ARB);
        glDisable(GL_ALPHA_TEST);

        glClearColor(0, 0, 0, 0);
        glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);
        glClear(GL_COLOR_BUFFER_BIT);

        if (buffer.fbid) {
            //glViewport(0, 0, scalex*screen_width/buffer.realWidth, scaley*screen_height/buffer.realHeight);
            glViewport(0, 0, scalex, scaley);
            rglDisplayFramebuffer(buffer, 0);
            glViewport(scalex, 0, scalex, scaley);
            rglDisplayFramebuffer(buffer, 1);

            glViewport(0, 0, scalex, scaley);
            glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
            glEnable(GL_BLEND);
            //glBlendFunc( GL_ONE, GL_ONE );
            glBlendFunc(GL_SRC_COLOR, GL_ONE_MINUS_DST_COLOR);
            if (chunkindex < 0) {
                //glColor4f(0.1, 0, 0.1, 0.5);
                glColor4f(0.6, 0, 0.6, 0.5);
                rglDisplayFlat(buffer);
            } else {
                glColor4f(0.6, 0, 0.6, 0.5);
                rglDisplayFlat(chunks[chunkindex]);
            }
        }

        {
            glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
            glDisable(GL_BLEND);


            glMatrixMode( GL_PROJECTION);
            glPushMatrix();
            glLoadIdentity();
            gluOrtho2D(0, screen_width, 0, screen_height);
            glMatrixMode(GL_MODELVIEW);
            glPushMatrix();
            glLoadIdentity();

            glViewport(0, 0, screen_width, screen_height);
            rglUseShader(0);

            glColor3f(1,0.5,0.5);
            gglPrintf(0, 0, "Fb #%d at $%x --> %x (%dx%d fmt %s-%d) upto %d %s", fbindex,
                buffer.addressStart, buffer.addressStop,
                buffer.width, buffer.height,
                (buffer.flags & RGL_RB_DEPTH)? "Z":rdpImageFormats[buffer.format], buffer.size,
                buffer.chunkId,
                (buffer.flags & RGL_RB_ERASED)? "ERASED":"");

            if (chunkindex >= 0) {
                gglPrintf(0, FS, "Chunk #%d", chunkindex);

                rglDisplayChunkInfo(chunks[chunkindex]);
            }

            if (oldchunkindex != chunkindex) {
                oldchunkindex = chunkindex;
                if (chunkindex >= 0)
                    for (i=0; i<nblines; i++)
                        if (lines[i] == chunks[chunkindex].tracePos)
                            tracepos = i;
            }
            rglDisplayTrace(traceX*scalex, screen_height*3/4, tracepos, tracepage);

            glMatrixMode( GL_PROJECTION);
            glPopMatrix();
            glMatrixMode(GL_MODELVIEW);
            glPopMatrix();
        }

        rglSwapBuffers();
    }

    rglShowCursor(SDL_DISABLE);
}

void rglCloseDebugger()
{
    if (font) {
        delete font;
        font = 0;
    }
    if (smallfont) {
        delete smallfont;
        smallfont = 0;
    }
    if (alphaShader) {
        rglDeleteShader(alphaShader);
        alphaShader = 0;
    }
}

void rglInitDebugger()
{
    if (!font) {
        char s[1024];
        extern char rgl_cwd[512];
        sprintf(s, "%s/"FONT, rgl_cwd);
        curfont = font = new FTGLTextureFont(s);
        sprintf(s, "%s/"SMALLFONT, rgl_cwd);
        smallfont = new FTGLTextureFont(s);
        if (!font || !smallfont) {
            LOGERROR("Couldn't load font '%s'\n", s);
            return;
        }
        font->FaceSize(FS);
        smallfont->FaceSize(SMALLFS);
    }

    if (!alphaShader) {
        alphaShader = rglCreateShader(
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
            "  gl_FragColor = gl_Color * texture2D(texture0, vec2(gl_TexCoord[0])).a; \n"
            "}                                 \n"
            );
    }   
}

void rdpBacktrace()
{
    int i=0;
    while (i <= rdpTracePos) {
        i += rdp_dasm(rdpTraceBuf, i, i+256, dasm)/4;
        printf("%4x %s\n", i, dasm);
    }
}

#endif
