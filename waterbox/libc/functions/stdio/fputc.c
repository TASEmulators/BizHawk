/* fputc( int, FILE * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

/* Write the value c (cast to unsigned char) to the given stream.
   Returns c if successful, EOF otherwise.
   If a write error occurs, the error indicator of the stream is set.
*/
int _PDCLIB_fputc_unlocked( int c, FILE * stream )
{
    if ( _PDCLIB_prepwrite( stream ) == EOF )
    {
        return EOF;
    }
    stream->buffer[stream->bufidx++] = (char)c;
    if ( ( stream->bufidx == stream->bufsize )                   /* _IOFBF */
           || ( ( stream->status & _IOLBF ) && ( (char)c == '\n' ) ) /* _IOLBF */
           || ( stream->status & _IONBF )                        /* _IONBF */
    )
    {
        /* buffer filled, unbuffered stream, or end-of-line. */
        return ( _PDCLIB_flushbuffer( stream ) == 0 ) ? c : EOF;
    }
    return c;
}

int fputc( int c, FILE * stream )
{
    _PDCLIB_flockfile( stream );
    int r = _PDCLIB_fputc_unlocked( c, stream );
    _PDCLIB_funlockfile( stream );
    return r;
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
