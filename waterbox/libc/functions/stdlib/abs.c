/* abs( int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdlib.h>

#ifndef REGTEST

int abs( int j )
{
    return ( j >= 0 ) ? j : -j;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"
#include <limits.h>

int main( void )
{
    TESTCASE( abs( 0 ) == 0 );
    TESTCASE( abs( INT_MAX ) == INT_MAX );
    TESTCASE( abs( INT_MIN + 1 ) == -( INT_MIN + 1 ) );
    return TEST_RESULTS;
}

#endif
