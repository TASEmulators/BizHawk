/* vfscanf( FILE *, const char *, va_list )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdarg.h>
#include <ctype.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

int _PDCLIB_vfscanf_unlocked( FILE * _PDCLIB_restrict stream, 
                      const char * _PDCLIB_restrict format, 
                      va_list arg )
{
    /* TODO: This function should interpret format as multibyte characters.  */
    struct _PDCLIB_status_t status;
    status.base = 0;
    status.flags = 0;
    status.n = 0;
    status.i = 0;
    status.current = 0;
    status.s = NULL;
    status.width = 0;
    status.prec = 0;
    status.stream = stream;
    va_copy( status.arg, arg );

    while ( *format != '\0' )
    {
        const char * rc;
        if ( ( *format != '%' ) || ( ( rc = _PDCLIB_scan( format, &status ) ) == format ) )
        {
            int c;
            /* No conversion specifier, match verbatim */
            if ( isspace( *format ) )
            {
                /* Whitespace char in format string: Skip all whitespaces */
                /* No whitespaces in input does not result in matching error */
                while ( isspace( c = getc( stream ) ) )
                {
                    ++status.i;
                }
                if ( ! feof( stream ) )
                {
                    _PDCLIB_ungetc_unlocked( c, stream );
                }
            }
            else
            {
                /* Non-whitespace char in format string: Match verbatim */
                if ( ( ( c = _PDCLIB_getc_unlocked( stream ) ) != *format ) || feof( stream ) )
                {
                    /* Matching error */
                    if ( ! feof( stream ) && ! ferror( stream ) )
                    {
                        _PDCLIB_ungetc_unlocked( c, stream );
                    }
                    else if ( status.n == 0 )
                    {
                        return EOF;
                    }
                    return status.n;
                }
                else
                {
                    ++status.i;
                }
            }
            ++format;
        }
        else
        {
            /* NULL return code indicates matching error */
            if ( rc == NULL )
            {
                break;
            }
            /* Continue parsing after conversion specifier */
            format = rc;
        }
    }
    va_end( status.arg );
    return status.n;
}

int vfscanf( FILE * _PDCLIB_restrict stream, 
             const char * _PDCLIB_restrict format, 
             va_list arg )
{
    _PDCLIB_flockfile( stream );
    int r = _PDCLIB_vfscanf_unlocked( stream, format, arg );
    _PDCLIB_funlockfile( stream );
    return r;
}

#endif

#ifdef TEST
#define _PDCLIB_FILEID "stdio/vfscanf.c"
#define _PDCLIB_FILEIO

#include "_PDCLIB_test.h"

static int testscanf( FILE * stream, char const * format, ... )
{
    va_list ap;
    va_start( ap, format );
    int result = vfscanf( stream, format, ap );
    va_end( ap );
    return result;
}

int main( void )
{
    FILE * source;
    TESTCASE( ( source = tmpfile() ) != NULL );
#include "scanf_testcases.h"
    TESTCASE( fclose( source ) == 0 );
    return TEST_RESULTS;
}

#endif

