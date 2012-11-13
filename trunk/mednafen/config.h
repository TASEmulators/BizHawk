//win32 msvc config
#pragma once
#define HAVE__MKDIR 1
#define LSB_FIRST
#define SIZEOF_DOUBLE 8
#define WANT_DEBUGGER 1
#define snprintf _snprintf
#define strcasecmp(x,y) _stricmp(x,y)
#define strncasecmp(x, y, l) strnicmp(x, y, l)
#define _(x) (x)
#define PSS "/"
#define round(x) (floorf((x) + 0.5f))
#define strdup _strdup
#define strtoull _strtoui64
#define strtoll _strtoi64
#define _USE_MATH_DEFINES
#define world_strtod strtod
#define WANT_PSX_EMU


#define MEDNAFEN_VERSION "0.999.999-WIP"
#define MEDNAFEN_VERSION_NUMERIC 0x999999
