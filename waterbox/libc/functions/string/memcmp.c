/* memcmp( const void *, const void *, size_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <string.h>

#ifndef REGTEST

int memcmp( const void * s1, const void * s2, size_t n )
{
    const unsigned char * p1 = (const unsigned char *) s1;
    const unsigned char * p2 = (const unsigned char *) s2;
    while ( n-- )
    {
        if ( *p1 != *p2 )
        {
            return *p1 - *p2;
        }
        ++p1;
        ++p2;
    }
    return 0;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    char const xxxxx[] = "xxxxx";
    TESTCASE( memcmp( abcde, abcdx, 5 ) < 0 );
    TESTCASE( memcmp( abcde, abcdx, 4 ) == 0 );
    TESTCASE( memcmp( abcde, xxxxx, 0 ) == 0 );
    TESTCASE( memcmp( xxxxx, abcde, 1 ) > 0 );
    return 0;
}
#endif
