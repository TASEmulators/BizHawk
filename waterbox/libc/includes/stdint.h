/* Integer types <stdint.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_STDINT_H
#define _PDCLIB_STDINT_H _PDCLIB_STDINT_H
#include "_PDCLIB_int.h"

/* Exact-width integer types. */

typedef _PDCLIB_int8_t  int8_t;
typedef _PDCLIB_int16_t int16_t;
typedef _PDCLIB_int32_t int32_t;
typedef _PDCLIB_int64_t int64_t;

typedef _PDCLIB_uint8_t  uint8_t;
typedef _PDCLIB_uint16_t uint16_t;
typedef _PDCLIB_uint32_t uint32_t;
typedef _PDCLIB_uint64_t uint64_t;

/* Minimum-width integer types */

/* You are allowed to add more types here, e.g. int_least24_t. For the standard
   types, int_leastN_t is equivalent to the corresponding exact type intN_t by
   definition.
*/

typedef _PDCLIB_int8_t  int_least8_t;
typedef _PDCLIB_int16_t int_least16_t;
typedef _PDCLIB_int32_t int_least32_t;
typedef _PDCLIB_int64_t int_least64_t;

typedef _PDCLIB_uint8_t  uint_least8_t;
typedef _PDCLIB_uint16_t uint_least16_t;
typedef _PDCLIB_uint32_t uint_least32_t;
typedef _PDCLIB_uint64_t uint_least64_t;

/* Fastest minimum-width integer types */

/* You are allowed to add more types here, e.g. int_fast24_t. */

typedef _PDCLIB_int_fast8_t  int_fast8_t;
typedef _PDCLIB_int_fast16_t int_fast16_t;
typedef _PDCLIB_int_fast32_t int_fast32_t;
typedef _PDCLIB_int_fast64_t int_fast64_t;

typedef _PDCLIB_uint_fast8_t  uint_fast8_t;
typedef _PDCLIB_uint_fast16_t uint_fast16_t;
typedef _PDCLIB_uint_fast32_t uint_fast32_t;
typedef _PDCLIB_uint_fast64_t uint_fast64_t;

/* Integer types capable of holding object pointers */

typedef _PDCLIB_intptr_t  intptr_t;
typedef _PDCLIB_uintptr_t uintptr_t;

/* Greatest-width integer types */

typedef _PDCLIB_intmax_t  intmax_t;
typedef _PDCLIB_uintmax_t uintmax_t;

/* Limits of specified-width integer types */

#ifdef __cplusplus
#ifndef __STDC_LIMIT_MACROS
#define _PDCLIB_NO_LIMIT_MACROS
#endif
#endif

#ifndef _PDCLIB_NO_LIMIT_MACROS

/* Limits of exact-width integer types */

#define INT8_MIN  _PDCLIB_INT8_MIN
#define INT8_MAX  _PDCLIB_INT8_MAX
#define UINT8_MAX _PDCLIB_UINT8_MAX

#define INT16_MIN  _PDCLIB_INT16_MIN
#define INT16_MAX  _PDCLIB_INT16_MAX
#define UINT16_MAX _PDCLIB_UINT16_MAX

#define INT32_MIN  _PDCLIB_INT32_MIN
#define INT32_MAX  _PDCLIB_INT32_MAX
#define UINT32_MAX _PDCLIB_UINT32_MAX

#define INT64_MIN  _PDCLIB_INT64_MIN
#define INT64_MAX  _PDCLIB_INT64_MAX
#define UINT64_MAX _PDCLIB_UINT64_MAX

/* Limits of minimum-width integer types */

/* For the standard widths, least and exact types are equivalent.
   You are allowed to add more types here, e.g. int_least24_t.
*/

#define INT_LEAST8_MIN  INT8_MIN
#define INT_LEAST8_MAX  INT8_MAX
#define UINT_LEAST8_MAX UINT8_MAX

#define INT_LEAST16_MIN  INT16_MIN
#define INT_LEAST16_MAX  INT16_MAX
#define UINT_LEAST16_MAX UINT16_MAX

#define INT_LEAST32_MIN  INT32_MIN
#define INT_LEAST32_MAX  INT32_MAX
#define UINT_LEAST32_MAX UINT32_MAX

#define INT_LEAST64_MIN  INT64_MIN
#define INT_LEAST64_MAX  INT64_MAX
#define UINT_LEAST64_MAX UINT64_MAX

/* Limits of fastest minimum-width integer types */

#define INT_FAST8_MIN  _PDCLIB_INT_FAST8_MIN
#define INT_FAST8_MAX  _PDCLIB_INT_FAST8_MAX
#define UINT_FAST8_MAX _PDCLIB_UINT_FAST8_MAX

#define INT_FAST16_MIN  _PDCLIB_INT_FAST16_MIN
#define INT_FAST16_MAX  _PDCLIB_INT_FAST16_MAX
#define UINT_FAST16_MAX _PDCLIB_UINT_FAST16_MAX

#define INT_FAST32_MIN  _PDCLIB_INT_FAST32_MIN
#define INT_FAST32_MAX  _PDCLIB_INT_FAST32_MAX
#define UINT_FAST32_MAX _PDCLIB_UINT_FAST32_MAX

#define INT_FAST64_MIN  _PDCLIB_INT_FAST64_MIN
#define INT_FAST64_MAX  _PDCLIB_INT_FAST64_MAX
#define UINT_FAST64_MAX _PDCLIB_UINT_FAST64_MAX

/* Limits of integer types capable of holding object pointers */

#define INTPTR_MIN  _PDCLIB_INTPTR_MIN
#define INTPTR_MAX  _PDCLIB_INTPTR_MAX
#define UINTPTR_MAX _PDCLIB_UINTPTR_MAX

/* Limits of greatest-width integer types */

#define INTMAX_MIN  _PDCLIB_INTMAX_MIN
#define INTMAX_MAX  _PDCLIB_INTMAX_MAX
#define UINTMAX_MAX _PDCLIB_UINTMAX_MAX

/* Limits of other integer types */

#define PTRDIFF_MIN _PDCLIB_PTRDIFF_MIN
#define PTRDIFF_MAX _PDCLIB_PTRDIFF_MAX

#define SIG_ATOMIC_MIN _PDCLIB_SIG_ATOMIC_MIN
#define SIG_ATOMIC_MAX _PDCLIB_SIG_ATOMIC_MAX

#define SIZE_MAX _PDCLIB_SIZE_MAX

#ifndef _PDCLIB_WCHAR_MIN_MAX_DEFINED
#define _PDCLIB_WCHAR_MIN_MAX_DEFINED
#define WCHAR_MIN _PDCLIB_WCHAR_MIN
#define WCHAR_MAX _PDCLIB_WCHAR_MAX
#endif

#define WINT_MIN _PDCLIB_WINT_MIN
#define WINT_MAX _PDCLIB_WINT_MAX

#endif

/* Macros for integer constants */

#ifdef __cplusplus
#ifndef __STDC_CONSTANT_MACROS
#define _PDCLIB_NO_CONSTANT_MACROS
#endif
#endif

#ifndef _PDCLIB_NO_CONSTANT_MACROS

/* Macros for minimum-width integer constants */

/* As the minimum-width types - for the required widths of 8, 16, 32, and 64
   bits - are expressed in terms of the exact-width types, the mechanism for
   these macros is to append the literal of that exact-width type to the macro
   parameter.
   This is considered a hack, as the author is not sure his understanding of
   the requirements of this macro is correct. Any input appreciated.
*/

/* Expand to an integer constant of specified value and type int_leastN_t */

#define INT8_C( value )  value
#define INT16_C( value ) value
#define INT32_C( value ) _PDCLIB_concat( value, _PDCLIB_INT32_LITERAL )
#define INT64_C( value ) _PDCLIB_concat( value, _PDCLIB_INT64_LITERAL )

/* Expand to an integer constant of specified value and type uint_leastN_t */

#define UINT8_C( value )  value
#define UINT16_C( value ) value
#define UINT32_C( value ) _PDCLIB_concat( value, _PDCLIB_UINT32_LITERAL )
#define UINT64_C( value ) _PDCLIB_concat( value, _PDCLIB_UINT64_LITERAL )

/* Macros for greatest-width integer constants */

/* Expand to an integer constant of specified value and type intmax_t */
#define INTMAX_C( value ) _PDCLIB_INTMAX_C( value )

/* Expand to an integer constant of specified value and type uintmax_t */
#define UINTMAX_C( value ) _PDCLIB_UINTMAX_C( value )

#endif

#endif
