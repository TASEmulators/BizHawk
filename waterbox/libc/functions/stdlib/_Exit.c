/* _Exit( int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdlib.h>
#include <stdio.h>

#ifndef REGTEST
#include "_PDCLIB_glue.h"

void _Exit( int status )
{
    /* TODO: Flush and close open streams. Remove tmpfile() files. Make this
       called on process termination automatically.
    */
    _PDCLIB_Exit( status );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    int UNEXPECTED_RETURN = 0;
    _Exit( 0 );
    TESTCASE( UNEXPECTED_RETURN );
    return TEST_RESULTS;
}

#endif
