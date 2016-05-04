/* _PDCLIB_seed

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

unsigned long int _PDCLIB_seed = 1;

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    /* no tests for raw data */
    return TEST_RESULTS;
}

#endif
