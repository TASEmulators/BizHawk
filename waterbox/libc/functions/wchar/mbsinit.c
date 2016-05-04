/* mbsinit( mbstate_t * ps )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <wchar.h>
#ifndef REGTEST
#include "_PDCLIB_encoding.h"
#include "_PDCLIB_locale.h"

static int _PDCLIB_mbsinit_l( const mbstate_t *ps, locale_t l )
{
    if( ps ) {
        return ps->_Surrogate == 0
            && ps->_PendState == 0
            && l->_Codec->__mbsinit(ps);
    } else return 1;
}

int mbsinit( const mbstate_t * ps )
{
    return _PDCLIB_mbsinit_l(ps, _PDCLIB_threadlocale());
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    mbstate_t mbs;
    memset(&mbs, 0, sizeof mbs);

    TESTCASE(mbsinit(NULL) != 0);
    TESTCASE(mbsinit(&mbs) != 0);

#ifndef REGTEST
    // Surrogate pending
    mbs._Surrogate = 0xFEED;
    TESTCASE(mbsinit(&mbs) == 0);

    mbs._Surrogate = 0;
    mbs._PendState = 1;
    TESTCASE(mbsinit(&mbs) == 0);
#endif
    return TEST_RESULTS;
}
#endif



