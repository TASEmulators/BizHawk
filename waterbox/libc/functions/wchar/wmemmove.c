/* wmemmove( wchar_t *, const wchar_t *, size_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

wchar_t * wmemmove( wchar_t * dest, const wchar_t * src, size_t n )
{
    wchar_t* rv = dest;
    if ( dest <= src )
    {
        while ( n-- )
        {
            *dest++ = *src++;
        }
    }
    else
    {
        src += n;
        dest += n;
        while ( n-- )
        {
            *--dest = *--src;
        }
    }
    return rv;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    wchar_t s[] = L"xxxxabcde";
    TESTCASE( wmemmove( s, s + 4, 5 ) == s );
    TESTCASE( s[0] == L'a' );
    TESTCASE( s[4] == L'e' );
    TESTCASE( s[5] == L'b' );
    TESTCASE( wmemmove( s + 4, s, 5 ) == s + 4 );
    TESTCASE( s[4] == L'a' );
    return TEST_RESULTS;
}
#endif
