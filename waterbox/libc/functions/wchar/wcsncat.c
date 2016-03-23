/* wcsncat( wchar_t *, const wchar_t *, size_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

wchar_t * wcsncat( wchar_t * _PDCLIB_restrict s1, 
                   const wchar_t * _PDCLIB_restrict s2, 
                   size_t n )
{
    wchar_t * rc = s1;
    while ( *s1 )
    {
        ++s1;
    }
    while ( n && ( *s1++ = *s2++ ) )
    {
        --n;
    }
    if ( n == 0 )
    {
        *s1 = '\0';
    }
    return rc;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    wchar_t s[] = L"xx\0xxxxxx";
    TESTCASE( wcsncat( s, wabcde, 10 ) == s );
    TESTCASE( s[2] == L'a' );
    TESTCASE( s[6] == L'e' );
    TESTCASE( s[7] == L'\0' );
    TESTCASE( s[8] == L'x' );
    s[0] = L'\0';
    TESTCASE( wcsncat( s, wabcdx, 10 ) == s );
    TESTCASE( s[4] == L'x' );
    TESTCASE( s[5] == L'\0' );
    TESTCASE( wcsncat( s, L"\0", 10 ) == s );
    TESTCASE( s[5] == L'\0' );
    TESTCASE( s[6] == L'e' );
    TESTCASE( wcsncat( s, wabcde, 0 ) == s );
    TESTCASE( s[5] == L'\0' );
    TESTCASE( s[6] == L'e' );
    TESTCASE( wcsncat( s, wabcde, 3 ) == s );
    TESTCASE( s[5] == L'a' );
    TESTCASE( s[7] == L'c' );
    TESTCASE( s[8] == L'\0' );
    return TEST_RESULTS;
}
#endif
