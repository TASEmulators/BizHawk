/* lldiv( long long int, long long int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <inttypes.h>

#ifndef REGTEST

imaxdiv_t imaxdiv( intmax_t numer, intmax_t denom )
{
    imaxdiv_t rc;
    rc.quot = numer / denom;
    rc.rem  = numer % denom;
    return rc;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    imaxdiv_t result;
    result = imaxdiv( (intmax_t)5, (intmax_t)2 );
    TESTCASE( result.quot == 2 && result.rem == 1 );
    result = imaxdiv( (intmax_t)-5, (intmax_t)2 );
    TESTCASE( result.quot == -2 && result.rem == -1 );
    result = imaxdiv( (intmax_t)5, (intmax_t)-2 );
    TESTCASE( result.quot == -2 && result.rem == 1 );
    TESTCASE( sizeof( result.quot ) == sizeof( intmax_t ) );
    TESTCASE( sizeof( result.rem )  == sizeof( intmax_t ) );
    return TEST_RESULTS;
}

#endif
