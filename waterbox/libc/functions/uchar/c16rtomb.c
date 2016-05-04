/* c16rtomb( char *, char16_t, mbstate_t * )

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

size_t c16rtomb_l(
    char        *restrict   s, 
    char16_t                c16,
    mbstate_t   *restrict   ps,
    locale_t     restrict   l
)
{
    const char16_t *restrict psrc = &c16;
    char buf[s ? 0 : MB_CUR_MAX];
    s =      s ? s : buf;

    if(!l->_Codec->__c16stombs) {
        // Codec doesn't support direct conversion - translate via UCS-4
        if(ps->_Surrogate == 0) {
            // No pending surrogate
            if((c16 & 0xF800) == 0xD800) {
                // Surrogate range
                if((c16 & 0x0400) == 0) {
                    // 0xD800 -> 0xDBFF leading surrogate
                    ps->_Surrogate = c16;

                    // Need more data
                    // Return 0 - we haven't output anything yet

                    /* STD: ISO/IEC 9899:2011 is very implcifit about this being
                     *      the correct return value. N1040, from which the 
                     *      function was adopted, is explicit about 0 being a 
                     *      valid return.
                     */
                    return (size_t) 0;
                } else {
                    // 0xDC00 -> 0xDFFF trailing surrogate
                    errno = EILSEQ;
                    return (size_t) -1;
                }
            } else {
                // BMP range - UTF16 == UCS-4, pass through to c32rtomb_l
                return c32rtomb_l(s, c16, ps, l);
            }
        } else {
            // We have a stored surrogate
            if((c16 & 0xFC00) == 0xDC00) {
                // Trailing surrogate
                char32_t c32 = (ps->_Surrogate & 0x3FF) << 10 | (c16 & 0x3FF);
                ps->_Surrogate = 0;
                return c32rtomb_l(s, c32, ps, l);
            } else {
                // Not a trailing surrogate - encoding error
                errno = EILSEQ;
                return (size_t) -1;
            }

        }
    } else {
        // Codec supports direct conversion
        size_t srcsz  = 1;
        size_t dstsz  = MB_CUR_MAX;
        size_t dstrem = dstsz;

        if(l->_Codec->__c16stombs(&s, &dstrem, &psrc, &srcsz, ps)) {
            // Successful conversion
            return dstsz - dstrem;
        } else {
            errno = EILSEQ;
            return (size_t) -1;
        }
    }
}

size_t c16rtomb(
    char        *restrict   s, 
    char16_t                c16,
    mbstate_t   *restrict   ps
)
{
    return c16rtomb_l(s, c16, ps, _PDCLIB_threadlocale());
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
