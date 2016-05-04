/* fseek( FILE *, long, int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

int _PDCLIB_fseek_unlocked( FILE * stream, long loffset, int whence )
{
    _PDCLIB_int64_t offset = loffset;
    if ( stream->status & _PDCLIB_FWRITE )
    {
        if ( _PDCLIB_flushbuffer( stream ) == EOF )
        {
            return EOF;
        }
    }
    stream->status &= ~ _PDCLIB_EOFFLAG;
    if ( stream->status & _PDCLIB_FRW )
    {
        stream->status &= ~ ( _PDCLIB_FREAD | _PDCLIB_FWRITE );
    }

    if ( whence == SEEK_CUR )
    {
        whence  = SEEK_SET;
        offset += _PDCLIB_ftell64_unlocked( stream );
    }

    return ( _PDCLIB_seek( stream, offset, whence ) != EOF ) ? 0 : EOF;
}

int fseek( FILE * stream, long loffset, int whence )
{
    _PDCLIB_flockfile( stream );
    int r = _PDCLIB_fseek_unlocked( stream, loffset, whence );
    _PDCLIB_funlockfile( stream );
    return r;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"
#include <string.h>

int main( void )
{
    FILE * fh;
    TESTCASE( ( fh = tmpfile() ) != NULL );
    TESTCASE( fwrite( teststring, 1, strlen( teststring ), fh ) == strlen( teststring ) );
    /* General functionality */
    TESTCASE( fseek( fh, -1, SEEK_END ) == 0  );
    TESTCASE( (size_t)ftell( fh ) == strlen( teststring ) - 1 );
    TESTCASE( fseek( fh, 0, SEEK_END ) == 0 );
    TESTCASE( (size_t)ftell( fh ) == strlen( teststring ) );
    TESTCASE( fseek( fh, 0, SEEK_SET ) == 0 );
    TESTCASE( ftell( fh ) == 0 );
    TESTCASE( fseek( fh, 5, SEEK_CUR ) == 0 );
    TESTCASE( ftell( fh ) == 5 );
    TESTCASE( fseek( fh, -3, SEEK_CUR ) == 0 );
    TESTCASE( ftell( fh ) == 2 );
    /* Checking behaviour around EOF */
    TESTCASE( fseek( fh, 0, SEEK_END ) == 0 );
    TESTCASE( ! feof( fh ) );
    TESTCASE( fgetc( fh ) == EOF );
    TESTCASE( feof( fh ) );
    TESTCASE( fseek( fh, 0, SEEK_END ) == 0 );
    TESTCASE( ! feof( fh ) );
    /* Checking undo of ungetc() */
    TESTCASE( fseek( fh, 0, SEEK_SET ) == 0 );
    TESTCASE( fgetc( fh ) == teststring[0] );
    TESTCASE( fgetc( fh ) == teststring[1] );
    TESTCASE( fgetc( fh ) == teststring[2] );
    TESTCASE( ftell( fh ) == 3 );
    TESTCASE( ungetc( teststring[2], fh ) == teststring[2] );
    TESTCASE( ftell( fh ) == 2 );
    TESTCASE( fgetc( fh ) == teststring[2] );
    TESTCASE( ftell( fh ) == 3 );
    TESTCASE( ungetc( 'x', fh ) == 'x' );
    TESTCASE( ftell( fh ) == 2 );
    TESTCASE( fgetc( fh ) == 'x' );
    TESTCASE( ungetc( 'x', fh ) == 'x' );
    TESTCASE( ftell( fh ) == 2 );
    TESTCASE( fseek( fh, 2, SEEK_SET ) == 0 );
    TESTCASE( fgetc( fh ) == teststring[2] );
    /* PDCLIB-7: Check that we handle the underlying file descriptor correctly
     *           in the SEEK_CUR case */
    TESTCASE( fseek( fh, 10, SEEK_SET ) == 0 );
    TESTCASE( ftell( fh ) == 10l );
    TESTCASE( fseek( fh, 0, SEEK_CUR ) == 0 );
    TESTCASE( ftell( fh ) == 10l );
    TESTCASE( fseek( fh, 2, SEEK_CUR ) == 0 );
    TESTCASE( ftell( fh ) == 12l );
    TESTCASE( fseek( fh, -1, SEEK_CUR ) == 0 );
    TESTCASE( ftell( fh ) == 11l );
    /* Checking error handling */
    TESTCASE( fseek( fh, -5, SEEK_SET ) == -1 );
    TESTCASE( fseek( fh, 0, SEEK_END ) == 0 );
    TESTCASE( fclose( fh ) == 0 );
    return TEST_RESULTS;
}

#endif

