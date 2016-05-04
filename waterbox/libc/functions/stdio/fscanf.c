/* fscanf( FILE *, const char *, ... )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdarg.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

int _PDCLIB_fscanf_unlocked( FILE * _PDCLIB_restrict stream, 
                     const char * _PDCLIB_restrict format, ... )
{
    int rc;
    va_list ap;
    va_start( ap, format );
    rc = _PDCLIB_vfscanf_unlocked( stream, format, ap );
    va_end( ap );
    return rc;
}

int fscanf( FILE * _PDCLIB_restrict stream, 
            const char * _PDCLIB_restrict format, ... )
{
    int rc;
    va_list ap;
    va_start( ap, format );
    rc = vfscanf( stream, format, ap );
    va_end( ap );
    return rc;
}

#endif

#ifdef TEST
#define _PDCLIB_FILEID "stdio/fscanf.c"
#define _PDCLIB_FILEIO

#include "_PDCLIB_test.h"

#define testscanf( stream, format, ... ) fscanf( stream, format, __VA_ARGS__ )

int main( void )
{
    FILE * source;
    TESTCASE( ( source = tmpfile() ) != NULL );
#include "scanf_testcases.h"
    TESTCASE( fclose( source ) == 0 );
    return TEST_RESULTS;
}

#endif
