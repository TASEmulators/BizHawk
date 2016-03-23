/* vfprintf( FILE *, const char *, va_list )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdarg.h>
#include <stdint.h>
#include <limits.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

static size_t filecb(void *p, const char *buf, size_t size)
{
    return _PDCLIB_fwrite_unlocked( buf, 1, size, (FILE*) p );
}

int _PDCLIB_vfprintf_unlocked( FILE * _PDCLIB_restrict stream,
                       const char * _PDCLIB_restrict format,
                       va_list arg )
{
    return _vcbprintf(stream, filecb, format, arg);
}

int vfprintf( FILE * _PDCLIB_restrict stream,
              const char * _PDCLIB_restrict format,
              va_list arg )
{
    _PDCLIB_flockfile( stream );
    int r = _PDCLIB_vfprintf_unlocked( stream, format, arg );
    _PDCLIB_funlockfile( stream );
    return r;
}

#endif

#ifdef TEST
#define _PDCLIB_FILEID "stdio/vfprintf.c"
#define _PDCLIB_FILEIO
#include <stddef.h>
#include "_PDCLIB_test.h"

static int testprintf( FILE * stream, const char * format, ... )
{
    int i;
    va_list arg;
    va_start( arg, format );
    i = vfprintf( stream, format, arg );
    va_end( arg );
    return i;
}

int main( void )
{
    FILE * target;
    TESTCASE( ( target = tmpfile() ) != NULL );
#include "printf_testcases.h"
    TESTCASE( fclose( target ) == 0 );
    return TEST_RESULTS;
}

#endif
