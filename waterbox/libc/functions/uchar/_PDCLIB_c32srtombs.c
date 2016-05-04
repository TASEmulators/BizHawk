/* _PDCLIB_c32srtombs( char *, const char32_t * *, size_t, mbstate_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef REGTEST
#include <uchar.h>
#include <errno.h>
#include <stdint.h>
#include "_PDCLIB_encoding.h"
#include "_PDCLIB_locale.h"

static size_t _PDCLIB_c32srtombs_l(
    char                *restrict   dst, 
    const char32_t     **restrict   src, 
    size_t                          len, 
    mbstate_t           *restrict   ps,
    _PDCLIB_locale_t     restrict   l)
{
    char *restrict *restrict dstp = dst ? &dst : NULL;
    len = dst ? len : SIZE_MAX;

    size_t srclen = _PDCLIB_c32slen(*src);
    size_t dstlen = len;

    if(l->_Codec->__c32stombs(dstp, &dstlen, src, &srclen, ps)) {
        // Successful conversion
        return len - dstlen;
    } else {
        // Failed conversion
        errno = EILSEQ;
        return (size_t) -1;
    }
}

size_t _PDCLIB_c32srtombs(
    char                *restrict   dst, 
    const char32_t     **restrict   src, 
    size_t                          len, 
    mbstate_t           *restrict   ps
)
{
    return _PDCLIB_c32srtombs_l(dst, src, len, ps, _PDCLIB_threadlocale());
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
