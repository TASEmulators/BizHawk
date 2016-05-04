/* srand( unsigned int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdlib.h>

#ifndef REGTEST

void srand( unsigned int seed )
{
    _PDCLIB_seed = seed;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    /* tested in rand.c */
    return TEST_RESULTS;
}

#endif
