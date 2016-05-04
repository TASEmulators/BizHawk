/* size_t mbrtoc32( char32_t *, const char *, size_t, mbstate_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef REGTEST
#include <uchar.h>
#include <errno.h>
#include <stdint.h>
#include <assert.h>
#include "_PDCLIB_encoding.h"
#include "_PDCLIB_locale.h"

size_t mbrtoc32_l(
    char32_t    *restrict   pc32,
    const char  *restrict   s, 
    size_t                  n,
    mbstate_t   *restrict   ps,
    locale_t     restrict   l
)
{
    size_t dstlen = 1;
    size_t nr = n;

    if(l->_Codec->__mbstoc32s(&pc32, &dstlen, &s, &nr, ps)) {
        // Successful conversion
        if(dstlen == 0) {
            // A character was output
            if(nr == n) {
                // The output character resulted entirely from stored state
                // With UTF-32, this shouldn't be possible?
                return (size_t) -3;
            } else if(pc32[-1] == 0) {
                // Was null character
                return 0;
            } else {
                // Count of processed characters
                return n - nr;
            }
        } else {
            assert(nr == 0 && "Must have processed whole input");
            return (size_t) -2;
        }
    } else {
        // Failed conversion
        errno = EILSEQ;
        return (size_t) -1;
    }
}

size_t mbrtoc32(
    char32_t    *restrict   pc32,
    const char  *restrict   s, 
    size_t                  n,
    mbstate_t   *restrict   ps
)
{
    return mbrtoc32_l(pc32, s, n, ps, _PDCLIB_threadlocale());
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
