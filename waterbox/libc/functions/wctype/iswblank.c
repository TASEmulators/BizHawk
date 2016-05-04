/* iswblank( wint_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wctype.h>
#ifndef REGTEST
#include "_PDCLIB_locale.h"

int iswblank( wint_t wc )
{
    return iswctype( wc, _PDCLIB_CTYPE_BLANK );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE(iswblank(L' '));
    TESTCASE(iswblank(L'\t'));
    TESTCASE(!iswblank(L'\n'));
    TESTCASE(!iswblank(L'a'));
    return TEST_RESULTS;
}
#endif
