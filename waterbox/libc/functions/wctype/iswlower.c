/* iswalnum( wint_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wctype.h>
#ifndef REGTEST
#include "_PDCLIB_locale.h"

int iswlower( wint_t wc )
{
    return iswctype( wc, _PDCLIB_CTYPE_LOWER );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE(iswlower(L'a'));
    TESTCASE(iswlower(L'e'));
    TESTCASE(iswlower(L'z'));
    TESTCASE(!iswlower(L'A'));
    TESTCASE(!iswlower(L'E'));
    TESTCASE(!iswlower(L'Z'));
    return TEST_RESULTS;
}
#endif
