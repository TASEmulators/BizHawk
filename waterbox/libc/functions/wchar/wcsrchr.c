/* wcsrchr( const wchar_t *, wchar_t );

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>
#include <stddef.h>

#ifndef REGTEST

wchar_t *wcsrchr(const wchar_t * haystack, wchar_t needle)
{
    wchar_t *found = NULL;
    while(*haystack) {
        if(*haystack == needle) found = (wchar_t*) haystack;
        haystack++;
    }
    return found;
}


#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    return TEST_RESULTS;
}

#endif
