/* isspace( int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <ctype.h>

#ifndef REGTEST
#include "_PDCLIB_locale.h"

int isspace( int c )
{
    return ( _PDCLIB_threadlocale()->_CType[c].flags & _PDCLIB_CTYPE_SPACE );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( isspace( ' ' ) );
    TESTCASE( isspace( '\f' ) );
    TESTCASE( isspace( '\n' ) );
    TESTCASE( isspace( '\r' ) );
    TESTCASE( isspace( '\t' ) );
    TESTCASE( isspace( '\v' ) );
    TESTCASE( ! isspace( 'a' ) );
    return TEST_RESULTS;
}

#endif
