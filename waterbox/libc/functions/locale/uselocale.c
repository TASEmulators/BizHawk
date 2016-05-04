/* uselocale( locale_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <locale.h>
#ifndef REGTEST
#include "_PDCLIB_locale.h"

#ifdef _PDCLIB_LOCALE_METHOD
locale_t uselocale( locale_t newloc )
{
    locale_t oldloc = _PDCLIB_threadlocale();

    if(newloc == LC_GLOBAL_LOCALE) {
        _PDCLIB_setthreadlocale(NULL);
    } else if(newloc != NULL) {
        _PDCLIB_setthreadlocale(newloc);
    }

    return oldloc;
}
#endif

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( NO_TESTDRIVER );
    return TEST_RESULTS;
}
#endif
