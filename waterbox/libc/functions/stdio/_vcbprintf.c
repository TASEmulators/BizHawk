/* vsnprintf( char *, size_t, const char *, va_list )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdarg.h>
#include <stdbool.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

/* returns true if callback-based output succeeded; else false */
static inline bool cbout(
    struct _PDCLIB_status_t * status,
    const char *buf,
    size_t size )
{
    size_t rv = status->write( status->ctx, buf, size );
    status->i += rv;
    return rv == size;
}

int _vcbprintf(
    void *p,
    size_t ( *cb ) ( void *p, const char *buf, size_t size ),
    const char *format,
    va_list arg )
{
    struct _PDCLIB_status_t status;
    status.base     = 0;
    status.flags    = 0;
    status.n        = 0;
    status.i        = 0;
    status.current  = 0;
    status.width    = 0;
    status.prec     = 0;
    status.ctx      = p;
    status.write    = cb;
    va_copy( status.arg, arg );

    /* Alternate between outputing runs of verbatim text and conversions */
    while ( *format != '\0' )
    {
        const char *mark = format;
        while ( *format != '\0' && *format != '%')
        {
            format++;
        }

        if ( mark != format )
        {
            if ( !cbout(&status, mark, format - mark) )
                return -1;
        }

        if ( *format == '%' ) {
            int consumed = _PDCLIB_print( format, &status );
            if ( consumed > 0 )
            {
                format += consumed;
            }
            else if ( consumed == 0 )
            {
                /* not a conversion specifier, print verbatim */
                if ( !cbout(&status, format++, 1) )
                    return -1;
            }
            else
            {
                /* I/O callback error */
                return -1;
            }
        }
    }

    va_end( status.arg );
    return status.i;
}

#endif

#ifdef TEST
#define _PDCLIB_FILEID "stdio/_vcbprintf.c"
#define _PDCLIB_STRINGIO
#include <stdint.h>
#include <stddef.h>
#include "_PDCLIB_test.h"

#ifndef REGTEST

static size_t testcb( void *p, const char *buf, size_t size )
{
    char **destbuf = p;
    memcpy(*destbuf, buf, size);
    *destbuf += size;
    return size;
}

static int testprintf( char * s, const char * format, ... )
{
    int i;
    va_list arg;
    va_start( arg, format );
    i = _vcbprintf( &s, testcb, format, arg );
    *s = 0;
    va_end( arg );
    return i;
}

#endif

int main( void )
{
#ifndef REGTEST
    char target[100];
#include "printf_testcases.h"
#endif
    return TEST_RESULTS;
}

#endif

