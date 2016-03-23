/* isprint( int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <ctype.h>

#ifndef REGTEST
#include "_PDCLIB_locale.h"

int isprint( int c )
{
    return ( _PDCLIB_threadlocale()->_CType[c].flags & _PDCLIB_CTYPE_GRAPH ) || ( c == ' ' );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( isprint( 'a' ) );
    TESTCASE( isprint( 'z' ) );
    TESTCASE( isprint( 'A' ) );
    TESTCASE( isprint( 'Z' ) );
    TESTCASE( isprint( '@' ) );
    TESTCASE( ! isprint( '\t' ) );
    TESTCASE( ! isprint( '\0' ) );
    TESTCASE( isprint( ' ' ) );
    return TEST_RESULTS;
}

#endif
