/* islower( int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <ctype.h>

#ifndef REGTEST
#include "_PDCLIB_locale.h"

int islower( int c )
{
    return ( _PDCLIB_threadlocale()->_CType[c].flags & _PDCLIB_CTYPE_LOWER );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( islower( 'a' ) );
    TESTCASE( islower( 'z' ) );
    TESTCASE( ! islower( 'A' ) );
    TESTCASE( ! islower( 'Z' ) );
    TESTCASE( ! islower( ' ' ) );
    TESTCASE( ! islower( '@' ) );
    return TEST_RESULTS;
}

#endif
