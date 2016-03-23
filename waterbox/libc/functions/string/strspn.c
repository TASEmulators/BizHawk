/* strspn( const char *, const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <string.h>

#ifndef REGTEST

size_t strspn( const char * s1, const char * s2 )
{
    size_t len = 0;
    const char * p;
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
    TESTCASE( strspn( abcde, "abc" ) == 3 );
    TESTCASE( strspn( abcde, "b" ) == 0 );
    TESTCASE( strspn( abcde, abcde ) == 5 );
    return TEST_RESULTS;
}
#endif
