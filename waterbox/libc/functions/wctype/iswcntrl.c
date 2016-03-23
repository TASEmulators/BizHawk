/* iswcntrl( wint_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wctype.h>
#ifndef REGTEST
#include "_PDCLIB_locale.h"

int iswcntrl( wint_t wc )
{
    return iswctype( wc, _PDCLIB_CTYPE_CNTRL );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE(iswcntrl(L'\0'));
    TESTCASE(iswcntrl(L'\n'));
    TESTCASE(iswcntrl(L'\v'));
    TESTCASE(!iswcntrl(L'\t'));
    TESTCASE(!iswcntrl(L'a'));
    return TEST_RESULTS;
}
#endif
