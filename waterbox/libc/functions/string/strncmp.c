/* strncmp( const char *, const char *, size_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <string.h>

#ifndef REGTEST

int strncmp( const char * s1, const char * s2, size_t n )
{
    while ( *s1 && n && ( *s1 == *s2 ) )
    {
        ++s1;
        ++s2;
        --n;
    }
    if ( n == 0 )
    {
        return 0;
    }
    else
    {
        return ( *(unsigned char *)s1 - *(unsigned char *)s2 );
    }
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    char cmpabcde[] = "abcde\0f";
    char cmpabcd_[] = "abcde\xfc";
    char empty[] = "";
    char x[] = "x";
    TESTCASE( strncmp( abcde, cmpabcde, 5 ) == 0 );
    TESTCASE( strncmp( abcde, cmpabcde, 10 ) == 0 );
    TESTCASE( strncmp( abcde, abcdx, 5 ) < 0 );
    TESTCASE( strncmp( abcdx, abcde, 5 ) > 0 );
    TESTCASE( strncmp( empty, abcde, 5 ) < 0 );
    TESTCASE( strncmp( abcde, empty, 5 ) > 0 );
    TESTCASE( strncmp( abcde, abcdx, 4 ) == 0 );
    TESTCASE( strncmp( abcde, x, 0 ) == 0 );
    TESTCASE( strncmp( abcde, x, 1 ) < 0 );
    TESTCASE( strncmp( abcde, cmpabcd_, 10 ) < 0 );
    return TEST_RESULTS;
}
#endif
