/* strxfrm( char *, const char *, size_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <string.h>

#ifndef REGTEST

#include "_PDCLIB_locale.h"

size_t strxfrm( char * _PDCLIB_restrict s1, const char * _PDCLIB_restrict s2, size_t n )
{
    const _PDCLIB_ctype_t *ctype = _PDCLIB_threadlocale()->_CType;
    size_t len = strlen( s2 );
    if ( len < n )
    {
        /* Cannot use strncpy() here as the filling of s1 with '\0' is not part
           of the spec.
        */
        while ( n-- && ( *s1++ = ctype[(unsigned char)*s2++].collation ) );
    }
    return len;
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    char s[] = "xxxxxxxxxxx";
    TESTCASE( strxfrm( NULL, "123456789012", 0 ) == 12 );
    TESTCASE( strxfrm( s, "123456789012", 12 ) == 12 );
    /*
    The following test case is true in *this* implementation, but doesn't have to.
    TESTCASE( s[0] == 'x' );
    */
    TESTCASE( strxfrm( s, "1234567890", 11 ) == 10 );
    TESTCASE( s[0] == '1' );
    TESTCASE( s[9] == '0' );
    TESTCASE( s[10] == '\0' );
    return TEST_RESULTS;
}
#endif

