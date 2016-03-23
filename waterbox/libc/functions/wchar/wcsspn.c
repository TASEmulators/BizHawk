/* wcsspn( const wchar_t *, const wchar_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

size_t wcsspn( const wchar_t * s1, const wchar_t * s2 )
{
    size_t len = 0;
    const wchar_t * p;
    while ( s1[ len ] )
    {
        p = s2;
        while ( *p )
        {
            if ( s1[len] == *p )
            {
                break;
            }
            ++p;
        }
        if ( ! *p )
        {
            return len;
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
    TESTCASE( wcsspn( wabcde, L"abc" ) == 3 );
    TESTCASE( wcsspn( wabcde, L"b" ) == 0 );
    TESTCASE( wcsspn( wabcde, wabcde ) == 5 );
    return TEST_RESULTS;
}
#endif
