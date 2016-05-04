/* strcspn( const char *, const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <string.h>

#ifndef REGTEST

size_t strcspn( const char * s1, const char * s2 )
{
    size_t len = 0;
    const char * p;
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
    TESTCASE( strcspn( abcde, "x" ) == 5 );
    TESTCASE( strcspn( abcde, "xyz" ) == 5 );
    TESTCASE( strcspn( abcde, "zyx" ) == 5 );
    TESTCASE( strcspn( abcdx, "x" ) == 4 );
    TESTCASE( strcspn( abcdx, "xyz" ) == 4 );
    TESTCASE( strcspn( abcdx, "zyx" ) == 4 );
    TESTCASE( strcspn( abcde, "a" ) == 0 );
    TESTCASE( strcspn( abcde, "abc" ) == 0 );
    TESTCASE( strcspn( abcde, "cba" ) == 0 );
    return TEST_RESULTS;
}
#endif
