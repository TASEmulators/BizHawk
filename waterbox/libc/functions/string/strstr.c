/* strstr( const char *, const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <string.h>

#ifndef REGTEST

char * strstr( const char * s1, const char * s2 )
{
    const char * p1 = s1;
    const char * p2;
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
            return (char *) s1;
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
    char s[] = "abcabcabcdabcde";
    TESTCASE( strstr( s, "x" ) == NULL );
    TESTCASE( strstr( s, "xyz" ) == NULL );
    TESTCASE( strstr( s, "a" ) == &s[0] );
    TESTCASE( strstr( s, "abc" ) == &s[0] );
    TESTCASE( strstr( s, "abcd" ) == &s[6] );
    TESTCASE( strstr( s, "abcde" ) == &s[10] );
    return TEST_RESULTS;
}
#endif
