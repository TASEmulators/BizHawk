/* wmemchr( const void *, int, size_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>

#ifndef REGTEST

wchar_t * wmemchr( const wchar_t * p, wchar_t c, size_t n )
{
    while ( n-- )
    {
        if ( *p == c )
        {
            return (wchar_t*) p;
        }
        ++p;
    }
    return NULL;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( wmemchr( wabcde, L'c', 5 ) == &wabcde[2] );
    TESTCASE( wmemchr( wabcde, L'a', 1 ) == &wabcde[0] );
    TESTCASE( wmemchr( wabcde, L'a', 0 ) == NULL );
    TESTCASE( wmemchr( wabcde, L'\0', 5 ) == NULL );
    TESTCASE( wmemchr( wabcde, L'\0', 6 ) == &wabcde[5] );
    return TEST_RESULTS;
}

#endif
