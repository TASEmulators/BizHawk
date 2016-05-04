/* strnlen( const char *, size_t len )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <string.h>
#include <stdint.h>

#ifndef REGTEST

size_t strnlen( const char * s, size_t maxlen )
{
    for( size_t len = 0; len != maxlen; len++ )
    {
        if(s[len] == '\0')
            return len;
    }
    return maxlen;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
#ifndef REGTEST
    TESTCASE( strnlen( abcde, 5 ) == 5 );
    TESTCASE( strnlen( abcde, 3 ) == 3 )
    TESTCASE( strnlen( "", SIZE_MAX ) == 0 );
#endif
    return TEST_RESULTS;
}
#endif
