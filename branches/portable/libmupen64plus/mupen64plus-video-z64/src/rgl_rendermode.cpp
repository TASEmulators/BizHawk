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

void rglRenderMode(rglRenderChunk_t & chunk)
{
    //int i;
    glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);
    if (RDP_GETOM_CYCLE_TYPE(chunk.rdpState.otherModes) < 2) {
        glDepthMask(RDP_GETOM_Z_UPDATE_EN(chunk.rdpState.otherModes)? GL_TRUE:GL_FALSE);
        if (RDP_GETOM_Z_COMPARE_EN(chunk.rdpState.otherModes))
            glDepthFunc(GL_LESS);
        else
            glDepthFunc(GL_ALWAYS);
    } else {
        glDepthMask(GL_FALSE);
        glDepthFunc(GL_ALWAYS);
    }


    //   if (RDP_GETOM_Z_MODE(chunk.rdpState.otherModes) & 1) {
    //     glEnable( GL_POLYGON_OFFSET_FILL );
    //     switch(RDP_GETOM_Z_MODE(chunk.rdpState.otherModes)) {
    //       case 3:
    //         glPolygonOffset( -3, -300 );
    //         break;
    //       default:
    //         // FIXME tune this value
    //         //glPolygonOffset( -3.0f, -3.0f );
    //         glPolygonOffset( -3, -40 );
    //         break;
    //     }
    //     //glDepthMask(GL_FALSE);
    //   } else {
    //     glDisable( GL_POLYGON_OFFSET_FILL );
    //   }
}



struct rglCombiner_t {
    rdpCombineModes_t combineModes;
    rdpOtherModes_t otherModes;
    rglShader_t * shader;
#ifndef RGL_EXACT_BLEND
    GLuint srcBlend, dstBlend;
#endif
    int format;
};
#define RGL_MAX_COMBINERS 128
static int rglNbCombiners;
static rglCombiner_t rglCombiners[RGL_MAX_COMBINERS];

void rglClearCombiners()
{
    int i;
    for (i=0; i<rglNbCombiners; i++)
        rglDeleteShader(rglCombiners[i].shader);
    rglNbCombiners = 0;
}


int rglT1Usage(rdpState_t & state)
{
    //return 1;
    int cycle = RDP_GETOM_CYCLE_TYPE(state.otherModes);
    if (cycle == RDP_CYCLE_TYPE_COPY) return 1;
    if (cycle >= 2) return 0;
    if (cycle == 1 && (
        RDP_GETCM_SUB_A_RGB1(state.combineModes)==2 ||
        RDP_GETCM_SUB_B_RGB1(state.combineModes)==2 ||
        RDP_GETCM_MUL_RGB1(state.combineModes)==2 ||
        RDP_GETCM_MUL_RGB1(state.combineModes)==9 ||
        RDP_GETCM_ADD_RGB1(state.combineModes)==2 ||
        RDP_GETCM_SUB_A_A1(state.combineModes)==2 ||
        RDP_GETCM_SUB_B_A1(state.combineModes)==2 ||
        RDP_GETCM_MUL_A1(state.combineModes)==2 ||
        RDP_GETCM_ADD_A1(state.combineModes)==2))
        return 1;
    if (
        (RDP_GETOM_CVG_TIMES_ALPHA(state.otherModes) &&
        !RDP_GETOM_ALPHA_CVG_SELECT(state.otherModes)) ||

        RDP_GETCM_SUB_A_RGB0(state.combineModes)==1 ||
        RDP_GETCM_SUB_B_RGB0(state.combineModes)==1 ||
        RDP_GETCM_MUL_RGB0(state.combineModes)==1 ||
        RDP_GETCM_MUL_RGB0(state.combineModes)==8 ||
        RDP_GETCM_ADD_RGB0(state.combineModes)==1 ||
        RDP_GETCM_SUB_A_A0(state.combineModes)==1 ||
        RDP_GETCM_SUB_B_A0(state.combineModes)==1 ||
        RDP_GETCM_MUL_A0(state.combineModes)==1 ||
        RDP_GETCM_ADD_A0(state.combineModes)==1)

        return 1;

    return 0;
}
int rglT2Usage(rdpState_t & state)
{
    //return 1;
    int cycle = RDP_GETOM_CYCLE_TYPE(state.otherModes);
    if (cycle >= 2) return 0;
    if (cycle == 1 && (
        RDP_GETCM_SUB_A_RGB1(state.combineModes)==1 ||
        RDP_GETCM_SUB_B_RGB1(state.combineModes)==1 ||
        RDP_GETCM_MUL_RGB1(state.combineModes)==1 ||
        RDP_GETCM_MUL_RGB1(state.combineModes)==8 ||
        RDP_GETCM_ADD_RGB1(state.combineModes)==1 ||
        RDP_GETCM_SUB_A_A1(state.combineModes)==1 ||
        RDP_GETCM_SUB_B_A1(state.combineModes)==1 ||
        RDP_GETCM_MUL_A1(state.combineModes)==1 ||
        RDP_GETCM_ADD_A1(state.combineModes)==1))
        return 1;

    if (
        RDP_GETCM_SUB_A_RGB0(state.combineModes)==2 ||
        RDP_GETCM_SUB_B_RGB0(state.combineModes)==2 ||
        RDP_GETCM_MUL_RGB0(state.combineModes)==2 ||
        RDP_GETCM_MUL_RGB0(state.combineModes)==9 ||
        RDP_GETCM_ADD_RGB0(state.combineModes)==2 ||
        RDP_GETCM_SUB_A_A0(state.combineModes)==2 ||
        RDP_GETCM_SUB_B_A0(state.combineModes)==2 ||
        RDP_GETCM_MUL_A0(state.combineModes)==2 ||
        RDP_GETCM_ADD_A0(state.combineModes)==2)

        return 1;

    return 0;
}


void rglSetCombiner(rglRenderChunk_t & chunk, int format)
{
    static char _1ma[64];
    static char t1[64];
    static char t1a[64];
    static char t2[64];
    static char t2a[64];
    static char prim_lod_frac[64];

    static const char *saRGB[] = {
        "c",        t1,         t2,             "p/*PRIM*/", 
        "gl_Color", "e",        "1.0/*NOISE*/", "1.0",
        "0.0",      "0.0",      "0.0",          "0.0",
        "0.0",      "0.0",      "0.0",          "0.0"
    };

    static const char *mRGB[] = {
        "c",                t1,                 t2,                 "p/*PRIM*/", 
        "gl_Color/*SHADE*/","e",                "0.0/*SCALE*/",     "c.a/*COMBINED_A*/",
        "t1.a/*TEXEL0_A*/", "t2.a/*TEXEL1_A*/", "p.a/*PRIM_A*/",    "gl_Color.a/*SHADEA*/",
        "e.a/*ENV_ALPHA*/", "0.5/*LOD_FRACTION*/","0.5/*PRIM_LOD_FRAC*/","k5/*K5*/",
        "0.0",              "0.0",              "0.0",              "0.0",
        "0.0",              "0.0",              "0.0",              "0.0",
        "0.0",              "0.0",              "0.0",              "0.0",
        "0.0",              "0.0",              "0.0",              "0.0"
    };

    static const char *aRGB[] = {
        "c",                t1,             t2,         "p/*PRIM*/", 
        "gl_Color/*SHADE*/","e/*ENV*/",     "1.0",      "0.0",
    };

    static const char *saA[] = {
        "c.a",          t1a,        t2a,        "p.a/*PRIM*/", 
        "gl_Color.a",   "e.a",      "1.0",      "0.0",
    };

    static const char *sbA[] = {
        "c.a",          t1a,        t2a,        "p.a/*PRIM*/", 
        "gl_Color.a",   "e.a",      "1.0",      "0.0",
    };

    static const char *mA[] = {
        "0.5/*LOD_FRACTION*/",      t1a,        t2a,            "p.a/*PRIM*/", 
        "gl_Color.a/*SHADE*/",      "e.a",      prim_lod_frac,  "0.0",
    };

    static const char *aA[] = {
        "c.a",                      t1a,        t2a,            "p.a/*PRIM*/", 
        "gl_Color.a/*SHADE*/",      "e.a",      "1.0",          "0.0",
    };

    const static char * bRGB[] =
    { "c/*PREV*/", "f", "b", "fog/*FOG*/" };
    const static char * bA[2][4] =
    { {"c.a/*PREVA*/", "fog.a/*FOGA*/", "gl_Color.a/*SHADEA*/", "0.0/*ZERO*/"},
    {_1ma/*"(1.0-c.a/ *PREVA)"*/, "0.0/*f.a*//*FRAGA*/", "1.0", "0.0"}}; // need clamping on 1-alpha ?


    rdpState_t & state = chunk.rdpState;
    static rglCombiner_t * c;
    uint32_t cycle = RDP_GETOM_CYCLE_TYPE(state.otherModes);
    int i; //, fmt, size;
    char * p;
    const char * alphaTest;
    const char * alphaTest2;
    const char * write;
    static char src[4*4096];

    float env[4];
    env[0] = RDP_GETC32_R(state.envColor)/255.0f;
    env[1] = RDP_GETC32_G(state.envColor)/255.0f;
    env[2] = RDP_GETC32_B(state.envColor)/255.0f;
    env[3] = RDP_GETC32_A(state.envColor)/255.0f;
    glTexEnvfv(GL_TEXTURE_ENV, GL_TEXTURE_ENV_COLOR, env);

    env[0] = RDP_GETC32_R(state.blendColor)/255.0f;
    env[1] = RDP_GETC32_G(state.blendColor)/255.0f;
    env[2] = RDP_GETC32_B(state.blendColor)/255.0f;
    env[3] = RDP_GETC32_A(state.blendColor)/255.0f;
    glLightfv(GL_LIGHT0, GL_AMBIENT, env);

    env[0] = RDP_GETC32_R(state.fogColor)/255.0f;
    env[1] = RDP_GETC32_G(state.fogColor)/255.0f;
    env[2] = RDP_GETC32_B(state.fogColor)/255.0f;
    env[3] = RDP_GETC32_A(state.fogColor)/255.0f;
    glLightfv(GL_LIGHT0, GL_DIFFUSE, env);

    glActiveTextureARB(GL_TEXTURE1_ARB);
    env[0] = state.k5/255.0f;
    glTexEnvfv(GL_TEXTURE_ENV, GL_TEXTURE_ENV_COLOR, env);
    if (cycle == RDP_CYCLE_TYPE_FILL) {
        if (0/*fb_size == 3*/) { // FIXME
            env[0] = RDP_GETC32_R(state.fillColor)/255.0f;
            env[1] = RDP_GETC32_G(state.fillColor)/255.0f;
            env[2] = RDP_GETC32_B(state.fillColor)/255.0f;
            env[3] = RDP_GETC32_A(state.fillColor)/255.0f;
        } else {
            env[0] = RDP_GETC16_R(state.fillColor)/31.0f;
            env[1] = RDP_GETC16_G(state.fillColor)/31.0f;
            env[2] = RDP_GETC16_B(state.fillColor)/31.0f;
            env[3] = RDP_GETC16_A(state.fillColor);
        }
    } else {
        env[0] = RDP_GETC32_R(state.primColor)/255.0f;
        env[1] = RDP_GETC32_G(state.primColor)/255.0f;
        env[2] = RDP_GETC32_B(state.primColor)/255.0f;
        env[3] = RDP_GETC32_A(state.primColor)/255.0f;
    }
    glLightfv(GL_LIGHT0, GL_SPECULAR, env);
    glActiveTextureARB(GL_TEXTURE0_ARB);
    rglAssert(glGetError() == GL_NO_ERROR);

    //   if (c && rglNbCombiners &&
    //       RDP_GETOM_CYCLE_TYPE(c->otherModes) == cycle &&
    //       (RDP_GETOM_CYCLE_TYPE(c->otherModes) >= 2 ||
    //        (!memcmp(&c->combineModes, &state.combineModes, sizeof(rdpCombineModes_t)) &&
    //         !memcmp(&c->otherModes, &state.otherModes, sizeof(rdpOtherModes_t))))) {
    //     return;
    //   }

    for (i=0; i<rglNbCombiners; i++) {
        c = rglCombiners + i;
        if (c->format == format &&
            RDP_GETOM_CYCLE_TYPE(c->otherModes) == cycle &&
            (RDP_GETOM_CYCLE_TYPE(c->otherModes) >= 2 ||
            (!memcmp(&c->combineModes, &state.combineModes, sizeof(rdpCombineModes_t))
            && !memcmp(&c->otherModes, &state.otherModes, sizeof(rdpOtherModes_t))
            ))) {
#ifdef RDP_DEBUG
                chunk.shader = c->shader;
#endif
                rglUseShader(c->shader);
                goto ok;
        }
    }

    if (rglNbCombiners == RGL_MAX_COMBINERS)
        rglClearCombiners();

    c = rglCombiners + rglNbCombiners++;
    c->otherModes = state.otherModes;
    c->combineModes = state.combineModes;
    c->format = format;
#ifndef RGL_EXACT_BLEND
    c->srcBlend = GL_ONE;
    c->dstBlend = GL_ZERO;
#endif

    switch (format & RGL_COMB_FMT) {
case RGL_COMB_FMT_RGBA:
    write = "gl_FragColor = c;";
    break;
case RGL_COMB_FMT_I:
    write = "gl_FragColor = vec4(c[0]);";
    break;
case RGL_COMB_FMT_DEPTH:
    write = "gl_FragDepth = c[0];";
    break;
    }

    if (cycle == RDP_CYCLE_TYPE_FILL) {
        sprintf(
            src, 
            "void main()                       \n"
            "{                                 \n"
            //"  c = gl_TextureEnvColor[1];\n"
            "  vec4 c = gl_LightSource[0].specular;\n"
            "  %s\n"
            "}                                 \n",
            write);
        c->shader = rglCreateShader(
            "void main()                                                    \n"
            "{                                                              \n"
            "  gl_Position = ftransform();                                  \n"
            "  gl_FrontColor = gl_Color;                                    \n"
            "  gl_BackColor = gl_Color;                                     \n"
            "  gl_TexCoord[0] = gl_MultiTexCoord0;                          \n"
            "}                                                              \n"
            ,
            src
            );
#ifdef RDP_DEBUG
        chunk.shader = c->shader;
#endif
        rglUseShader(c->shader);
        goto ok;
    }

    alphaTest = "";
    alphaTest2 = "";

    if (//cycle < 2 && // CHECK THIS
        RDP_GETOM_CVG_TIMES_ALPHA(chunk.rdpState.otherModes)
        //&& rglT1Usage(chunk.rdpState)
        ) {
            if (RDP_GETOM_ALPHA_CVG_SELECT(chunk.rdpState.otherModes))
                alphaTest = "if (c.a < 0.5) discard; \n";
            else
                alphaTest = "if (t1.a < 0.5) discard; \n";
            alphaTest2 = "if (c.a < 0.5) discard; \n";
    }
    else if (RDP_GETOM_ALPHA_COMPARE_EN(chunk.rdpState.otherModes) &&
        !RDP_GETOM_ALPHA_CVG_SELECT(chunk.rdpState.otherModes)) {
            if (RDP_GETC32_A(chunk.rdpState.blendColor) > 0) {
                alphaTest = "if (c.a < b.a) discard; \n";
                alphaTest2 =
                    "  vec4 b = gl_LightSource[0].ambient;  \n"
                    "  if (c.a < b.a) discard; \n";
                //alphaTest2 = "if (c.a < 0.5) discard; \n";
            } else {
                alphaTest = "if (c.a == 0.0) discard; \n";
                alphaTest2 = "if (c.a == 0.0) discard; \n";
            }
    }

    if (cycle == RDP_CYCLE_TYPE_COPY) {
        sprintf(
            src, 
            "uniform sampler2D texture0;       \n"
            "                                  \n"
            "void main()                       \n"
            "{                                 \n"
            "  vec4 c = texture2D(texture0, vec2(gl_TexCoord[0])); \n"
            "  %s"
            "  %s\n"
            "}                                 \n",
            alphaTest2,
            write
            );
        c->shader = rglCreateShader(
            "void main()                                                    \n"
            "{                                                              \n"
            "  gl_Position = ftransform();                                  \n"
            "  gl_FrontColor = gl_Color;                                    \n"
            "  gl_BackColor = gl_Color;                                    \n"
            "  gl_TexCoord[0] = gl_MultiTexCoord0;                          \n"
            "}                                                              \n"
            ,
            src
            );
#ifdef RDP_DEBUG
        chunk.shader = c->shader;
#endif
        rglUseShader(c->shader);
        goto ok;
    }


    p = src;
    p +=
        sprintf(p,
        "uniform sampler2D texture0;       \n"
        "uniform sampler2D texture2;       \n"
#ifdef RGL_EXACT_BLEND
        "uniform sampler2D texture1;       \n"
#endif
        "                                  \n"
        "void main()                       \n"
        "{                                 \n"
        "vec4  c = vec4(0,0,0,0);\n"
        "vec4  e = gl_TextureEnvColor[0];\n"
        "float k5 = gl_TextureEnvColor[1][0];\n"
        "vec4  p = gl_LightSource[0].specular;\n"
#ifdef RGL_EXACT_BLEND
        "vec4  f = texture2D(texture1, vec2(gl_FragCoord.x/(2048.0*gl_TexCoord[1].x), gl_FragCoord.y/(2048.0*gl_TexCoord[1].y))); \n"
#endif
        "vec4  fog = gl_LightSource[0].diffuse;    \n"
        "vec4  b = gl_LightSource[0].ambient;  \n");

    switch (format & RGL_COMB_IN0) {
case 0:
    p +=
        sprintf(p,
        "vec4 t1 = texture2D(texture0, vec2(gl_TexCoord[0]));\n");
    break;
case RGL_COMB_IN0_DEPTH:
    p +=
        sprintf(p,
        "vec4 t1 = vec4(texture2D(texture0, vec2(gl_TexCoord[0]))[0]);\n");
    break;
    }
    switch (format & RGL_COMB_IN1) {
case 0:
    p +=
        sprintf(p,
        "vec4 t2 = texture2D(texture2, vec2(gl_TexCoord[2]));\n");
    break;
case RGL_COMB_IN1_DEPTH:
    p +=
        sprintf(p,
        "vec4 t2 = vec4(texture2D(texture2, vec2(gl_TexCoord[2]))[0]);\n");
    break;
    }

    const char * comb, * comb2;
    comb2 = 0;
    //   switch (RDP_GETOM_CVG_DEST(state.otherModes))
    //   {
    //     case 3:
    //       comb = "c = clamp(vec4((vec3(%s) - vec3(%s)) * vec3(%s) + vec3(%s), (%s - %s) * %s + %s), 0.0, 1.0);\n";
    //       break;
    //     case 2:
    //       comb = "c = vec4((vec3(%s) - vec3(%s)) * vec3(%s) + vec3(%s), (%s - %s) * %s + %s);\n";
    //       //comb = "c = vec4((vec3(%s) - vec3(%s)) * vec3(%s) + vec3(%s), t1.a*((%s - %s) * %s + %s));\n";
    //       break;
    //     case 0:
    //       //comb2 = "c = vec4((vec3(%s) - vec3(%s)) * vec3(%s) + vec3(%s), t1.a);\n";
    //     case 1:
    //       comb = "c = vec4((vec3(%s) - vec3(%s)) * vec3(%s) + vec3(%s), (%s - %s) * %s + %s);\n";
    //       break;
    //   }
    comb = "c = clamp(vec4((vec3(%s) - vec3(%s)) * vec3(%s) + vec3(%s), (%s - %s) * %s + %s), 0.0, 1.0);\n";
    strcpy(prim_lod_frac, "0.5/*PRIM_LOD_FRAC*/");
    strcpy(t1, "t1");
    strcpy(t1a, "t1.a");
    if (format & RGL_COMB_TILE7) {
        strcpy(t2, "t1");
        strcpy(t2a, "t1.a");
    } else {
        strcpy(t2, "t2");
        strcpy(t2a, "t2.a");
    }
    p +=
        sprintf(p,
        comb
        ,
        saRGB[RDP_GETCM_SUB_A_RGB0(state.combineModes)],
        saRGB[RDP_GETCM_SUB_B_RGB0(state.combineModes)],
        mRGB[RDP_GETCM_MUL_RGB0(state.combineModes)],
        aRGB[RDP_GETCM_ADD_RGB0(state.combineModes)],
        saA[RDP_GETCM_SUB_A_A0(state.combineModes)],
        sbA[RDP_GETCM_SUB_B_A0(state.combineModes)],
        mA[RDP_GETCM_MUL_A0(state.combineModes)],
        aA[RDP_GETCM_ADD_A0(state.combineModes)]
    );

    if (cycle == RDP_CYCLE_TYPE_2) {
        if (!(format & RGL_COMB_TILE7)) {
            strcpy(t1, "t2");
            strcpy(t1a, "t2.a");
            strcpy(t2, "t1");
            strcpy(t2a, "t1.a");
        }
        //strcpy(prim_lod_frac, "0.0/*PRIM_LOD_FRAC*/");
        //     if (!RDP_GETOM_ALPHA_CVG_SELECT(chunk.rdpState.otherModes))
        //       p +=
        //         sprintf(p, "  c.a = t1.a; \n");

        p +=
            sprintf(p,
            comb2? comb2 : comb
            ,
            saRGB[RDP_GETCM_SUB_A_RGB1(state.combineModes)],
            saRGB[RDP_GETCM_SUB_B_RGB1(state.combineModes)],
            mRGB[RDP_GETCM_MUL_RGB1(state.combineModes)],
            aRGB[RDP_GETCM_ADD_RGB1(state.combineModes)],
            saA[RDP_GETCM_SUB_A_A1(state.combineModes)],
            sbA[RDP_GETCM_SUB_B_A1(state.combineModes)],
            mA[RDP_GETCM_MUL_A1(state.combineModes)],
            aA[RDP_GETCM_ADD_A1(state.combineModes)]
        );
    }

    //   if (!RDP_GETOM_CVG_TIMES_ALPHA(state.otherModes))
    //     p += sprintf(p, "c.a = t1.a; \n");

    p += sprintf(p, "%s", alphaTest);


    const char * blender;
    blender = "c = vec4(float(%s)*vec3(%s) + float(%s)*vec3(%s), 1.0); \n";
#ifdef RGL_EXACT_BLEND
    const char * noblender = "c.a = 1.0;\n";
#endif

    int m1b, m1a, m2b, m2a;

    //LOG("New combiner / blender :\n%s", rglCombiner2String(state));

    if (cycle == RDP_CYCLE_TYPE_2) {
        if (RDP_GETOM_FORCE_BLEND(state.otherModes)) {
#ifndef RGL_EXACT_BLEND
            if (RDP_GETOM_BLEND_M1A_0(state.otherModes) != 1 &&
                RDP_GETOM_BLEND_M2A_0(state.otherModes) != 1) {
#endif
                    sprintf(_1ma, "(1.0 - %s)", bA[0][RDP_GETOM_BLEND_M1B_0(state.otherModes)]);
                    p +=
                        sprintf(
                        p,
                        "c = vec4(float(%s)*vec3(%s) + float(%s)*vec3(%s), c.a); \n"
                        ,bA[0][RDP_GETOM_BLEND_M1B_0(state.otherModes)],
                        bRGB[RDP_GETOM_BLEND_M1A_0(state.otherModes)],
                        bA[1][RDP_GETOM_BLEND_M2B_0(state.otherModes)],
                        bRGB[RDP_GETOM_BLEND_M2A_0(state.otherModes)]
                    );
#ifndef RGL_EXACT_BLEND
            } else {
                LOG("Blender error : fragment in cycle 1\n%s", rglCombiner2String(state));
            }
#endif

            m1b = RDP_GETOM_BLEND_M1B_1(state.otherModes);
            m1a = RDP_GETOM_BLEND_M1A_1(state.otherModes);
            m2b = RDP_GETOM_BLEND_M2B_1(state.otherModes);
            m2a = RDP_GETOM_BLEND_M2A_1(state.otherModes);
        } else {
            m1b = RDP_GETOM_BLEND_M1B_0(state.otherModes);
            m1a = RDP_GETOM_BLEND_M1A_0(state.otherModes);
            m2b = RDP_GETOM_BLEND_M2B_0(state.otherModes);
            m2a = RDP_GETOM_BLEND_M2A_0(state.otherModes);
        }
    } else {
        m1b = RDP_GETOM_BLEND_M1B_0(state.otherModes);
        m1a = RDP_GETOM_BLEND_M1A_0(state.otherModes);
        m2b = RDP_GETOM_BLEND_M2B_0(state.otherModes);
        m2a = RDP_GETOM_BLEND_M2A_0(state.otherModes);
    }

    if (RDP_GETOM_FORCE_BLEND(state.otherModes) || cycle == RDP_CYCLE_TYPE_2) {
#ifndef RGL_EXACT_BLEND
        if (m1a == 1 || m2a == 1) {
            if (/*(m1a != 1 || m1b == 3) &&*/ (m2a == 1 || m2b == 3)) {
                int src = GL_ZERO, dst = GL_ONE;
                const char * alpha = "c.a";
                switch (m1b) {
case 0: // c.a
    src = GL_SRC_ALPHA;
    break;
case 1: // fog.a
    src = GL_SRC_ALPHA;
    alpha = "fog.a";
    //             LOGERROR("Unsupported src alpha : FOG\n");
    //             LOGERROR(rglCombiner2String(state));
    break;
case 2: // shade.a
    src = GL_SRC_ALPHA;
    alpha = "gl_Color.a";
    //             LOGERROR("Unsupported src alpha : SHADE\n");
    //             LOGERROR(rglCombiner2String(state));
    break;
case 3: // 0
    src = GL_ZERO;
    break;
                }
                switch (m1a) {
case 0: // c
    if (m1b != 0 /* c.a */)
        p += sprintf(
        p, "c.a = %s; \n", alpha);
    break;
case 1: // f
    LOGERROR("Unsupported src color : FRAG\n");
    LOGERROR("%s", rglCombiner2String(state));
    break;
case 2: // b
    p += sprintf(
        p, "c = vec4(vec3(b), %s); \n", alpha);
    break;
case 3: // fog
    p += sprintf(
        p, "c = vec4(vec3(fog), %s); \n", alpha);
    break;
                }
                switch (m2b) {
case 0:
    switch (m1b) {
case 3:
    dst = GL_ONE;
    break;
default:
    dst = GL_ONE_MINUS_SRC_ALPHA;
    break;
    }
    break;
case 1:
    dst = GL_DST_ALPHA;
    break;
case 2:
    dst = GL_ONE;
    break;
case 3:
    dst = GL_ZERO;
    break;
                }

                c->srcBlend = src;
                c->dstBlend = dst;
            } else {
                LOGERROR("Unsuported blender :\n");
                LOGERROR("%s", rglCombiner2String(state));
            }
        }
        else
#endif
        {
            sprintf(_1ma, "(1.0 - %s)", bA[0][m1b]);
            p +=
                sprintf(p, blender, bA[0][m1b], bRGB[m1a], bA[1][m2b], bRGB[m2a]);
        }
    } else {
#ifdef RGL_EXACT_BLEND
        p +=
            sprintf(p,
            noblender
            );
#endif
    }

    p +=
        sprintf(
        p,
        "%s \n"
        "}                                 \n"
        ,write
        );

    rglAssert(p < src+sizeof(src));

#ifdef RGL_EXACT_BLEND
    //printf("Creating combiner : \n%s", src);
#endif

    c->shader = rglCreateShader(
        "void main()                                                    \n"
        "{                                                              \n"
        "  gl_Position = ftransform();                                  \n"
        "  gl_FrontColor = gl_Color;                                    \n"
        "  gl_BackColor = gl_FrontColor;                                \n"
        "  gl_TexCoord[0] = gl_MultiTexCoord0;                          \n"
#ifdef RGL_EXACT_BLEND
        "  gl_TexCoord[1] = gl_MultiTexCoord1;                          \n"
#endif
        "  gl_TexCoord[2] = gl_MultiTexCoord2;                          \n"
        "}                                                              \n"
        ,
        src);

#ifdef RDP_DEBUG
    chunk.shader = c->shader;
#endif
    rglUseShader(c->shader);
    rglAssert(glGetError() == GL_NO_ERROR);

    int location;
    location = glGetUniformLocationARB(c->shader->prog, "texture0");
    glUniform1iARB(location, 0);
#ifdef RGL_EXACT_BLEND
    location = glGetUniformLocationARB(c->shader->prog, "texture1");
    glUniform1iARB(location, 1);
#endif
    location = glGetUniformLocationARB(c->shader->prog, "texture2");
    glUniform1iARB(location, 2);
    rglAssert(glGetError() == GL_NO_ERROR);

ok:;
#ifndef RGL_EXACT_BLEND
    if ((format & RGL_COMB_FMT) == RGL_COMB_FMT_DEPTH ||
        (c->srcBlend == GL_ONE && c->dstBlend == GL_ZERO))
        glDisable(GL_BLEND);
    else {
        glEnable(GL_BLEND);
        if ((format & RGL_COMB_FMT) == RGL_COMB_FMT_RGBA)
            glBlendFuncSeparate(c->srcBlend, c->dstBlend, GL_ZERO, GL_ONE);
        else
            glBlendFunc(c->srcBlend, c->dstBlend);
    }
#endif
}
