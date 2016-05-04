/* wcscat( wchar_t *, const wchar_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

wchar_t * wcscat( wchar_t * _PDCLIB_restrict s1, 
                  const wchar_t * _PDCLIB_restrict s2 )
{
    wchar_t * rc = s1;
    if ( *s1 )
    {
        while ( *++s1 );
    }
    while ( (*s1++ = *s2++) );
    return rc;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    wchar_t s[] = L"xx\0xxxxxx";
    TESTCASE( wcscat( s, wabcde ) == s );
    TESTCASE( s[2] == L'a' );
    TESTCASE( s[6] == L'e' );
    TESTCASE( s[7] == L'\0' );
    TESTCASE( s[8] == L'x' );
    s[0] = L'\0';
    TESTCASE( wcscat( s, wabcdx ) == s );
    TESTCASE( s[4] == L'x' );
    TESTCASE( s[5] == L'\0' );
    TESTCASE( wcscat( s, L"\0" ) == s );
    TESTCASE( s[5] == L'\0' );
    TESTCASE( s[6] == L'e' );
    return TEST_RESULTS;
}
#endif
