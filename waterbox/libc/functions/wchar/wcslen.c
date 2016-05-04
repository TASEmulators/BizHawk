/* wcslen( const wchar_t * );

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

size_t wcslen( const wchar_t * str )
{
    size_t n = 0;
    while(*(str++)) n++;
    return n;
}


#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    return TEST_RESULTS;
}

#endif
