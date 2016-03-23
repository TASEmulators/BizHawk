/* iswalnum( wint_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wctype.h>
#ifndef REGTEST
#include "_PDCLIB_locale.h"

int iswalnum( wint_t wc )
{
    return iswctype( wc, _PDCLIB_CTYPE_ALPHA | _PDCLIB_CTYPE_DIGIT );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE(iswalnum(L'a'));
    TESTCASE(iswalnum(L'z'));
    TESTCASE(iswalnum(L'E'));
    TESTCASE(iswalnum(L'3'));
    TESTCASE(!iswalnum(L';'));
    return TEST_RESULTS;
}
#endif
