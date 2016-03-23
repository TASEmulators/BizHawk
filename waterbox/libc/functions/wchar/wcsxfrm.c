/* wcsxfrm( char *, const char *, size_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

/* See notes on wcscoll. */
size_t wcsxfrm( wchar_t * _PDCLIB_restrict s1, const wchar_t * _PDCLIB_restrict s2, size_t n )
{
    wcsncpy(s1, s2, n);
    return wcslen(s2);
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    return TEST_RESULTS;
}
#endif

