/* wcrtomb( char * s, wchar_t wc, mbstate_t * ps )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef REGTEST
#include <wchar.h>
#include <errno.h>
#include <stdint.h>
#include <assert.h>
#include <stdlib.h>
#include "_PDCLIB_encoding.h"
#include "_PDCLIB_locale.h"

#if 0
/*
   TODO: Other conversion functions call static ..._l helpers, but this one
   does not, making this function "defined but not used".
*/
static size_t wcrtomb_l(
    char        *restrict   s, 
    wchar_t                 wc,
    mbstate_t   *restrict   ps,
    locale_t     restrict   l
)
{
    return _PDCLIB_cwcrtomb_l(s, wc, ps, l);
}
#endif

size_t wcrtomb(
    char        *restrict   s, 
    wchar_t                 wc,
    mbstate_t   *restrict   ps
)
{
    static mbstate_t st;
    return _PDCLIB_cwcrtomb(s, wc, ps ? ps : &st);
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( NO_TESTDRIVER );
    return TEST_RESULTS;
}
#endif
