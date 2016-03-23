/* iswupper( wint_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wctype.h>
#ifndef REGTEST
#include "_PDCLIB_locale.h"

int iswupper( wint_t wc )
{
    return iswctype( wc, _PDCLIB_CTYPE_UPPER );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE(!iswupper(L'a'));
    TESTCASE(!iswupper(L'e'));
    TESTCASE(!iswupper(L'z'));
    TESTCASE(iswupper(L'A'));
    TESTCASE(iswupper(L'E'));
    TESTCASE(iswupper(L'Z'));
    return TEST_RESULTS;
}
#endif
