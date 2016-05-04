/* wcspbrk( const wchar_t *, const wchar_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

wchar_t * wcspbrk( const wchar_t * s1, const wchar_t * s2 )
{
    const wchar_t * p1 = s1;
    const wchar_t * p2;
    while ( *p1 )
    {
        p2 = s2;
        while ( *p2 )
        {
            if ( *p1 == *p2++ )
            {
                return (wchar_t *) p1;
            }
        }
        ++p1;
    }
    return NULL;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( wcspbrk( wabcde, L"x" ) == NULL );
    TESTCASE( wcspbrk( wabcde, L"xyz" ) == NULL );
    TESTCASE( wcspbrk( wabcdx, L"x" ) == &wabcdx[4] );
    TESTCASE( wcspbrk( wabcdx, L"xyz" ) == &wabcdx[4] );
    TESTCASE( wcspbrk( wabcdx, L"zyx" ) == &wabcdx[4] );
    TESTCASE( wcspbrk( wabcde, L"a" ) == &wabcde[0] );
    TESTCASE( wcspbrk( wabcde, L"abc" ) == &wabcde[0] );
    TESTCASE( wcspbrk( wabcde, L"cba" ) == &wabcde[0] );
    return TEST_RESULTS;
}
#endif
