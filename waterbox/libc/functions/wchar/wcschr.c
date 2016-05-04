/* wcschr( const wchar_t *, wchar_t );

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>
#include <stddef.h>

#ifndef REGTEST

wchar_t *wcschr(const wchar_t * haystack, wchar_t needle)
{
    while(*haystack) {
        if(*haystack == needle) return (wchar_t*) haystack;
        haystack++;
    }
    return NULL;
}


#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    return TEST_RESULTS;
}

#endif
