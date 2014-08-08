#ifndef PORT_H
#define PORT_H

#include "types.h"

/* if a >= 0 return x else y*/
#define isel(a, x, y) ((x & (~(a >> 31))) + (y & (a >> 31)))

#ifdef FRONTEND_SUPPORTS_RGB565
/* 16bit color - RGB565 */
#define RED_MASK  0xf800
#define GREEN_MASK 0x7e0
#define BLUE_MASK 0x1f
#define RED_EXPAND 3
#define GREEN_EXPAND 2
#define BLUE_EXPAND 3
#define RED_SHIFT 11
#define GREEN_SHIFT 5
#define BLUE_SHIFT 0
#define CONVERT_COLOR(color) (((color & 0x001f) << 11) | ((color & 0x03e0) << 1) | ((color & 0x0200) >> 4) | ((color & 0x7c00) >> 10))
#else
/* 16bit color - RGB555 */
#define RED_MASK  0x7c00
#define GREEN_MASK 0x3e0
#define BLUE_MASK 0x1f
#define RED_EXPAND 3
#define GREEN_EXPAND 3
#define BLUE_EXPAND 3
#define RED_SHIFT 10
#define GREEN_SHIFT 5
#define BLUE_SHIFT 0
#define CONVERT_COLOR(color) ((((color & 0x1f) << 10) | (((color & 0x3e0) >> 5) << 5) | (((color & 0x7c00) >> 10))) & 0x7fff)
#endif

#ifdef _MSC_VER
#include <stdlib.h>
#define strcasecmp _stricmp
#endif

#ifdef USE_CACHE_PREFETCH
#if defined(__ANDROID__)
#define CACHE_PREFETCH(prefetch) prefetch(&prefetch);
#elif defined(_XBOX)
#define CACHE_PREFETCH(prefetch) __dcbt(0, &prefetch);
#else
#define CACHE_PREFETCH(prefetch) __dcbt(&prefetch);
#endif
#else
#define CACHE_PREFETCH(prefetch)
#endif

#define READ16LE(x) *((u16 *)x)
#define READ32LE(x) *((u32 *)x)
#define WRITE16LE(x,v) *((u16 *)x) = (v)
#define WRITE32LE(x,v) *((u32 *)x) = (v)

#endif
