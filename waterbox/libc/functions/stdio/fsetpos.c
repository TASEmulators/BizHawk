/* fsetpos( FILE *, const fpos_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

int _PDCLIB_fsetpos_unlocked( FILE * stream, 
                      const _PDCLIB_fpos_t * pos )
{
    if ( stream->status & _PDCLIB_FWRITE )
    {
        if ( _PDCLIB_flushbuffer( stream ) == EOF )
        {
            return EOF;
        }
    }
    if ( _PDCLIB_seek( stream, pos->offset, SEEK_SET ) == EOF )
    {
        return EOF;
    }
    stream->pos.mbs = pos->mbs;
    
    return 0;
}

int fsetpos( FILE * stream, 
             const _PDCLIB_fpos_t * pos )
{
    _PDCLIB_flockfile( stream );
    int res = _PDCLIB_fsetpos_unlocked( stream, pos );
    _PDCLIB_funlockfile( stream );
    return res;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    /* fsetpos() tested together with fsetpos(). */
    return TEST_RESULTS;
}

#endif
