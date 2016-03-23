/* Unicode utilities <uchar.h>

 This file is part of the Public Domain C Library (PDCLib).
 Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_UCHAR_H
#define _PDCLIB_UCHAR_H _PDCLIB_UCHAR_H
#include "_PDCLIB_int.h"

#ifdef __cplusplus
extern "C" {
#endif

#ifndef _PDCLIB_SIZE_T_DEFINED
#define _PDCLIB_SIZE_T_DEFINED _PDCLIB_SIZE_T_DEFINED
typedef _PDCLIB_size_t size_t;
#endif

#ifndef _PDCLIB_MBSTATE_T_DEFINED
#define _PDCLIB_MBSTATE_T_DEFINED _PDCLIB_MBSTATE_T_DEFINED
typedef _PDCLIB_mbstate_t mbstate_t;
#endif

#ifndef __cplusplus

/* These are built-in types in C++ */

#ifndef _PDCLIB_CHAR16_T_DEFINED
#define _PDCLIB_CHAR16_T_DEFINED _PDCLIB_CHAR16_T_DEFINED
typedef _PDCLIB_uint_least16_t char16_t;
#endif

#ifndef _PDCLIB_CHAR32_T_DEFINED
#define _PDCLIB_CHAR32_T_DEFINED _PDCLIB_CHAR32_T_DEFINED
typedef _PDCLIB_uint_least32_t char32_t;
#endif

#endif

size_t mbrtoc16( char16_t * _PDCLIB_restrict pc16, const char * _PDCLIB_restrict s, size_t n, mbstate_t * _PDCLIB_restrict ps );

size_t c16rtomb( char * _PDCLIB_restrict s, char16_t c16, mbstate_t * _PDCLIB_restrict ps );

size_t mbrtoc32( char32_t * _PDCLIB_restrict pc32, const char * _PDCLIB_restrict s, size_t n, mbstate_t * _PDCLIB_restrict ps);

size_t c32rtomb( char * _PDCLIB_restrict s, char32_t c32, mbstate_t * _PDCLIB_restrict ps);

#if defined(_PDCLIB_EXTENSIONS)

/* Analogous to strlen() / wcslen() */

size_t _PDCLIB_c16slen( const char16_t * str );

size_t _PDCLIB_c32slen( const char32_t * str );

/* String generalizations of the above functions */

size_t _PDCLIB_mbsrtoc16s( char16_t * _PDCLIB_restrict dst, const char * * _PDCLIB_restrict src, size_t len, mbstate_t * _PDCLIB_restrict ps );

size_t _PDCLIB_mbsrtoc32s( char32_t * _PDCLIB_restrict dst, const char * * _PDCLIB_restrict src, size_t len, mbstate_t * _PDCLIB_restrict ps );

size_t _PDCLIB_c16srtombs( char * _PDCLIB_restrict dst, const char16_t * * _PDCLIB_restrict src, size_t len, mbstate_t * _PDCLIB_restrict ps );

size_t _PDCLIB_c32srtombs( char * _PDCLIB_restrict dst, const char32_t * * _PDCLIB_restrict src, size_t len, mbstate_t * _PDCLIB_restrict ps );
#endif

#endif
