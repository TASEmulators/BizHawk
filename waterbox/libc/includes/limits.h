/* Sizes of integer types <limits.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_LIMITS_H
#define _PDCLIB_LIMITS_H _PDCLIB_LIMITS_H
#include "_PDCLIB_int.h"

/* MSVC 2010 defines this to 5, which is enough for UTF-8 but might rule out
   stateful encodings (like ISO/IEC 2022). GCC 5.3 defines this to 16, which
   is meant to ensure future compatibility. For the same reason, we go along
   with GCC's definition.
   http://lists.gnu.org/archive/html/bug-gnulib/2015-05/msg00001.html
*/
#define MB_LEN_MAX 16

#define LLONG_MIN  _PDCLIB_LLONG_MIN
#define LLONG_MAX  _PDCLIB_LLONG_MAX
#define ULLONG_MAX _PDCLIB_ULLONG_MAX

#define CHAR_BIT   _PDCLIB_CHAR_BIT
#define CHAR_MAX   _PDCLIB_CHAR_MAX
#define CHAR_MIN   _PDCLIB_CHAR_MIN
#define SCHAR_MAX  _PDCLIB_SCHAR_MAX
#define SCHAR_MIN  _PDCLIB_SCHAR_MIN
#define UCHAR_MAX  _PDCLIB_UCHAR_MAX
#define SHRT_MAX   _PDCLIB_SHRT_MAX
#define SHRT_MIN   _PDCLIB_SHRT_MIN
#define INT_MAX    _PDCLIB_INT_MAX
#define INT_MIN    _PDCLIB_INT_MIN
#define LONG_MAX   _PDCLIB_LONG_MAX
#define LONG_MIN   _PDCLIB_LONG_MIN
#define USHRT_MAX  _PDCLIB_USHRT_MAX
#define UINT_MAX   _PDCLIB_UINT_MAX
#define ULONG_MAX  _PDCLIB_ULONG_MAX

#endif
