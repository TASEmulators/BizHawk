/* setbuf( FILE *, char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>

#ifndef REGTEST

void setbuf( FILE * _PDCLIB_restrict stream, char * _PDCLIB_restrict buf )
{
    if ( buf == NULL )
    {
        setvbuf( stream, buf, _IONBF, BUFSIZ );
    }
    else
    {
        setvbuf( stream, buf, _IOFBF, BUFSIZ );
    }
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
    /* TODO: Extend testing once setvbuf() is finished. */
#ifndef REGTEST
    char buffer[ BUFSIZ + 1 ];
    FILE * fh;
    /* full buffered */
    TESTCASE( ( fh = tmpfile() ) != NULL );
    setbuf( fh, buffer );
    TESTCASE( fh->buffer == buffer );
    TESTCASE( fh->bufsize == BUFSIZ );
    TESTCASE( ( fh->status & ( _IOFBF | _IONBF | _IOLBF ) ) == _IOFBF );
    TESTCASE( fclose( fh ) == 0 );
    /* not buffered */
    TESTCASE( ( fh = tmpfile() ) != NULL );
    setbuf( fh, NULL );
    TESTCASE( ( fh->status & ( _IOFBF | _IONBF | _IOLBF ) ) == _IONBF );
    TESTCASE( fclose( fh ) == 0 );
#else
    puts( " NOTEST setbuf() test driver is PDCLib-specific." );
#endif
    return TEST_RESULTS;
}

#endif
