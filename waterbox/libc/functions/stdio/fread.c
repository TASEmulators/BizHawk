/* fwrite( void *, size_t, size_t, FILE * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

#include <stdbool.h>
#include <string.h>

size_t _PDCLIB_fread_unlocked( 
    void * _PDCLIB_restrict ptr, 
    size_t size, size_t nmemb, 
    FILE * _PDCLIB_restrict stream 
)
{
    if ( _PDCLIB_prepread( stream ) == EOF )
    {
        return 0;
    }
    char * dest = (char *)ptr;
    size_t nmemb_i;
    for ( nmemb_i = 0; nmemb_i < nmemb; ++nmemb_i )
    {
        size_t numread = _PDCLIB_getchars( &dest[ nmemb_i * size ], size, EOF, 
                                           stream );
        if( numread != size )
            break;
    }
    return nmemb_i;
}

size_t fread( void * _PDCLIB_restrict ptr, 
              size_t size, size_t nmemb, 
              FILE * _PDCLIB_restrict stream )
{
    _PDCLIB_flockfile( stream );
    size_t r = _PDCLIB_fread_unlocked( ptr, size, nmemb, stream );
    _PDCLIB_funlockfile( stream );
    return r;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    FILE * fh;
    char const * message = "Testing fwrite()...\n";
    char buffer[21];
    buffer[20] = 'x';
    TESTCASE( ( fh = tmpfile() ) != NULL );
    /* fwrite() / readback */
    TESTCASE( fwrite( message, 1, 20, fh ) == 20 );
    rewind( fh );
    TESTCASE( fread( buffer, 1, 20, fh ) == 20 );
    TESTCASE( memcmp( buffer, message, 20 ) == 0 );
    TESTCASE( buffer[20] == 'x' );
    /* same, different nmemb / size settings */
    rewind( fh );
    TESTCASE( memset( buffer, '\0', 20 ) == buffer );
    TESTCASE( fwrite( message, 5, 4, fh ) == 4 );
    rewind( fh );
    TESTCASE( fread( buffer, 5, 4, fh ) == 4 );
    TESTCASE( memcmp( buffer, message, 20 ) == 0 );
    TESTCASE( buffer[20] == 'x' );
    /* same... */
    rewind( fh );
    TESTCASE( memset( buffer, '\0', 20 ) == buffer );
    TESTCASE( fwrite( message, 20, 1, fh ) == 1 );
    rewind( fh );
    TESTCASE( fread( buffer, 20, 1, fh ) == 1 );
    TESTCASE( memcmp( buffer, message, 20 ) == 0 );
    TESTCASE( buffer[20] == 'x' );
    /* Done. */
    TESTCASE( fclose( fh ) == 0 );
    return TEST_RESULTS;
}

#endif

