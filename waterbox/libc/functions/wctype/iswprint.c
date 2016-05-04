/* iswprint( wint_t )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wctype.h>
#ifndef REGTEST
#include "_PDCLIB_locale.h"

int iswprint( wint_t wc )
{
    return iswctype( wc, _PDCLIB_CTYPE_GRAPH | _PDCLIB_CTYPE_SPACE );
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{

    return TEST_RESULTS;
}
#endif
