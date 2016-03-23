/* c32rtomb( char *, char32_t, mbstate_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef REGTEST
#include <uchar.h>
#include <errno.h>
#include <stdint.h>
#include <assert.h>
#include <stdlib.h>
#include "_PDCLIB_encoding.h"
#include "_PDCLIB_locale.h"

size_t c32rtomb_l(
    char        *restrict   s, 
    char32_t                c32,
    mbstate_t   *restrict   ps,
    locale_t     restrict   l
)
{
    char buf[s ? 0 : MB_CUR_MAX];
    s =      s ? s : buf;

    const char32_t *restrict psrc = &c32;
    size_t srcsz  = 1;
    size_t dstsz  = MB_CUR_MAX;
    size_t dstrem = dstsz;

    if(l->_Codec->__c32stombs(&s, &dstrem, &psrc, &srcsz, ps)) {
        // Successful conversion
        return dstsz - dstrem;
    } else {
        errno = EILSEQ;
        return (size_t) -1;
    }
}

size_t c32rtomb(
    char        *restrict   s, 
    char32_t                c32,
    mbstate_t   *restrict   ps
)
{
    return c32rtomb_l(s, c32, ps, _PDCLIB_threadlocale());
}

#endif

#ifdef TEST
#include "_PDCLIB_test.h"

int main( void )
{
    TESTCASE( NO_TESTDRIVER );
    return TEST_RESULTS;
}
#endif
