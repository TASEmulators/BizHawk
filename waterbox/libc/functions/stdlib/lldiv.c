/* lldiv( long long int, long long int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdlib.h>

#ifndef REGTEST

lldiv_t lldiv( long long int numer, long long int denom )
{
    lldiv_t rc;
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
    lldiv_t result;
    result = lldiv( 5ll, 2ll );
    TESTCASE( result.quot == 2 && result.rem == 1 );
    result = lldiv( -5ll, 2ll );
    TESTCASE( result.quot == -2 && result.rem == -1 );
    result = lldiv( 5ll, -2ll );
    TESTCASE( result.quot == -2 && result.rem == 1 );
    TESTCASE( sizeof( result.quot ) == sizeof( long long ) );
    TESTCASE( sizeof( result.rem )  == sizeof( long long ) );
    return TEST_RESULTS;
}

#endif
