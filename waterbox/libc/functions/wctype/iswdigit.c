/* iswdigit( wint_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wctype.h>
#ifndef REGTEST
#include "_PDCLIB_locale.h"

int iswdigit( wint_t wc )
{
    return iswctype( wc, _PDCLIB_CTYPE_DIGIT );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE(iswdigit(L'0'));
    TESTCASE(iswdigit(L'1'));
    TESTCASE(iswdigit(L'2'));
    TESTCASE(iswdigit(L'3'));
    TESTCASE(iswdigit(L'4'));
    TESTCASE(iswdigit(L'5'));
    TESTCASE(iswdigit(L'6'));
    TESTCASE(iswdigit(L'7'));
    TESTCASE(iswdigit(L'8'));
    TESTCASE(iswdigit(L'9'));
    TESTCASE(!iswdigit(L'a'));
    TESTCASE(!iswdigit(L'b'));
    TESTCASE(!iswdigit(L'c'));
    TESTCASE(!iswdigit(L'd'));
    TESTCASE(!iswdigit(L'e'));
    TESTCASE(!iswdigit(L'f'));
    TESTCASE(!iswdigit(L'A'));
    TESTCASE(!iswdigit(L'B'));
    TESTCASE(!iswdigit(L'C'));
    TESTCASE(!iswdigit(L'D'));
    TESTCASE(!iswdigit(L'E'));
    TESTCASE(!iswdigit(L'F'));
    TESTCASE(!iswdigit(L'g'));
    TESTCASE(!iswdigit(L'G'));
    TESTCASE(!iswdigit(L'x'));
    TESTCASE(!iswdigit(L'X'));
    TESTCASE(!iswdigit(L' '));
    return TEST_RESULTS;
}
#endif
