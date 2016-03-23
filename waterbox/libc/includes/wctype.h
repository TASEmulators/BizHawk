/* Wide character classification and mapping utilities <wctype.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_WCTYPE_H
#define _PDCLIB_WCTYPE_H _PDCLIB_WCTYPE_H
#include "_PDCLIB_int.h"

#ifdef __cplusplus
extern "C" {
#endif

#ifndef _PDCLIB_WINT_T_DEFINED
#define _PDCLIB_WINT_T_DEFINED _PDCLIB_WINT_T_DEFINED
typedef _PDCLIB_wint_t wint_t;
#endif

#ifndef _PDCLIB_WEOF_DEFINED
#define _PDCLIB_WEOF_DEFINED _PDCLIB_WEOF_DEFINED
#define WEOF _PDCLIB_WEOF
#endif

/* Scalar type representing locale-specific character mappings */
typedef int wctrans_t;

/* Scalar type representing locale-specific character classifications */
typedef int wctype_t;

/* Character classification functions */

int iswalnum( wint_t _Wc );
int iswalpha( wint_t _Wc );
int iswblank( wint_t _Wc );
int iswcntrl( wint_t _Wc );
int iswdigit( wint_t _Wc );
int iswgraph( wint_t _Wc );
int iswlower( wint_t _Wc );
int iswprint( wint_t _Wc );
int iswpunct( wint_t _Wc );
int iswspace( wint_t _Wc );
int iswupper( wint_t _Wc );
int iswxdigit( wint_t _Wc );

/* Extensible character classification functions */

int iswctype( wint_t _Wc, wctype_t _Desc );
wctype_t wctype( const char * _Property );

/* Wide character case mapping utilities */

wint_t towlower( wint_t _Wc );
wint_t towupper( wint_t _Wc );

/* Extensible wide character case mapping functions */

wint_t towctrans( wint_t _Wc, wctrans_t _Desc );
wctrans_t wctrans( const char * _Property );

#ifdef __cplusplus
}
#endif

#endif
