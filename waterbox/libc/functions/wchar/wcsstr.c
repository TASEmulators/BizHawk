/* wcsstr( const wchar_t *, const wchar_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

wchar_t * wcsstr( const wchar_t * s1, const wchar_t * s2 )
{
    const wchar_t * p1 = s1;
    const wchar_t * p2;
    while ( *s1 )
    {
        p2 = s2;
        while ( *p2 && ( *p1 == *p2 ) )
        {
            ++p1;
            ++p2;
        }
        if ( ! *p2 )
        {
            return (wchar_t *) s1;
        }
        ++s1;
        p1 = s1;
    }
    return NULL;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    wchar_t s[] = L"abcabcabcdabcde";
    TESTCASE( wcsstr( s, L"x" ) == NULL );
    TESTCASE( wcsstr( s, L"xyz" ) == NULL );
    TESTCASE( wcsstr( s, L"a" ) == &s[0] );
    TESTCASE( wcsstr( s, L"abc" ) == &s[0] );
    TESTCASE( wcsstr( s, L"abcd" ) == &s[6] );
    TESTCASE( wcsstr( s, L"abcde" ) == &s[10] );
    return TEST_RESULTS;
}
#endif
