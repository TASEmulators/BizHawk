/* _PDCLIB_fillbuffer( FILE * stream )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>

#ifndef REGTEST
#include "_PDCLIB_glue.h"
#include "_PDCLIB_io.h"

int _PDCLIB_fillbuffer( FILE * stream )
{
    size_t bytesRead;
    bool ok = stream->ops->read( stream->handle, stream->buffer, stream->bufsize,
                        &bytesRead);

    if( ok ) {
        if( bytesRead == 0 ) {
            stream->status |= _PDCLIB_EOFFLAG;
            return EOF;
        }
        stream->pos.offset += bytesRead;
        stream->bufend = bytesRead;
        stream->bufidx = 0;
        return 0;
    } else {
        stream->status |= _PDCLIB_ERRORFLAG;
        return EOF;
    }
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

