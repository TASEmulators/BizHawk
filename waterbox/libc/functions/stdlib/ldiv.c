/* ldiv( long int, long int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdlib.h>

#ifndef REGTEST

ldiv_t ldiv( long int numer, long int denom )
{
    ldiv_t rc;
    rc.quot = numer / denom;
    rc.rem  = numer % denom;
    /* TODO: pre-C99 compilers might require modulus corrections */
    return rc;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    ldiv_t result;
    result = ldiv( 5, 2 );
    TESTCASE( result.quot == 2 && result.rem == 1 );
    result = ldiv( -5, 2 );
    TESTCASE( result.quot == -2 && result.rem == -1 );
    result = ldiv( 5, -2 );
    TESTCASE( result.quot == -2 && result.rem == 1 );
    TESTCASE( sizeof( result.quot ) == sizeof( long ) );
    TESTCASE( sizeof( result.rem )  == sizeof( long ) );
    return TEST_RESULTS;
}

#endif
