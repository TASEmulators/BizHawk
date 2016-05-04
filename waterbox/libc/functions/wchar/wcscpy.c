/* wchar_t * wcscpy( wchar_t *, const wchar_t * );

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

wchar_t *wcscpy( wchar_t * _PDCLIB_restrict dest, 
                 const wchar_t * _PDCLIB_restrict src)
{
    wchar_t * rv = dest;
    while(*src) {
        *(dest++) = *(src++);
    }

    return rv;
}


#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    return TEST_RESULTS;
}

#endif
