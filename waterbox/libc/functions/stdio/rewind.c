/* rewind( FILE * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

void _PDCLIB_rewind_unlocked( FILE * stream )
{
    stream->status &= ~ _PDCLIB_ERRORFLAG;
    _PDCLIB_fseek_unlocked( stream, 0L, SEEK_SET );
}

void rewind( FILE * stream )
{
    _PDCLIB_flockfile(stream);
    _PDCLIB_rewind_unlocked(stream);
    _PDCLIB_funlockfile(stream);
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    /* Testing covered by ftell.c */
    return TEST_RESULTS;
}

#endif
