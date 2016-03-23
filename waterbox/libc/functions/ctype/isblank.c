/* isblank( int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <ctype.h>

#ifndef REGTEST
#include "_PDCLIB_locale.h"

int isblank( int c )
{
    return ( _PDCLIB_threadlocale()->_CType[c].flags & _PDCLIB_CTYPE_BLANK );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( isblank( ' ' ) );
    TESTCASE( isblank( '\t' ) );
    TESTCASE( ! isblank( '\v' ) );
    TESTCASE( ! isblank( '\r' ) );
    TESTCASE( ! isblank( 'x' ) );
    TESTCASE( ! isblank( '@' ) );
    return TEST_RESULTS;
}

#endif
