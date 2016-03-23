/* ftell( FILE * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdint.h>
#include <limits.h>
#include <errno.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

long int _PDCLIB_ftell_unlocked( FILE * stream )
{
    uint_fast64_t off64 = _PDCLIB_ftell64_unlocked( stream );

    if ( off64 > LONG_MAX )
    {
        /* integer overflow */
        errno = ERANGE;
        return -1;
    }
    return off64;
}

long int ftell( FILE * stream )
{
    _PDCLIB_flockfile( stream );
    long int off = _PDCLIB_ftell_unlocked( stream );
    _PDCLIB_funlockfile( stream );
    return off;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"
#include <stdlib.h>
#ifndef REGTEST
#include "_PDCLIB_io.h"
#endif

int main( void )
{
    /* Testing all the basic I/O functions individually would result in lots
       of duplicated code, so I took the liberty of lumping it all together
       here.
    */
    /* The following functions delegate their tests to here:
       fgetc fflush rewind fputc ungetc fseek
       flushbuffer seek fillbuffer prepread prepwrite
    */
    char * buffer = (char*)malloc( 4 );
    FILE * fh;
    TESTCASE( ( fh = tmpfile() ) != NULL );
    TESTCASE( setvbuf( fh, buffer, _IOLBF, 4 ) == 0 );
    /* Testing ungetc() at offset 0 */
    rewind( fh );
    TESTCASE( ungetc( 'x', fh ) == 'x' );
    TESTCASE( ftell( fh ) == -1l );
    rewind( fh );
    TESTCASE( ftell( fh ) == 0l );
    /* Commence "normal" tests */
    TESTCASE( fputc( '1', fh ) == '1' );
    TESTCASE( fputc( '2', fh ) == '2' );
    TESTCASE( fputc( '3', fh ) == '3' );
    /* Positions incrementing as expected? */
    TESTCASE( ftell( fh ) == 3l );
    TESTCASE_NOREG( fh->pos.offset == 0l );
    TESTCASE_NOREG( fh->bufidx == 3l );
    /* Buffer properly flushed when full? */
    TESTCASE( fputc( '4', fh ) == '4' );
    TESTCASE_NOREG( fh->pos.offset == 4l );
    TESTCASE_NOREG( fh->bufidx == 0 );
    /* fflush() resetting positions as expected? */
    TESTCASE( fputc( '5', fh ) == '5' );
    TESTCASE( fflush( fh ) == 0 );
    TESTCASE( ftell( fh ) == 5l );
    TESTCASE_NOREG( fh->pos.offset == 5l );
    TESTCASE_NOREG( fh->bufidx == 0l );
    /* rewind() resetting positions as expected? */
    rewind( fh );
    TESTCASE( ftell( fh ) == 0l );
    TESTCASE_NOREG( fh->pos.offset == 0 );
    TESTCASE_NOREG( fh->bufidx == 0 );
    /* Reading back first character after rewind for basic read check */
    TESTCASE( fgetc( fh ) == '1' );
    /* TODO: t.b.c. */
    TESTCASE( fclose( fh ) == 0 );
    return TEST_RESULTS;
}

#endif

