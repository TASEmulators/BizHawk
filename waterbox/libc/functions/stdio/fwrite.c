/* fwrite( const void *, size_t, size_t, FILE * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"
#include "_PDCLIB_glue.h"

#include <stdbool.h>
#include <string.h>

size_t _PDCLIB_fwrite_unlocked( const void *restrict vptr,
               size_t size, size_t nmemb,
               FILE * _PDCLIB_restrict stream )
{
    if ( _PDCLIB_prepwrite( stream ) == EOF )
    {
        return 0;
    }

    const char *restrict ptr = vptr;
    size_t nmemb_i;
    for ( nmemb_i = 0; nmemb_i < nmemb; ++nmemb_i )
    {
        for ( size_t size_i = 0; size_i < size; ++size_i )
        {
            char c = ptr[ nmemb_i * size + size_i ];
            stream->buffer[ stream->bufidx++ ] = c;

            if ( stream->bufidx == stream->bufsize || ( c == '\n' && stream->status & _IOLBF ) )
            {
                if ( _PDCLIB_flushbuffer( stream ) == EOF )
                {
                    /* Returning number of objects completely buffered */
                    return nmemb_i;
                }
            }
        }

        if ( stream->status & _IONBF )
        {
            if ( _PDCLIB_flushbuffer( stream ) == EOF )
            {
                /* Returning number of objects completely buffered */
                return nmemb_i;
            }
        }
    }
    return nmemb_i;
}

size_t fwrite( const void * _PDCLIB_restrict ptr,
               size_t size, size_t nmemb,
               FILE * _PDCLIB_restrict stream )
{
    _PDCLIB_flockfile( stream );
    size_t r = _PDCLIB_fwrite_unlocked( ptr, size, nmemb, stream );
    _PDCLIB_funlockfile( stream );
    return r;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    /* Testing covered by fread(). */
    return TEST_RESULTS;
}

#endif

