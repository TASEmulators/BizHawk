/* iswalpha( wint_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wctype.h>
#ifndef REGTEST
#include "_PDCLIB_locale.h"

int iswalpha( wint_t wc )
{
    return iswctype( wc, _PDCLIB_CTYPE_ALPHA );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE(iswalpha(L'a'));
    TESTCASE(iswalpha(L'z'));
    TESTCASE(iswalpha(L'E'));
    TESTCASE(!iswalpha(L'3'));
    TESTCASE(!iswalpha(L';'));
    return TEST_RESULTS;
}
#endif
