/* _PDCLIB_mbsrtoc32s( char32_t *, const char * *, size_t, mbstate_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef REGTEST
#include <uchar.h>
#include <errno.h>
#include <stdint.h>
#include <string.h>
#include "_PDCLIB_encoding.h"
#include "_PDCLIB_locale.h"

static size_t _PDCLIB_mbsrtoc32s_l
(
    char32_t        *restrict   dst, 
    const char     **restrict   src, 
    size_t                      len, 
    mbstate_t       *restrict   ps,
    locale_t         restrict   l
)
{
    size_t dstlen = len = dst ? len : SIZE_MAX;
    char32_t *restrict *restrict dstp = dst ? &dst : NULL;

    size_t                     srclen = strlen(*src);
    if(l->_Codec->__mbstoc32s(dstp, &dstlen, src, &srclen, ps)) {
        return len - dstlen;
    } else {
        errno = EILSEQ;
        return (size_t) -1;
    }
}

size_t _PDCLIB_mbsrtoc32s(
    char32_t        *restrict   dst, 
    const char     **restrict   src, 
    size_t                      len, 
    mbstate_t       *restrict   ps
)
{
    return _PDCLIB_mbsrtoc32s_l(dst, src, len, ps, _PDCLIB_threadlocale());
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
