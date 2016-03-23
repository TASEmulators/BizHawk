/* Extended multibyte and wide character utilities <wchar.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_WCHAR_H
#define _PDCLIB_WCHAR_H _PDCLIB_WCHAR_H
#include "_PDCLIB_int.h"

#ifdef __cplusplus
extern "C" {
#endif

#ifndef _PDCLIB_SIZE_T_DEFINED
#define _PDCLIB_SIZE_T_DEFINED _PDCLIB_SIZE_T_DEFINED
typedef _PDCLIB_size_t size_t;
#endif

#ifndef __cplusplus

#ifndef _PDCLIB_WCHAR_T_DEFINED
#define _PDCLIB_WCHAR_T_DEFINED _PDCLIB_WCHAR_T_DEFINED
typedef _PDCLIB_wchar_t wchar_t;
#endif

#endif

#ifndef _PDCLIB_WINT_T_DEFINED
#define _PDCLIB_WINT_T_DEFINED _PDCLIB_WINT_T_DEFINED
typedef _PDCLIB_wint_t wint_t;
#endif

#ifndef _PDCLIB_MBSTATE_T_DEFINED
#define _PDCLIB_MBSTATE_T_DEFINED _PDCLIB_MBSTATE_T_DEFINED
typedef _PDCLIB_mbstate_t mbstate_t;
#endif

struct tm;

#ifndef _PDCLIB_NULL_DEFINED
#define _PDCLIB_NULL_DEFINED _PDCLIB_NULL_DEFINED
#define NULL _PDCLIB_NULL
#endif

#ifndef _PDCLIB_WCHAR_MIN_MAX_DEFINED
#define _PDCLIB_WCHAR_MIN_MAX_DEFINED _PDCLIB_WCHAR_MIN_MAX_DEFINED
#define WCHAR_MIN _PDCLIB_WCHAR_MIN
#define WCHAR_MAX _PDCLIB_WCHAR_MAX
#endif

#ifndef _PDCLIB_WEOF_DEFINED
#define _PDCLIB_WEOF_DEFINED _PDCLIB_WEOF_DEFINED
#define WEOF _PDCLIB_WEOF
#endif

/* Wide character string handling */
wchar_t * wcscpy( wchar_t * _PDCLIB_restrict s1, const wchar_t * _PDCLIB_restrict s2 );
wchar_t * wcsncpy( wchar_t * _PDCLIB_restrict s1, const wchar_t * _PDCLIB_restrict s2, size_t n );
wchar_t * wmemcpy( wchar_t * _PDCLIB_restrict s1, const wchar_t * _PDCLIB_restrict s2, size_t n );
wchar_t * wmemmove( wchar_t * s1, const wchar_t * s2, size_t n );
wchar_t * wcscat( wchar_t * _PDCLIB_restrict s1, const wchar_t * _PDCLIB_restrict s2 );
wchar_t * wcsncat( wchar_t * _PDCLIB_restrict s1, const wchar_t * _PDCLIB_restrict s2, size_t n );
int wcscmp( const wchar_t * s1, const wchar_t * s2 );
int wcscoll( const wchar_t * s1, const wchar_t * s2 );
int wcsncmp( const wchar_t * s1, const wchar_t * s2, size_t n );
size_t wcsxfrm( wchar_t * _PDCLIB_restrict s1, const wchar_t * _PDCLIB_restrict s2, size_t n );
int wmemcmp( const wchar_t * s1, const wchar_t * s2, size_t n );
wchar_t * wcschr( const wchar_t * s, wchar_t c );
size_t wcscspn( const wchar_t * s1, const wchar_t * s2 );
wchar_t * wcspbrk( const wchar_t * s1, const wchar_t * s2 );
wchar_t * wcsrchr( const wchar_t * s, wchar_t c );
size_t wcsspn( const wchar_t * s1, const wchar_t * s2 );
wchar_t * wcsstr( const wchar_t * s1, const wchar_t * s2 );
wchar_t * wcstok( wchar_t * _PDCLIB_restrict s1, const wchar_t * _PDCLIB_restrict s2, wchar_t * * _PDCLIB_restrict ptr );
wchar_t * wmemchr( const wchar_t * s, wchar_t c, size_t n );
size_t wcslen( const wchar_t * s );
wchar_t * wmemset( wchar_t * s, wchar_t c, size_t n );

#if 0
size_t wcsftime( wchar_t * _PDCLIB_restrict s, size_t maxsize, const wchar_t * _PDCLIB_restrict format, const struct tm * _PDCLIB_restrict timeptr );
#endif

/* Wide character I/O */
int fwprintf( _PDCLIB_file_t * _PDCLIB_restrict stream, const wchar_t * _PDCLIB_restrict format, ... );
int fwscanf( _PDCLIB_file_t * _PDCLIB_restrict stream, const wchar_t * _PDCLIB_restrict format, ... );
int swprintf( wchar_t * _PDCLIB_restrict s, size_t n, const wchar_t * _PDCLIB_restrict format, ... );
int swscanf( const wchar_t * _PDCLIB_restrict s, const wchar_t * _PDCLIB_restrict format, ... );
int vfwprintf( _PDCLIB_file_t * _PDCLIB_restrict stream, const wchar_t * _PDCLIB_restrict format, _PDCLIB_va_list arg );
int vfwscanf( _PDCLIB_file_t * _PDCLIB_restrict stream, const wchar_t * _PDCLIB_restrict format, _PDCLIB_va_list arg );
int vswprintf( wchar_t * _PDCLIB_restrict s, size_t n, const wchar_t * _PDCLIB_restrict format, _PDCLIB_va_list arg );
int vswscanf( const wchar_t * _PDCLIB_restrict s, const wchar_t * _PDCLIB_restrict format, _PDCLIB_va_list arg );
int vwprintf( const wchar_t * _PDCLIB_restrict format, _PDCLIB_va_list arg );
int vwscanf( const wchar_t * _PDCLIB_restrict format, _PDCLIB_va_list arg );
int wprintf( const wchar_t * _PDCLIB_restrict format, ... );
int wscanf( const wchar_t * _PDCLIB_restrict format, ... );
wint_t fgetwc( _PDCLIB_file_t * stream );
wchar_t * fgetws( wchar_t * _PDCLIB_restrict s, int n, _PDCLIB_file_t * _PDCLIB_restrict stream );
wint_t fputwc( wchar_t c, _PDCLIB_file_t * stream );
int fputws( const wchar_t * _PDCLIB_restrict s, _PDCLIB_file_t * _PDCLIB_restrict stream );
int fwide( _PDCLIB_file_t * stream, int mode );
wint_t getwc( _PDCLIB_file_t * stream );
wint_t getwchar( void );
wint_t putwc( wchar_t c, _PDCLIB_file_t * stream );
wint_t putwchar( wchar_t c );
wint_t ungetwc( wint_t c, _PDCLIB_file_t * stream );

#if _PDCLIB_GNU_SOURCE
wint_t getwc_unlocked( _PDCLIB_file_t * stream );
wint_t getwchar_unlocked( void );
wint_t fgetwc_unlocked( _PDCLIB_file_t * stream );
wint_t fputwc_unlocked( wchar_t wc, _PDCLIB_file_t * stream );
wint_t putwc_unlocked( wchar_t wc, _PDCLIB_file_t * stream );
wint_t putwchar_unlocked( wchar_t wc );
wchar_t * fgetws_unlocked( wchar_t * ws, int n, _PDCLIB_file_t * stream );
int fputws_unlocked( const wchar_t * ws, _PDCLIB_file_t * stream );
#endif

/* Wide character <-> Numeric conversions */

/*
double wcstod( const wchar_t * _PDCLIB_restrict nptr, wchar_t * * _PDCLIB_restrict endptr );
float wcstof( const wchar_t * _PDCLIB_restrict nptr, wchar_t * * _PDCLIB_restrict endptr );
long double wcstold( const wchar_t * _PDCLIB_restrict nptr, wchar_t * * _PDCLIB_restrict endptr );
*/

long int wcstol( const wchar_t * _PDCLIB_restrict nptr, wchar_t * * _PDCLIB_restrict endptr, int base );
long long int wcstoll( const wchar_t * _PDCLIB_restrict nptr, wchar_t * * _PDCLIB_restrict endptr, int base );
unsigned long int wcstoul( const wchar_t * _PDCLIB_restrict nptr, wchar_t * * _PDCLIB_restrict endptr, int base );
unsigned long long int wcstoull( const wchar_t * _PDCLIB_restrict nptr, wchar_t * * _PDCLIB_restrict endptr, int base );

/* Character set conversion */
wint_t btowc( int c );
int wctob( wint_t c );
int mbsinit( const mbstate_t * ps );
size_t mbrlen( const char * _PDCLIB_restrict s, size_t n, mbstate_t * _PDCLIB_restrict ps );
size_t mbrtowc( wchar_t * _PDCLIB_restrict pwc, const char * _PDCLIB_restrict s, size_t n, mbstate_t * _PDCLIB_restrict ps );
size_t wcrtomb( char * _PDCLIB_restrict s, wchar_t wc, mbstate_t * _PDCLIB_restrict ps );
size_t mbsrtowcs( wchar_t * _PDCLIB_restrict dst, const char * * _PDCLIB_restrict src, size_t len, mbstate_t * _PDCLIB_restrict ps );
size_t wcsrtombs( char * _PDCLIB_restrict dst, const wchar_t * * _PDCLIB_restrict src, size_t len, mbstate_t * _PDCLIB_restrict ps );

#ifdef __cplusplus
}
#endif

#endif
