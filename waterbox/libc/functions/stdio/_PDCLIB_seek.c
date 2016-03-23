/* int64_t _PDCLIB_seek( FILE *, int64_t, int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdint.h>
#include <errno.h>
#ifndef REGTEST
#include "_PDCLIB_io.h"

int_fast64_t _PDCLIB_seek( FILE * stream, 
                           int_fast64_t offset, 
                           int whence )
{
    int_fast64_t newPos;
    if(!stream->ops->seek(stream->handle, offset, whence, &newPos)) {
        return EOF;
    }

    stream->ungetidx = 0;
    stream->bufidx = 0;
    stream->bufend = 0;
    stream->pos.offset = newPos;
    return newPos;
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

