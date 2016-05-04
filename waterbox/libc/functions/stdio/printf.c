/* printf( const char *, ... )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdarg.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

int printf( const char * _PDCLIB_restrict format, ... )
{
    int rc;
    va_list ap;
    va_start( ap, format );
    rc = vfprintf( stdout, format, ap );
    va_end( ap );
    return rc;
}

int _PDCLIB_printf_unlocked( const char * _PDCLIB_restrict format, ... )
{
    int rc;
    va_list ap;
    va_start( ap, format );
    rc = _PDCLIB_vfprintf_unlocked( stdout, format, ap );
    va_end( ap );
    return rc;
}

#endif

#ifdef TEST
#define _PDCLIB_FILEID "stdio/printf.c"
#define _PDCLIB_FILEIO
#include <stdint.h>
#include <stddef.h>

#include "_PDCLIB_test.h"

#define testprintf( stream, ... ) printf( __VA_ARGS__ )

int main( void )
{
    FILE * target;
    TESTCASE( ( target = freopen( testfile, "wb+", stdout ) ) != NULL );
#include "printf_testcases.h"
    TESTCASE( fclose( target ) == 0 );
    TESTCASE( remove( testfile ) == 0 );
    return TEST_RESULTS;
}

#endif
