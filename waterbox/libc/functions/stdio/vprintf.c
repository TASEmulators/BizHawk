/* vprintf( const char *, va_list )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdarg.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

int _PDCLIB_vprintf_unlocked( const char * _PDCLIB_restrict format, 
                              _PDCLIB_va_list arg )
{
    return _PDCLIB_vfprintf_unlocked( stdout, format, arg );
}

int vprintf( const char * _PDCLIB_restrict format, _PDCLIB_va_list arg )
{
    return vfprintf( stdout, format, arg );
}

#endif

#ifdef TEST
#define _PDCLIB_FILEID "stdio/vprintf.c"
#define _PDCLIB_FILEIO
#include <stdint.h>
#include <stddef.h>
#include "_PDCLIB_test.h"

static int testprintf( FILE * stream, const char * format, ... )
{
    int i;
    va_list arg;
    va_start( arg, format );
    i = vprintf( format, arg );
    va_end( arg );
    return i;
}

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
