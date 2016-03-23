/* wcscspn( const wchar_t *, const wchar_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

size_t wcscspn( const wchar_t * s1, const wchar_t * s2 )
{
    size_t len = 0;
    const wchar_t * p;
    while ( s1[len] )
    {
        p = s2;
        while ( *p )
        {
            if ( s1[len] == *p++ )
            {
                return len;
            }
        }
        ++len;
    }
    return len;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( wcscspn( wabcde, L"x" ) == 5 );
    TESTCASE( wcscspn( wabcde, L"xyz" ) == 5 );
    TESTCASE( wcscspn( wabcde, L"zyx" ) == 5 );
    TESTCASE( wcscspn( wabcdx, L"x" ) == 4 );
    TESTCASE( wcscspn( wabcdx, L"xyz" ) == 4 );
    TESTCASE( wcscspn( wabcdx, L"zyx" ) == 4 );
    TESTCASE( wcscspn( wabcde, L"a" ) == 0 );
    TESTCASE( wcscspn( wabcde, L"abc" ) == 0 );
    TESTCASE( wcscspn( wabcde, L"cba" ) == 0 );
    return TEST_RESULTS;
}
#endif
