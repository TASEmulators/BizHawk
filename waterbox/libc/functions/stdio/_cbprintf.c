/* _cbprintf( void *, size_t (*)( void *, const char *, size_t ), const char *, ... )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdint.h>
#include <stdarg.h>

#ifndef REGTEST

int _cbprintf(
    void * p,
    size_t (*cb)( void*, const char*, size_t ),
    const char * _PDCLIB_restrict format,
    ...)
{
    int rc;
    va_list ap;
    va_start( ap, format );
    rc = _vcbprintf( p, cb, format, ap );
    va_end( ap );
    return rc;
}

#endif

#ifdef TEST
#define _PDCLIB_FILEID "stdio/sprintf.c"
#define _PDCLIB_STRINGIO
#include <stddef.h>

#include "_PDCLIB_test.h"

static char * bufptr;
static size_t testcb( void *p, const char *buf, size_t size )
{
    memcpy(bufptr, buf, size);
    bufptr += size;
    *bufptr = '\0';
    return size;
}

#define testprintf( s, ... ) _cbprintf( bufptr = s, testcb, __VA_ARGS__ )

int main( void )
{
#ifndef REGTEST
    char target[100];
#include "printf_testcases.h"
#endif
    return TEST_RESULTS;
}

#endif
