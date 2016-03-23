/* ispunct( int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <ctype.h>

#ifndef REGTEST
#include "_PDCLIB_locale.h"

int ispunct( int c )
{
    return ( _PDCLIB_threadlocale()->_CType[c].flags & _PDCLIB_CTYPE_PUNCT );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( ! ispunct( 'a' ) );
    TESTCASE( ! ispunct( 'z' ) );
    TESTCASE( ! ispunct( 'A' ) );
    TESTCASE( ! ispunct( 'Z' ) );
    TESTCASE( ispunct( '@' ) );
    TESTCASE( ispunct( '.' ) );
    TESTCASE( ! ispunct( '\t' ) );
    TESTCASE( ! ispunct( '\0' ) );
    TESTCASE( ! ispunct( ' ' ) );
    return TEST_RESULTS;
}

#endif
