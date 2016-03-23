/* getenv( const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

/* This is a stub implementation of getenv
*/

#include <string.h>
#include <stdlib.h>

#ifndef REGTEST

char * getenv( const char * name )
{
    return NULL;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    return TEST_RESULTS;
}

#endif
