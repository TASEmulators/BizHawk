/* wctrans( const char * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wctype.h>
#ifndef REGTEST
#include <string.h>
#include "_PDCLIB_locale.h"

wctrans_t wctrans( const char * property )
{
    if(!property) {
        return 0;
    } else if(strcmp(property, "tolower") == 0) {
        return _PDCLIB_WCTRANS_TOLOWER;
    } else if(strcmp(property, "toupper") == 0) {
        return _PDCLIB_WCTRANS_TOUPPER;
    } else {
        return 0;
    }
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE(wctrans("") == 0);
    TESTCASE(wctrans("invalid") == 0);
    TESTCASE(wctrans("toupper") != 0);
    TESTCASE(wctrans("tolower") != 0);
    return TEST_RESULTS;
}
#endif
