/* _PDCLIB_atomax( const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef REGTEST
#include "_PDCLIB_int.h"
#include <string.h>
#include <ctype.h>

_PDCLIB_intmax_t _PDCLIB_atomax( const char * s )
{
    _PDCLIB_intmax_t rc = 0;
    char sign = '+';
    const char * x;
    /* TODO: In other than "C" locale, additional patterns may be defined     */
    while ( isspace( *s ) ) ++s;
    if ( *s == '+' ) ++s;
    else if ( *s == '-' ) sign = *(s++);
    /* TODO: Earlier version was missing tolower() but was not caught by tests */
    while ( ( x = memchr( _PDCLIB_digits, tolower(*(s++)), 10 ) ) != NULL )
    {
        rc = rc * 10 + ( x - _PDCLIB_digits );
    }
    return ( sign == '+' ) ? rc : -rc;
}
#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
#ifndef REGTEST
    /* basic functionality */
    TESTCASE( _PDCLIB_atomax( "123" ) == 123 );
    /* testing skipping of leading whitespace and trailing garbage */
    TESTCASE( _PDCLIB_atomax( " \n\v\t\f123xyz" ) == 123 );
#endif
    return TEST_RESULTS;
}

#endif
