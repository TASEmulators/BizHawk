/* quick_exit( int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdlib.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

/* TODO - "except that a function is called after any previously registered
   functions that had already been called at the time it was registered.
*/

/* TODO: 32 is guaranteed. This should be dynamic but ATM gives problems
   with my malloc.
*/
#define NUMBER_OF_SLOTS 40

void (*_PDCLIB_quickexitstack[ NUMBER_OF_SLOTS ])( void ) = { 0 };
size_t _PDCLIB_quickexitptr = NUMBER_OF_SLOTS;

void quick_exit( int status )
{
    while ( _PDCLIB_quickexitptr < NUMBER_OF_SLOTS )
    {
        _PDCLIB_quickexitstack[ _PDCLIB_quickexitptr++ ]();
    }
    _Exit( status );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    /* Unwinding of regstack tested in at_quick_exit(). */
    return TEST_RESULTS;
}

#endif
