/* imaxabs( intmax_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <inttypes.h>

#ifndef REGTEST

intmax_t imaxabs( intmax_t j )
{
    return ( j >= 0 ) ? j : -j;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"
#include <limits.h>

int main( void )
{
    TESTCASE( imaxabs( (intmax_t)0 ) == 0 );
    TESTCASE( imaxabs( INTMAX_MAX ) == INTMAX_MAX );
    TESTCASE( imaxabs( INTMAX_MIN + 1 ) == -( INTMAX_MIN + 1 ) );
    return TEST_RESULTS;
}

#endif
