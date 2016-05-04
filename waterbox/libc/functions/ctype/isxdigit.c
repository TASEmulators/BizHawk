/* isxdigit( int )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <ctype.h>

#ifndef REGTEST
#include "_PDCLIB_locale.h"

int isxdigit( int c )
{
    return ( _PDCLIB_threadlocale()->_CType[c].flags & _PDCLIB_CTYPE_XDIGT );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( isxdigit( '0' ) );
    TESTCASE( isxdigit( '9' ) );
    TESTCASE( isxdigit( 'a' ) );
    TESTCASE( isxdigit( 'f' ) );
    TESTCASE( ! isxdigit( 'g' ) );
    TESTCASE( isxdigit( 'A' ) );
    TESTCASE( isxdigit( 'F' ) );
    TESTCASE( ! isxdigit( 'G' ) );
    TESTCASE( ! isxdigit( '@' ) );
    TESTCASE( ! isxdigit( ' ' ) );
    return TEST_RESULTS;
}

#endif
