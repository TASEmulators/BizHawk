//win32 msvc config
#pragma once
#ifndef __LIBRETRO_CONFIG_H
#define __LIBRETRO_CONFIG_H

#define HAVE__MKDIR 1

#if defined(_MSC_VER) && !defined(XBOX360)
#define LSB_FIRST
#endif

#define SIZEOF_DOUBLE 8
#define WANT_DEBUGGER 0

#define _(x) (x)
#define PSS "/"
#define round(x) (floorf((x) + 0.5f))

#ifdef _MSC_VER
#define strdup _strdup
#define strtoull _strtoui64
#define strtoll _strtoi64
#define snprintf _snprintf
#define strcasecmp(x,y) _stricmp(x,y)
#define strncasecmp(x, y, l) strnicmp(x, y, l)
#endif

#define _USE_MATH_DEFINES
#define world_strtod strtod

#define MEDNAFEN_VERSION "0.999.999-WIP"
#define MEDNAFEN_VERSION_NUMERIC 0x999999

#endif /* __LIBRETRO_CONFIG_H
