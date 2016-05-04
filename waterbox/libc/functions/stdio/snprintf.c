/* snprintf( char *, size_t, const char *, ... )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdarg.h>

#ifndef REGTEST

int snprintf( char * _PDCLIB_restrict s, size_t n, const char * _PDCLIB_restrict format, ...)
{
    int rc;
    va_list ap;
    va_start( ap, format );
    rc = vsnprintf( s, n, format, ap );
    va_end( ap );
    return rc;
}

#endif

#ifdef TEST
#define _PDCLIB_FILEID "stdio/snprintf.c"
#define _PDCLIB_STRINGIO
#include <stdint.h>
#include <stddef.h>

#include "_PDCLIB_test.h"

#define testprintf( s, ... ) snprintf( s, 100, __VA_ARGS__ )

int main( void )
{
    char target[100];
#include "printf_testcases.h"
    TESTCASE( snprintf( NULL, 0, "foo" ) == 3 );
    TESTCASE( snprintf( NULL, 0, "%d", 100 ) == 3 );
    return TEST_RESULTS;
}

#endif
