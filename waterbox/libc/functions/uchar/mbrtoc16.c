/* size_t mbrtoc16( char16_t *, const char *, size_t, mbstate_t * )

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

size_t mbrtoc16_l(
    char16_t    *restrict   pc16,
    const char  *restrict   s, 
    size_t                  n,
    mbstate_t   *restrict   ps,
    locale_t     restrict   l
)
{
    size_t dstlen = 1;
    size_t nr = n;

    if(!l->_Codec->__mbstoc16s) {
        // No UTF-16 support in codec. Must synthesize on top of UCS-4 support.

        if(ps->_Surrogate) {
            // If a pending surrogate is stored in the state
            *pc16 = ps->_Surrogate;
            ps->_Surrogate = 0;
            return (size_t) -3;
        }

        char32_t c32;
        size_t res = mbrtoc32_l(&c32, s, n, ps, l);
        if(res != (size_t) -1) {
            // Conversion was successful. Check for surrogates
            if(c32 <= 0xFFFF) {
                // BMP char
                *pc16 = c32;
            } else {
                // Supplementary char
                *pc16 = 0xD800 | (c32 >> 10);
                ps->_Surrogate = 0xDC00 | (c32 & 0x3FF);
            }
        }
        return res;
    } else if(l->_Codec->__mbstoc16s(&pc16, &dstlen, &s, &nr, ps)) {
        // Successful conversion
        if(dstlen == 0) {
            // A character was output
            if(nr == n) {
                // The output character resulted entirely from stored state
                return (size_t) -3;
            } else if(pc16[-1] == 0) {
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

size_t mbrtoc16(
    char16_t    *restrict   pc16,
    const char  *restrict   s, 
    size_t                  n,
    mbstate_t   *restrict   ps
)
{
    return mbrtoc16_l(pc16, s, n, ps, _PDCLIB_threadlocale());
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
