/* vscanf( const char *, va_list )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdarg.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

int _PDCLIB_vscanf_unlocked( const char * _PDCLIB_restrict format,
                             _PDCLIB_va_list arg )
{
    return _PDCLIB_vfscanf_unlocked( stdin, format, arg );
}

int vscanf( const char * _PDCLIB_restrict format, _PDCLIB_va_list arg )
{
    return vfscanf( stdin, format, arg );
}

#endif

#ifdef TEST
#define _PDCLIB_FILEID "stdio/vscanf.c"
#define _PDCLIB_FILEIO

#include "_PDCLIB_test.h"

static int testscanf( FILE * stream, const char * format, ... )
{
    int i;
    va_list arg;
    va_start( arg, format );
    i = vscanf( format, arg );
    va_end( arg );
    return i;
}

int main( void )
{
    FILE * source;
    TESTCASE( ( source = freopen( testfile, "wb+", stdin ) ) != NULL );
#include "scanf_testcases.h"
    TESTCASE( fclose( source ) == 0 );
    TESTCASE( remove( testfile ) == 0 );
    return TEST_RESULTS;
}

#endif
