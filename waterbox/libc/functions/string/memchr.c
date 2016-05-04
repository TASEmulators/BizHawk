/* memchr( const void *, int, size_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <string.h>

#ifndef REGTEST

void * memchr( const void * s, int c, size_t n )
{
    const unsigned char * p = (const unsigned char *) s;
    while ( n-- )
    {
        if ( *p == (unsigned char) c )
        {
            return (void *) p;
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
    TESTCASE( memchr( abcde, 'c', 5 ) == &abcde[2] );
    TESTCASE( memchr( abcde, 'a', 1 ) == &abcde[0] );
    TESTCASE( memchr( abcde, 'a', 0 ) == NULL );
    TESTCASE( memchr( abcde, '\0', 5 ) == NULL );
    TESTCASE( memchr( abcde, '\0', 6 ) == &abcde[5] );
    return TEST_RESULTS;
}

#endif
