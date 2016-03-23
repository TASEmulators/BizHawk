/* iswgraph( wint_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wctype.h>
#ifndef REGTEST
#include "_PDCLIB_locale.h"

int iswgraph( wint_t wc )
{
    return iswctype( wc, _PDCLIB_CTYPE_GRAPH );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE(iswgraph(L'a'));
    TESTCASE(iswgraph(L'z'));
    TESTCASE(iswgraph(L'E'));
    TESTCASE(!iswgraph(L' '));
    TESTCASE(!iswgraph(L'\t'));
    TESTCASE(!iswgraph(L'\n'));
    return TEST_RESULTS;
}
#endif
