/* div( int, int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdlib.h>

#ifndef REGTEST

div_t div( int numer, int denom )
{
    div_t rc;
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
    div_t result;
    result = div( 5, 2 );
    TESTCASE( result.quot == 2 && result.rem == 1 );
    result = div( -5, 2 );
    TESTCASE( result.quot == -2 && result.rem == -1 );
    result = div( 5, -2 );
    TESTCASE( result.quot == -2 && result.rem == 1 );
    TESTCASE( sizeof( result.quot ) == sizeof( int ) );
    TESTCASE( sizeof( result.rem )  == sizeof( int ) );
    return TEST_RESULTS;
}

#endif
