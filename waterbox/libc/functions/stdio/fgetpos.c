/* fgetpos( FILE * , fpos_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

int _PDCLIB_fgetpos_unlocked( FILE * _PDCLIB_restrict stream, _PDCLIB_fpos_t * _PDCLIB_restrict pos )
{
    pos->offset = stream->pos.offset + stream->bufidx - stream->ungetidx;
    pos->mbs    = stream->pos.mbs;
    /* TODO: Add mbstate. */
    return 0;
}

int fgetpos( FILE * _PDCLIB_restrict stream, _PDCLIB_fpos_t * _PDCLIB_restrict pos )
{
    _PDCLIB_flockfile( stream );
    int res = _PDCLIB_fgetpos_unlocked( stream, pos );
    _PDCLIB_funlockfile( stream );
    return res;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"
#include <string.h>

int main( void )
{
    FILE * fh;
    fpos_t pos1, pos2;
    TESTCASE( ( fh = tmpfile() ) != NULL );
    TESTCASE( fgetpos( fh, &pos1 ) == 0 );
    TESTCASE( fwrite( teststring, 1, strlen( teststring ), fh ) == strlen( teststring ) );
    TESTCASE( (size_t)ftell( fh ) == strlen( teststring ) );
    TESTCASE( fgetpos( fh, &pos2 ) == 0 );
    TESTCASE( fsetpos( fh, &pos1 ) == 0 );
    TESTCASE( ftell( fh ) == 0 );
    TESTCASE( fsetpos( fh, &pos2 ) == 0 );
    TESTCASE( (size_t)ftell( fh ) == strlen( teststring ) );
    TESTCASE( fclose( fh ) == 0 );
    return TEST_RESULTS;
}

#endif
