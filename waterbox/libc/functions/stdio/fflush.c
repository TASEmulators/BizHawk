/* fflush( FILE * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

extern FILE * _PDCLIB_filelist;

int _PDCLIB_fflush_unlocked( FILE * stream )
{
    if ( stream == NULL )
    {
        stream = _PDCLIB_filelist;
        /* TODO: Check what happens when fflush( NULL ) encounters write errors, in other libs */
        int rc = 0;
        while ( stream != NULL )
        {
            if ( stream->status & _PDCLIB_FWRITE )
            {
                if ( _PDCLIB_flushbuffer( stream ) == EOF )
                {
                    rc = EOF;
                }
            }
            stream = stream->next;
        }
        return rc;
    }
    else
    {
        return _PDCLIB_flushbuffer( stream );
    }
}

int fflush( FILE * stream )
{
    _PDCLIB_flockfile( stream );
    int res = _PDCLIB_fflush_unlocked(stream);
    _PDCLIB_funlockfile( stream );
    return res;
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

