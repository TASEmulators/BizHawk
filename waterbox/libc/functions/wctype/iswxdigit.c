/* iswxdigit( wint_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wctype.h>
#ifndef REGTEST
#include "_PDCLIB_locale.h"

int iswxdigit( wint_t wc )
{
    return iswctype( wc, _PDCLIB_CTYPE_XDIGT );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE(iswxdigit(L'0'));
    TESTCASE(iswxdigit(L'1'));
    TESTCASE(iswxdigit(L'2'));
    TESTCASE(iswxdigit(L'3'));
    TESTCASE(iswxdigit(L'4'));
    TESTCASE(iswxdigit(L'5'));
    TESTCASE(iswxdigit(L'6'));
    TESTCASE(iswxdigit(L'7'));
    TESTCASE(iswxdigit(L'8'));
    TESTCASE(iswxdigit(L'9'));
    TESTCASE(iswxdigit(L'a'));
    TESTCASE(iswxdigit(L'b'));
    TESTCASE(iswxdigit(L'c'));
    TESTCASE(iswxdigit(L'd'));
    TESTCASE(iswxdigit(L'e'));
    TESTCASE(iswxdigit(L'f'));
    TESTCASE(iswxdigit(L'A'));
    TESTCASE(iswxdigit(L'B'));
    TESTCASE(iswxdigit(L'C'));
    TESTCASE(iswxdigit(L'D'));
    TESTCASE(iswxdigit(L'E'));
    TESTCASE(iswxdigit(L'F'));
    TESTCASE(!iswxdigit(L'g'));
    TESTCASE(!iswxdigit(L'G'));
    TESTCASE(!iswxdigit(L'x'));
    TESTCASE(!iswxdigit(L'X'));
    TESTCASE(!iswxdigit(L' '));
    return TEST_RESULTS;
}
#endif
