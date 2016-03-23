/* sscanf( const char *, const char *, ... )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdarg.h>

#ifndef REGTEST

int sscanf( const char * _PDCLIB_restrict s, const char * _PDCLIB_restrict format, ... )
{
    int rc;
    va_list ap;
    va_start( ap, format );
    rc = vsscanf( s, format, ap );
    va_end( ap );
    return rc;
}

#endif

#ifdef TEST
#define _PDCLIB_FILEID "stdio/sscanf.c"
#define _PDCLIB_STRINGIO

#include "_PDCLIB_test.h"

#define testscanf( s, format, ... ) sscanf( s, format, __VA_ARGS__ )

int main( void )
{
    char source[100];
#include "scanf_testcases.h"
    return TEST_RESULTS;
}

#endif

