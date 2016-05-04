/* Common definitions <stddef.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_STDDEF_H
#define _PDCLIB_STDDEF_H _PDCLIB_STDDEF_H
#include "_PDCLIB_config.h"
#include "_PDCLIB_int.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef _PDCLIB_ptrdiff_t ptrdiff_t;

#ifndef _PDCLIB_SIZE_T_DEFINED
#define _PDCLIB_SIZE_T_DEFINED _PDCLIB_SIZE_T_DEFINED
typedef _PDCLIB_size_t size_t;
#endif

#ifndef __cplusplus
#ifndef _PDCLIB_WCHAR_T_DEFINED
#define _PDCLIB_WCHAR_T_DEFINED _PDCLIB_WCHAR_T_DEFINED
typedef _PDCLIB_wchar_t   wchar_t;
#endif
#endif

#ifndef _PDCLIB_NULL_DEFINED
#define _PDCLIB_NULL_DEFINED _PDCLIB_NULL_DEFINED
#define NULL _PDCLIB_NULL
#endif

#define offsetof( type, member ) _PDCLIB_offsetof( type, member )

#ifdef __cplusplus
}
#endif

#endif
