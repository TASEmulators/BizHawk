/* _PDCLIB_digits

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef REGTEST
#include "_PDCLIB_int.h"
#endif

char _PDCLIB_digits[] = "0123456789abcdefghijklmnopqrstuvwxyz";

/* For _PDCLIB/print.c only; obsolete with ctype.h */
char _PDCLIB_Xdigits[] = "0123456789ABCDEF";

#ifdef TEST
#include "_PDCLIB_test.h"

#include <string.h>

int main( void )
{
#ifndef REGTEST
    TESTCASE( strcmp( _PDCLIB_digits, "0123456789abcdefghijklmnopqrstuvwxyz" ) == 0 );
    TESTCASE( strcmp( _PDCLIB_Xdigits, "0123456789ABCDEF" ) == 0 );
#endif
    return TEST_RESULTS;
}

#endif
