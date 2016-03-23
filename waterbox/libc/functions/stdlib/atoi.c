/* atoi( const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdlib.h>

#ifndef REGTEST

int atoi( const char * s )
{
    return (int) _PDCLIB_atomax( s );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    /* no tests for a simple wrapper */
    return TEST_RESULTS;
}

#endif
