/* wcsncmp( const wchar_t *, const wchar_t *, size_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

int wcsncmp( const wchar_t * s1, const wchar_t * s2, size_t n )
{
    while ( *s1 && n && ( *s1 == *s2 ) )
    {
        ++s1;
        ++s2;
        --n;
    }
    if ( n == 0 )
    {
        return 0;
    }
    else
    {
        return ( *(wchar_t *)s1 - *(wchar_t *)s2 );
    }
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    wchar_t cmpabcde[] = L"abcde\0f";
    wchar_t cmpabcd_[] = L"abcde\xfc";
    wchar_t empty[] = L"";
    wchar_t x[] = L"x";
    TESTCASE( wcsncmp( wabcde, cmpabcde, 5 ) == 0 );
    TESTCASE( wcsncmp( wabcde, cmpabcde, 10 ) == 0 );
    TESTCASE( wcsncmp( wabcde, wabcdx, 5 ) < 0 );
    TESTCASE( wcsncmp( wabcdx, wabcde, 5 ) > 0 );
    TESTCASE( wcsncmp( empty, wabcde, 5 ) < 0 );
    TESTCASE( wcsncmp( wabcde, empty, 5 ) > 0 );
    TESTCASE( wcsncmp( wabcde, wabcdx, 4 ) == 0 );
    TESTCASE( wcsncmp( wabcde, x, 0 ) == 0 );
    TESTCASE( wcsncmp( wabcde, x, 1 ) < 0 );
    TESTCASE( wcsncmp( wabcde, cmpabcd_, 10 ) < 0 );
    return TEST_RESULTS;
}
#endif
