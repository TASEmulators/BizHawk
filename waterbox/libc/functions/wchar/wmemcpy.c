/* wmemcpy( wchar_t *, const wchar_t *, size_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

wchar_t * wmemcpy( wchar_t * _PDCLIB_restrict dest, 
                   const wchar_t * _PDCLIB_restrict src, 
                   size_t n )
{
    wchar_t* rv = dest;
    while ( n-- )
    {
        *dest++ = *src++;
    }
    return rv;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    wchar_t s[] = L"xxxxxxxxxxx";
    TESTCASE( wmemcpy( s, wabcde, 6 ) == s );
    TESTCASE( s[4] == L'e' );
    TESTCASE( s[5] == L'\0' );
    TESTCASE( wmemcpy( s + 5, wabcde, 5 ) == s + 5 );
    TESTCASE( s[9] == L'e' );
    TESTCASE( s[10] == L'x' );
    return TEST_RESULTS;
}
#endif
