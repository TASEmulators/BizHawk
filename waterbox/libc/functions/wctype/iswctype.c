/* iswctype( wint_t, wctype_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wctype.h>
#ifndef REGTEST
#include "_PDCLIB_locale.h"

int _PDCLIB_iswctype_l( wint_t wc, wctype_t desc, locale_t l )
{
    wc = _PDCLIB_unpackwint( wc );

    _PDCLIB_wcinfo_t *info = _PDCLIB_wcgetinfo( l, wc );

    if(!info) return 0;

    return info->flags & desc;
}

int iswctype( wint_t wc, wctype_t desc )
{
    return _PDCLIB_iswctype_l( wc, desc, _PDCLIB_threadlocale() );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( iswctype(L'a', wctype("alpha")));
    TESTCASE( iswctype(L'z', wctype("alpha")));
    TESTCASE( iswctype(L'E', wctype("alpha")));
    TESTCASE(!iswctype(L'3', wctype("alpha")));
    TESTCASE(!iswctype(L';', wctype("alpha")));

    TESTCASE( iswctype(L'a', wctype("alnum")));
    TESTCASE( iswctype(L'3', wctype("alnum")));
    TESTCASE(!iswctype(L';', wctype("alnum")));

    TESTCASE( iswctype(L' ',  wctype("blank")));
    TESTCASE( iswctype(L'\t', wctype("blank")));
    TESTCASE(!iswctype(L'\n', wctype("blank")));
    TESTCASE(!iswctype(L';',  wctype("blank")));

    TESTCASE( iswctype(L'\0', wctype("cntrl")));
    TESTCASE( iswctype(L'\n', wctype("cntrl")));
    TESTCASE( iswctype(L'\v', wctype("cntrl")));
    TESTCASE(!iswctype(L'\t', wctype("cntrl")));
    TESTCASE(!iswctype(L'a',  wctype("cntrl")));

    TESTCASE( iswctype(L'0',  wctype("digit")));
    TESTCASE( iswctype(L'1',  wctype("digit")));
    TESTCASE( iswctype(L'2',  wctype("digit")));
    TESTCASE( iswctype(L'3',  wctype("digit")));
    TESTCASE( iswctype(L'4',  wctype("digit")));
    TESTCASE( iswctype(L'5',  wctype("digit")));
    TESTCASE( iswctype(L'6',  wctype("digit")));
    TESTCASE( iswctype(L'7',  wctype("digit")));
    TESTCASE( iswctype(L'8',  wctype("digit")));
    TESTCASE( iswctype(L'9',  wctype("digit")));
    TESTCASE(!iswctype(L'X',  wctype("digit")));
    TESTCASE(!iswctype(L'?',  wctype("digit")));

    TESTCASE( iswctype(L'a',  wctype("graph")));
    TESTCASE( iswctype(L'z',  wctype("graph")));
    TESTCASE( iswctype(L'E',  wctype("graph")));
    TESTCASE( iswctype(L'E',  wctype("graph")));
    TESTCASE(!iswctype(L' ',  wctype("graph")));
    TESTCASE(!iswctype(L'\t', wctype("graph")));
    TESTCASE(!iswctype(L'\n', wctype("graph")));

    TESTCASE( iswctype(L'a',  wctype("lower")));
    TESTCASE( iswctype(L'e',  wctype("lower")));
    TESTCASE( iswctype(L'z',  wctype("lower")));
    TESTCASE(!iswctype(L'A',  wctype("lower")));
    TESTCASE(!iswctype(L'E',  wctype("lower")));
    TESTCASE(!iswctype(L'Z',  wctype("lower")));

    TESTCASE(!iswctype(L'a',  wctype("upper")));
    TESTCASE(!iswctype(L'e',  wctype("upper")));
    TESTCASE(!iswctype(L'z',  wctype("upper")));
    TESTCASE( iswctype(L'A',  wctype("upper")));
    TESTCASE( iswctype(L'E',  wctype("upper")));
    TESTCASE( iswctype(L'Z',  wctype("upper")));

    TESTCASE( iswctype(L'Z',  wctype("print")));
    TESTCASE( iswctype(L'a',  wctype("print")));
    TESTCASE( iswctype(L';',  wctype("print")));
    TESTCASE( iswctype(L'\t', wctype("print")));
    TESTCASE(!iswctype(L'\0', wctype("print")));

    TESTCASE( iswctype(L';',  wctype("punct")));
    TESTCASE( iswctype(L'.',  wctype("punct")));
    TESTCASE( iswctype(L'?',  wctype("punct")));
    TESTCASE(!iswctype(L' ',  wctype("punct")));
    TESTCASE(!iswctype(L'Z',  wctype("punct")));

    TESTCASE( iswctype(L' ',  wctype("space")));
    TESTCASE( iswctype(L'\t', wctype("space")));

    TESTCASE( iswctype(L'0',  wctype("xdigit")));
    TESTCASE( iswctype(L'1',  wctype("xdigit")));
    TESTCASE( iswctype(L'2',  wctype("xdigit")));
    TESTCASE( iswctype(L'3',  wctype("xdigit")));
    TESTCASE( iswctype(L'4',  wctype("xdigit")));
    TESTCASE( iswctype(L'5',  wctype("xdigit")));
    TESTCASE( iswctype(L'6',  wctype("xdigit")));
    TESTCASE( iswctype(L'7',  wctype("xdigit")));
    TESTCASE( iswctype(L'8',  wctype("xdigit")));
    TESTCASE( iswctype(L'9',  wctype("xdigit")));
    TESTCASE( iswctype(L'a',  wctype("xdigit")));
    TESTCASE( iswctype(L'b',  wctype("xdigit")));
    TESTCASE( iswctype(L'c',  wctype("xdigit")));
    TESTCASE( iswctype(L'd',  wctype("xdigit")));
    TESTCASE( iswctype(L'e',  wctype("xdigit")));
    TESTCASE( iswctype(L'f',  wctype("xdigit")));
    TESTCASE( iswctype(L'A',  wctype("xdigit")));
    TESTCASE( iswctype(L'B',  wctype("xdigit")));
    TESTCASE( iswctype(L'C',  wctype("xdigit")));
    TESTCASE( iswctype(L'D',  wctype("xdigit")));
    TESTCASE( iswctype(L'E',  wctype("xdigit")));
    TESTCASE( iswctype(L'F',  wctype("xdigit")));
    TESTCASE(!iswctype(L'g',  wctype("xdigit")));
    TESTCASE(!iswctype(L'G',  wctype("xdigit")));
    TESTCASE(!iswctype(L'x',  wctype("xdigit")));
    TESTCASE(!iswctype(L'X',  wctype("xdigit")));
    TESTCASE(!iswctype(L' ',  wctype("xdigit")));

    return TEST_RESULTS;
}
#endif
