/* iswpunct( wint_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wctype.h>
#ifndef REGTEST
#include "_PDCLIB_locale.h"

int iswpunct( wint_t wc )
{
    return iswctype( wc, _PDCLIB_CTYPE_PUNCT );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE(iswpunct(L';'));
    TESTCASE(iswpunct(L'?'));
    TESTCASE(iswpunct(L'.'));
    TESTCASE(!iswpunct(L' '));
    TESTCASE(!iswpunct(L'Z'));

    return TEST_RESULTS;
}
#endif
