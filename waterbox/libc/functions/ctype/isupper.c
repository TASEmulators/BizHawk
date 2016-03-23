/* isupper( int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <ctype.h>

#ifndef REGTEST
#include "_PDCLIB_locale.h"

int isupper( int c )
{
    return ( _PDCLIB_threadlocale()->_CType[c].flags & _PDCLIB_CTYPE_UPPER );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( isupper( 'A' ) );
    TESTCASE( isupper( 'Z' ) );
    TESTCASE( ! isupper( 'a' ) );
    TESTCASE( ! isupper( 'z' ) );
    TESTCASE( ! isupper( ' ' ) );
    TESTCASE( ! isupper( '@' ) );
    return TEST_RESULTS;
}

#endif
