/* rand( void )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdlib.h>

#ifndef REGTEST

int rand( void )
{
    _PDCLIB_seed = _PDCLIB_seed * 1103515245 + 12345;
    return (int)( _PDCLIB_seed / 65536 ) % 32768;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    int rnd1, rnd2;
    TESTCASE( ( rnd1 = rand() ) < RAND_MAX );
    TESTCASE( ( rnd2 = rand() ) < RAND_MAX );
    srand( 1 );
    TESTCASE( rand() == rnd1 );
    TESTCASE( rand() == rnd2 );
    return TEST_RESULTS;
}

#endif
