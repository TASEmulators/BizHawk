/* fprintf( FILE *, const char *, ... )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdarg.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

int _PDCLIB_fprintf_unlocked( FILE * _PDCLIB_restrict stream, 
                      const char * _PDCLIB_restrict format, ... )
{
    int rc;
    va_list ap;
    va_start( ap, format );
    rc = _PDCLIB_vfprintf_unlocked( stream, format, ap );
    va_end( ap );
    return rc;
}

int fprintf( FILE * _PDCLIB_restrict stream,
             const char * _PDCLIB_restrict format, ... )
{
    int rc;
    va_list ap;
    va_start( ap, format );
    _PDCLIB_flockfile( stream );
    rc = _PDCLIB_vfprintf_unlocked( stream, format, ap );
    _PDCLIB_funlockfile( stream );
    va_end( ap );
    return rc;
}

#endif

#ifdef TEST
#include <stdint.h>
#include <stddef.h>
#define _PDCLIB_FILEID "stdio/fprintf.c"
#define _PDCLIB_FILEIO

#include "_PDCLIB_test.h"

#define testprintf( stream, ... ) fprintf( stream, __VA_ARGS__ )

int main( void )
{
    FILE * target;
    TESTCASE( ( target = tmpfile() ) != NULL );
#include "printf_testcases.h"
    TESTCASE( fclose( target ) == 0 );
    return TEST_RESULTS;
}

#endif

