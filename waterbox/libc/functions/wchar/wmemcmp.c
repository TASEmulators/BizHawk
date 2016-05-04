/* wmemcmp( const wchar_t *, const wchar_t *, size_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

int wmemcmp( const wchar_t * p1, const wchar_t * p2, size_t n )
{
    while ( n-- )
    {
        if ( *p1 != *p2 )
        {
            return *p1 - *p2;
        }
        ++p1;
        ++p2;
    }
    return 0;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    wchar_t const xxxxx[] = L"xxxxx";
    TESTCASE( wmemcmp( wabcde, wabcdx, 5 ) < 0 );
    TESTCASE( wmemcmp( wabcde, wabcdx, 4 ) == 0 );
    TESTCASE( wmemcmp( wabcde, xxxxx,  0 ) == 0 );
    TESTCASE( wmemcmp( xxxxx,  wabcde, 1 ) > 0 );
    return 0;
}
#endif
