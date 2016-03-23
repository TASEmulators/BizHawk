/* strlen( const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <string.h>

#ifndef REGTEST

size_t strlen( const char * s )
{
    size_t rc = 0;
    while ( s[rc] )
    {
        ++rc;
    }
    return rc;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( strlen( abcde ) == 5 );
    TESTCASE( strlen( "" ) == 0 );
    return TEST_RESULTS;
}
#endif
