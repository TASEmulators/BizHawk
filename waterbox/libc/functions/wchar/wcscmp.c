/* wcscmp( const wchar_t *, const wchar_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

int wcscmp( const wchar_t * s1, const wchar_t * s2 )
{
    while ( ( *s1 ) && ( *s1 == *s2 ) )
    {
        ++s1;
        ++s2;
    }
    return ( *(wchar_t *)s1 - *(wchar_t *)s2 );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    wchar_t cmpabcde[] = L"abcde";
    wchar_t cmpabcd_[] = L"abcd\xfc";
    wchar_t empty[] = L"";
    TESTCASE( wcscmp( wabcde, cmpabcde ) == 0 );
    TESTCASE( wcscmp( wabcde, wabcdx ) < 0 );
    TESTCASE( wcscmp( wabcdx, wabcde ) > 0 );
    TESTCASE( wcscmp( empty, wabcde ) < 0 );
    TESTCASE( wcscmp( wabcde, empty ) > 0 );
    TESTCASE( wcscmp( wabcde, cmpabcd_ ) < 0 );
    return TEST_RESULTS;
}
#endif
