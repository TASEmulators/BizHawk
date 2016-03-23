/* puts( const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

extern char * _PDCLIB_eol;

int _PDCLIB_puts_unlocked( const char * s )
{
    if ( _PDCLIB_prepwrite( stdout ) == EOF )
    {
        return EOF;
    }
    while ( *s != '\0' )
    {
        stdout->buffer[ stdout->bufidx++ ] = *s++;
        if ( stdout->bufidx == stdout->bufsize )
        {
            if ( _PDCLIB_flushbuffer( stdout ) == EOF )
            {
                return EOF;
            }
        }
    }
    stdout->buffer[ stdout->bufidx++ ] = '\n';
    if ( ( stdout->bufidx == stdout->bufsize ) ||
         ( stdout->status & ( _IOLBF | _IONBF ) ) )
    {
        return _PDCLIB_flushbuffer( stdout );
    }
    else
    {
        return 0;
    }
}

int puts( const char * s )
{
    _PDCLIB_flockfile( stdout );
    int r = _PDCLIB_puts_unlocked( s );
    _PDCLIB_funlockfile( stdout );
    return r;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    FILE * fh;
    char const * message = "SUCCESS testing puts()";
    char buffer[23];
    buffer[22] = 'x';
    TESTCASE( ( fh = freopen( testfile, "wb+", stdout ) ) != NULL );
    TESTCASE( puts( message ) >= 0 );
    rewind( fh );
    TESTCASE( fread( buffer, 1, 22, fh ) == 22 );
    TESTCASE( memcmp( buffer, message, 22 ) == 0 );
    TESTCASE( buffer[22] == 'x' );
    TESTCASE( fclose( fh ) == 0 );
    TESTCASE( remove( testfile ) == 0 );
    return TEST_RESULTS;
}

#endif

