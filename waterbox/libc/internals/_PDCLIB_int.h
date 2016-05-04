/* PDCLib internal integer logic <_PDCLIB_int.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef __PDCLIB_INT_H
#define __PDCLIB_INT_H __PDCLIB_INT_H

/* -------------------------------------------------------------------------- */
/* You should not have to edit anything in this file; if you DO have to, it   */
/* would be considered a bug / missing feature: notify the author(s).         */
/* -------------------------------------------------------------------------- */

#include "_PDCLIB_config.h"
#include "_PDCLIB_aux.h"

/* null pointer constant */
#define _PDCLIB_NULL 0

/* -------------------------------------------------------------------------- */
/* Limits of native datatypes                                                 */
/* -------------------------------------------------------------------------- */
/* The definition of minimum limits for unsigned datatypes is done because    */
/* later on we will "construct" limits for other abstract types:              */
/* USHRT -> _PDCLIB_ + USHRT + _MIN -> _PDCLIB_USHRT_MIN -> 0                 */
/* INT -> _PDCLIB_ + INT + _MIN -> _PDCLIB_INT_MIN -> ... you get the idea.   */
/* -------------------------------------------------------------------------- */

/* Setting 'char' limits                                                      */
#define _PDCLIB_CHAR_BIT    8
#define _PDCLIB_UCHAR_MIN   0
#define _PDCLIB_UCHAR_MAX   0xff
#define _PDCLIB_SCHAR_MIN   (-0x7f - 1)
#define _PDCLIB_SCHAR_MAX   0x7f
#ifdef  _PDCLIB_CHAR_SIGNED
#define _PDCLIB_CHAR_MIN    _PDCLIB_SCHAR_MIN
#define _PDCLIB_CHAR_MAX    _PDCLIB_SCHAR_MAX
#else
#define _PDCLIB_CHAR_MIN    0
#define _PDCLIB_CHAR_MAX    _PDCLIB_UCHAR_MAX
#endif

/* Setting 'short' limits                                                     */
#if     _PDCLIB_SHRT_BYTES == 2
#define _PDCLIB_SHRT_MAX      0x7fff
#define _PDCLIB_SHRT_MIN      (-0x7fff - 1)
#define _PDCLIB_USHRT_MAX     0xffff
#else
#error Unsupported width of 'short' (not 16 bit).
#endif
#define _PDCLIB_USHRT_MIN 0

#if _PDCLIB_INT_BYTES < _PDCLIB_SHRT_BYTES
#error Bogus setting: short > int? Check _PDCLIB_config.h.
#endif

/* Setting 'int' limits                                                       */
#if     _PDCLIB_INT_BYTES == 2
#define _PDCLIB_INT_MAX   0x7fff
#define _PDCLIB_INT_MIN   (-0x7fff - 1)
#define _PDCLIB_UINT_MAX  0xffffU
#elif   _PDCLIB_INT_BYTES == 4
#define _PDCLIB_INT_MAX   0x7fffffff
#define _PDCLIB_INT_MIN   (-0x7fffffff - 1)
#define _PDCLIB_UINT_MAX  0xffffffffU
#elif _PDCLIB_INT_BYTES   == 8
#define _PDCLIB_INT_MAX   0x7fffffffffffffff
#define _PDCLIB_INT_MIN   (-0x7fffffffffffffff - 1)
#define _PDCLIB_UINT_MAX  0xffffffffffffffff
#else
#error Unsupported width of 'int' (neither 16, 32, nor 64 bit).
#endif
#define _PDCLIB_UINT_MIN 0

/* Setting 'long' limits                                                      */
#if   _PDCLIB_LONG_BYTES   == 4
#define _PDCLIB_LONG_MAX   0x7fffffffL
#define _PDCLIB_LONG_MIN   (-0x7fffffffL - 1L)
#define _PDCLIB_ULONG_MAX  0xffffffffUL
#elif   _PDCLIB_LONG_BYTES == 8
#define _PDCLIB_LONG_MAX   0x7fffffffffffffffL
#define _PDCLIB_LONG_MIN   (-0x7fffffffffffffffL - 1L)
#define _PDCLIB_ULONG_MAX  0xffffffffffffffffUL
#else
#error Unsupported width of 'long' (neither 32 nor 64 bit).
#endif
#define _PDCLIB_ULONG_MIN 0

/* Setting 'long long' limits                                                 */
#if _PDCLIB_LLONG_BYTES    == 8
#define _PDCLIB_LLONG_MAX  0x7fffffffffffffffLL
#define _PDCLIB_LLONG_MIN  (-0x7fffffffffffffffLL - 1LL)
#define _PDCLIB_ULLONG_MAX 0xffffffffffffffffULL
#elif _PDCLIB_LLONG_BYTES  == 16
#define _PDCLIB_LLONG_MAX  0x7fffffffffffffffffffffffffffffffLL
#define _PDCLIB_LLONG_MIN  (-0x7fffffffffffffffffffffffffffffffLL - 1LL)
#define _PDCLIB_ULLONG_MAX 0xffffffffffffffffffffffffffffffffULL
#else
#error Unsupported width of 'long long' (neither 64 nor 128 bit).
#endif
#define _PDCLIB_ULLONG_MIN 0

/* -------------------------------------------------------------------------- */
/* <stdint.h> exact-width types and their limits                              */
/* -------------------------------------------------------------------------- */
/* Note that, for the "standard" widths of 8, 16, 32 and 64 bit, the "LEAST"  */
/* types are identical to the "exact-width" types, by definition.             */

/* Setting 'int8_t', its limits, its literal, and conversion macros.          */
#if     _PDCLIB_CHAR_BIT == 8
typedef signed char        _PDCLIB_int8_t;
typedef unsigned char      _PDCLIB_uint8_t;
typedef signed char        _PDCLIB_int_least8_t;
typedef unsigned char      _PDCLIB_uint_least8_t;
#define _PDCLIB_INT8_MAX   _PDCLIB_CHAR_MAX
#define _PDCLIB_INT8_MIN   _PDCLIB_CHAR_MIN
#define _PDCLIB_UINT8_MAX  _PDCLIB_UCHAR_MAX
#define _PDCLIB_8_CONV     hh
#else
#error Unsupported width of char (not 8 bits).
#endif

/* Setting 'int16_t', its limits, its literal, and conversion macros.         */
#if     _PDCLIB_INT_BYTES  == 2
typedef signed int         _PDCLIB_int16_t;
typedef unsigned int       _PDCLIB_uint16_t;
typedef signed int         _PDCLIB_int_least16_t;
typedef unsigned int       _PDCLIB_uint_least16_t;
#define _PDCLIB_INT16_MAX  _PDCLIB_INT_MAX
#define _PDCLIB_INT16_MIN  _PDCLIB_INT_MIN
#define _PDCLIB_UINT16_MAX _PDCLIB_UINT_MAX
#define _PDCLIB_16_CONV
#elif   _PDCLIB_SHRT_BYTES == 2
typedef signed short       _PDCLIB_int16_t;
typedef unsigned short     _PDCLIB_uint16_t;
typedef signed short       _PDCLIB_int_least16_t;
typedef unsigned short     _PDCLIB_uint_least16_t;
#define _PDCLIB_INT16_MAX  _PDCLIB_SHRT_MAX
#define _PDCLIB_INT16_MIN  _PDCLIB_SHRT_MIN
#define _PDCLIB_UINT16_MAX _PDCLIB_USHRT_MAX
#define _PDCLIB_16_CONV    h
#else
#error Neither 'short' nor 'int' are 16-bit.
#endif

/* Setting 'int32_t', its limits, its literal, and conversion macros.         */
#if     _PDCLIB_INT_BYTES  == 4
typedef signed int         _PDCLIB_int32_t;
typedef unsigned int       _PDCLIB_uint32_t;
typedef signed int         _PDCLIB_int_least32_t;
typedef unsigned int       _PDCLIB_uint_least32_t;
#define _PDCLIB_INT32_MAX  _PDCLIB_INT_MAX
#define _PDCLIB_INT32_MIN  _PDCLIB_INT_MIN
#define _PDCLIB_UINT32_MAX _PDCLIB_UINT_MAX
#define _PDCLIB_INT32_LITERAL
#define _PDCLIB_UINT32_LITERAL
#define _PDCLIB_32_CONV
#elif   _PDCLIB_LONG_BYTES == 4
typedef signed long        _PDCLIB_int32_t;
typedef unsigned long      _PDCLIB_uint32_t;
typedef signed long        _PDCLIB_int_least32_t;
typedef unsigned long      _PDCLIB_uint_least32_t;
#define _PDCLIB_INT32_MAX  _PDCLIB_LONG_MAX
#define _PDCLIB_INT32_MIN  _PDCLIB_LONG_MIN
#define _PDCLIB_UINT32_MAX _PDCLIB_LONG_MAX
#define _PDCLIB_INT32_LITERAL  l
#define _PDCLIB_UINT32_LITERAL ul
#define _PDCLIB_32_CONV        l
#else
#error Neither 'int' nor 'long' are 32-bit.
#endif

/* Setting 'int64_t', its limits, its literal, and conversion macros.         */
#if     _PDCLIB_LONG_BYTES == 8 && !defined(_PDCLIB_INT64_IS_LLONG)
typedef signed long        _PDCLIB_int64_t;
typedef unsigned long      _PDCLIB_uint64_t;
typedef signed long        _PDCLIB_int_least64_t;
typedef unsigned long      _PDCLIB_uint_least64_t;
#define _PDCLIB_INT64_MAX  _PDCLIB_LONG_MAX
#define _PDCLIB_INT64_MIN  _PDCLIB_LONG_MIN
#define _PDCLIB_UINT64_MAX  _PDCLIB_ULONG_MAX
#define _PDCLIB_INT64_LITERAL  l
#define _PDCLIB_UINT64_LITERAL ul
#define _PDCLIB_64_CONV        l
#elif _PDCLIB_LLONG_BYTES  == 8
typedef signed long long   _PDCLIB_int64_t;
typedef unsigned long long _PDCLIB_uint64_t;
typedef signed long long   _PDCLIB_int_least64_t;
typedef unsigned long long _PDCLIB_uint_least64_t;
#define _PDCLIB_INT64_MAX  _PDCLIB_LLONG_MAX
#define _PDCLIB_INT64_MIN  _PDCLIB_LLONG_MIN
#define _PDCLIB_UINT64_MAX  _PDCLIB_ULLONG_MAX
#define _PDCLIB_INT64_LITERAL  ll
#define _PDCLIB_UINT64_LITERAL ull
#define _PDCLIB_64_CONV        ll
#else
#error Neither 'long' nor 'long long' are 64-bit.
#endif

/* -------------------------------------------------------------------------- */
/* <stdint.h> "fastest" types and their limits                                */
/* -------------------------------------------------------------------------- */
/* This is, admittedly, butt-ugly. But at least it's ugly where the average   */
/* user of PDCLib will never see it, and makes <_PDCLIB_config.h> much        */
/* cleaner.                                                                   */
/* -------------------------------------------------------------------------- */

typedef _PDCLIB_fast8          _PDCLIB_int_fast8_t;
typedef unsigned _PDCLIB_fast8 _PDCLIB_uint_fast8_t;
#define _PDCLIB_INT_FAST8_MIN  _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_FAST8 ), _MIN )
#define _PDCLIB_INT_FAST8_MAX  _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_FAST8 ), _MAX )
#define _PDCLIB_UINT_FAST8_MAX _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_U, _PDCLIB_FAST8 ), _MAX )

typedef _PDCLIB_fast16          _PDCLIB_int_fast16_t;
typedef unsigned _PDCLIB_fast16 _PDCLIB_uint_fast16_t;
#define _PDCLIB_INT_FAST16_MIN  _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_FAST16 ), _MIN )
#define _PDCLIB_INT_FAST16_MAX  _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_FAST16 ), _MAX )
#define _PDCLIB_UINT_FAST16_MAX _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_U, _PDCLIB_FAST16 ), _MAX )

typedef _PDCLIB_fast32          _PDCLIB_int_fast32_t;
typedef unsigned _PDCLIB_fast32 _PDCLIB_uint_fast32_t;
#define _PDCLIB_INT_FAST32_MIN  _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_FAST32 ), _MIN )
#define _PDCLIB_INT_FAST32_MAX  _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_FAST32 ), _MAX )
#define _PDCLIB_UINT_FAST32_MAX _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_U, _PDCLIB_FAST32 ), _MAX )

typedef _PDCLIB_fast64          _PDCLIB_int_fast64_t;
typedef unsigned _PDCLIB_fast64 _PDCLIB_uint_fast64_t;
#define _PDCLIB_INT_FAST64_MIN  _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_FAST64 ), _MIN )
#define _PDCLIB_INT_FAST64_MAX  _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_FAST64 ), _MAX )
#define _PDCLIB_UINT_FAST64_MAX _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_U, _PDCLIB_FAST64 ), _MAX )

/* -------------------------------------------------------------------------- */
/* Various <stddef.h> typedefs and limits                                     */
/* -------------------------------------------------------------------------- */

typedef _PDCLIB_ptrdiff     _PDCLIB_ptrdiff_t;
#define _PDCLIB_PTRDIFF_MIN _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_PTRDIFF ), _MIN )
#define _PDCLIB_PTRDIFF_MAX _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_PTRDIFF ), _MAX )

typedef _PDCLIB_size     _PDCLIB_size_t;
#define _PDCLIB_SIZE_MAX _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_SIZE ), _MAX )

typedef _PDCLIB_wint      _PDCLIB_wint_t;

#ifndef __cplusplus
    typedef _PDCLIB_wchar     _PDCLIB_wchar_t;
#else
    typedef wchar_t _PDCLIB_wchar_t;
#endif
#define _PDCLIB_WCHAR_MIN _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_WCHAR ), _MIN )
#define _PDCLIB_WCHAR_MAX _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_WCHAR ), _MAX )

#define _PDCLIB_SIG_ATOMIC_MIN _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_SIG_ATOMIC ), _MIN )
#define _PDCLIB_SIG_ATOMIC_MAX _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_SIG_ATOMIC ), _MAX )

typedef _PDCLIB_intptr          _PDCLIB_intptr_t;
typedef unsigned _PDCLIB_intptr _PDCLIB_uintptr_t;
#define _PDCLIB_INTPTR_MIN  _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_INTPTR ), _MIN )
#define _PDCLIB_INTPTR_MAX  _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_INTPTR ), _MAX )
#define _PDCLIB_UINTPTR_MAX _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_U, _PDCLIB_INTPTR ), _MAX )

typedef _PDCLIB_intmax          _PDCLIB_intmax_t;
typedef unsigned _PDCLIB_intmax _PDCLIB_uintmax_t;
#define _PDCLIB_INTMAX_MIN  _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_INTMAX ), _MIN )
#define _PDCLIB_INTMAX_MAX  _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_, _PDCLIB_INTMAX ), _MAX )
#define _PDCLIB_UINTMAX_MAX _PDCLIB_concat( _PDCLIB_concat( _PDCLIB_U, _PDCLIB_INTMAX ), _MAX )
#define _PDCLIB_INTMAX_C( value )  _PDCLIB_concat( value, _PDCLIB_INTMAX_LITERAL )
#define _PDCLIB_UINTMAX_C( value ) _PDCLIB_concat( value, _PDCLIB_concat( u, _PDCLIB_INTMAX_LITERAL ) )

/* -------------------------------------------------------------------------- */
/* Various <time.h> internals                                                 */
/* -------------------------------------------------------------------------- */

typedef _PDCLIB_time            _PDCLIB_time_t;
typedef _PDCLIB_clock           _PDCLIB_clock_t;

/* -------------------------------------------------------------------------- */
/* Internal data types                                                        */
/* -------------------------------------------------------------------------- */

/* Structure required by both atexit() and exit() for handling atexit functions */
struct _PDCLIB_exitfunc_t
{
    struct _PDCLIB_exitfunc_t * next;
    void (*func)( void );
};

/* -------------------------------------------------------------------------- */
/* Declaration of helper functions (implemented in functions/_PDCLIB).        */
/* -------------------------------------------------------------------------- */

/* This is the main function called by atoi(), atol() and atoll().            */
_PDCLIB_intmax_t _PDCLIB_atomax( const char * s );

/* Two helper functions used by strtol(), strtoul() and long long variants.   */
const char * _PDCLIB_strtox_prelim( const char * p, char * sign, int * base );
_PDCLIB_uintmax_t _PDCLIB_strtox_main( const char ** p, unsigned int base, _PDCLIB_uintmax_t error, _PDCLIB_uintmax_t limval, int limdigit, char * sign );

/* Digits arrays used by various integer conversion functions */
extern char _PDCLIB_digits[];
extern char _PDCLIB_Xdigits[];

/* -------------------------------------------------------------------------- */
/* Sanity checks                                                              */
/* -------------------------------------------------------------------------- */

#if _PDCLIB_C_VERSION >= 2011
#ifdef __cplusplus
#define _Static_assert static_assert
#endif
_Static_assert( sizeof( short ) == _PDCLIB_SHRT_BYTES, "_PDCLIB_SHRT_BYTES incorrectly defined, check _PDCLIB_config.h" );
_Static_assert( sizeof( int ) == _PDCLIB_INT_BYTES, "_PDCLIB_INT_BYTES incorrectly defined, check _PDCLIB_config.h" );
_Static_assert( sizeof( long ) == _PDCLIB_LONG_BYTES, "_PDCLIB_LONG_BYTES incorrectly defined, check _PDCLIB_config.h" );
_Static_assert( sizeof( long long ) == _PDCLIB_LLONG_BYTES, "_PDCLIB_LLONG_BYTES incorrectly defined, check _PDCLIB_config.h" );
_Static_assert( ( (char)-1 < 0 ) == _PDCLIB_CHAR_SIGNED, "_PDCLIB_CHAR_SIGNED incorrectly defined, check _PDCLIB_config.h" );
_Static_assert( sizeof( _PDCLIB_wchar ) == sizeof( L'x' ), "_PDCLIB_wchar incorrectly defined, check _PDCLIB_config.h" );
_Static_assert( sizeof( void * ) == sizeof( _PDCLIB_intptr ), "_PDCLIB_intptr incorrectly defined, check _PDCLIB_config.h" );
_Static_assert( sizeof( sizeof( 1 ) ) == sizeof( _PDCLIB_size ), "_PDCLIB_size incorrectly defined, check _PDCLIB_config.h" );
_Static_assert( sizeof( &_PDCLIB_digits[1] - &_PDCLIB_digits[0] ) == sizeof( _PDCLIB_ptrdiff ), "_PDCLIB_ptrdiff incorrectly defined, check _PDCLIB_config.h" );
#endif

/* -------------------------------------------------------------------------- */
/* locale / wchar / uchar                                                     */
/* -------------------------------------------------------------------------- */

#ifndef __cplusplus
typedef _PDCLIB_uint16_t        _PDCLIB_char16_t;
typedef _PDCLIB_uint32_t        _PDCLIB_char32_t;
#else
typedef char16_t                _PDCLIB_char16_t;
typedef char32_t                _PDCLIB_char32_t;
#endif

typedef struct _PDCLIB_mbstate {
    union {
        /* Is this the best way to represent this? Is this big enough? */
        _PDCLIB_uint64_t _St64[15];
        _PDCLIB_uint32_t _St32[31];
        _PDCLIB_uint16_t _St16[62];
        unsigned char    _StUC[124];
        signed   char    _StSC[124];
                 char    _StC [124];
    };

    /* c16/related functions: Surrogate storage
     *
     * If zero, no surrogate pending. If nonzero, surrogate.
     */
    _PDCLIB_uint16_t     _Surrogate;

    /* In cases where the underlying codec is capable of regurgitating a
     * character without consuming any extra input (e.g. a surrogate pair in a
     * UCS-4 to UTF-16 conversion) then these fields are used to track that
     * state. In particular, they are used to buffer/fake the input for mbrtowc
     * and similar functions.
     *
     * See _PDCLIB_encoding.h for values of _PendState and the resultant value
     * in _PendChar.
     */
    unsigned char _PendState;
             char _PendChar;
} _PDCLIB_mbstate_t;

typedef struct _PDCLIB_locale    *_PDCLIB_locale_t;
typedef struct lconv              _PDCLIB_lconv_t;

_PDCLIB_size_t _PDCLIB_mb_cur_max( void );

/* wide-character EOF */
#define _PDCLIB_WEOF ((wint_t) -1)

/* -------------------------------------------------------------------------- */
/* stdio                                                                      */
/* -------------------------------------------------------------------------- */

/* Position / status structure for getpos() / fsetpos(). */
typedef struct _PDCLIB_fpos
{
    _PDCLIB_int_fast64_t offset; /* File position offset */
    _PDCLIB_mbstate_t    mbs;    /* Multibyte parsing state */
} _PDCLIB_fpos_t;

typedef struct _PDCLIB_fileops  _PDCLIB_fileops_t;
typedef union  _PDCLIB_fd       _PDCLIB_fd_t;
typedef struct _PDCLIB_file     _PDCLIB_file_t; // Rename to _PDCLIB_FILE?

/* Status structure required by _PDCLIB_print(). */
struct _PDCLIB_status_t
{
    /* XXX This structure is horrible now. scanf needs its own */

    int              base;   /* base to which the value shall be converted   */
    _PDCLIB_int_fast32_t flags; /* flags and length modifiers                */
    unsigned         n;      /* print: maximum characters to be written (snprintf) */
                             /* scan:  number matched conversion specifiers  */
    unsigned         i;      /* number of characters read/written            */
    unsigned         current;/* chars read/written in the CURRENT conversion */
    unsigned         width;  /* specified field width                        */
    int              prec;   /* specified field precision                    */

    union {
        void *           ctx;    /* context for callback */
        const char *     s;      /* input string for scanf */
    };

    union {
        _PDCLIB_size_t ( *write ) ( void *p, const char *buf, _PDCLIB_size_t size );
        _PDCLIB_file_t *stream;  /* for scanf */
    };
    _PDCLIB_va_list  arg;    /* argument stack                               */
};

#endif
