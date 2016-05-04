/* _PDCLIB_flushbuffer( struct _PDCLIB_file_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <string.h>

#ifndef REGTEST
#include "_PDCLIB_glue.h"
#include "_PDCLIB_io.h"


static int flushsubbuffer( FILE * stream, size_t length )
{
    size_t justWrote;
    size_t written = 0;
    int rv = 0;

#if 0
    // Very useful for debugging buffering issues
    char l = '<', r = '>';
    stream->ops->write( stream->handle, &l,  1, &justWrote );
#endif

    while( written != length )
    {
        size_t toWrite = length - written;

        bool res = stream->ops->write( stream->handle, stream->buffer + written,
                              toWrite, &justWrote);
        written += justWrote;
        stream->pos.offset += justWrote;

        if (!res)
        {
            stream->status |= _PDCLIB_ERRORFLAG;
            rv = EOF;
            break;
        }
    }

#if 0
    stream->ops->write( stream->handle, &r,  1, &justWrote );
#endif

    stream->bufidx   -= written;
#ifdef _PDCLIB_NEED_EOL_TRANSLATION
    stream->bufnlexp -= written;
#endif
    memmove( stream->buffer, stream->buffer + written, stream->bufidx );

    return rv;
}

int _PDCLIB_flushbuffer( FILE * stream )
{
#ifdef _PDCLIB_NEED_EOL_TRANSLATION
    // if a text stream, and this platform needs EOL translation, well...
    if ( ! ( stream->status & _PDCLIB_FBIN ) )
    {
        // Special case: buffer is full and we start with a \n
        if ( stream->bufnlexp == 0
            && stream->bufidx == stream->bufend
            && stream->buffer[0] == '\n' )
        {
            char cr = '\r';
            size_t written = 0;
            bool res = stream->ops->write( stream->handle, &cr, 1, &written );

            if (!res) {
                stream->status |= _PDCLIB_ERRORFLAG;
                return EOF;
            }

        }

        for ( ; stream->bufnlexp < stream->bufidx; stream->bufnlexp++ )
        {
            if (stream->buffer[stream->bufnlexp] == '\n' ) {
                if ( stream->bufidx == stream->bufend ) {
                    // buffer is full. Need to print out everything up till now
                    if( flushsubbuffer( stream, stream->bufnlexp - 1 ) )
                    {
                        return EOF;
                    }
                }

                // we have spare space in buffer. Shift everything 1char and
                // insert \r
                memmove( &stream->buffer[stream->bufnlexp + 1],
                         &stream->buffer[stream->bufnlexp],
                         stream->bufidx - stream->bufnlexp );
                stream->buffer[stream->bufnlexp] = '\r';

                stream->bufnlexp++;
                stream->bufidx++;
            }
        }
    }
#endif
    return flushsubbuffer( stream, stream->bufidx );
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

