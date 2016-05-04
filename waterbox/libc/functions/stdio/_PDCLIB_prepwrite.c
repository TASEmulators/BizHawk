/* _PDCLIB_prepwrite( FILE * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <errno.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"
   
int _PDCLIB_prepwrite( FILE * stream )
{
    if ( ( stream->bufidx < stream->bufend ) || ( stream->ungetidx > 0 ) ||
         ( stream->status & ( _PDCLIB_FREAD | _PDCLIB_ERRORFLAG | _PDCLIB_WIDESTREAM | _PDCLIB_EOFFLAG ) ) ||
         ! ( stream->status & ( _PDCLIB_FWRITE | _PDCLIB_FAPPEND | _PDCLIB_FRW ) ) )
    {
        /* Function called on illegal (e.g. input) stream.
           See the comments on implementation-defined errno values in
           <_PDCLIB_config.h>.
        */
        errno = EINVAL;
        stream->status |= _PDCLIB_ERRORFLAG;
        return EOF;
    }
    stream->status |= _PDCLIB_FWRITE | _PDCLIB_BYTESTREAM;
    return 0;
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

