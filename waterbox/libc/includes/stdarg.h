/* Variable arguments <stdarg.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_STDARG_H
#define _PDCLIB_STDARG_H _PDCLIB_STDARG_H
#include "_PDCLIB_aux.h"
#include "_PDCLIB_config.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef _PDCLIB_va_list va_list;

#define va_arg( ap, type )    _PDCLIB_va_arg( ap, type )
#define va_copy( dest, src )  _PDCLIB_va_copy( dest, src )
#define va_end( ap )          _PDCLIB_va_end( ap )
#define va_start( ap, parmN ) _PDCLIB_va_start( ap, parmN )

#ifdef __cplusplus
}
#endif

#endif
