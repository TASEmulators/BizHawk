/* strcmp( const char *, const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <string.h>

#ifndef REGTEST

int strcmp( const char * s1, const char * s2 )
{
    while ( ( *s1 ) && ( *s1 == *s2 ) )
    {
        ++s1;
        ++s2;
    }
    return ( *(unsigned char *)s1 - *(unsigned char *)s2 );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    char cmpabcde[] = "abcde";
    char cmpabcd_[] = "abcd\xfc";
    char empty[] = "";
    TESTCASE( strcmp( abcde, cmpabcde ) == 0 );
    TESTCASE( strcmp( abcde, abcdx ) < 0 );
    TESTCASE( strcmp( abcdx, abcde ) > 0 );
    TESTCASE( strcmp( empty, abcde ) < 0 );
    TESTCASE( strcmp( abcde, empty ) > 0 );
    TESTCASE( strcmp( abcde, cmpabcd_ ) < 0 );
    return TEST_RESULTS;
}
#endif
