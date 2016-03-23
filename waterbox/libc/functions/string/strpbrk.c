/* strpbrk( const char *, const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <string.h>

#ifndef REGTEST

char * strpbrk( const char * s1, const char * s2 )
{
    const char * p1 = s1;
    const char * p2;
    while ( *p1 )
    {
        p2 = s2;
        while ( *p2 )
        {
            if ( *p1 == *p2++ )
            {
                return (char *) p1;
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
    TESTCASE( strpbrk( abcde, "x" ) == NULL );
    TESTCASE( strpbrk( abcde, "xyz" ) == NULL );
    TESTCASE( strpbrk( abcdx, "x" ) == &abcdx[4] );
    TESTCASE( strpbrk( abcdx, "xyz" ) == &abcdx[4] );
    TESTCASE( strpbrk( abcdx, "zyx" ) == &abcdx[4] );
    TESTCASE( strpbrk( abcde, "a" ) == &abcde[0] );
    TESTCASE( strpbrk( abcde, "abc" ) == &abcde[0] );
    TESTCASE( strpbrk( abcde, "cba" ) == &abcde[0] );
    return TEST_RESULTS;
}
#endif
