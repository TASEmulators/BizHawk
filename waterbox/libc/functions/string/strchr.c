/* strchr( const char *, int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <string.h>

#ifndef REGTEST

char * strchr( const char * s, int c )
{
    do
    {
        if ( *s == (char) c )
        {
            return (char *) s;
        }
    } while ( *s++ );
    return NULL;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    char abccd[] = "abccd";
    TESTCASE( strchr( abccd, 'x' ) == NULL );
    TESTCASE( strchr( abccd, 'a' ) == &abccd[0] );
    TESTCASE( strchr( abccd, 'd' ) == &abccd[4] );
    TESTCASE( strchr( abccd, '\0' ) == &abccd[5] );
    TESTCASE( strchr( abccd, 'c' ) == &abccd[2] );
    return TEST_RESULTS;
}
#endif
